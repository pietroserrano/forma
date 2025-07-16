# Modifiche Consigliate ai Workflow GitHub Actions

Per integrare meglio Nerdbank.GitVersioning con i workflow e gestire la concorrenza, dovresti apportare le seguenti modifiche ai file:

## 1. File `nuget-deploy.yml` (Core Components)

```yaml
name: NuGet Core Package Deploy

on:
  push:
    branches:
      - 'v*'
      - 'release/v*'
    tags:
      - 'v*-core'
      - 'v*'
  workflow_dispatch:
    inputs:
      releaseType:
        description: 'Release type (preview/stable)'
        required: true
        default: 'preview'
        type: choice
        options:
          - preview
          - stable

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.nbgv.outputs.SimpleVersion }}
      previewVersion: ${{ steps.nbgv.outputs.NuGetPackageVersion }}
    
    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    # Determina versione usando Nerdbank.GitVersioning
    - name: Set version with Nerdbank.GitVersioning
      id: nbgv
      uses: dotnet/nbgv@master
      with:
        setAllVars: true
        
    - name: Set release type
      id: release_type
      run: |
        if [[ "${{ github.ref }}" == refs/tags/* ]]; then
          echo "RELEASE_TYPE=stable" >> $GITHUB_OUTPUT
        elif [[ "${{ github.ref }}" == refs/heads/v* ]]; then
          echo "RELEASE_TYPE=stable" >> $GITHUB_OUTPUT
        elif [[ "${{ github.ref }}" == refs/heads/release/* ]]; then
          echo "RELEASE_TYPE=preview" >> $GITHUB_OUTPUT
        else
          echo "RELEASE_TYPE=${{ github.event.inputs.releaseType || 'preview' }}" >> $GITHUB_OUTPUT
        fi
        
    - name: Display version info
      run: |
        echo "Building Version: ${{ steps.nbgv.outputs.NuGetPackageVersion }}"
        echo "Release Type: ${{ steps.release_type.outputs.RELEASE_TYPE }}"
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Test
      run: dotnet test --configuration Release --no-build
      
    - name: Pack Core
      run: dotnet pack src/Forma.Core/Forma.Core.csproj -c Release -o packages -p:UseProjectReferences=false
      
    - name: Pack Mediator
      run: dotnet pack src/Forma.Mediator/Forma.Mediator.csproj -c Release -o packages -p:UseProjectReferences=false
      
    - name: Pack Decorator
      run: dotnet pack src/Forma.Decorator/Forma.Decorator.csproj -c Release -o packages -p:UseProjectReferences=false
    
    - name: Add NuGet Source
      run: dotnet nuget add source ${{ secrets.NUGET_SOURCE }} --name nuget-org
      
    - name: Push to NuGet
      if: success()
      run: dotnet nuget push "./packages/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source nuget-org --skip-duplicate
```

## 2. File `nuget-component-deploy.yml` (Component Packages)

