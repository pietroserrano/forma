#!/usr/bin/env pwsh

# test-workflow.ps1 - Script for testing GitHub Actions locally with act
# 
# Examples:
#   # Test core package deployment workflow with default settings
#   .\test-workflow.ps1 -WorkflowType core
#
#   # Test component package deployment workflow for chains
#   .\test-workflow.ps1 -WorkflowType component -Component chains
# 
#   # Test with local NuGet server
#   .\test-workflow.ps1 -WorkflowType core -UseLocalNuget
#
#   # Test with custom local NuGet server settings
#   .\test-workflow.ps1 -WorkflowType component -Component pubsub -UseLocalNuget -NugetServerPort 5000

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("core", "component")]
    [string]$WorkflowType,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0-test",
    
    [Parameter(Mandatory=$false)]
    [string]$Component = "chains",
    
    [Parameter(Mandatory=$false)]
    [switch]$UseLocalNuget,
    
    [Parameter(Mandatory=$false)]
    [string]$NugetContainerName = "local-nuget-server",
    
    [Parameter(Mandatory=$false)]
    [string]$NugetServerPort = "5555"
)

# Check if Docker is running
try {
    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker is not running. Start Docker and try again."
        exit 1
    }
}
catch {
    Write-Error "Unable to connect to Docker. Make sure Docker is installed and running."
    exit 1
}

# Check if act is installed
if (!(Get-Command act -ErrorAction SilentlyContinue)) {
    Write-Error "act is not installed. Install act with 'winget install nektos.act' or follow the instructions in scripts/install-act.md"
    exit 1
}

# Function to start local NuGet server using Docker
function Start-LocalNugetServer {
    param (
        [string]$ContainerName,
        [string]$Port
    )

    # Check if container already exists
    $containerExists = docker ps -a --filter name=$ContainerName --format "{{.Names}}"
    if ($containerExists) {
        Write-Host "NuGet server container '$ContainerName' already exists." -ForegroundColor Yellow
        
        # Check if it's running
        $isRunning = docker ps --filter name=$ContainerName --format "{{.Names}}"
        if (!$isRunning) {
            Write-Host "Starting existing NuGet server container..." -ForegroundColor Cyan
            docker start $ContainerName
        }
    }
    else {
        Write-Host "Creating and starting NuGet server container..." -ForegroundColor Cyan
        
        # Create volume for persistent storage
        $volumeName = "$ContainerName-data"
        $volumeExists = docker volume ls --filter name=$volumeName --format "{{.Name}}"
        if (!$volumeExists) {
            docker volume create $volumeName
        }
        
        # Start BaGet NuGet server
        # BaGet is a lightweight NuGet server implementation
        docker run -d --name $ContainerName `
            -p "${Port}:80" `
            -e ApiKey=TEST-API-KEY `
            -v "${volumeName}:/var/baget" `
            --restart unless-stopped `
            loicsharma/baget:latest
            
        # Give the container a moment to start up
        Start-Sleep -Seconds 3
    }
    
    # Verify the container is running
    $isRunning = docker ps --filter name=$ContainerName --format "{{.Names}}"
    if (!$isRunning) {
        Write-Error "Failed to start NuGet server container."
        return $false
    }
    
    Write-Host "Local NuGet server is running at http://localhost:$Port" -ForegroundColor Green
    Write-Host "You can push packages with API key: TEST-API-KEY" -ForegroundColor Green
    return $true
}

# Function to stop NuGet server
function Stop-LocalNugetServer {
    param (
        [string]$ContainerName,
        [bool]$Remove = $false
    )
    
    $containerExists = docker ps -a --filter name=$ContainerName --format "{{.Names}}"
    if ($containerExists) {
        if ($Remove) {
            Write-Host "Removing NuGet server container..." -ForegroundColor Yellow
            docker rm -f $ContainerName
        }
        else {
            Write-Host "Stopping NuGet server container..." -ForegroundColor Yellow
            docker stop $ContainerName
        }
    }
}

# Determine which workflow to test and set parameters
$workflowFile = ""
$eventName = "push"
$eventJson = ""

if ($WorkflowType -eq "core") {
    $workflowFile = ".github/workflows/nuget-deploy.yml"
    $tagName = "v${Version}-core"
    $eventJson = @"
{
  "ref": "refs/tags/$tagName",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
"@
}
else {
    $workflowFile = ".github/workflows/nuget-component-deploy.yml"
    $tagName = "v${Version}-${Component}"
    $eventJson = @"
{
  "ref": "refs/tags/$tagName",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
"@
}

# Handle local NuGet server if requested
$localNugetRunning = $false
$nugetSourceUrl = "https://api.nuget.org/v3/index.json"
$nugetApiKey = "fake-api-key"

if ($UseLocalNuget) {
    $localNugetRunning = Start-LocalNugetServer -ContainerName $NugetContainerName -Port $NugetServerPort
    
    if ($localNugetRunning) {
        $nugetSourceUrl = "http://localhost:${NugetServerPort}/v3/index.json"
        $nugetApiKey = "TEST-API-KEY"
        
        # Create a temporary workflow file with the modified NuGet source
        $tempFolder = Join-Path $env:TEMP "forma-workflow-temp"
        if (!(Test-Path -Path $tempFolder)) {
            New-Item -ItemType Directory -Path $tempFolder | Out-Null
        }
        
        $originalWorkflowPath = Join-Path $PSScriptRoot ".." $workflowFile
        $tempWorkflowPath = Join-Path $tempFolder (Split-Path $workflowFile -Leaf)
        
        # Read original workflow
        $workflowContent = Get-Content -Path $originalWorkflowPath -Raw        # Replace the GitHub NuGet source with our local source
        $workflowContent = $workflowContent -replace "    - name: Add NuGet source\s+run: dotnet nuget add source --name github --username .+ --password .+ .+", @"
    - name: Add Local NuGet Source
      run: dotnet nuget add source --name local $nugetSourceUrl --allow-insecure-connections
"@
          # Replace the Push to NuGet step to use our local server
        $workflowContent = $workflowContent -replace "    - name: Push to NuGet\s+if: success\(\)\s+run: dotnet nuget push [`"].*[`"] --api-key .* --source .*", @"
    - name: Push to Local NuGet Server
      if: success()
      run: dotnet nuget push "./packages/*.nupkg" --api-key $nugetApiKey --source local --skip-duplicate --no-symbols --no-service-endpoint true
"@
        
        # Write to temp file
        Set-Content -Path $tempWorkflowPath -Value $workflowContent
        
        # Update workflow file path
        $workflowFile = $tempWorkflowPath
        
        Write-Host "Using local NuGet server at $nugetSourceUrl with API key: $nugetApiKey" -ForegroundColor Cyan
    }
    else {
        Write-Warning "Failed to start local NuGet server. Falling back to fake NuGet publishing."
    }
}

