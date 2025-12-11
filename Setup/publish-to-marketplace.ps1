param(
    [Parameter(Mandatory=$true)][string] $VsixPath,
    [Parameter(Mandatory=$true)][string] $Publisher,
    [Parameter(Mandatory=$true)][string] $Pat
)

# This script uploads a VSIX to Visual Studio Marketplace using the TFX CLI tool.
# It requires a Personal Access Token (PAT) with "Marketplace" publish scope.

function Get-VsixMetadata {
    param([string]$path)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    $sr = $null
    $reader = $null
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -ieq "extension.vsixmanifest" -or $_.FullName -ieq "source.extension.vsixmanifest" } | Select-Object -First 1
        if (-not $entry) { return $null }
        $sr = $entry.Open()
        $reader = New-Object System.IO.StreamReader($sr)
        $xmlContent = $reader.ReadToEnd()
        $xml = [xml]$xmlContent
        
        # Handle namespace for VSIX manifest
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("vsix", "http://schemas.microsoft.com/developer/vsx-schema/2011")
        
        $metadata = @{}
        # Get Identity element from Metadata section
        $identityNode = $xml.SelectSingleNode("//vsix:PackageManifest/vsix:Metadata/vsix:Identity", $ns)
        if ($identityNode) {
            $metadata.publisher = $identityNode.GetAttribute("Publisher")
            $metadata.id = $identityNode.GetAttribute("Id")
            $metadata.version = $identityNode.GetAttribute("Version")
        }
        return $metadata
    }
    finally {
        if ($reader) { $reader.Dispose() }
        if ($sr) { $sr.Dispose() }
        if ($zip) { $zip.Dispose() }
    }
}

# Verify VSIX file exists
if (-not (Test-Path $VsixPath)) {
    Write-Error "VSIX file not found: $VsixPath"
    exit 1
}

$metadata = Get-VsixMetadata -path $VsixPath
if (-not $metadata) { 
    Write-Error "Unable to read VSIX manifest metadata."
    exit 1
}

Write-Host "Extension ID: $($metadata.id)"
Write-Host "Publisher: $($metadata.publisher)"
Write-Host "Version: $($metadata.version)"

if ($metadata.publisher -ne $Publisher) {
    Write-Warning "VSIX publisher ('$($metadata.publisher)') does not match provided publisher ('$Publisher')."
}

# Check if Node.js is installed
$nodeVersion = $null
$nodeInstalled = $false
try {
    $nodeVersion = node --version 2>&1
    $nodeInstalled = ($LASTEXITCODE -eq 0)
}
catch {
    $nodeInstalled = $false
}

if (-not $nodeInstalled) {
    Write-Error "Node.js is not installed or not in PATH. TFX CLI requires Node.js."
    exit 1
}
Write-Host "Node.js version: $nodeVersion"

# Check if tfx-cli is installed
$tfxVersion = $null
$tfxInstalled = $false
try {
    $tfxVersion = tfx version 2>&1
    $tfxInstalled = ($LASTEXITCODE -eq 0)
}
catch {
    $tfxInstalled = $false
}

if (-not $tfxInstalled) {
    Write-Host "TFX CLI not found. Installing tfx-cli..."
    npm install -g tfx-cli
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install tfx-cli"
        exit 1
    }
    Write-Host "TFX CLI installed successfully."
}
else {
    Write-Host "TFX CLI version: $tfxVersion"
}

# Publish extension using TFX CLI
Write-Host "Publishing extension to Visual Studio Marketplace..."

# Note: Passing PAT via --token is the standard method recommended by Microsoft
# TFX CLI doesn't support reading token from stdin or file
# The token is handled securely by TFX CLI and not exposed in logs
tfx extension publish --vsix "$VsixPath" --token $Pat --no-prompt

if ($LASTEXITCODE -eq 0) {
    Write-Host "Extension published successfully to Visual Studio Marketplace!"
    exit 0
}
else {
    Write-Error "Failed to publish extension. TFX CLI exit code: $LASTEXITCODE"
    Write-Host ""
    Write-Host "Troubleshooting tips:"
    Write-Host "1. Verify that the PAT token has 'Marketplace (Publish)' scope"
    Write-Host "2. Verify that the publisher '$Publisher' exists in Visual Studio Marketplace"
    Write-Host "3. Check if the extension already exists and needs to be updated"
    Write-Host "4. Visit https://marketplace.visualstudio.com/manage/publishers/$Publisher to manage your extensions"
    exit 1
}
