# Forma Release Guide

This document describes the release strategy for Forma libraries and provides instructions on how to perform new releases.

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
