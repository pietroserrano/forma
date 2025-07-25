# Forma

<div align="center">
  <img src="assets/forma-icon.svg" alt="Forma Logo" width="200">
</div>

**Forma** is a lightweight and modular .NET library that provides abstractions and infrastructure for implementing common behavioral design patterns such as **Mediator**, **Decorator**, **Pipeline**, and more.

> Build composable, decoupled, and maintainable application flows using clean architectural principles.

[![Build and Test](https://github.com/pietroserrano/forma/actions/workflows/build-test.yml/badge.svg)](https://github.com/pietroserrano/forma/actions/workflows/build-test.yml)

## NuGet Packages

[![Forma.Core](https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core)](https://www.nuget.org/packages/Forma.Core/)
<!--[![Forma.Core PreRelease](https://img.shields.io/nuget/vpre/Forma.Core.svg?label=Forma.Core%20(preview))](https://www.nuget.org/packages/Forma.Core/)-->

[![Forma.Mediator](https://img.shields.io/nuget/v/Forma.Mediator.svg?label=Forma.Mediator)](https://www.nuget.org/packages/Forma.Mediator/)
<!--[![Forma.Mediator PreRelease](https://img.shields.io/nuget/vpre/Forma.Mediator.svg?label=Forma.Mediator%20(preview))](https://www.nuget.org/packages/Forma.Mediator/)-->

[![Forma.Decorator](https://img.shields.io/nuget/v/Forma.Decorator.svg?label=Forma.Decorator)](https://www.nuget.org/packages/Forma.Decorator/)
<!--[![Forma.Decorator PreRelease](https://img.shields.io/nuget/vpre/Forma.Decorator.svg?label=Forma.Decorator%20(preview))](https://www.nuget.org/packages/Forma.Decorator/)-->

[![Forma.Chains](https://img.shields.io/nuget/v/Forma.Chains.svg?label=Forma.Chains)](https://www.nuget.org/packages/Forma.Chains/)
<!--[![Forma.Chains PreRelease](https://img.shields.io/nuget/vpre/Forma.Chains.svg?label=Forma.Chains%20(preview))](https://www.nuget.org/packages/Forma.Chains/)-->

[![Forma.PubSub.InMemory](https://img.shields.io/nuget/v/Forma.PubSub.InMemory.svg?label=Forma.PubSub.InMemory)](https://www.nuget.org/packages/Forma.PubSub.InMemory/)
<!--[![Forma.PubSub.InMemory PreRelease](https://img.shields.io/nuget/vpre/Forma.PubSub.InMemory.svg?label=Forma.PubSub.InMemory%20(preview))](https://www.nuget.org/packages/Forma.PubSub.InMemory/)-->

---

## ✨ Features

- ✅ Abstract and extensible interfaces (`IMediator`, `IRequestHandler`, `IPipelineBehavior`, `IDecorator`, etc.)
- ✅ Built-in support for request/response messaging (Mediator Pattern)
- ✅ Seamless integration with .NET Dependency Injection
- ✅ Auto-registration of handlers and behaviors via reflection
- ✅ Support for custom pipeline behaviors (e.g. logging, retry, caching)
- ✅ Zero dependencies in the core package

---

## 📦 Installation

Available on [NuGet.org](https://www.nuget.org/):

```bash
# Core components
dotnet add package Forma.Core
dotnet add package Forma.Mediator
dotnet add package Forma.Decorator

# Additional components
dotnet add package Forma.Chains
dotnet add package Forma.PubSub.InMemory
```

## Benchmark
| Method                      | Categories          | Mean     | Error   | StdDev   | Median   | Rank |
|---------------------------- |-------------------- |---------:|--------:|---------:|---------:|-----:|
| Forma_RequestWithResponse   | RequestWithResponse | 334.8 ns | 6.23 ns | 10.92 ns | 332.0 ns |    1 |
| MediatR_RequestWithResponse | RequestWithResponse | 492.4 ns | 9.54 ns | 10.98 ns | 491.9 ns |    2 |
|                             |                     |          |         |          |          |      |
| Forma_SendAsync_object      | SendAsObject        | 335.7 ns | 6.46 ns |  8.63 ns | 335.2 ns |    1 |
| MediatR_Send_object         | SendAsObject        | 452.4 ns | 9.31 ns | 26.25 ns | 441.3 ns |    2 |
|                             |                     |          |         |          |          |      |
| Forma_SimpleRequest         | SimpleRequest       | 283.0 ns | 5.50 ns |  5.40 ns | 282.7 ns |    1 |
| MediatR_SimpleRequest       | SimpleRequest       | 412.1 ns | 7.47 ns | 10.71 ns | 408.1 ns |    2 |

## 🛠 Development

### Requirements
- .NET 9.0 SDK or higher
- Visual Studio 2025 or other .NET-compatible IDE

### Useful Commands

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run -c Release --project src/Forma.Benchmarks/Forma.Benchmarks.csproj
```

## 📋 Documentation

### Getting Started
- [Installation](#installation) - How to install Forma packages
- [Features](#features) - Overview of Forma capabilities
- [Development](#development) - Local development setup

### Release & Development
- **[Release Guide](./docs/release-guide.md)** - Comprehensive guide for releasing packages
- [Testing GitHub Actions](./docs/testing-github-actions.md) - How to test workflows locally
- [Project vs NuGet References](./docs/project-vs-nuget-references.md) - Development vs CI/CD build configuration

### Technical Documentation
- [Workflow Documentation](./.github/workflows/README.md) - GitHub Actions workflows overview
- [Logo Assets](./assets/README.md) - Forma logo and branding guidelines

### Quick References
- See the [Release Guide Quick Reference](./docs/release-guide.md#quick-reference) for common commands

## 🚀 Release Process

Forma uses a hybrid approach for releasing its NuGet packages:

- **Core Releases** (`Forma.Core`, `Forma.Mediator`, `Forma.Decorator`): Released together with the same version
- **Component Releases** (`Forma.Chains`, `Forma.PubSub.InMemory`): Released independently

For more information on the release process, see the [Release Guide](./docs/release-guide.md).

## 👥 Contributing

Contributions are welcome! If you'd like to contribute to Forma:

1. Fork the repository
2. Create a branch for your feature (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Before contributing, please review the development documentation in the `docs/` folder.
