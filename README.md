# Forma

**Forma** is a lightweight and modular .NET library that provides abstractions and infrastructure for implementing common behavioral design patterns such as **Mediator**, **Decorator**, **Pipeline**, and more.

> Build composable, decoupled, and maintainable application flows using clean architectural principles.

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

Coming soon on [NuGet.org](https://www.nuget.org/):

```bash
dotnet add package Forma.Core
dotnet add package Forma.Mediator
dotnet add package Forma.Decorator
