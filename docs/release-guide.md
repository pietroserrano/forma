# Forma Release Guide

This document describes the release strategy for Forma libraries and provides instructions on how to perform new releases.

## Quick Start: Releasing with Git Workflow

### Releasing Preview Versions

1. **Create a release branch**:
   ```powershell
   git checkout main
   git pull
   git checkout -b release/v1.0
   ```

2. **Verify the preview version**:
   ```powershell
   dotnet build /p:GetBuildVersion=true
   # Or, for more detailed version info:
   dotnet tool install -g nbgv
   nbgv get-version
   ```
   With our Nerdbank.GitVersioning configuration, this should produce a version like `1.0.0-preview`.

3. **Make necessary adjustments** to prepare for release (documentation updates, etc.)

4. **Commit your changes**:
   ```powershell
   git commit -a -m "Prepare for 1.0.0-preview release"
   ```

5. **Push the branch** to trigger the CI/CD pipeline:
   ```powershell
   git push -u origin release/v1.0
   ```

6. **For subsequent preview releases** (e.g., preview.1, preview.2):
   - Make additional changes to the release branch
   - Commit and push the changes
   - The version number will increment automatically (e.g., 1.0.0-preview.1, 1.0.0-preview.2)

### Promoting to Stable Release

When you're ready to promote a preview to a stable release:

1. **Create a version branch** matching the pattern in `publicReleaseRefSpec`:
   ```powershell
   git checkout release/v1.0
   git checkout -b v1.0
   git push -u origin v1.0
   ```

2. **Verify the stable version**:
   ```powershell
   dotnet build /p:GetBuildVersion=true
   # Or: nbgv get-version
   ```
   This should now produce a clean version like `1.0.0` (without the preview tag).

3. **Merge into main** (optional, but recommended):
   ```powershell
   git checkout main
   git merge v1.0
   git push
   ```

4. **Create a release tag** (optional, for better tracking):
   ```powershell
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

## Release Strategies

Forma adopts a **hybrid approach** for releasing its NuGet packages:

### 1. Synchronized Release (Core)

The fundamental components of the Forma ecosystem are released together with the same version:

- `Forma.Core`
- `Forma.Mediator`
- `Forma.Decorator`

This ensures consistency and compatibility between the base libraries, which often evolve together.

### 2. Independent Release (Components)

More specialized components can be released independently:

- `Forma.Chains`
- `Forma.PubSub.InMemory`
- Other future components...

This allows releasing new versions only when necessary, without forcing updates across the entire ecosystem.

## Versioning

Forma follows [Semantic Versioning](https://semver.org/) conventions:

- **MAJOR**: Incompatible changes with previous versions
- **MINOR**: Backward-compatible new features
- **PATCH**: Backward-compatible bug fixes

## Release Process

### Prerequisites

Before proceeding with a release:

1. Make sure all tests pass
2. Check that documentation is up-to-date
3. Verify that benchmarks have been run if performance changes were made

### Using the Release Scripts

We've created scripts that facilitate the release process:

#### PowerShell (Windows)

```powershell
# Release core components
.\scripts\release-forma.ps1 -Component core -Version 2.0.0

# Release a specific component
.\scripts\release-forma.ps1 -Component chains -Version 1.3.1
.\scripts\release-forma.ps1 -Component pubsub -Version 1.0.5
```

#### Bash (Linux/macOS)

```bash
# Make the script executable
chmod +x ./scripts/release-forma.sh

# Release core components
./scripts/release-forma.sh -c core -v 2.0.0

# Release a specific component
./scripts/release-forma.sh -c chains -v 1.3.1
./scripts/release-forma.sh -c pubsub -v 1.0.5
```

### What the Release Scripts Do

1. Check that there are no uncommitted changes
2. Update the version in the appropriate files:
   - For core releases: update the version in `Directory.Build.props`
   - For component releases: update the version in the specific `.csproj` file
3. Commit the changes and create a Git tag in the format:
   - For core releases: `v2.0.0-core`
   - For component releases: `v1.3.1-chains` or `v1.0.5-pubsub`
4. Push the commits and tags to GitHub

### GitHub Actions Workflows

Pushing tags automatically triggers GitHub Actions workflows that:

1. Build the projects
2. Run the tests
3. Package the projects as NuGet packages with `-p:UseProjectReferences=false` to use package references instead of project references
4. Publish the packages to NuGet.org

> **Note**: The workflows are configured to use the `UseProjectReferences=false` property when creating packages. This ensures that each NuGet package references other Forma packages correctly rather than using project references, which is essential for published packages to work properly. For more details on this approach, see the [Project vs NuGet References guide](./riferimenti-progetti-vs-nuget.md).

## Testing Releases Locally

Before officially publishing, you can test GitHub Actions workflows locally using the `act` tool. For more details, see the [GitHub Actions testing documentation](./testing-github-actions.md).

## Compatibility Notes

- Core component releases should be planned together to ensure compatibility
- Independent components can have dependencies on specific versions of core components
- If an independent component needs a new core feature, a new version of the core components should be released first

## Gestione delle Versioni con Nerdbank.GitVersioning e GitHub Actions

Con l'integrazione di Nerdbank.GitVersioning nei workflow di GitHub Actions:

### Trigger dei Workflow

- **Workflow per i componenti core** si attivano automaticamente su:
  - Push ai branch `v*` (es. v1.0) → rilasci stabili
  - Push ai branch `release/v*` (es. release/v1.0) → rilasci preview
  - Attivazione manuale tramite workflow_dispatch

- **Workflow per i componenti specifici** si attivano solo tramite:
  - Attivazione manuale (workflow_dispatch) specificando il componente e il tipo di rilascio

### Concorrenza e Dipendenze

- Il workflow dei componenti verifica che i pacchetti core siano disponibili prima di procedere
- Questo garantisce che i componenti utilizzino le dipendenze core appropriate

### Versionamento Automatico

- Nerdbank.GitVersioning determina automaticamente la versione in base al branch/tag:
  - Branch `release/v*` → versioni con suffisso "-preview"
  - Branch `v*` → versioni stabili senza suffissi
  - La numerazione incrementale (es. preview.1, preview.2) viene gestita automaticamente

### Come Verificare la Versione Corrente

Per verificare quale versione verrà generata dal tuo branch corrente:

```powershell
# Installa lo strumento Nerdbank.GitVersioning
dotnet tool install -g nbgv

# Visualizza le informazioni di versione
nbgv get-version
```
