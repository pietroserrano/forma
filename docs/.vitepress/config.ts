import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Forma',
  description: 'Lightweight and modular .NET library for behavioral design patterns',
  lang: 'en-US',
  base: '/forma/',

  head: [
    ['link', { rel: 'icon', href: '/forma/favicon.svg', type: 'image/svg+xml' }],
    ['meta', { name: 'theme-color', content: '#5D87E8' }],
    ['meta', { name: 'og:type', content: 'website' }],
    ['meta', { name: 'og:site_name', content: 'Forma Docs' }],
  ],

  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'Forma',

    nav: [
      { text: 'Getting Started', link: '/getting-started' },
      {
        text: 'Packages',
        items: [
          { text: 'Forma.Core', link: '/packages/core' },
          { text: 'Forma.Core.FP', link: '/packages/fp' },
          { text: 'Forma.Mediator', link: '/packages/mediator' },
          { text: 'Forma.Decorator', link: '/packages/decorator' },
          { text: 'Forma.Chains', link: '/packages/chains' },
          { text: 'Forma.PubSub.InMemory', link: '/packages/pubsub' },
        ],
      },
      {
        text: 'Guides',
        items: [
          { text: 'Console App', link: '/guides/console-app' },
          { text: 'ASP.NET Core Web API', link: '/guides/web-api' },
        ],
      },
      {
        text: 'NuGet',
        link: 'https://www.nuget.org/packages?q=Forma+pserrano',
        target: '_blank',
      },
    ],

    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'What is Forma?', link: '/' },
          { text: 'Getting Started', link: '/getting-started' },
        ],
      },
      {
        text: 'Packages',
        items: [
          { text: 'Forma.Core', link: '/packages/core' },
          { text: 'Forma.Core.FP', link: '/packages/fp' },
          { text: 'Forma.Mediator', link: '/packages/mediator' },
          { text: 'Forma.Decorator', link: '/packages/decorator' },
          { text: 'Forma.Chains', link: '/packages/chains' },
          { text: 'Forma.PubSub.InMemory', link: '/packages/pubsub' },
        ],
      },
      {
        text: 'Integration Guides',
        items: [
          { text: 'Console Application', link: '/guides/console-app' },
          { text: 'ASP.NET Core Web API', link: '/guides/web-api' },
        ],
      },
    ],

    search: {
      provider: 'local',
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/pietroserrano/forma' },
    ],

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2025 Pietro Serrano',
    },

    editLink: {
      pattern: 'https://github.com/pietroserrano/forma/edit/main/docs/:path',
      text: 'Edit this page on GitHub',
    },
  },
})
