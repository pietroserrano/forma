# Automated Git Tag and GitHub Release Documentation

## Overview

The Forma repository now automatically creates Git tags and GitHub releases when executing release actions. This functionality has been added to the reusable NuGet publish workflow (`nuget-publish-reusable.yml`).

## How It Works

### Workflow Sequence

1. **Build and Test**: Standard .NET build and test processes
2. **NuGet Package Creation**: Creates and publishes NuGet packages
3. **Version Detection**: Uses Nerdbank.GitVersioning to determine the current version
4. **Stable Release Check**: Determines if this is a stable release (no prerelease suffix)
5. **Git Tag Creation**: Creates an annotated Git tag with the version (e.g., `v1.1.0`) - **only for stable releases**
6. **GitHub Release Creation**: Creates a GitHub release with detailed release notes - **only for stable releases**

### Automatic Features

- **Stable Release Detection**: Only creates tags and releases for stable versions (no prerelease suffix)
- **Prerelease Handling**: Preview/prerelease versions only publish to NuGet, skip tag/release creation
- **Duplicate Prevention**: Checks for existing tags/releases before creating new ones
- **Version Consistency**: Uses the same version from Nerdbank.GitVersioning for all components
- **Rich Release Notes**: Includes package information, version details, and NuGet links
- **Error Handling**: Only creates tags/releases on successful package publishing

## Usage

### Individual Package Release

To release a single package with automatic tag/release creation:

```bash
# Trigger the individual release workflow (creates tags/releases for stable versions)
gh workflow run release-core.yml
```

### Multiple Package Release

To release multiple packages with automatic tag/release creation:

```bash
# Release all packages (creates tags/releases for stable versions)
gh workflow run release-all.yml -f packages=all

# Release specific packages (creates tags/releases for stable versions)
gh workflow run release-all.yml -f packages=core,mediator
```

### Development Builds

Development builds on the `develop` branch automatically publish preview packages to NuGet but **do not** create Git tags or GitHub releases:

```bash
# These only create NuGet packages with preview suffixes (e.g., v1.1.0-preview123)
git push origin develop
```

## Version Types and Behavior

### Stable Releases
- **Format**: `v1.1.0`, `v1.2.0`, etc. (no prerelease suffix)
- **Triggers**: Manual release workflows (`release-*.yml`)
- **Behavior**: 
  - ✅ Publishes to NuGet
  - ✅ Creates Git tag
  - ✅ Creates GitHub release

### Preview/Prerelease Builds
- **Format**: `v1.1.0-preview123`, `v1.2.0-preview456`, etc.
- **Triggers**: Automatic builds on `develop` branch
- **Behavior**:
  - ✅ Publishes to NuGet
  - ❌ Does not create Git tag
  - ❌ Does not create GitHub release

This design ensures that only stable, production-ready releases create permanent Git tags and GitHub releases, while preview builds are available via NuGet for testing.

## Generated Artifacts

### Git Tags

- **Format**: `v{major}.{minor}.{patch}` (e.g., `v1.1.0`)
- **Type**: Annotated tags with descriptive messages
- **Message**: `Release {PackageName} v{version}`

### GitHub Releases

- **Title**: `{PackageName} v{version}`
- **Tag**: Same as Git tag
- **Release Notes**: Include:
  - Package name and version
  - Changes summary
  - Assembly version information
  - Publication timestamp
  - Direct link to NuGet package

### Example Release Notes

```markdown
## Forma.Core v1.1.0

This release contains the following package:
- **Forma.Core** version v1.1.0

### Changes
- Package published to NuGet
- Automated release created by GitHub Actions

### Package Information
- **Project**: Forma.Core
- **Version**: v1.1.0
- **Assembly Version**: 1.1.0.0
- **Published**: 2025-01-21 09:17:10 UTC

View this package on [NuGet.org](https://www.nuget.org/packages/Forma.Core).
```

## Configuration

### Required Permissions

The workflow requires the following permissions:
- `contents: write` - For creating Git tags and releases
- `packages: write` - For publishing NuGet packages

### Environment Requirements

- GitHub Actions environment with GitHub CLI (`gh`) installed
- Nerdbank.GitVersioning tool (`nbgv`)
- Git configuration for automation

## Security Considerations

- Uses `github-actions[bot]` identity for Git operations
- Only creates releases after successful package publishing
- Validates existing tags/releases to prevent conflicts
- Uses GitHub token with minimal required permissions

## Troubleshooting

### Common Issues

1. **Duplicate Tags**: The workflow automatically detects and skips existing tags
2. **Permission Errors**: Ensure the workflow has `contents: write` permission
3. **Version Detection Failures**: Check Nerdbank.GitVersioning configuration in `version.json`

### Manual Override

If manual tag/release creation is needed:

```bash
# Create tag manually
git tag -a v1.1.0 -m "Manual release v1.1.0"
git push origin v1.1.0

# Create release manually
gh release create v1.1.0 --title "Manual Release v1.1.0" --notes "Manual release notes"
```

## Migration Notes

- **Existing Workflows**: No changes required to existing release workflows
- **Backward Compatibility**: All existing functionality is preserved
- **New Behavior**: Tags and releases are now created automatically on successful package publishing
- **Manual Releases**: Can still be created manually if needed