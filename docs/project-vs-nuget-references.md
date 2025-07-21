# Project vs NuGet References Guide

This project is configured to use conditional references between projects, allowing for:
- Using direct project references during local development
- Using NuGet packages when building in CI/CD environments (build pipelines)

## How It Works

The system is implemented through the MSBuild property `UseProjectReferences` defined in `Directory.Build.props`. By default, this property is set to `true`, meaning that local development uses direct project references.

When you want to build using NuGet packages instead of project references, you can set the `UseProjectReferences` property to `false`.

## Local Development Usage

No special action is needed. The `.csproj` files are configured to use project references by default.

```xml
<!-- Example of conditional ItemGroup in a .csproj file -->
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
    <ProjectReference Include="..\Forma.Core\Forma.Core.csproj" />
</ItemGroup>
```

## CI/CD Usage

To build using NuGet packages instead of project references, simply pass the MSBuild property during build:

```bash
dotnet build -p:UseProjectReferences=false
```

Or during packaging:

```bash
dotnet pack -p:UseProjectReferences=false
```

## Version Management

The version of NuGet packages to use is defined by the `FormaVersion` property in `Directory.Build.props`, defaulting to `1.0.*`.

This format allows specifying fixed major and minor versions while leaving the patch version flexible. The `1.0.*` format ensures that the latest patch version of available `1.0.x` packages is always selected.

If you need to use a specific version, you can override this property during build:

```bash
dotnet build -p:UseProjectReferences=false -p:FormaVersion=1.2.3
```

## Versioning with Nerdbank.GitVersioning

The project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) to automatically manage package versions. This tool assigns semantic version numbers based on Git commits and tags, making the versioning process completely automatic.

The versioning configuration is defined in the `version.json` file at the repository root.

## Legacy Tag Format

For releasing new versions of a component, you can create tags with the format `v{version}-{component}`:

```bash
git tag v1.0.0-chains
git push origin v1.0.0-chains
```

or

```bash
git tag v1.0.0-pubsub
git push origin v1.0.0-pubsub
```

The GitHub Actions workflow `nuget-component-deploy.yml` will handle the rest.

> **Note**: This legacy approach is still supported but the new branch-based release strategy is recommended for new releases.