#!/usr/bin/env pwsh

# test-workflow.ps1 - Script per testare GitHub Actions localmente con act

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("core", "component")]
    [string]$WorkflowType,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0-test",
    
    [Parameter(Mandatory=$false)]
    [string]$Component = "chains"
)

# Verifica che Docker sia in esecuzione
try {
    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker non è in esecuzione. Avvia Docker e riprova."
        exit 1
    }
}
catch {
    Write-Error "Impossibile connettersi a Docker. Assicurati che Docker sia installato e in esecuzione."
    exit 1
}

# Verifica che act sia installato
if (!(Get-Command act -ErrorAction SilentlyContinue)) {
    Write-Error "act non è installato. Installa act con 'winget install nektos.act' o segui le istruzioni in scripts/install-act.md"
    exit 1
}

# Determina quale workflow testare e imposta i parametri
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

# Salva l'evento in un file temporaneo
$eventFile = Join-Path $env:TEMP "github-event-$([Guid]::NewGuid().ToString()).json"
$eventJson | Set-Content -Path $eventFile

Write-Host "Testando il workflow di tipo '$WorkflowType' con tag '$tagName'..." -ForegroundColor Yellow

# Esegui act e simula un evento di push di tag
try {
    Write-Host "Comando act: act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64" -ForegroundColor Cyan
    
    # Esegui act con --dry-run per vedere prima cosa verrà eseguito
    act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64 --dry-run
    
    $confirmation = Read-Host "Vuoi procedere con l'esecuzione effettiva? [s/N]"
    if ($confirmation -eq 'S' -or $confirmation -eq 's') {
        # Esegui act davvero
        act push --eventpath $eventFile -W $workflowFile --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64
    }
    else {
        Write-Host "Esecuzione annullata." -ForegroundColor Yellow
    }
}
catch {
    Write-Error "Errore durante l'esecuzione di act: $_"
}
finally {
    # Rimuovi il file temporaneo
    if (Test-Path $eventFile) {
        Remove-Item -Path $eventFile -Force
    }
}
