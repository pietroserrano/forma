# GitHub Actions Workflows

This directory contains the GitHub Actions workflows for the Forma project, designed for optimized NuGet package releases using Nerdbank.GitVersioning.

## Workflow Overview

### Branch-based Release Strategy

- **`release_vX.Y` branches**: Stable version releases (no preview suffix)
- **`develop` branch**: Preview version releases (with `-preview` suffix)
- **`main` branch**: Not used for releases (CI/CD only)
- **Git tags**: Manual release triggers (supports legacy tags)

### Workflow Files

#### Core Workflows

1. **`nuget-publish-reusable.yml`** - Centralized reusable workflow for NuGet publishing
   - Handles build, test, pack, and publish steps
   - Configurable project paths and test filters
   - Uploads artifacts for debugging

2. **`build-test.yml`** - Continuous integration workflow
   - Runs on push/PR to main, develop, and release branches
   - Builds and tests all projects
   - Manual trigger available

3. **`benchmarks.yml`** - Performance benchmarking
   - Weekly scheduled runs
   - Manual trigger available
   - Uploads benchmark results as artifacts

#### Release Workflows

4. **`release-core.yml`** - Releases core packages (Forma.Core, Forma.Mediator, Forma.Decorator)
   - Automatic change detection
   - Sequential dependency-aware publishing
   - Manual trigger with force-publish option

5. **`release-chains.yml`** - Releases Forma.Chains package
   - Change detection for chains-specific files
   - Manual trigger with force-publish option

6. **`release-pubsub.yml`** - Releases Forma.PubSub.InMemory package
   - Change detection for pubsub-specific files
   - Manual trigger with force-publish option

7. **`release-all.yml`** - Manual release workflow for all packages
   - Selective package publishing (individual or all)
   - Force-publish option
   - Comprehensive release summary

#### Legacy Files (Backup)

- `nuget-deploy.yml.old` - Legacy core package deploy workflow
- `nuget-component-deploy.yml.old` - Legacy component package deploy workflow

## Usage

### Automatic Releases

1. **Create a release branch**: `git checkout -b release_v1.2`
2. **Push changes**: Changes are automatically detected and relevant packages are published
3. **Develop branch**: Push to develop for preview releases

### Manual Releases

1. **Individual packages**: Use `release-core.yml`, `release-chains.yml`, or `release-pubsub.yml`
2. **Multiple packages**: Use `release-all.yml` with package selection
3. **Force publish**: Use the force-publish option to override change detection

### Tag-based Releases

Legacy tag support is maintained:
- `v1.2.3` - Releases all packages
- `v1.2.3-core` - Releases core packages only
- `v1.2.3-chains` - Releases chains package only
- `v1.2.3-pubsub` - Releases pubsub package only

## Secrets Required

- `NUGET_API_KEY` - NuGet.org API key for publishing
- `NUGET_SOURCE` - (Optional) Custom NuGet source URL

## Change Detection

The workflows use path filters to detect changes:

- **Core packages**: Monitor `src/Forma.Core/**`, `src/Forma.Mediator/**`, `src/Forma.Decorator/**`
- **Chains package**: Monitor `src/Forma.Chains/**`
- **PubSub package**: Monitor `src/Forma.PubSub.InMemory/**`
- **Global**: Monitor `Directory.Build.props`, `version.json`

## Version Management

Versions are automatically managed by Nerdbank.GitVersioning:

- **Stable releases**: `release_vX.Y` branches produce `X.Y.Z` versions
- **Preview releases**: `develop` branch produces `X.Y.Z-preview.N` versions
- **Build metadata**: Includes commit information for traceability

## Workflow Dependencies

```
nuget-publish-reusable.yml (base workflow)
├── release-core.yml (uses reusable)
├── release-chains.yml (uses reusable)
├── release-pubsub.yml (uses reusable)
└── release-all.yml (uses reusable)
```

## Troubleshooting

- **Failed builds**: Check the build-test workflow first
- **Version conflicts**: Verify Nerdbank.GitVersioning configuration in `version.json`
- **Missing secrets**: Ensure `NUGET_API_KEY` is configured in repository settings
- **Change detection**: Review path filters if packages aren't being published when expected