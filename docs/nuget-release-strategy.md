# NuGet Release Strategy

This document outlines the new optimized GitHub Actions workflow strategy for NuGet package releases in the Forma project.

## Overview

The new workflow system implements a branch-based release strategy with centralized, reusable workflows that handle versioning automatically through Nerdbank.GitVersioning.

## Branch Strategy

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

## Package Structure

The project is organized into the following NuGet packages:

### Core Packages
- **Forma.Core**: Foundation library with core abstractions
- **Forma.Mediator**: Mediator pattern implementation
- **Forma.Decorator**: Decorator pattern utilities

### Component Packages
- **Forma.Chains**: Chain of responsibility implementation
- **Forma.PubSub.InMemory**: In-memory publish-subscribe implementation

## Workflow Architecture

### Centralized Reusable Workflow
All package publishing goes through `nuget-publish-reusable.yml` which:
- Handles build, test, pack, and publish steps
- Supports configurable project paths and test filters
- Manages NuGet source configuration
- Uploads artifacts for debugging
- Provides consistent error handling

### Individual Package Workflows
Each package group has its own workflow:
- `release-core.yml`: Publishes core packages with dependency management
- `release-chains.yml`: Publishes Forma.Chains package
- `release-pubsub.yml`: Publishes Forma.PubSub.InMemory package

### Manual Release Workflow
`release-all.yml` provides manual control for:
- Selective package publishing
- Force publishing without change detection
- Bulk releases across all packages

## Change Detection

Workflows use intelligent change detection to avoid unnecessary publishes:

- **Path-based filtering**: Only relevant file changes trigger builds
- **Dependency awareness**: Core package changes trigger dependent package checks
- **Global configuration**: Changes to `Directory.Build.props` and `version.json` affect all packages

## Triggers

### Automatic Triggers
1. **Push to release branches**: `release_vX.Y`
2. **Push to develop branch**: For preview releases
3. **Git tags**: Legacy support (`v*`, `v*-core`, `v*-chains`, `v*-pubsub`)

### Manual Triggers
1. **Individual workflows**: Run specific package workflows manually
2. **Bulk workflow**: Use `release-all.yml` for multiple packages
3. **Force publish**: Override change detection when needed

## Usage Examples

### Creating a Stable Release
```bash
# Create release branch
git checkout -b release_v1.2

# Make changes and push
git add .
git commit -m "Prepare v1.2 release"
git push origin release_v1.2
```

### Creating a Preview Release
```bash
# Work on develop branch
git checkout develop

# Make changes and push
git add .
git commit -m "Add new feature"
git push origin develop
```

### Manual Release
1. Go to Actions tab in GitHub
2. Select "Release All Packages" workflow
3. Click "Run workflow"
4. Choose packages to release (or "all")
5. Optionally enable "force-publish"

## Version Configuration

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

## Security

### Required Secrets
- `NUGET_API_KEY`: NuGet.org API key for publishing packages
- `NUGET_SOURCE`: (Optional) Custom NuGet source URL

### Best Practices
- API keys are scoped to the minimum required permissions
- Secrets are never logged or exposed in workflows
- Package uploads use `--skip-duplicate` to prevent conflicts

## Monitoring and Debugging

### Artifacts
- All workflows upload NuGet packages as artifacts
- Artifacts are retained for 7 days for debugging
- Build logs provide detailed information about versioning and publishing

### Status Reporting
- Workflow summaries show which packages were published
- Change detection results are displayed in job outputs
- Failed jobs provide clear error messages and suggestions

## Migration from Legacy System

### Backward Compatibility
- Legacy tag formats are still supported (`v*-core`, `v*-chains`, `v*-pubsub`)
- Existing project structure requires no changes
- `Directory.Build.props` configuration is preserved

### Deprecated Workflows
- `nuget-deploy.yml` → Replaced by `release-core.yml`
- `nuget-component-deploy.yml` → Replaced by `release-chains.yml` and `release-pubsub.yml`

## Troubleshooting

### Common Issues

1. **Package not publishing**: Check change detection filters and ensure relevant files were modified
2. **Version conflicts**: Verify branch naming matches expected patterns
3. **Build failures**: Run `build-test.yml` workflow to identify issues
4. **Missing dependencies**: Ensure Core packages are published before dependent packages

### Debug Steps

1. Check workflow run logs for detailed error messages
2. Download artifacts to inspect generated packages
3. Verify secrets are configured correctly
4. Test locally with `dotnet pack` to isolate build issues

## Future Enhancements

### Planned Improvements
- Integration with GitHub Releases for changelog generation
- Automated testing in multiple environments before publishing
- Package validation and quality gates
- Performance benchmarking integration with releases

### Customization Options
- Add custom test filters for specific packages
- Configure different NuGet sources per package
- Implement approval workflows for production releases
- Add notification integrations (Slack, Teams, etc.)