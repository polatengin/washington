import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  tutorialSidebar: [
    'introduction',
    'playground/index',
    'getting-started',
    {
      type: 'category',
      label: 'CLI',
      items: ['cli/commands', 'cli/configuration'],
    },
    {
      type: 'category',
      label: 'VS Code Extension',
      items: ['vscode-extension/index', 'vscode-extension/settings'],
    },
    {
      type: 'category',
      label: 'GitHub Action',
      items: ['github-action/index', 'github-action/examples'],
    },
    {
      type: 'category',
      label: 'Guides',
      items: [
        'guides/contributing',
        'guides/how-estimates-work',
        'guides/supported-resources',
        'guides/troubleshooting',
      ],
    },
  ],
};

export default sidebars;
