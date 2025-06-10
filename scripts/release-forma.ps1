#!/usr/bin/env pwsh

# release-forma.ps1 - Uno script per gestire i rilasci ibridi di Forma

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("core", "chains", "pubsub")]
    [string]$Component,

    [Parameter(Mandatory=$true)]
    [string]$Version
)

# Verifica che git sia disponibile
if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) {
    Write-Error "Git non è disponibile nel PATH. Per favore, installa git."
    exit 1
}

# Verifica che non ci siano modifiche non committate
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error "Ci sono modifiche non committate nel repository. Commita o stasha le tue modifiche prima di rilasciare."
    Write-Output $gitStatus
    exit 1
}

# Genera il tag appropriato basato sul componente
$tag = "v$Version-$Component"

# Funzione per verificare che il tag non esista già
function Test-TagExists {
    param (
        [string]$Tag
    )
    
    $existingTags = git tag -l
    return $existingTags -contains $Tag
}

# Verifica se il tag esiste già
if (Test-TagExists -Tag $tag) {
    Write-Error "Il tag '$tag' esiste già. Per favore, usa una versione diversa."
    exit 1
}

# Processo di rilascio per componente "core"
if ($Component -eq "core") {
    Write-Output "Rilascio del Core con versione $Version..."

    # Aggiorna il file Directory.Build.props
    $buildPropsPath = "Directory.Build.props"
    $buildPropsContent = Get-Content $buildPropsPath
    $buildPropsContent = $buildPropsContent -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    Set-Content $buildPropsPath $buildPropsContent
    
    # Commita l'aggiornamento della versione
    git add $buildPropsPath
    git commit -m "Bump version to $Version for Core release"
    
    # Crea e pusha il tag
    git tag $tag
    git push origin $tag
    git push

    Write-Output "Tag '$tag' creato e pushato. Il workflow GitHub Actions dovrebbe attivarsi a breve."
}
# Processo di rilascio per componenti specifici (chains, pubsub, ecc.)
else {
    Write-Output "Rilascio del componente $Component con versione $Version..."

    # Determina il percorso del file csproj in base al componente
    $csprojPath = ""
    switch ($Component) {
        "chains" { $csprojPath = "src\Forma.Chains\Forma.Chains.csproj" }
        "pubsub" { $csprojPath = "src\Forma.PubSub.InMemory\Forma.PubSub.InMemory.csproj" }
    }

    # Aggiorna la versione nel file .csproj specifico
    $csprojContent = Get-Content $csprojPath
    if ($csprojContent -match '<Version>.*</Version>') {
        $csprojContent = $csprojContent -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    } else {
        # Se non esiste un tag Version, lo aggiungiamo dopo PropertyGroup
        $csprojContent = $csprojContent -replace '<PropertyGroup>', "<PropertyGroup>`n    <Version>$Version</Version>"
    }
    Set-Content $csprojPath $csprojContent
    
    # Commita l'aggiornamento della versione
    git add $csprojPath
    git commit -m "Bump version to $Version for $Component release"
    
    # Crea e pusha il tag
    git tag $tag
    git push origin $tag
    git push

    Write-Output "Tag '$tag' creato e pushato. Il workflow GitHub Actions dovrebbe attivarsi a breve."
}

Write-Output "Processo di rilascio completato con successo."
