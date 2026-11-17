$ErrorActionPreference = "Stop"

$ftpHost = 'ftp://208.91.198.227'
$ftpUser = 'patnag5f'
$ftpPass = 'CYHBrand@PassAK'
$webRemotePath = '/cyhastro.patnaorthoclinic.com'
$apiRemotePath = '/cyhastroapi.patnaorthoclinic.com'
$localWebPublish = 'D:\MyAppAi\CYHAstroFinal\publish\web'
$localApiPublish = 'D:\MyAppAi\CYHAstroFinal\publish\api'

function Upload-Curl($localFile, $remoteFullPath) {
    if (-not (Test-Path $localFile)) {
        Write-Host "Skipped (not found): $localFile"
        return
    }
    $url = "$ftpHost$remoteFullPath"
    Write-Host "Uploading $localFile -> $remoteFullPath ..."
    & curl.exe --silent --show-error --fail --ftp-create-dirs -u "${ftpUser}:${ftpPass}" -T "$localFile" "$url"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to upload $remoteFullPath (exit code $LASTEXITCODE)"
    }
    Write-Host "OK: $remoteFullPath"
}

function Delete-Curl($remoteFullPath) {
    Write-Host "Deleting $remoteFullPath ..."
    & curl.exe --silent --show-error -u "${ftpUser}:${ftpPass}" "$ftpHost/" -Q "DELE $remoteFullPath" | Out-Null
    Write-Host "Deleted (or not present): $remoteFullPath"
}

# Create temporary app_offline.htm locally
$offlineLocal = Join-Path $env:TEMP 'cyhastro_app_offline.htm'
Set-Content -Path $offlineLocal -Value '<html><body style="font-family:sans-serif;text-align:center;padding:50px;background:#0a0d14;color:#fff;"><h2>CYHAstro is updating... Back online in a few seconds!</h2></body></html>' -Encoding UTF8

Write-Host "=== Step 1: Deploying CYHAstro.Web to $webRemotePath ==="
Upload-Curl $offlineLocal "$webRemotePath/app_offline.htm"
Start-Sleep -Seconds 3

try {
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Web.dll') "$webRemotePath/CYHAstro.Web.dll"
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Web.pdb') "$webRemotePath/CYHAstro.Web.pdb"
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Infrastructure.dll') "$webRemotePath/CYHAstro.Infrastructure.dll"
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Infrastructure.pdb') "$webRemotePath/CYHAstro.Infrastructure.pdb"
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Core.dll') "$webRemotePath/CYHAstro.Core.dll"
    Upload-Curl (Join-Path $localWebPublish 'CYHAstro.Core.pdb') "$webRemotePath/CYHAstro.Core.pdb"
    Upload-Curl (Join-Path $localWebPublish 'wwwroot\css\site.css') "$webRemotePath/wwwroot/css/site.css"

    # Check live appsettings.json & appsettings.Production.json Gemini key (read from env or appsettings.Local.json)
    $geminiKey = $env:GEMINI_API_KEY
    if ([string]::IsNullOrWhiteSpace($geminiKey) -and (Test-Path "D:\MyAppAi\CYHAstroFinal\CYHAstro.Web\appsettings.Local.json")) {
        $localCfg = Get-Content "D:\MyAppAi\CYHAstroFinal\CYHAstro.Web\appsettings.Local.json" -Raw | ConvertFrom-Json
        $geminiKey = $localCfg.GeminiSettings.ApiKey
    }
    foreach ($cfgName in @("appsettings.json", "appsettings.Production.json")) {
        $liveSettings = & curl.exe --silent -u "${ftpUser}:${ftpPass}" "$ftpHost$webRemotePath/$cfgName"
        if (-not [string]::IsNullOrWhiteSpace($geminiKey) -and $liveSettings -match "YOUR_GEMINI_API_KEY_HERE") {
            Write-Host "Live $cfgName still has placeholder Gemini key! Updating Gemini key..."
            $updatedSettings = $liveSettings -replace "YOUR_GEMINI_API_KEY_HERE", $geminiKey
            $tmpSettings = Join-Path $env:TEMP "cyhastro_live_$cfgName"
            Set-Content -Path $tmpSettings -Value $updatedSettings -Encoding UTF8
            Upload-Curl $tmpSettings "$webRemotePath/$cfgName"
            Remove-Item $tmpSettings -Force
        }
    }
}
finally {
    Delete-Curl "$webRemotePath/app_offline.htm"
}

Write-Host "=== Step 2: Checking CYHAstro.Api at $apiRemotePath ==="
$apiList = & curl.exe --silent -u "${ftpUser}:${ftpPass}" "$ftpHost$apiRemotePath/"
if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($apiList)) {
    Write-Host "Found API folder on server. Deploying CYHAstro.Api..."
    Upload-Curl $offlineLocal "$apiRemotePath/app_offline.htm"
    Start-Sleep -Seconds 2
    try {
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Api.dll') "$apiRemotePath/CYHAstro.Api.dll"
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Api.pdb') "$apiRemotePath/CYHAstro.Api.pdb"
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Infrastructure.dll') "$apiRemotePath/CYHAstro.Infrastructure.dll"
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Infrastructure.pdb') "$apiRemotePath/CYHAstro.Infrastructure.pdb"
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Core.dll') "$apiRemotePath/CYHAstro.Core.dll"
        Upload-Curl (Join-Path $localApiPublish 'CYHAstro.Core.pdb') "$apiRemotePath/CYHAstro.Core.pdb"
    }
    finally {
        Delete-Curl "$apiRemotePath/app_offline.htm"
    }
} else {
    Write-Host "API remote directory $apiRemotePath not present on this FTP account (or empty)."
}

Remove-Item $offlineLocal -Force -ErrorAction SilentlyContinue
Write-Host "=== Deployment Finished Successfully! ==="
