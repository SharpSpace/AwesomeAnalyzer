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

# The extension ID for the marketplace is just the ID part without the GUID suffix
# For example, "AwesomeAnalyzer.9591450a-2975-42c9-95d4-7e4b1f15d053" becomes "AwesomeAnalyzer"
$extensionId = $metadata.id
if ($extensionId -match '^(.+?)\.[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$') {
    $extensionId = $Matches[1]
}

Write-Host "Extension ID: $extensionId"
Write-Host "Publisher: $Publisher"
Write-Host "Version: $($metadata.version)"

# The Visual Studio Marketplace API endpoint for updating extensions
# https://learn.microsoft.com/en-us/rest/api/azure/devops/extensionmanagement/extensions/update
$updateUri = "https://marketplace.visualstudio.com/_apis/gallery/publisher/$Publisher/extension?api-version=7.1-preview.1"

Write-Host "Uploading to: $updateUri"

try {
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    # Build multipart content
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"$(Split-Path -Leaf $VsixPath)`"",
        "Content-Type: application/octet-stream$LF"
    )
    $bodyStart = $bodyLines -join $LF
    $bodyEnd = "$LF--$boundary--$LF"
    
    # Combine all parts
    $bodyStartBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyStart)
    $bodyEndBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyEnd)
    
    # Create combined byte array
    $contentLength = $bodyStartBytes.Length + $fileContent.Length + $bodyEndBytes.Length
    $content = New-Object byte[] $contentLength
    [Array]::Copy($bodyStartBytes, 0, $content, 0, $bodyStartBytes.Length)
    [Array]::Copy($fileContent, 0, $content, $bodyStartBytes.Length, $fileContent.Length)
    [Array]::Copy($bodyEndBytes, 0, $content, $bodyStartBytes.Length + $fileContent.Length, $bodyEndBytes.Length)
    
    # Create web request
    $request = [System.Net.HttpWebRequest]::Create($updateUri)
    $request.Method = "PUT"
    $request.ContentType = "multipart/form-data; boundary=$boundary"
    $request.ContentLength = $content.Length
    $request.Headers.Add("Authorization", "Basic $base64Pat")
    $request.Headers.Add("Accept", "application/json")
    
    # Write the content to the request stream
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($content, 0, $content.Length)
    $requestStream.Close()
    
    # Get the response
    $response = $request.GetResponse()
    $statusCode = [int]$response.StatusCode
    Write-Host "Response status: $statusCode $($response.StatusDescription)"
    
    $responseStream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($responseStream)
    $responseBody = $reader.ReadToEnd()
    $reader.Close()
    $response.Close()
    
    Write-Host "Extension updated successfully."
    if ($responseBody) {
        Write-Host "Response: $responseBody"
    }
}
catch {
    Write-Error "Failed to update extension: $($_.Exception.Message)"
    if ($_.Exception.InnerException) {
        Write-Error "Inner exception: $($_.Exception.InnerException.Message)"
    }
    if ($_.Exception -is [System.Net.WebException]) {
        $webResponse = $_.Exception.Response
        if ($webResponse) {
            $statusCode = [int]$webResponse.StatusCode
            Write-Error "Status code: $statusCode"
            $reader = New-Object System.IO.StreamReader($webResponse.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Error "Response: $responseBody"
            $reader.Close()
        }
    }
    Write-Error "Note: This script only updates existing extensions. If the extension does not exist on the marketplace, it must be created manually first."
    exit 1
}

if ($responseBody) {
    try {
        $responseObj = $responseBody | ConvertFrom-Json
        if ($responseObj.extensionId) {
            $version = if ($responseObj.versions) { $responseObj.versions[0].version } else { $metadata.version }
            Write-Host "Publish succeeded. Extension: $($responseObj.extensionId), Version: $version"
            exit 0
        }
        else {
            Write-Host "Publish completed successfully."
            exit 0
        }
    }
    catch {
        Write-Host "Publish completed successfully."
        exit 0
    }
}
else {
    Write-Host "Publish completed successfully."
    exit 0
}
