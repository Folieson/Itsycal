# Copyright (c) Microsoft Corporation
# The Microsoft Corporation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Regenerates the MSIX/EXE brand assets from the upstream ItsyCal app icon.

.DESCRIPTION
The upstream macOS icon (MIT) is kept as `windows/src/ItsycalForWindows/Assets/SourceIcon.png`.
This script resizes it into the exact asset names the package manifest, the executable and
the Inno Setup installer expect. Run it after changing the source icon; commit the output.

.EXAMPLE
.\windows\scripts\New-BrandAssets.ps1
#>

[CmdletBinding()]
param(
    [string]$SourcePng,
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
if (-not $SourcePng) {
    $SourcePng = Join-Path $RepoRoot 'windows\src\ItsycalForWindows\Assets\SourceIcon.png'
}
if (-not (Test-Path $SourcePng)) {
    throw "Source icon not found: $SourcePng"
}

$assets = Join-Path $RepoRoot 'windows\src\ItsycalForWindows\Assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

$source = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePng).Path)
Write-Host ("Source icon: {0} ({1}x{2})" -f (Split-Path $SourcePng -Leaf), $source.Width, $source.Height)

function New-ResizedPng {
    param(
        [string]$Name,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Image]$Image,
        [string]$Destination
    )

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $scale = [Math]::Min($Width / $Image.Width, $Height / $Image.Height)
        $drawWidth = [int][Math]::Round($Image.Width * $scale)
        $drawHeight = [int][Math]::Round($Image.Height * $scale)
        $offsetX = [int](($Width - $drawWidth) / 2)
        $offsetY = [int](($Height - $drawHeight) / 2)

        $graphics.DrawImage($Image, $offsetX, $offsetY, $drawWidth, $drawHeight)
    }
    finally {
        $graphics.Dispose()
    }

    $path = Join-Path $Destination $Name
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host ("wrote {0} ({1}x{2})" -f $Name, $Width, $Height)
}

# Package logos (scale-200 assets carry the 200% pixel size).
New-ResizedPng -Name 'StoreLogo.png' -Width 50 -Height 50 -Image $source -Destination $assets
New-ResizedPng -Name 'Square44x44Logo.scale-200.png' -Width 88 -Height 88 -Image $source -Destination $assets
New-ResizedPng -Name 'Square44x44Logo.targetsize-24_altform-unplated.png' -Width 24 -Height 24 -Image $source -Destination $assets
New-ResizedPng -Name 'Square150x150Logo.scale-200.png' -Width 300 -Height 300 -Image $source -Destination $assets
New-ResizedPng -Name 'Wide310x150Logo.scale-200.png' -Width 620 -Height 300 -Image $source -Destination $assets
New-ResizedPng -Name 'SplashScreen.scale-200.png' -Width 1240 -Height 600 -Image $source -Destination $assets
New-ResizedPng -Name 'LockScreenLogo.scale-200.png' -Width 48 -Height 48 -Image $source -Destination $assets

# Multi-size ICO for the executable and the installer.
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = @()
foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($source, 0, 0, $size, $size)
    }
    finally {
        $graphics.Dispose()
    }

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += ,$stream.ToArray()
    $bitmap.Dispose()
    $stream.Dispose()
}

$icoPath = Join-Path $assets 'ItsycalIcon.ico'
$icoStream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($icoStream)
$writer.Write([UInt16]0)              # reserved
$writer.Write([UInt16]1)              # icon type
$writer.Write([UInt16]$sizes.Count)   # image count
$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))  # width
    $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))  # height
    $writer.Write([byte]0)             # color palette
    $writer.Write([byte]0)             # reserved
    $writer.Write([UInt16]1)           # color planes
    $writer.Write([UInt16]32)          # bits per pixel
    $writer.Write([UInt32]$frames[$i].Length)
    $writer.Write([UInt32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) { $writer.Write($frame) }
$writer.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $icoStream.ToArray())
$writer.Dispose()
$icoStream.Dispose()
Write-Host "wrote ItsycalIcon.ico ($($sizes -join ', ') px)"

$source.Dispose()
Write-Host 'Brand assets regenerated.'
