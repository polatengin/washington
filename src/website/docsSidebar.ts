export type DocsSidebarDocItem = {
  type: 'doc';
  id: string;
};

export type DocsSidebarCategoryItem = {
  type: 'category';
  label: string;
  items: DocsSidebarDocItem[];
};

export type DocsSidebarItem = DocsSidebarDocItem | DocsSidebarCategoryItem;

export type DocsNavigationItem = {
  href: string;
  order: number;
};

const doc = (id: string): DocsSidebarDocItem => ({
  type: 'doc',
  id,
});

const category = (label: string, items: readonly string[]): DocsSidebarCategoryItem => ({
  type: 'category',
  label,
  items: items.map(doc),
});

export const docsSidebar: DocsSidebarItem[] = [
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
  doc('roadmap'),
  doc('release-notes'),
];

function normalizeRoute(value: string) {
  const trimmed = value.trim();

  if (!trimmed || trimmed === '/') {
    return '/';
  }

  const withLeadingSlash = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
  return withLeadingSlash.endsWith('/') ? withLeadingSlash.slice(0, -1) : withLeadingSlash;
}

function routeFromDocId(id: string) {
  if (id === 'introduction') {
    return '/';
  }

  return normalizeRoute(`/${id.replace(/\/index$/, '')}`);
}

export function buildDocsNavigation(items: readonly DocsSidebarItem[] = docsSidebar): DocsNavigationItem[] {
  const navigation: DocsNavigationItem[] = [];

  const visit = (sidebarItems: readonly DocsSidebarItem[]) => {
    for (const item of sidebarItems) {
      if (item.type === 'doc') {
        navigation.push({
          href: routeFromDocId(item.id),
          order: navigation.length,
        });
        continue;
      }

      visit(item.items);
    }
  };

  visit(items);
  return navigation;
}

const docsSidebarModule = {
  docsSidebar,
  buildDocsNavigation,
};

export default docsSidebarModule;