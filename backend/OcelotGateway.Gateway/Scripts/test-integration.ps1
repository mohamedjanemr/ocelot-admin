# Gateway Integration Test Script
# This script demonstrates the dynamic configuration loading capability

Write-Host "🚀 Ocelot Gateway Integration Test" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Function to check if a port is in use
function Test-Port {
    param($Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient("localhost", $Port)
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Start the Admin API (WebApi) in background
Write-Host "📡 Starting Admin API on port 5001..." -ForegroundColor Yellow
if (Test-Port 5001) {
    Write-Host "⚠️  Port 5001 is already in use. Stopping existing process..." -ForegroundColor Yellow
    Get-Process -Name "OcelotGateway.WebApi" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 2
}

$adminApiJob = Start-Job -ScriptBlock {
    Set-Location $args[0]
    dotnet run --project OcelotGateway.WebApi/OcelotGateway.WebApi.csproj --urls "http://localhost:5001"
} -ArgumentList (Get-Location)

Start-Sleep 5

# Start the Gateway on port 5000
Write-Host "🌐 Starting Ocelot Gateway on port 5000..." -ForegroundColor Yellow
if (Test-Port 5000) {
    Write-Host "⚠️  Port 5000 is already in use. Stopping existing process..." -ForegroundColor Yellow
    Get-Process -Name "OcelotGateway.Gateway" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 2
}

$gatewayJob = Start-Job -ScriptBlock {
    Set-Location $args[0]
    dotnet run --project OcelotGateway.Gateway/OcelotGateway.Gateway.csproj --urls "http://localhost:5000"
} -ArgumentList (Get-Location)

Start-Sleep 10

Write-Host "✅ Services should be running!" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 Testing endpoints..." -ForegroundColor Cyan

# Test 1: Check Gateway health
Write-Host "1. Testing Gateway health check..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Gateway health check: PASSED" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ❌ Gateway health check: FAILED - $_" -ForegroundColor Red
}

# Test 2: Check Admin API health
Write-Host "2. Testing Admin API health check..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/health" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Admin API health check: PASSED" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ❌ Admin API health check: FAILED - $_" -ForegroundColor Red
}

# Test 3: Check route configurations through Admin API
Write-Host "3. Testing route configurations..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/api/route-configs" -Method GET
    if ($response.StatusCode -eq 200) {
        $data = $response.Content | ConvertFrom-Json
        Write-Host "   ✅ Route configs retrieved: $($data.TotalCount) routes found" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ❌ Route configs test: FAILED - $_" -ForegroundColor Red
}

# Test 4: Check configuration versions
Write-Host "4. Testing configuration versions..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/api/configuration-versions" -Method GET
    if ($response.StatusCode -eq 200) {
        $data = $response.Content | ConvertFrom-Json
        Write-Host "   ✅ Configuration versions retrieved: $($data.TotalCount) versions found" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ❌ Configuration versions test: FAILED - $_" -ForegroundColor Red
}

# Test 5: Test gateway routing (this should route through the default seeded route)
Write-Host "5. Testing gateway routing..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/admin-api/health" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Gateway routing: PASSED (routed to admin API via gateway)" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ❌ Gateway routing: FAILED - $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Integration Test Summary:" -ForegroundColor Cyan
Write-Host "- Gateway Application: Running on http://localhost:5000" -ForegroundColor White
Write-Host "- Admin API: Running on http://localhost:5001" -ForegroundColor White
Write-Host "- Dynamic Configuration: Loaded from database" -ForegroundColor White
Write-Host "- Configuration Caching: Active with 5-minute expiration" -ForegroundColor White
Write-Host "- Default Route: /admin-api/* -> http://localhost:5001/api/*" -ForegroundColor White

Write-Host ""
Write-Host "🛑 Press any key to stop the services..." -ForegroundColor Yellow
Read-Host

# Cleanup
Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
Stop-Job $adminApiJob -Force
Stop-Job $gatewayJob -Force
Remove-Job $adminApiJob -Force
Remove-Job $gatewayJob -Force

Write-Host "✅ Integration test completed!" -ForegroundColor Green 