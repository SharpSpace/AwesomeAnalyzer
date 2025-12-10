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

# Create new extension (multipart) - upload the VSIX
$uri = "https://marketplace.visualstudio.com/_apis/gallery/publishers/$Publisher/extensions?api-version=6.0-preview.1"
$body = Get-Item -Path $VsixPath

Write-Host "Uploading to Marketplace: $uri"
$response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -InFile $VsixPath -ContentType "application/octet-stream"

if ($response -and $response.id) {
    Write-Host "Publish succeeded. Extension id: $($response.versions[0].version)"
    exit 0
}
else {
    Write-Error "Publish failed. Response: $($response | ConvertTo-Json -Depth 5)"
    exit 1
}
