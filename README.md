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
