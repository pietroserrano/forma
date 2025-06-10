#!/usr/bin/env pwsh

# test-workflow.ps1 - Script for testing GitHub Actions locally with act

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("core", "component")]
    [string]$WorkflowType,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0-test",
    
    [Parameter(Mandatory=$false)]
    [string]$Component = "chains"
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

# Save event to a temporary file
$eventFile = Join-Path $env:TEMP "github-event-$([Guid]::NewGuid().ToString()).json"
$eventJson | Set-Content -Path $eventFile

Write-Host "Testing workflow type '$WorkflowType' with tag '$tagName'..." -ForegroundColor Yellow

# Run act and simulate a tag push event
try {
    Write-Host "Act command: act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64" -ForegroundColor Cyan
    
    # Run act with --dryrun to see what will be executed
    act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64 --dryrun
    
    $confirmation = Read-Host "Do you want to proceed with the actual execution? [y/N]"
    if ($confirmation -eq 'Y' -or $confirmation -eq 'y') {
        # Actually run act
        act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64
    }
    else {
        Write-Host "Execution cancelled." -ForegroundColor Yellow
    }
}
catch {
    Write-Error "Error during act execution: $_"
}
finally {
    # Remove temporary file
    if (Test-Path $eventFile) {
        Remove-Item -Path $eventFile -Force
    }
}