```yaml
name: NuGet Component Package Deploy

on:
  push:
    tags:
      - 'v*-chains'
      - 'v*-pubsub'
  workflow_dispatch:
    inputs:
      component:
        description: 'Component to deploy'
        required: true
        type: choice
        options:
          - chains
          - pubsub
      releaseType:
        description: 'Release type (preview/stable)'
        required: true
        default: 'preview'
        type: choice
        options:
          - preview
          - stable

jobs:
  # Verifica che i pacchetti core siano disponibili prima di procedere
  check-core-packages:
    runs-on: ubuntu-latest
    steps:
      - name: Check Core Packages Availability
        run: |
          CORE_VERSION=$(curl -s "https://api.nuget.org/v3-flatcontainer/forma.core/index.json" | jq -r '.versions[-1]')
          MEDIATOR_VERSION=$(curl -s "https://api.nuget.org/v3-flatcontainer/forma.mediator/index.json" | jq -r '.versions[-1]')
          DECORATOR_VERSION=$(curl -s "https://api.nuget.org/v3-flatcontainer/forma.decorator/index.json" | jq -r '.versions[-1]')
          
          echo "Core: $CORE_VERSION, Mediator: $MEDIATOR_VERSION, Decorator: $DECORATOR_VERSION"
          
          if [ -z "$CORE_VERSION" ] || [ -z "$MEDIATOR_VERSION" ] || [ -z "$DECORATOR_VERSION" ]; then
            echo "One or more core packages are missing on NuGet"
            exit 1
          fi

  deploy:
    runs-on: ubuntu-latest
    needs: [check-core-packages]
    
    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Set version with Nerdbank.GitVersioning
      id: nbgv
      uses: dotnet/nbgv@master
      with:
        setAllVars: true
        
    - name: Determine component
      id: get_info
      run: |
        if [[ "${{ github.ref }}" == refs/tags/* ]]; then
          REF="${GITHUB_REF#refs/tags/v}"
          COMPONENT=$(echo $REF | cut -d'-' -f2)
        else
          COMPONENT=${{ github.event.inputs.component }}
        fi
        echo "COMPONENT=$COMPONENT" >> $GITHUB_OUTPUT
        
    - name: Set release type
      id: release_type
      run: |
        if [[ "${{ github.ref }}" == refs/tags/* ]]; then
          echo "RELEASE_TYPE=stable" >> $GITHUB_OUTPUT
        else
          echo "RELEASE_TYPE=${{ github.event.inputs.releaseType || 'preview' }}" >> $GITHUB_OUTPUT
        fi
        
    - name: Display version info
      run: |
        echo "Building Component: ${{ steps.get_info.outputs.COMPONENT }}"
        echo "Version: ${{ steps.nbgv.outputs.NuGetPackageVersion }}"
        echo "Release Type: ${{ steps.release_type.outputs.RELEASE_TYPE }}"
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Test specific component
      if: steps.get_info.outputs.COMPONENT == 'chains'
      run: |
        dotnet test tests/Forma.Tests/Forma.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Forma.Tests.Chains"
        
    - name: Test specific component
      if: steps.get_info.outputs.COMPONENT == 'pubsub'
      run: |
        dotnet test tests/Forma.Tests/Forma.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Forma.Tests.PubSub"
        
    - name: Pack Chains
      if: steps.get_info.outputs.COMPONENT == 'chains'
      run: dotnet pack src/Forma.Chains/Forma.Chains.csproj -c Release -o packages -p:UseProjectReferences=false
      
    - name: Pack PubSub InMemory
      if: steps.get_info.outputs.COMPONENT == 'pubsub'
      run: dotnet pack src/Forma.PubSub.InMemory/Forma.PubSub.InMemory.csproj -c Release -o packages -p:UseProjectReferences=false
      
    - name: Add NuGet Source
      run: dotnet nuget add source ${{ secrets.NUGET_SOURCE }} --name nuget-org
      
    - name: Push to NuGet
      if: success()
      run: dotnet nuget push "./packages/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source nuget-org --skip-duplicate
```

## 3. Aggiornamento alla Documentazione nel release-guide.md

Aggiungi una sezione sulla gestione automatizzata delle versioni:

```markdown
### Gestione delle Versioni con Nerdbank.GitVersioning e GitHub Actions

Con l'integrazione di Nerdbank.GitVersioning nei workflow di GitHub Actions:

1. **Trigger dei Workflow**:
   - I workflow per i componenti core si attivano automaticamente su:
     - Push ai branch `v*` (es. v1.0) → rilasci stabili
     - Push ai branch `release/v*` (es. release/v1.0) → rilasci preview
     - Push di tag `v*` o `v*-core`
     - Attivazione manuale tramite workflow_dispatch
   
   - I workflow per i componenti si attivano su:
     - Push di tag specifici (es. v1.0.0-chains)
     - Attivazione manuale specificando il componente

2. **Concorrenza e Dipendenze**:
   - Il workflow dei componenti verifica che i pacchetti core siano disponibili prima di procedere
   - Questo garantisce che i componenti utilizzino le dipendenze core appropriate

3. **Versionamento Automatico**:
   - Nerdbank.GitVersioning determina automaticamente la versione in base al branch/tag
   - Le versioni preview hanno il suffisso "-preview"
   - Le versioni stabili non hanno suffissi
```