# Save event to a temporary file
$eventFile = Join-Path $env:TEMP "github-event-$([Guid]::NewGuid().ToString()).json"
$eventJson | Set-Content -Path $eventFile

Write-Host "Testing workflow type '$WorkflowType' with tag '$tagName'..." -ForegroundColor Yellow

# Run act and simulate a tag push event
try {
    $actCommand = "act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=$nugetApiKey --container-architecture linux/amd64"
    
    Write-Host "Act command: $actCommand" -ForegroundColor Cyan
    
    # Run act with --dryrun to see what will be executed
    Invoke-Expression "$actCommand --dryrun"
    
    # $confirmation = Read-Host "Do you want to proceed with the actual execution? [y/N]"
    # if ($confirmation -eq 'Y' -or $confirmation -eq 'y') {
    #     # Actually run act
    #     Invoke-Expression $actCommand
    # }
    # else {
    #     Write-Host "Execution cancelled." -ForegroundColor Yellow
    # }

    Invoke-Expression $actCommand
}
catch {
    Write-Error "Error during act execution: $_"
}
finally {
    # Clean up resources
    if (Test-Path $eventFile) {
        Remove-Item -Path $eventFile -Force
    }
    
    # Cleanup temp workflow file if created
    if ($UseLocalNuget -and $localNugetRunning) {
        $tempFolder = Join-Path $env:TEMP "forma-workflow-temp"
        if (Test-Path -Path $tempFolder) {
            Remove-Item -Path $tempFolder -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        # Ask if user wants to stop the NuGet server
        $stopServer = Read-Host "Do you want to stop the local NuGet server? [y/N]"
        if ($stopServer -eq 'Y' -or $stopServer -eq 'y') {
            Stop-LocalNugetServer -ContainerName $NugetContainerName
        }
        else {
            Write-Host "NuGet server is still running at http://localhost:$NugetServerPort" -ForegroundColor Green
            Write-Host "You can manage the container manually with 'docker stop/start/rm $NugetContainerName'" -ForegroundColor Yellow
        }
    }
}
