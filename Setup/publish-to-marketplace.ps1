param(
    [Parameter(Mandatory=$true)][string] $VsixPath,
    [Parameter(Mandatory=$true)][string] $Publisher,
    [Parameter(Mandatory=$true)][string] $Pat
)

# This script uploads a VSIX to Visual Studio Marketplace using the Microsoft Marketplace REST API.
# It requires a Personal Access Token (PAT) with "Marketplace" publish scope.

function Get-VsixMetadata {
    param([string]$path)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -ieq "extension.vsixmanifest" -or $_.FullName -ieq "source.extension.vsixmanifest" } | Select-Object -First 1
        if (-not $entry) { return $null }
        $sr = $entry.Open()
        $reader = New-Object System.IO.StreamReader($sr)
        $xml = [xml]$reader.ReadToEnd()
        $metadata = @{}
        $metadata.publisher = $xml.Package.Identity.publisher
        $metadata.id = $xml.Package.Identity.Id
        $metadata.version = $xml.Package.Identity.Version
        return $metadata
    }
    finally {
        $zip.Dispose()
    }
}

$metadata = Get-VsixMetadata -path $VsixPath
if (-not $metadata) { Write-Error "Unable to read VSIX manifest metadata."; exit 1 }

if ($metadata.publisher -ne $Publisher) {
    Write-Warning "VSIX publisher ('$($metadata.publisher)') does not match provided publisher ('$Publisher')."
}

$base64Pat = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))
$headers = @{ Authorization = "Basic $base64Pat"; Accept = "application/json" }

# Upload VSIX to Visual Studio Marketplace
# Read the VSIX file as bytes for upload
$fileContent = [System.IO.File]::ReadAllBytes($VsixPath)

# Update existing extension on Visual Studio Marketplace (does not create new extensions)
$updateUri = "https://marketplace.visualstudio.com/_apis/gallery/publishers/$Publisher/extensions/$($metadata.id)?api-version=7.1-preview.1"

Write-Host "Updating existing extension: $updateUri"

try {
    $response = Invoke-RestMethod -Uri $updateUri `
        -Method Put `
        -Headers $headers `
        -Body $fileContent `
        -ContentType "application/octet-stream"
    
    Write-Host "Extension updated successfully."
}
catch {
    Write-Error "Failed to update extension: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Error "Response: $responseBody"
    }
    Write-Error "Note: This script only updates existing extensions. If the extension does not exist on the marketplace, it must be created manually first."
    exit 1
}

if ($response -and $response.id) {
    $version = if ($response.versions) { $response.versions[0].version } else { $metadata.version }
    Write-Host "Publish succeeded. Extension: $($response.extensionId), Version: $version"
    exit 0
}
else {
    Write-Error "Publish completed but response format unexpected. Response: $($response | ConvertTo-Json -Depth 5)"
    exit 1
}
