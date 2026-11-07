#!/usr/bin/env node
// scaffold-check.mjs — validate the agent surface and the Command Palette wiring.
//
// Usage:
//   node scripts/scaffold-check.mjs [--root <path>]
//
// Zero dependencies; Node.js >= 20. Exit code 0 when valid, 1 otherwise.
// Checks: required files, skill frontmatter and size, decision-record paths, role-card
// sections, the AGENTS.md instruction-chain budget, local-overlay tracking, and the
// CmdPal manifest/CLSID wiring that the host silently depends on.

import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const MAX_SKILL_CHARS = 8192;
const SKILL_TARGET_CHARS = 6000;
const MAX_CHAIN_BYTES = 65536;
const CHAIN_WARN_BYTES = 32768;
const INSTRUCTION_CANDIDATES = ['AGENTS.md', 'CLAUDE.md', 'AGENTS.local.md', 'CLAUDE.local.md'];
const NOTE_STATUSES = ['proposed', 'implemented', 'archived'];
const NOTE_KINDS = ['architecture', 'feature', 'bug-fix', 'simplification', 'process', 'testing'];
const ROLE_SECTIONS = ['## Mission', '## Boundaries', '## Procedure', '## Report format', '## Done criteria'];
const KEBAB = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const NOTE_NAME = /^(\d{4})-(\d{2})-(\d{2})-[a-z0-9]+(?:-[a-z0-9]+)*\.md$/;
const CLSID = /^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$/;

const PROJECT_DIR = 'windows/src/ItsycalForWindows';
const REQUIRED_FILES = [
  'AGENTS.md',
  'windows/AGENTS.md',
  'README.txt',
  'windows/README.md',
  '.agents/agents',
  '.agents/notes',
  '.agents/skills',
  '.githooks/commit-msg',
  '.githooks/pre-commit',
  '.githooks/pre-push',
  '.githooks/README.md',
  '.github/workflows/windows-build.yml',
  'scripts/check-commit-message.sh',
  'scripts/scaffold-check.mjs',
  `${PROJECT_DIR}/ItsycalForWindows.csproj`,
  `${PROJECT_DIR}/Package.appxmanifest`,
  `${PROJECT_DIR}/Program.cs`,
  `${PROJECT_DIR}/ItsycalForWindows.cs`,
  `${PROJECT_DIR}/ItsycalForWindowsCommandsProvider.cs`,
];

const failures = [];
const warnings = [];
const toPosix = (value) => value.split(path.sep).join('/');

function fail(file, reason) {
  failures.push(`${file}: ${reason}`);
}

function warn(file, reason) {
  warnings.push(`${file}: ${reason}`);
}

function readText(abs) {
  return fs.readFileSync(abs, 'utf8');
}

function walk(absDir, onFile) {
  const entries = fs.readdirSync(absDir, { withFileTypes: true });
  for (const entry of entries) {
    const abs = path.join(absDir, entry.name);
    if (entry.isDirectory()) walk(abs, onFile);
    else if (entry.isFile()) onFile(abs);
  }
}

function parseArgs(argv) {
  let root = null;
  for (let i = 0; i < argv.length; i += 1) {
    if (argv[i] === '--root') {
      root = argv[i + 1] ?? null;
      i += 1;
    } else if (argv[i] === '--help' || argv[i] === '-h') {
      process.stdout.write('usage: node scripts/scaffold-check.mjs [--root <path>]\n');
      process.exit(0);
    } else {
      process.stderr.write(`scaffold-check: unknown argument ${argv[i]}\n`);
      process.exit(2);
    }
  }
  return { root };
}

function findGitRoot(startDir) {
  let dir = path.resolve(startDir);
  for (;;) {
    if (fs.existsSync(path.join(dir, '.git'))) return dir;
    const parent = path.dirname(dir);
    if (parent === dir) return null;
    dir = parent;
  }
}

// Probe git without capturing output: some sandboxed runtimes refuse to create the pipes
// that `stdio: 'pipe'` needs, while exit-code-only spawns still work.
function gitSucceeds(root, args) {
  try {
    execFileSync('git', args, { cwd: root, stdio: 'ignore', timeout: 15000 });
    return true;
  } catch {
    return false;
  }
}

