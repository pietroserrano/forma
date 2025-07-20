# Quick Reference: NuGet Release Workflows

## Branch Commands

### Stable Release
```bash
git checkout -b release_v1.2
# ... make changes ...
git push origin release_v1.2
```

### Preview Release  
```bash
git checkout develop
# ... make changes ...
git push origin develop
```

## Manual Workflow Triggers

| Workflow | Purpose | Input Options |
|----------|---------|---------------|
| `release-core.yml` | Core packages | `force-publish: true/false` |
| `release-chains.yml` | Chains package | `force-publish: true/false` |
| `release-pubsub.yml` | PubSub package | `force-publish: true/false` |
| `release-all.yml` | All packages | `packages: "all"` or `"core,chains"` etc.<br>`force-publish: true/false` |

## Package Dependencies

```
Forma.Core (base)
├── Forma.Mediator (depends on Core)
├── Forma.Decorator (depends on Core)
├── Forma.Chains (depends on Core)
└── Forma.PubSub.InMemory (depends on Core)
```

## Version Examples

| Branch/Tag | Version Output | Notes |
|------------|----------------|-------|
| `release_v1.2` | `1.2.0` | Stable release |
| `develop` | `1.3.0-preview.45` | Preview release |
| `v1.2.3` | `1.2.3` | Tag-based release |
| `v1.2.3-core` | `1.2.3` | Legacy tag (core only) |

## File Change Triggers

| Files Changed | Packages Affected |
|---------------|-------------------|
| `src/Forma.Core/**` | Core, Mediator, Decorator, Chains, PubSub |
| `src/Forma.Mediator/**` | Mediator only |
| `src/Forma.Decorator/**` | Decorator only |
| `src/Forma.Chains/**` | Chains only |
| `src/Forma.PubSub.InMemory/**` | PubSub only |
| `Directory.Build.props` | All packages |
| `version.json` | All packages |

## Secrets Setup

1. Go to repository Settings → Secrets and variables → Actions
2. Add secrets:
   - `NUGET_API_KEY` (required)
   - `NUGET_SOURCE` (optional, defaults to nuget.org)

## Emergency Commands

### Force Release All Packages
1. Go to Actions → "Release All Packages"
2. Run workflow with:
   - `packages: "all"`
   - `force-publish: true`

### Release Single Package
1. Go to Actions → Choose specific workflow
2. Run workflow with:
   - `force-publish: true`