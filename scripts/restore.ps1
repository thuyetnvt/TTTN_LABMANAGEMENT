param(
    [Parameter(Mandatory = $true)][string]$DatabaseBackup,
    [Parameter(Mandatory = $true)][string]$UploadsArchive,
    [string]$UploadVolume = "",
    [string]$DataProtectionArchive = "",
    [string]$DataProtectionVolume = "",
    [switch]$ConfirmRestore
)

$ErrorActionPreference = "Stop"
if (-not $ConfirmRestore) {
    throw "Restore sẽ ghi đè dữ liệu database và file upload. Truyền -ConfirmRestore sau khi đã xác nhận môi trường đích."
}
if (-not (Test-Path -LiteralPath $DatabaseBackup)) { throw "Không tìm thấy file database backup." }
if (-not (Test-Path -LiteralPath $UploadsArchive)) { throw "Không tìm thấy archive upload backup." }

if ([string]::IsNullOrWhiteSpace($UploadVolume)) {
    $UploadVolume = (& docker volume ls --format '{{.Name}}' | Where-Object { $_ -match 'equipment_uploads$' } | Select-Object -First 1)
}
if ([string]::IsNullOrWhiteSpace($UploadVolume)) { throw "Không tìm thấy volume equipment_uploads. Hãy truyền -UploadVolume." }

Write-Host "Dừng backend/frontend trước khi restore"
& docker compose stop backend frontend | Out-Host

Write-Host "Restore database"
Get-Content -Raw -LiteralPath $DatabaseBackup | & docker compose exec -T db sh -c 'mysql -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE"'
if ($LASTEXITCODE -ne 0) { throw "Restore database thất bại." }

$archiveDirectory = (Resolve-Path (Split-Path -Parent $UploadsArchive)).Path.Replace('\', '/')
$archiveName = Split-Path $UploadsArchive -Leaf
Write-Host "Restore volume upload $UploadVolume"
& docker run --rm -v "${UploadVolume}:/target" -v "${archiveDirectory}:/backup:ro" alpine:3.20 sh -c "find /target -mindepth 1 -delete && tar xzf /backup/$archiveName -C /target"
if ($LASTEXITCODE -ne 0) { throw "Restore volume upload thất bại." }

if (-not [string]::IsNullOrWhiteSpace($DataProtectionArchive)) {
    if (-not (Test-Path -LiteralPath $DataProtectionArchive)) { throw "Không tìm thấy archive Data Protection." }
    if ([string]::IsNullOrWhiteSpace($DataProtectionVolume)) {
        $DataProtectionVolume = (& docker volume ls --format '{{.Name}}' |
            Where-Object { $_ -match 'backend_data_protection(?:_v\d+)?$' } |
            Sort-Object -Descending |
            Select-Object -First 1)
    }
    if ([string]::IsNullOrWhiteSpace($DataProtectionVolume)) { throw "Không tìm thấy volume Data Protection. Hãy truyền -DataProtectionVolume." }
    $dataProtectionDirectory = (Resolve-Path (Split-Path -Parent $DataProtectionArchive)).Path.Replace('\', '/')
    $dataProtectionName = Split-Path $DataProtectionArchive -Leaf
    Write-Host "Restore volume Data Protection $DataProtectionVolume"
    & docker run --rm -v "${DataProtectionVolume}:/target" -v "${dataProtectionDirectory}:/backup:ro" alpine:3.20 sh -c "find /target -mindepth 1 -delete && tar xzf /backup/$dataProtectionName -C /target"
    if ($LASTEXITCODE -ne 0) { throw "Restore volume Data Protection thất bại." }
}

& docker compose up -d backend frontend | Out-Host
Write-Host "Restore hoàn tất. Kiểm tra docker compose ps, /health, đăng nhập và mở file upload trước khi bàn giao."
