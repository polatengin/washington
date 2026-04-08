import {existsSync} from 'node:fs';
import {themes as prismThemes} from 'prism-react-renderer';
import type {PluginOptions} from '@docusaurus/plugin-content-docs';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';
import type {PrismTheme} from 'prism-react-renderer';

const showLastUpdateMetadata =
  process.env.DOCUSAURUS_ENABLE_LAST_UPDATE === undefined
    ? existsSync(new URL('../../.git', import.meta.url))
    : process.env.DOCUSAURUS_ENABLE_LAST_UPDATE === 'true';

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
  doc('release-notes'),
] satisfies Awaited<ReturnType<PluginOptions['sidebarItemsGenerator']>>;

const withYamlTokenStyles = (
  theme: PrismTheme,
  keyColor: string,
  punctuationColor: string,
): PrismTheme => ({
  ...theme,
  styles: [
    ...theme.styles,
    {
      types: ['atrule', 'key'],
      languages: ['yaml'],
      style: {
        color: keyColor,
        fontStyle: 'normal',
      },
    },
    {
      types: ['scalar', 'string'],
      languages: ['yaml'],
      style: {
        color: theme.plain.color ?? 'inherit',
      },
    },
    {
      types: ['punctuation'],
      languages: ['yaml'],
      style: {
        color: punctuationColor,
      },
    },
  ],
});

const prismTheme = withYamlTokenStyles(prismThemes.github, '#005cc5', '#6a737d');
const prismDarkTheme = withYamlTokenStyles(prismThemes.dracula, '#8be9fd', '#f8f8f2');

const config: Config = {
  title: 'Bicep Cost Estimator',
  tagline: 'Bicep Cost Estimator',
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
          showLastUpdateAuthor: showLastUpdateMetadata,
          showLastUpdateTime: showLastUpdateMetadata,
          sidebarItemsGenerator: () => docsSidebar,
          editUrl: 'https://github.com/polatengin/washington/tree/main/',
        },
        blog: false,
        theme: {
          customCss: './static/assets/bundle.css',
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
          label: 'CLI',
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
      ],
    },
    prism: {
      additionalLanguages: ['bash', 'yaml'],
      theme: prismTheme,
      darkTheme: prismDarkTheme,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
