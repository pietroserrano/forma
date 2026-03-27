<script setup>
// static showcase component — no reactive state needed
</script>

<template>
  <!-- ── Badges ───────────────────────────────────────────── -->
  <div class="hb-row">
    <a href="https://github.com/pietroserrano/forma/actions/workflows/build-test.yml" target="_blank" rel="noreferrer">
      <img src="https://github.com/pietroserrano/forma/actions/workflows/build-test.yml/badge.svg" alt="Build and Test" />
    </a>
    <a href="https://www.nuget.org/packages/Forma.Core/" target="_blank" rel="noreferrer">
      <img src="https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core&color=5D87E8" alt="Forma.Core" />
    </a>
    <a href="https://www.nuget.org/packages/Forma.Mediator/" target="_blank" rel="noreferrer">
      <img src="https://img.shields.io/nuget/v/Forma.Mediator.svg?label=Forma.Mediator&color=7C4DFF" alt="Forma.Mediator" />
    </a>
    <a href="https://www.nuget.org/packages/Forma.Decorator/" target="_blank" rel="noreferrer">
      <img src="https://img.shields.io/nuget/v/Forma.Decorator.svg?label=Forma.Decorator&color=FF5252" alt="Forma.Decorator" />
    </a>
    <a href="https://www.nuget.org/packages/Forma.Chains/" target="_blank" rel="noreferrer">
      <img src="https://img.shields.io/nuget/v/Forma.Chains.svg?label=Forma.Chains&color=00BCD4" alt="Forma.Chains" />
    </a>
    <a href="https://www.nuget.org/packages/Forma.PubSub.InMemory/" target="_blank" rel="noreferrer">
      <img src="https://img.shields.io/nuget/v/Forma.PubSub.InMemory.svg?label=Forma.PubSub.InMemory&color=43A047" alt="Forma.PubSub.InMemory" />
    </a>
  </div>

  <!-- ── Stats strip ──────────────────────────────────────── -->
  <section class="hs-stats">
    <div class="hs-stats__inner">
      <div class="hs-stat">
        <span class="hs-stat__num">33%</span>
        <span class="hs-stat__label">Faster than MediatR</span>
      </div>
      <div class="hs-stat__sep" />
      <div class="hs-stat">
        <span class="hs-stat__num">5</span>
        <span class="hs-stat__label">Independent packages</span>
      </div>
      <div class="hs-stat__sep" />
      <div class="hs-stat">
        <span class="hs-stat__num">0</span>
        <span class="hs-stat__label">Core dependencies</span>
      </div>
      <div class="hs-stat__sep" />
      <div class="hs-stat">
        <span class="hs-stat__num">.NET 6+</span>
        <span class="hs-stat__label">Compatible</span>
      </div>
    </div>
  </section>

  <!-- ── Install cards ────────────────────────────────────── -->
  <section class="hs-section hs-install">
    <div class="hs-section__inner">
      <p class="hs-eyebrow">Get started</p>
      <h2 class="hs-title">Pick what you need</h2>
      <p class="hs-sub">
        Each package is fully independent — install only the patterns your project uses.
      </p>
      <div class="hs-install__grid">
        <div class="hs-pkg hs-pkg--blue">
          <div class="hs-pkg__top">
            <span class="hs-pkg__icon">🔀</span>
            <span class="hs-pkg__name">Mediator</span>
          </div>
          <code class="hs-pkg__cmd">dotnet add package Forma.Mediator</code>
        </div>
        <div class="hs-pkg hs-pkg--purple">
          <div class="hs-pkg__top">
            <span class="hs-pkg__icon">🎨</span>
            <span class="hs-pkg__name">Decorator</span>
          </div>
          <code class="hs-pkg__cmd">dotnet add package Forma.Decorator</code>
        </div>
        <div class="hs-pkg hs-pkg--red">
          <div class="hs-pkg__top">
            <span class="hs-pkg__icon">⛓️</span>
            <span class="hs-pkg__name">Chains</span>
          </div>
          <code class="hs-pkg__cmd">dotnet add package Forma.Chains</code>
        </div>
        <div class="hs-pkg hs-pkg--cyan">
          <div class="hs-pkg__top">
            <span class="hs-pkg__icon">📡</span>
            <span class="hs-pkg__name">PubSub</span>
          </div>
          <code class="hs-pkg__cmd">dotnet add package Forma.PubSub.InMemory</code>
        </div>
      </div>
    </div>
  </section>

  <!-- ── Code preview ─────────────────────────────────────── -->
  <section class="hs-section hs-code-preview">
    <div class="hs-section__inner hs-code-preview__grid">
      <div class="hs-code-preview__copy">
        <p class="hs-eyebrow">In action</p>
        <h2 class="hs-title">Clean, decoupled, testable</h2>
        <p>
          Define a request, write a handler — Forma wires everything through the DI container.
          No base classes, no magic strings, no reflection surprises.
        </p>
        <a class="hs-cta" href="/getting-started">Explore all patterns →</a>
      </div>

      <div class="hs-win">
        <div class="hs-win__bar">
          <span class="hs-win__dot hs-win__dot--red" />
          <span class="hs-win__dot hs-win__dot--yellow" />
          <span class="hs-win__dot hs-win__dot--green" />
          <span class="hs-win__file">GetProductHandler.cs</span>
        </div>
        <pre class="hs-win__body"><code><span class="cmt">// 1. Define the query</span>
<span class="kw">public record</span> GetProductQuery(<span class="kw">int</span> Id)
    : IRequest&lt;Product&gt;;

<span class="cmt">// 2. Implement the handler</span>
<span class="kw">public class</span> <span class="tp">GetProductHandler</span>
    : IRequestHandler&lt;GetProductQuery, Product&gt;
{
    <span class="kw">public</span> Task&lt;Product&gt; Handle(
        GetProductQuery q, CancellationToken ct)
        =&gt; _repo.FindAsync(q.Id, ct);
}

<span class="cmt">// 3. Register once, call anywhere</span>
builder.Services.AddFormaMediator(
    <span class="kw">typeof</span>(Program).Assembly);

<span class="kw">var</span> product = <span class="kw">await</span> sender.Send(
    <span class="kw">new</span> GetProductQuery(<span class="nm">42</span>));</code></pre>
      </div>
    </div>
  </section>
</template>
