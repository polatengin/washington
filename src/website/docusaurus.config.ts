import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Bicep Cost Estimator',
  tagline: 'Azure Cost Estimator',
  favicon: 'img/logo.svg',
  url: 'https://bicepcostestimator.net',

  future: {
    v4: true,
  },

  baseUrl: '/',
  organizationName: 'polatengin',
  projectName: 'washington',
  trailingSlash: false,

  onBrokenLinks: 'warn',

  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn'
    }
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          path: '../../docs',
          routeBasePath: '/',
          editUrl:
            'https://github.com/polatengin/washington/tree/main/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Bicep Cost Estimator',
      logo: {
        alt: 'Bicep Cost Estimator Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          label: 'Getting Started',
          href: '/getting-started',
          position: 'left',
        },
        {
          label: 'CLI Commands',
          href: '/cli/commands',
          position: 'left',
        },
        {
          label: 'VS Code Extension',
          href: '/vscode-extension',
          position: 'left',
        },
        {
          label: 'GitHub Action',
          href: '/github-action',
          position: 'left',
        },
        {
          html: `
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px;vertical-align:text-bottom;">
              <circle cx="11" cy="11" r="8"/>
              <path d="M21 21l-4.35-4.35"/>
            </svg>
            Search
          `,
          href: '/search',
          position: 'right',
        },
        {
          label: 'GitHub',
          href: 'https://github.com/polatengin/washington',
          position: 'right',
          className: 'navbar-github-link',
        },
        {
          label: 'Issues',
          href: 'https://github.com/polatengin/washington/issues',
          position: 'right',
          className: 'navbar-issues-link',
        },
        {
          label: 'Discussions',
          href: 'https://github.com/polatengin/washington/discussions',
          position: 'right',
          className: 'navbar-discussions-link',
        },
      ],
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
