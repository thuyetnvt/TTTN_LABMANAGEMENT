param(
    [string]$ProjectName = "labmanagement-e2e"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "docker-compose.e2e.yml"
$frontendDirectory = Join-Path $repositoryRoot "lab-frontend"

try {
    docker compose -p $ProjectName -f $composeFile up -d --build --wait

    $env:E2E_BASE_URL = "http://127.0.0.1:8081"
    $env:E2E_TEST_DATABASE = "1"
    $env:E2E_BUSINESS_FLOW = "1"
    $env:E2E_ADMIN_USERNAME = "admin"
    $env:E2E_MANAGER_USERNAME = "truonglab"
    $env:E2E_DEPUTY_USERNAME = "pholab"
    $env:E2E_TEACHER_USERNAME = "giangvien1"
    $env:E2E_STUDENT_USERNAME = "sv1"
    $env:E2E_ADMIN_PASSWORD = "E2e-Test-Password-2026!"
    $env:E2E_BUSINESS_PASSWORD = "E2e-Test-Password-2026!"
    $env:E2E_ROLE_PASSWORD = "E2e-Test-Password-2026!"

    Push-Location $frontendDirectory
    try {
        npm run test:e2e:full
    }
    finally {
        Pop-Location
    }
}
finally {
    docker compose -p $ProjectName -f $composeFile down --volumes --remove-orphans
}
