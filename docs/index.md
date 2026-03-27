---
layout: home

hero:
  name: "Forma"
  text: "Behavioral Patterns for .NET"
  tagline: Lightweight, modular, and blazing fast. Build composable, decoupled, and maintainable application flows with clean architectural principles.
  image:
    src: /favicon.svg
    alt: Forma Logo
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/pietroserrano/forma

features:
  - icon: 🔀
    title: Mediator Pattern
    details: Decouple request senders from handlers. First-class support for CQRS with commands, queries, pipeline behaviors, pre/post-processors and auto-registration.
    link: /packages/mediator
    linkText: Learn more
  - icon: 🎨
    title: Decorator Pattern
    details: Add cross-cutting concerns (logging, caching, validation, retry) to any service without touching its implementation — pure Dependency Injection.
    link: /packages/decorator
    linkText: Learn more
  - icon: ⛓️
    title: Chain of Responsibility
    details: Build sequential processing pipelines with early-exit support. Perfect for validation pipelines, payment flows, and multi-step workflows.
    link: /packages/chains
    linkText: Learn more
  - icon: 📡
    title: Publish / Subscribe
    details: In-memory event-driven messaging. Decouple producers and consumers with background service integration and structured logging.
    link: /packages/pubsub
    linkText: Learn more
  - icon: ⚡
    title: Blazing Fast
    details: Up to 33 % faster than MediatR in benchmarks. Optimized for zero-allocation hot paths with minimal overhead.
    link: /getting-started#benchmarks
    linkText: See benchmarks
  - icon: 📦
    title: Zero Core Dependencies
    details: Forma.Core has no external dependencies. Each additional package adds only what it needs from Microsoft.Extensions.*.
    link: /packages/core
    linkText: Explore core
---

<div class="home-badges">

[![Build and Test](https://github.com/pietroserrano/forma/actions/workflows/build-test.yml/badge.svg)](https://github.com/pietroserrano/forma/actions/workflows/build-test.yml)
[![Forma.Core](https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core)](https://www.nuget.org/packages/Forma.Core/)
[![Forma.Mediator](https://img.shields.io/nuget/v/Forma.Mediator.svg?label=Forma.Mediator)](https://www.nuget.org/packages/Forma.Mediator/)
[![Forma.Decorator](https://img.shields.io/nuget/v/Forma.Decorator.svg?label=Forma.Decorator)](https://www.nuget.org/packages/Forma.Decorator/)
[![Forma.Chains](https://img.shields.io/nuget/v/Forma.Chains.svg?label=Forma.Chains)](https://www.nuget.org/packages/Forma.Chains/)
[![Forma.PubSub.InMemory](https://img.shields.io/nuget/v/Forma.PubSub.InMemory.svg?label=Forma.PubSub.InMemory)](https://www.nuget.org/packages/Forma.PubSub.InMemory/)

</div>

<style>
.home-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  justify-content: center;
  padding: 32px 24px 48px;
}
.home-badges img {
  height: 20px;
}
</style>
