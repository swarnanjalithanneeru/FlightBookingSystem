# Start-AllServices.ps1
# Helper script to launch all 5 microservices and the API Gateway in separate console windows for local testing.

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$baseDir = $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Starting FlightBookingSystem Microservices & API Gateway" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. User Service (Port 5001)
Write-Host "Starting User Service on http://localhost:5001/swagger ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/Services/UserService'; dotnet run --launch-profile http"

# 2. Flight Service (Port 5002)
Write-Host "Starting Flight Service on http://localhost:5002/swagger ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/Services/FlightService'; dotnet run --launch-profile http"

# 3. Booking Service (Port 5003)
Write-Host "Starting Booking Service on http://localhost:5003/swagger ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/Services/BookingService'; dotnet run --launch-profile http"

# 4. Passenger Service (Port 5004)
Write-Host "Starting Passenger Service on http://localhost:5004/swagger ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/Services/PassengerService'; dotnet run --launch-profile http"

# 5. Payment Service (Port 5005)
Write-Host "Starting Payment Service on http://localhost:5005/swagger ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/Services/PaymentService'; dotnet run --launch-profile http"

Start-Sleep -Seconds 3

# 6. API Gateway (Port 5000)
Write-Host "Starting API Gateway (YARP) on http://localhost:5000/swagger ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$baseDir/src/ApiGateway'; dotnet run --launch-profile http"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " All services started! Open Swagger UI at:" -ForegroundColor Cyan
Write-Host " API Gateway Swagger UI: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host " User Service Swagger:   http://localhost:5001/swagger" -ForegroundColor White
Write-Host " Flight Service Swagger: http://localhost:5002/swagger" -ForegroundColor White
Write-Host " Booking Service Swagger: http://localhost:5003/swagger" -ForegroundColor White
Write-Host " Passenger Service Swagger: http://localhost:5004/swagger" -ForegroundColor White
Write-Host " Payment Service Swagger: http://localhost:5005/swagger" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
