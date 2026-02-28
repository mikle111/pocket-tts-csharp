# PowerShell script to build C# native libraries using Docker
$ErrorActionPreference = "Stop"

Write-Host "Building Pocket TTS C# native libraries using Docker..." -ForegroundColor Green

# Check if Docker is available
try {
    docker --version | Out-Null
}
catch {
    Write-Host "Error: Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Check if Docker daemon is running
try {
    docker ps | Out-Null
}
catch {
    Write-Host "Error: Docker daemon is not running" -ForegroundColor Red
    Write-Host "Please start Docker Desktop" -ForegroundColor Yellow
    exit 1
}

# Build the Docker image with cross-compilation support
Write-Host "Building Docker image..." -ForegroundColor Cyan
docker build -f Dockerfile.csharp -t pocket-tts-csharp-builder .

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Docker build failed" -ForegroundColor Red
    exit 1
}

# Create a temporary container to extract the artifacts
Write-Host "Creating temporary container..." -ForegroundColor Cyan
$containerId = docker create pocket-tts-csharp-builder

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to create container" -ForegroundColor Red
    exit 1
}

# Create runtime directories
Write-Host "Creating runtime directories..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path "runtimes\win-x64\native" | Out-Null
New-Item -ItemType Directory -Force -Path "runtimes\linux-x64\native" | Out-Null

try {
    # Extract Linux library
    Write-Host "Extracting Linux x64 library..." -ForegroundColor Cyan
    docker cp "${containerId}:/linux-x64/libpocket_tts.so" "runtimes\linux-x64\native\"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to extract Linux library" -ForegroundColor Red
        throw
    }

    # Extract Windows library
    Write-Host "Extracting Windows x64 library..." -ForegroundColor Cyan
    docker cp "${containerId}:/win-x64/pocket_tts.dll" "runtimes\win-x64\native\"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to extract Windows library" -ForegroundColor Red
        throw
    }

    Write-Host ""
    Write-Host "Docker build complete!" -ForegroundColor Green
    Write-Host "Native libraries extracted to: runtimes\" -ForegroundColor Green
    Write-Host ""
    Write-Host "Built libraries:" -ForegroundColor Cyan
    Write-Host "  - runtimes\linux-x64\native\libpocket_tts.so" -ForegroundColor White
    Write-Host "  - runtimes\win-x64\native\pocket_tts.dll" -ForegroundColor White
}
finally {
    # Clean up container
    Write-Host ""
    Write-Host "Cleaning up temporary container..." -ForegroundColor Cyan
    docker rm $containerId | Out-Null
}