function rel(root, abs) {
  return toPosix(path.relative(root, abs)) || '.';
}

// --- checks -------------------------------------------------------------------------

function checkRequiredFiles(root) {
  for (const required of REQUIRED_FILES) {
    if (!fs.existsSync(path.join(root, required))) {
      fail(required, 'required by the agent scaffold but missing');
    }
  }
}

function parseFrontmatter(text) {
  const match = /^---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)/.exec(text);
  if (!match) return null;
  const lines = match[1].split(/\r?\n/);
  const fields = {};
  for (let i = 0; i < lines.length; i += 1) {
    const field = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(lines[i]);
    if (!field) continue;
    const [, key, rawValue] = field;
    const value = rawValue.trim();
    if (value === '>' || value === '|' || value === '>-' || value === '|-') {
      const collected = [];
      while (i + 1 < lines.length && /^\s+\S/.test(lines[i + 1])) {
        i += 1;
        collected.push(lines[i].trim());
      }
      fields[key] = collected.join(' ');
    } else {
      fields[key] = value.replace(/^["']|["']$/g, '');
    }
  }
  return { fields, body: text.slice(match[0].length) };
}

function checkSkillFile(root, abs, expectedName) {
  const file = rel(root, abs);
  const size = fs.statSync(abs).size;
  if (size >= MAX_SKILL_CHARS) {
    fail(file, `${size} chars; DSH trims tool results at ${MAX_SKILL_CHARS} characters, split it into references/`);
  } else if (size > SKILL_TARGET_CHARS) {
    warn(file, `${size} chars; target is under ${SKILL_TARGET_CHARS} characters`);
  }
  const frontmatter = parseFrontmatter(readText(abs));
  if (!frontmatter) {
    fail(file, 'missing YAML frontmatter delimited by --- lines');
    return;
  }
  const { fields, body } = frontmatter;
  if (!fields.name) {
    fail(file, 'frontmatter is missing `name`');
  } else {
    if (!KEBAB.test(fields.name)) fail(file, `\`name\` must be kebab-case, got "${fields.name}"`);
    if (expectedName && fields.name !== expectedName) {
      fail(file, `\`name\` "${fields.name}" must equal the directory/file name "${expectedName}"`);
    }
  }
  const description = fields.description ?? '';
  if (description.length < 10) {
    fail(file, 'frontmatter needs a `description` of at least 10 characters');
  } else {
    if (description.length > 500) {
      fail(file, `\`description\` is ${description.length} characters; the catalog caps it at 500`);
    } else if (description.length > 300) {
      warn(file, `\`description\` is ${description.length} characters; keep it under 300 for readability`);
    }
    if (!/^Use when\b/.test(description)) {
      warn(file, '`description` should start with "Use when ..." because it is the only routing signal');
    }
  }
  if (fields.whenToUse) warn(file, '`whenToUse` is not used for catalog routing; fold it into `description`');
  if (body.trim() === '') fail(file, 'skill body is empty');
}

function checkSkills(root) {
  const skillsDir = path.join(root, '.agents', 'skills');
  if (!fs.existsSync(skillsDir)) return;
  for (const entry of fs.readdirSync(skillsDir, { withFileTypes: true })) {
    const abs = path.join(skillsDir, entry.name);
    if (entry.isDirectory()) {
      const skillFile = path.join(abs, 'SKILL.md');
      if (!fs.existsSync(skillFile)) fail(rel(root, abs), 'skill directory has no SKILL.md');
      else checkSkillFile(root, skillFile, entry.name);
    } else if (entry.name.endsWith('.md')) {
      checkSkillFile(root, abs, entry.name.slice(0, -3));
    }
  }
  walk(skillsDir, (abs) => {
    if (path.basename(abs) !== 'SKILL.md') return;
    const depth = path.relative(skillsDir, abs).split(path.sep).length;
    if (depth > 2) {
      fail(rel(root, abs), 'nested SKILL.md is never discovered by DSH; keep <name>/SKILL.md one level deep');
    }
  });
}

function checkNotes(root) {
  const notesDir = path.join(root, '.agents', 'notes');
  if (!fs.existsSync(notesDir)) return;
  walk(notesDir, (abs) => {
    if (path.basename(abs) === '.gitkeep' || !abs.endsWith('.md')) return;
    const relPath = toPosix(path.relative(notesDir, abs));
    if (relPath === 'README.md' || relPath === 'TEMPLATE.md') return;
    const parts = relPath.split('/');
    if (parts.length !== 3) {
      fail(`.agents/notes/${relPath}`, 'expected <status>/<kind>/<YYYY-MM-DD>-<slug>.md');
      return;
    }
    const [status, kind, name] = parts;
    if (!NOTE_STATUSES.includes(status)) {
      fail(`.agents/notes/${relPath}`, `status must be one of ${NOTE_STATUSES.join(', ')}`);
      return;
    }
    if (!NOTE_KINDS.includes(kind)) {
      fail(`.agents/notes/${relPath}`, `kind must be one of ${NOTE_KINDS.join(', ')}`);
      return;
    }
    const dateMatch = NOTE_NAME.exec(name);
    if (!dateMatch) {
      fail(`.agents/notes/${relPath}`, 'file name must be <YYYY-MM-DD>-<kebab-slug>.md');
      return;
    }
    const [, year, month, day] = dateMatch;
    const date = new Date(`${year}-${month}-${day}T00:00:00Z`);
    if (Number.isNaN(date.getTime())) fail(`.agents/notes/${relPath}`, 'invalid date in file name');
    if (readText(abs).trim() === '') fail(`.agents/notes/${relPath}`, 'note is empty');
  });
}

function checkRoleCards(root) {
  const agentsDir = path.join(root, '.agents', 'agents');
  if (!fs.existsSync(agentsDir)) return;
  const cards = fs.readdirSync(agentsDir).filter((name) => name.endsWith('.md'));
  if (cards.length === 0) {
    fail('.agents/agents', 'no role cards; add lead/implementer/reviewer/researcher/verifier');
    return;
  }
  for (const card of cards) {
    const relPath = rel(root, path.join(agentsDir, card));
    const text = readText(path.join(agentsDir, card));
    for (const section of ROLE_SECTIONS) {
      if (!text.includes(section)) fail(relPath, `missing required section "${section}"`);
    }
  }
}

function checkInstructionBudget(root, cwd) {
  const resolvedCwd = path.resolve(cwd);
  const withinRoot = resolvedCwd === root || resolvedCwd.startsWith(root + path.sep);
  const chainCwd = withinRoot ? resolvedCwd : root;

  const dirs = [];
  let dir = chainCwd;
  for (;;) {
    dirs.unshift(dir);
    if (dir === root) break;
    const parent = path.dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }

  let total = 0;
  for (const current of dirs) {
    for (const name of INSTRUCTION_CANDIDATES) {
      const abs = path.join(current, name);
      if (fs.existsSync(abs)) total += fs.statSync(abs).size;
    }
  }

  if (total > MAX_CHAIN_BYTES) {
    fail('AGENTS.md', `instruction chain is ${total} bytes; the loader budget is ${MAX_CHAIN_BYTES} bytes`);
  } else if (total > CHAIN_WARN_BYTES) {
    warn('AGENTS.md', `instruction chain is ${total} bytes; keep it under ${CHAIN_WARN_BYTES} bytes for prompt cost`);
  }
}

function checkLocalOverlays(root) {
  let gitAvailable = true;
  for (const overlay of ['AGENTS.local.md', 'CLAUDE.local.md']) {
    if (!fs.existsSync(path.join(root, overlay))) continue;
    if (gitSucceeds(root, ['ls-files', '--error-unmatch', '--', overlay])) {
      fail(overlay, 'personal instruction overlay is tracked by git; remove it from the index');
    } else if (!gitSucceeds(root, ['rev-parse', '--is-inside-work-tree'])) {
      gitAvailable = false;
    }
  }
  if (!gitAvailable) warn('git', 'not a git worktree or git is unavailable; skipped tracking checks');
}

// --- Command Palette wiring ---------------------------------------------------------

function checkCmdPalProject(root) {
  const projectDir = path.join(root, PROJECT_DIR);
  const csprojPath = path.join(projectDir, 'ItsycalForWindows.csproj');
  const manifestPath = path.join(projectDir, 'Package.appxmanifest');
  const extensionPath = path.join(projectDir, 'ItsycalForWindows.cs');
  const providerPath = path.join(projectDir, 'ItsycalForWindowsCommandsProvider.cs');

  if (fs.existsSync(csprojPath)) {
    const csproj = readText(csprojPath);
    const relPath = rel(root, csprojPath);
    if (!/net10\.0-windows/i.test(csproj)) fail(relPath, 'TargetFramework must target net10.0-windows');
    if (!/<EnableMsixTooling>\s*true\s*</i.test(csproj)) fail(relPath, 'EnableMsixTooling must be true for an MSIX extension');
    if (!/Microsoft\.CommandPalette\.Extensions/.test(csproj)) fail(relPath, 'missing the Microsoft.CommandPalette.Extensions package reference');
  }

  if (fs.existsSync(providerPath)) {
    const provider = readText(providerPath);
    if (!/GetDockBands\s*\(/.test(provider)) {
      fail(rel(root, providerPath), 'the provider must override GetDockBands() for the dock calendar band');
    }
  }

  if (!fs.existsSync(manifestPath) || !fs.existsSync(extensionPath)) return;

  const manifest = readText(manifestPath);
  const manifestRel = rel(root, manifestPath);
  const extension = readText(extensionPath);

  if (!manifest.includes('com.microsoft.commandpalette')) {
    fail(manifestRel, 'missing the uap3 AppExtension registration for com.microsoft.commandpalette');
  }
  if (!manifest.includes('CmdPalProvider')) {
    fail(manifestRel, 'missing the CmdPalProvider activation block');
  }
  if (!manifest.includes('Itsycal for Windows')) {
    fail(manifestRel, 'DisplayName must be "Itsycal for Windows"');
  }
  if (/CN=Microsoft Corporation/.test(manifest)) {
    fail(manifestRel, 'Publisher still uses the template identity; set the development or release identity');
  }
  if (!/Name="appointments"/.test(manifest)) {
    warn(manifestRel, 'the appointments capability is not declared; events will be unavailable in the packaged app');
  }

  const guid = /\[Guid\("([^"]+)"\)\]/.exec(extension)?.[1]?.toUpperCase();
  if (!guid || !CLSID.test(guid)) {
    fail(rel(root, extensionPath), 'missing or malformed [Guid] CLSID on the IExtension class');
    return;
  }

  const classId = /\<com:Class\s+Id="([^"]+)"/.exec(manifest)?.[1]?.toUpperCase();
  const createId = /CreateInstance\s+ClassId="([^"]+)"/.exec(manifest)?.[1]?.toUpperCase();
  if (classId !== guid) {
    fail(manifestRel, `com:Class Id (${classId ?? 'missing'}) must equal the extension CLSID ${guid}`);
  }
  if (createId !== guid) {
    fail(manifestRel, `CreateInstance ClassId (${createId ?? 'missing'}) must equal the extension CLSID ${guid}`);
  }
}

// --- run ----------------------------------------------------------------------------

const { root: rootArg } = parseArgs(process.argv.slice(2));
const startDir = rootArg ? path.resolve(rootArg) : process.cwd();
const root = findGitRoot(startDir) ?? startDir;

checkRequiredFiles(root);
checkSkills(root);
checkNotes(root);
checkRoleCards(root);
checkInstructionBudget(root, process.cwd());
checkLocalOverlays(root);
checkCmdPalProject(root);

for (const line of failures) process.stdout.write(`FAIL ${line}\n`);
for (const line of warnings) process.stdout.write(`WARN ${line}\n`);

process.stdout.write(`scaffold-check: ${failures.length} failure(s), ${warnings.length} warning(s)\n`);
process.exit(failures.length === 0 ? 0 : 1);
