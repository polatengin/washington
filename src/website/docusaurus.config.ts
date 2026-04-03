import {themes as prismThemes} from 'prism-react-renderer';
import type {PluginOptions} from '@docusaurus/plugin-content-docs';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const doc = (id: string) => ({
  type: 'doc' as const,
  id,
});

const category = (label: string, items: readonly string[]) => ({
  type: 'category' as const,
  label,
  items: items.map(doc),
});

const docsSidebar = [
  doc('introduction'),
  doc('playground/index'),
  doc('getting-started'),
  category('CLI', ['cli/commands', 'cli/configuration']),
  category('VS Code Extension', ['vscode-extension/index', 'vscode-extension/settings']),
  category('GitHub Action', ['github-action/index', 'github-action/examples']),
  category('Guides', [
    'guides/contributing',
    'guides/how-estimates-work',
    'guides/supported-resources',
    'guides/troubleshooting',
  ]),
] satisfies Awaited<ReturnType<PluginOptions['sidebarItemsGenerator']>>;

const config: Config = {
  title: 'Bicep Cost Estimator',
  tagline: 'Azure Cost Estimator',
  favicon: 'assets/logo.svg',
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
          path: '../../docs',
          routeBasePath: '/',
          sidebarItemsGenerator: () => docsSidebar,
          editUrl:
            'https://github.com/polatengin/washington/tree/main/',
        },
        blog: false,
        theme: {
          customCss: './static/assets/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'assets/social-card.png',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Bicep Cost Estimator',
      logo: {
        alt: 'Bicep Cost Estimator Logo',
        src: 'assets/logo.svg',
      },
      items: [
        {
          label: 'Playground',
          href: '/playground',
          position: 'left',
        },
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
