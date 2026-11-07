# Copyright (c) Microsoft Corporation
# The Microsoft Corporation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

<#
.SYNOPSIS
Installs the Itsycal for Windows development MSIX package on this machine.

.DESCRIPTION
The extension is an MSIX-packaged COM server, so a plain build is not enough: the package
must be registered with a signing certificate the machine trusts. CI produces a development
package signed with an ephemeral self-signed certificate (subject `CN=ItsycalForWindows-Dev`)
and uploads the `.msix` plus the public `.cer`.

This script trusts that public certificate in the machine-wide `TrustedPeople` store and
registers the package. It must run elevated: MSIX sideloading trust is machine-wide.

.PARAMETER MsixPath
Path to the downloaded `ItsycalForWindows_*.msix`.

.PARAMETER CertPath
Path to the matching `devcert.cer` from the same CI artifact.

.PARAMETER NoRestart
Do not restart Command Palette after installation.

.EXAMPLE
.\scripts\deploy-local.ps1 -MsixPath .\ItsycalForWindows_0.1.0.0_x64_Debug.msix -CertPath .\devcert.cer
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MsixPath,

    [Parameter(Mandatory = $true)]
    [string]$CertPath,

    [switch]$NoRestart
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell: MSIX trust is stored machine-wide.'
}

$msix = (Resolve-Path -LiteralPath $MsixPath).Path
$cert = (Resolve-Path -LiteralPath $CertPath).Path

Import-Certificate -FilePath $cert -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
Write-Host "Trusted the development certificate: $cert"

Add-AppxPackage -Path $msix
Write-Host "Installed: $msix"

if (-not $NoRestart) {
    $commandPalette = Get-AppxPackage -Name 'Microsoft.CommandPalette'
    if ($commandPalette) {
        $appId = (Get-AppxPackageManifest $commandPalette).Package.Applications.Application.Id
        Get-Process 'Microsoft.CmdPal.UI' -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Seconds 2
        Start-Process "shell:AppsFolder\$($commandPalette.PackageFamilyName)!$appId"
        Write-Host 'Restarted Command Palette. Enable the "Itsycal for Windows" dock band in its Dock settings.'
    } else {
        Write-Host 'Command Palette package not found; restart PowerToys or Command Palette manually.'
    }
}
