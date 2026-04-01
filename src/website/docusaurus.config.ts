import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Washington',
  tagline: 'Azure Cost Estimator',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://bicepcostestimate.net',
  baseUrl: '/',
  trailingSlash: false,

  organizationName: 'polatengin',
  projectName: 'washington',

  onBrokenLinks: 'throw',

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
      title: 'Washington',
      logo: {
        alt: 'Washington Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/polatengin/washington',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/getting-started',
            },
            {
              label: 'CLI',
              to: '/cli/commands',
            },
          ],
        },
        {
          title: 'Tools',
          items: [
            {
              label: 'VS Code Extension',
              to: '/vscode-extension',
            },
            {
              label: 'GitHub Action',
              to: '/github-action',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/polatengin/washington',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Washington Project. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
