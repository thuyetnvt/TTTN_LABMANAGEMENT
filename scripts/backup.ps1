param(
    [string]$OutputDirectory = "backups",
    [string]$UploadVolume = "",
    [string]$DataProtectionVolume = ""
)

$ErrorActionPreference = "Stop"
$resolvedOutput = (New-Item -ItemType Directory -Force -Path $OutputDirectory).FullName
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$databaseBackup = Join-Path $resolvedOutput "lab-management-$stamp.sql"
$uploadsBackup = Join-Path $resolvedOutput "equipment-uploads-$stamp.tar.gz"
$dataProtectionBackup = Join-Path $resolvedOutput "data-protection-keys-$stamp.tar.gz"

Write-Host "Đang backup MySQL vào $databaseBackup"
& docker compose exec -T db sh -c 'mysqldump -u root -p"$MYSQL_ROOT_PASSWORD" --single-transaction --routines --triggers "$MYSQL_DATABASE"' | Set-Content -Path $databaseBackup -Encoding UTF8
if ($LASTEXITCODE -ne 0) { throw "Backup MySQL thất bại." }

if ([string]::IsNullOrWhiteSpace($UploadVolume)) {
    $UploadVolume = (& docker volume ls --format '{{.Name}}' | Where-Object { $_ -match 'equipment_uploads$' } | Select-Object -First 1)
}
if ([string]::IsNullOrWhiteSpace($UploadVolume)) { throw "Không tìm thấy volume equipment_uploads. Hãy truyền -UploadVolume." }

$outputMount = $resolvedOutput.Replace('\', '/')
Write-Host "Đang backup volume $UploadVolume vào $uploadsBackup"
& docker run --rm -v "${UploadVolume}:/source:ro" -v "${outputMount}:/backup" alpine:3.20 tar czf "/backup/$(Split-Path $uploadsBackup -Leaf)" -C /source .
if ($LASTEXITCODE -ne 0) { throw "Backup volume upload thất bại." }

if ([string]::IsNullOrWhiteSpace($DataProtectionVolume)) {
    $DataProtectionVolume = (& docker volume ls --format '{{.Name}}' |
        Where-Object { $_ -match 'backend_data_protection(?:_v\d+)?$' } |
        Sort-Object -Descending |
        Select-Object -First 1)
}
if (-not [string]::IsNullOrWhiteSpace($DataProtectionVolume)) {
    Write-Host "Đang backup volume Data Protection $DataProtectionVolume vào $dataProtectionBackup"
    & docker run --rm -v "${DataProtectionVolume}:/source:ro" -v "${outputMount}:/backup" alpine:3.20 tar czf "/backup/$(Split-Path $dataProtectionBackup -Leaf)" -C /source .
    if ($LASTEXITCODE -ne 0) { throw "Backup volume Data Protection thất bại." }
}

Write-Host "Hoàn tất: $databaseBackup và $uploadsBackup$(if ($DataProtectionVolume) { " và $dataProtectionBackup" })"
