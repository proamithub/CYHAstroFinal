$ftpHost = 'ftp://208.91.198.227'
$ftpUser = 'patnag5f'
$ftpPass = 'CYHBrand@PassAK'
$remotePath = '/cyhastro.patnaorthoclinic.com'
$localPublish = 'D:\samle pro\CyhAstro\publish\web'

function Upload-File($localFile, $remoteName) {
    $uri = [System.Uri]("$ftpHost$remotePath/$remoteName")
    $ftp = [System.Net.FtpWebRequest]::Create($uri)
    $ftp.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $ftp.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
    $ftp.UseBinary = $true
    $ftp.UsePassive = $true
    $bytes = [System.IO.File]::ReadAllBytes($localFile)
    $ftp.ContentLength = $bytes.Length
    $stream = $ftp.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    Write-Host "Uploaded: $remoteName ($($bytes.Length) bytes)"
}

# Upload app_offline.htm
$offlineContent = [System.Text.Encoding]::UTF8.GetBytes('<html><body>Site updating...</body></html>')
$uri2 = [System.Uri]("$ftpHost$remotePath/app_offline.htm")
$ftp2 = [System.Net.FtpWebRequest]::Create($uri2)
$ftp2.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
$ftp2.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
$ftp2.UseBinary = $true
$ftp2.UsePassive = $true
$ftp2.ContentLength = $offlineContent.Length
$s = $ftp2.GetRequestStream()
$s.Write($offlineContent, 0, $offlineContent.Length)
$s.Close()
Write-Host "app_offline.htm uploaded - site offline"
Start-Sleep -Seconds 2

# Upload all DLLs + PDBs + Static Assets
Upload-File (Join-Path $localPublish 'CYHAstro.Web.dll') 'CYHAstro.Web.dll'
Upload-File (Join-Path $localPublish 'CYHAstro.Web.pdb') 'CYHAstro.Web.pdb'
Upload-File (Join-Path $localPublish 'CYHAstro.Infrastructure.dll') 'CYHAstro.Infrastructure.dll'
Upload-File (Join-Path $localPublish 'CYHAstro.Infrastructure.pdb') 'CYHAstro.Infrastructure.pdb'
Upload-File (Join-Path $localPublish 'CYHAstro.Core.dll') 'CYHAstro.Core.dll'
Upload-File (Join-Path $localPublish 'CYHAstro.Core.pdb') 'CYHAstro.Core.pdb'

# Upload updated site.css for mobile & speed optimizations
Upload-File (Join-Path $localPublish 'wwwroot\css\site.css') 'wwwroot/css/site.css'

Write-Host "All DLLs and CSS uploaded successfully!"

# Remove app_offline.htm
$uri3 = [System.Uri]("$ftpHost$remotePath/app_offline.htm")
$ftp3 = [System.Net.FtpWebRequest]::Create($uri3)
$ftp3.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
$ftp3.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
$ftp3.UsePassive = $true
try {
    $resp = $ftp3.GetResponse()
    $resp.Close()
    Write-Host "app_offline.htm removed - site LIVE again!"
} catch {
    Write-Host "Note: Could not remove app_offline.htm - remove manually"
}
