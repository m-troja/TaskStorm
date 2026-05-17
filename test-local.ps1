$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$composeFile = "TaskStorm.API\docker-compose.local.yml"

Write-Host "==================================================" -ForegroundColor DarkCyan
Write-Host " TaskStorm Local Bootstrap" -ForegroundColor DarkCyan
Write-Host "==================================================" -ForegroundColor DarkCyan

# ==================================================
# 1. VALIDATE
# ==================================================

Write-Host "`n=== 1. Validate compose ===" -ForegroundColor Cyan

docker compose -f $composeFile config 2>$null
if ($LASTEXITCODE -ne 0) { exit 1 }

# ==================================================
# 2. CLEANUP
# ==================================================

Write-Host "`n=== 2. Cleanup ===" -ForegroundColor Cyan

docker compose -f $composeFile down -v --remove-orphans

# ==================================================
# 3. BUILD
# ==================================================

Write-Host "`n=== 3. Build ===" -ForegroundColor Cyan

docker compose -f $composeFile build --no-cache
if ($LASTEXITCODE -ne 0) { exit 1 }

# ==================================================
# 4. START INFRA
# ==================================================

Write-Host "`n=== 4. Start RabbitMQ ===" -ForegroundColor Cyan

docker compose -f $composeFile up -d rabbitmq.local
if ($LASTEXITCODE -ne 0) { exit 1 }

# ==================================================
# 5. WAIT RABBITMQ (HEALTHCHECK)
# ==================================================

Write-Host "`n=== 5. Waiting for RabbitMQ ===" -ForegroundColor Cyan

$maxRetries = 30
$retry = 0
$ready = $false

while ($retry -lt $maxRetries) {

    $status = docker inspect --format='{{.State.Health.Status}}' taskstorm-rabbitmq-local 2>$null

    Write-Host "RabbitMQ status: $status"

    if ($status -eq "healthy") {
        $ready = $true
        break
    }

    Start-Sleep -Seconds 2
    $retry++
}

if (-not $ready) {
    Write-Host "[ERROR] RabbitMQ not healthy" -ForegroundColor Red
    docker compose -f $composeFile logs rabbitmq.local
    exit 1
}

Write-Host "[OK] RabbitMQ ready" -ForegroundColor Green

# ==================================================
# 6. START API
# ==================================================

Write-Host "`n=== 6. Start API ===" -ForegroundColor Cyan

docker compose -f $composeFile up -d taskstorm.api
if ($LASTEXITCODE -ne 0) { exit 1 }

Start-Sleep -Seconds 5

# ==================================================
# 7. STATUS
# ==================================================

Write-Host "`n=== 7. Containers ===" -ForegroundColor Cyan

docker compose -f $composeFile ps

# ==================================================
# 8. TESTS
# ==================================================

Write-Host "`n=== 8. Tests ===" -ForegroundColor Cyan

dotnet test TaskStorm.Tests/TaskStorm.Tests.csproj --no-build
$testResult = $LASTEXITCODE

# ==================================================
# 9. LOGS ON FAIL
# ==================================================

if ($testResult -ne 0) {
    Write-Host "`n[ERROR] Tests failed" -ForegroundColor Red
    docker compose -f $composeFile logs taskstorm.api
}

# ==================================================
# 10. ENDPOINTS
# ==================================================

Write-Host "`n=== ENDPOINTS ===" -ForegroundColor Cyan

Write-Host "API:      http://localhost:5167"
Write-Host "RabbitMQ: http://localhost:15672"
Write-Host "Logs:     C:\tmp\docker\"

exit $testResult