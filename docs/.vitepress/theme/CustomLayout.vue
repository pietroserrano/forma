<script setup lang="ts">
import DefaultTheme from 'vitepress/theme'
const { Layout } = DefaultTheme
</script>

<template>
  <Layout>
    <template #home-hero-before>
      <section class="fh">
        <!-- ── Animated background ───────────────────── -->
        <div class="fh-bg" aria-hidden="true">
          <div class="fh-bg__orb fh-bg__orb--a" />
          <div class="fh-bg__orb fh-bg__orb--b" />
          <div class="fh-bg__orb fh-bg__orb--c" />
          <div class="fh-bg__dots" />
        </div>

        <div class="fh-inner">
          <!-- ── Status badge ──────────────────────────── -->
          <div class="fh-badge">
            <span class="fh-badge__dot" />
            Open source &nbsp;·&nbsp; MIT License
          </div>

          <!-- ── Main title ────────────────────────────── -->
          <h1 class="fh-title">
            <span class="fh-title__eyebrow">Behavioral Patterns for .NET</span>
            <span class="fh-title__name" aria-label="Forma">
              <span class="fh-title__letter" style="--d:0ms">F</span>
              <span class="fh-title__letter" style="--d:60ms">O</span>
              <span class="fh-title__letter" style="--d:120ms">R</span>
              <span class="fh-title__letter" style="--d:180ms">M</span>
              <span class="fh-title__letter" style="--d:240ms">A</span>
            </span>
          </h1>

          <!-- ── Tagline + pattern ticker ──────────────── -->
          <p class="fh-tagline">
            Lightweight &amp; composable. Zero boilerplate.
          </p>
          <p class="fh-ticker-wrap" role="text" aria-label="CQRS Mediator, Decorator Pattern, Chain of Responsibility, Publish / Subscribe">
            <span class="fh-ticker">
              <span class="fh-ticker__inner">
                <span>CQRS &amp; Mediator</span>
                <span>Decorator Pattern</span>
                <span>Chain of Responsibility</span>
                <span>Publish / Subscribe</span>
              </span>
            </span>
          </p>

          <!-- ── Pattern chips ─────────────────────────── -->
          <div class="fh-chips" aria-hidden="true">
            <span class="fh-chip fh-chip--blue">🔀 Mediator</span>
            <span class="fh-chip fh-chip--purple">🎨 Decorator</span>
            <span class="fh-chip fh-chip--red">⛓️ Chains</span>
            <span class="fh-chip fh-chip--cyan">📡 PubSub</span>
          </div>

          <!-- ── Action buttons ────────────────────────── -->
          <div class="fh-actions">
            <a href="/getting-started" class="fh-btn fh-btn--brand">
              Get started <span aria-hidden="true">→</span>
            </a>
            <a
              href="https://github.com/pietroserrano/forma"
              target="_blank"
              rel="noreferrer"
              class="fh-btn fh-btn--ghost"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.21 11.39.6.11.82-.26.82-.58l-.01-2.04c-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76-1.34-1.76-1.09-.75.08-.73.08-.73 1.2.08 1.84 1.24 1.84 1.24 1.07 1.83 2.81 1.3 3.49 1 .11-.78.42-1.3.76-1.6-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.13-.3-.54-1.52.12-3.18 0 0 1.01-.32 3.3 1.23a11.5 11.5 0 0 1 3-.4c1.02 0 2.04.14 3 .4 2.28-1.55 3.29-1.23 3.29-1.23.66 1.66.25 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.81 5.63-5.48 5.92.43.37.81 1.1.81 2.22l-.01 3.29c0 .32.21.69.82.58C20.56 21.8 24 17.3 24 12c0-6.63-5.37-12-12-12z"/>
              </svg>
              GitHub
            </a>
          </div>
        </div>

        <!-- Bottom fade into page background -->
        <div class="fh-fade" aria-hidden="true" />
      </section>
    </template>
  </Layout>
</template>
