# Forma Release Guide

This comprehensive document describes the release strategy for Forma libraries and provides instructions on how to perform new releases using the automated GitHub Actions workflows.

## Table of Contents

1. [Release Strategy](#release-strategy)
2. [Package Structure](#package-structure)
3. [Branch Strategy](#branch-strategy)
4. [Versioning](#versioning)
5. [Release Methods](#release-methods)
6. [GitHub Actions Workflows](#github-actions-workflows)
7. [Manual Release Workflows](#manual-release-workflows)
8. [Automated Git Tags and Releases](#automated-git-tags-and-releases)
9. [Change Detection](#change-detection)
10. [Testing Releases Locally](#testing-releases-locally)
11. [Quick Reference](#quick-reference)
12. [Troubleshooting](#troubleshooting)

## Release Strategy

Forma adopts a **hybrid approach** for releasing its NuGet packages:

### 1. Synchronized Release (Core)

The fundamental components of the Forma ecosystem are released together with the same version:

- `Forma.Core` - Foundation library with core abstractions
- `Forma.Mediator` - Mediator pattern implementation
- `Forma.Decorator` - Decorator pattern utilities

This ensures consistency and compatibility between the base libraries, which often evolve together.

### 2. Independent Release (Components)

More specialized components can be released independently:

- `Forma.Chains` - Chain of responsibility implementation
- `Forma.PubSub.InMemory` - In-memory publish-subscribe implementation
- Other future components...

This allows releasing new versions only when necessary, without forcing updates across the entire ecosystem.

## Package Structure

### Package Dependencies

```
Forma.Core (base)
├── Forma.Mediator (depends on Core)
├── Forma.Decorator (depends on Core)
├── Forma.Chains (depends on Core)
└── Forma.PubSub.InMemory (depends on Core)
```

## Branch Strategy

The project implements a **branch-based release strategy** with automatic versioning through Nerdbank.GitVersioning:

### Release Branches
- **Pattern**: `release_vX.Y` (e.g., `release_v1.0`, `release_v2.1`)
- **Purpose**: Stable releases
- **Versioning**: Produces clean versions like `1.0.0`, `1.0.1`, `2.1.0`
- **Triggers**: Automatic package publishing when changes are detected

### Development Branch
- **Branch**: `develop`
- **Purpose**: Preview releases for testing and early adoption
- **Versioning**: Produces preview versions like `1.0.0-preview.123`
- **Triggers**: Automatic package publishing when changes are detected

### Main Branch
- **Branch**: `main`
- **Purpose**: CI/CD and development only
- **Releases**: No automatic releases (build and test only)

## Versioning

Forma follows [Semantic Versioning](https://semver.org/) conventions:

- **MAJOR**: Incompatible changes with previous versions
- **MINOR**: Backward-compatible new features
- **PATCH**: Backward-compatible bug fixes

### Version Configuration

The `version.json` file configures Nerdbank.GitVersioning:

```json
{
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/release_v\\d+(?:\\.\\d+)*$",
    "^refs/tags/v\\d+(?:\\.\\d+)*(?:\\.\\d+)*$"
  ],
  "branchesConfig": {
    "develop": {
      "prerelease": "preview",
      "versionIncrement": "minor"
    },
    "release_v.*": {
      "prerelease": "",
      "versionIncrement": "patch"
    }
  }
}
```

### Version Examples

| Branch/Tag | Version Output | Notes |
|------------|----------------|-------|
| `release_v1.2` | `1.2.0` | Stable release |
| `develop` | `1.3.0-preview.45` | Preview release |
| `v1.2.3` | `1.2.3` | Tag-based release |
| `v1.2.3-core` | `1.2.3` | Legacy tag (core only) |

## Release Methods

### 1. Automatic Branch-based Releases

#### Creating a Stable Release
```bash
# Create release branch
git checkout -b release_v1.2

# Make changes and push
git add .
git commit -m "Prepare v1.2 release"
git push origin release_v1.2
```

#### Creating a Preview Release
```bash
# Work on develop branch
git checkout develop

# Make changes and push
git add .
git commit -m "Add new feature"
git push origin develop
```

### 2. Manual Workflow Triggers

| Workflow | Purpose | Input Options |
|----------|---------|---------------|
| `release-core.yml` | Core packages | `force-publish: true/false` |
| `release-chains.yml` | Chains package | `force-publish: true/false` |
| `release-pubsub.yml` | PubSub package | `force-publish: true/false` |
| `release-all.yml` | All packages | `packages: "all"` or `"core,chains"` etc.<br>`force-publish: true/false` |

### 3. Legacy Tag-based Releases

Legacy tag support is maintained:
- `v1.2.3` - Releases all packages
- `v1.2.3-core` - Releases core packages only
- `v1.2.3-chains` - Releases chains package only
- `v1.2.3-pubsub` - Releases pubsub package only

## GitHub Actions Workflows

### Workflow Architecture

#### Centralized Reusable Workflow
All package publishing goes through `nuget-publish-reusable.yml` which:
- Handles build, test, pack, and publish steps
- Supports configurable project paths and test filters
- Manages NuGet source configuration
- Uploads artifacts for debugging
- Provides consistent error handling

#### Individual Package Workflows
Each package group has its own workflow:
- `release-core.yml`: Publishes core packages with dependency management
- `release-chains.yml`: Publishes Forma.Chains package
- `release-pubsub.yml`: Publishes Forma.PubSub.InMemory package

### Prerequisites

Before proceeding with a release:

1. Make sure all tests pass
2. Check that documentation is up-to-date
3. Verify that benchmarks have been run if performance changes were made

### Workflow Triggers

#### Automatic Triggers
1. **Push to release branches**: `release_vX.Y`
2. **Push to develop branch**: For preview releases
3. **Git tags**: Legacy support (`v*`, `v*-core`, `v*-chains`, `v*-pubsub`)

#### Manual Triggers
1. **Individual workflows**: Run specific package workflows manually
2. **Bulk workflow**: Use `release-all.yml` for multiple packages
3. **Force publish**: Override change detection when needed

## Manual Release Workflows

### Using GitHub Interface

1. Go to Actions tab in GitHub
2. Select the appropriate workflow:
   - "Release Core Packages" for core components
   - "Release Chains Package" for Forma.Chains
   - "Release PubSub Package" for Forma.PubSub.InMemory
   - "Release All Packages" for multiple packages
3. Click "Run workflow"
4. Configure options:
   - Choose packages to release (for release-all workflow)
   - Enable "force-publish" to override change detection
5. Click "Run workflow"

### Emergency Commands

#### Force Release All Packages
1. Go to Actions → "Release All Packages"
2. Run workflow with:
   - `packages: "all"`
   - `force-publish: true`

#### Release Single Package
1. Go to Actions → Choose specific workflow
2. Run workflow with:
   - `force-publish: true`

## Automated Git Tags and Releases

### Overview

The repository automatically creates Git tags and GitHub releases when executing release actions. This functionality is integrated into the reusable NuGet publish workflow.

### How It Works

#### Workflow Sequence

1. **Build and Test**: Standard .NET build and test processes
2. **NuGet Package Creation**: Creates and publishes NuGet packages
3. **Version Detection**: Uses Nerdbank.GitVersioning to determine the current version
4. **Stable Release Check**: Determines if this is a stable release (no prerelease suffix)
5. **Git Tag Creation**: Creates an annotated Git tag with the version (e.g., `v1.1.0`, `v1.2.0-preview123`)
6. **GitHub Release Creation**: Creates a GitHub release with detailed release notes (marked as pre-release for preview versions)

### Version Types and Behavior

#### Stable Releases
- **Format**: `v1.1.0`, `v1.2.0`, etc. (no prerelease suffix)
- **Triggers**: Manual release workflows or release branch pushes
- **Behavior**: 
  - ✅ Publishes to NuGet
  - ✅ Creates Git tag
  - ✅ Creates GitHub release (standard release)

#### Preview/Prerelease Builds
- **Format**: `v1.1.0-preview123`, `v1.2.0-preview456`, etc.
- **Triggers**: Automatic builds on `develop` branch or manual release workflows with preview versions
- **Behavior**:
  - ✅ Publishes to NuGet
  - ✅ Creates Git tag
  - ✅ Creates GitHub pre-release (marked as pre-release)

### Generated Artifacts

#### Git Tags
- **Format**: `v{major}.{minor}.{patch}` (e.g., `v1.1.0`)
- **Type**: Annotated tags with descriptive messages
- **Message**: `Release {PackageName} v{version}`

#### GitHub Releases
- **Title**: `{PackageName} v{version}`
- **Tag**: Same as Git tag
- **Release Notes**: Include:
  - Package name and version
  - Changes summary
  - Assembly version information
  - Publication timestamp
  - Direct link to NuGet package

## Change Detection

Workflows use intelligent change detection to avoid unnecessary publishes:

### File Change Triggers

| Files Changed | Packages Affected |
|---------------|-------------------|
| `src/Forma.Core/**` | Core, Mediator, Decorator, Chains, PubSub |
| `src/Forma.Mediator/**` | Mediator only |
| `src/Forma.Decorator/**` | Decorator only |
| `src/Forma.Chains/**` | Chains only |
| `src/Forma.PubSub.InMemory/**` | PubSub only |
| `Directory.Build.props` | All packages |
| `version.json` | All packages |

### Detection Features
- **Path-based filtering**: Only relevant file changes trigger builds
- **Dependency awareness**: Core package changes trigger dependent package checks
- **Global configuration**: Changes to `Directory.Build.props` and `version.json` affect all packages

## Testing Releases Locally

Before officially publishing, you can test GitHub Actions workflows locally using the `act` tool.

### Prerequisites

1. **Docker**: Make sure Docker is installed and running on your system
2. **act**: Install the act tool for running GitHub Actions locally

### Installation

#### Windows
```powershell
winget install nektos.act
```

#### macOS/Linux
```bash
# Using Homebrew
brew install act

# Using installation script (Linux)
curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash
```

### Usage

#### PowerShell (Windows)
```powershell
# Test the core release workflow
.\scripts\test-workflow.ps1 -WorkflowType core -Version 2.0.0-test

# Test a specific component release workflow
.\scripts\test-workflow.ps1 -WorkflowType component -Component chains -Version 1.3.0-test
```

#### Bash (Linux/macOS)
```bash
# Make the script executable
chmod +x ./scripts/test-workflow.sh

# Test the core release workflow
./scripts/test-workflow.sh -t core -v 2.0.0-test

# Test a specific component release workflow
./scripts/test-workflow.sh -t component -c chains -v 1.3.0-test
```

### Limitations

- `act` uses Docker images to simulate GitHub Actions environments
- Some aspects of the official GitHub runners may differ from the local environment
- Actions that require access to GitHub APIs may require authentication tokens
- Git operations (tag creation) and GitHub CLI operations (release creation) may not work in the containerized environment

## Quick Reference

### Branch Commands

#### Stable Release
```bash
git checkout -b release_v1.2
# ... make changes ...
git push origin release_v1.2
```

#### Preview Release  
```bash
git checkout develop
# ... make changes ...
git push origin develop
```

### Secrets Setup

1. Go to repository Settings → Secrets and variables → Actions
2. Add secrets:
   - `NUGET_API_KEY` (required)
   - `NUGET_SOURCE` (optional, defaults to nuget.org)

## Troubleshooting

### Common Issues

1. **Package not publishing**: Check change detection filters and ensure relevant files were modified
2. **Version conflicts**: Verify branch naming matches expected patterns
3. **Build failures**: Run `build-test.yml` workflow to identify issues
4. **Missing dependencies**: Ensure Core packages are published before dependent packages
5. **Duplicate Tags**: The workflow automatically detects and skips existing tags
6. **Permission Errors**: Ensure the workflow has `contents: write` permission
7. **Version Detection Failures**: Check Nerdbank.GitVersioning configuration in `version.json`

### Debug Steps

1. Check workflow run logs for detailed error messages
2. Download artifacts to inspect generated packages
3. Verify secrets are configured correctly
4. Test locally with `dotnet pack` to isolate build issues

### Manual Override

If manual tag/release creation is needed:

```bash
# Create tag manually
git tag -a v1.1.0 -m "Manual release v1.1.0"
git push origin v1.1.0

# Create release manually
gh release create v1.1.0 --title "Manual Release v1.1.0" --notes "Manual release notes"
```

## Security

### Required Secrets
- `NUGET_API_KEY`: NuGet.org API key for publishing packages
- `NUGET_SOURCE`: (Optional) Custom NuGet source URL

### Best Practices
- API keys are scoped to the minimum required permissions
- Secrets are never logged or exposed in workflows
- Package uploads use `--skip-duplicate` to prevent conflicts

### Required Permissions

The workflow requires the following permissions:
- `contents: write` - For creating Git tags and releases
- `packages: write` - For publishing NuGet packages

## Compatibility Notes

- Core component releases should be planned together to ensure compatibility
- Independent components can have dependencies on specific versions of core components
- If an independent component needs a new core feature, a new version of the core components should be released first

> **Note**: The workflows are configured to use the `UseProjectReferences=false` property when creating packages. This ensures that each NuGet package references other Forma packages correctly rather than using project references, which is essential for published packages to work properly. For more details on this approach, see the [Project vs NuGet References guide](./project-vs-nuget-references.md).
