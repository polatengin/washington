import React, {useCallback, useEffect, useRef, useState} from 'react';
import Head from '@docusaurus/Head';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import BrowserOnly from '@docusaurus/BrowserOnly';
import styles from './search.module.css';

interface SearchResult {
  title: string;
  category: string;
  href: string;
  body: string;
}

function SearchContent() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const searchFnRef = useRef<((value: string) => Promise<SearchResult[]>) | null>(null);
  const allDocsRef = useRef<SearchResult[]>([]);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const initialQuery = params.get('q');

    if (initialQuery) {
      setQuery(initialQuery);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadSearchIndex() {
      try {
        const documentsResponse = await fetch('/docfind/documents.json');

        if (!documentsResponse.ok) {
          throw new Error('Could not load search documents.');
        }

        const documents = await documentsResponse.json() as SearchResult[];

        if (cancelled) {
          return;
        }

        allDocsRef.current = documents;

        try {
          // @ts-expect-error runtime URL, not a bundled module
          const module = await import(/* webpackIgnore: true */ '/docfind/docfind.js');

          if (cancelled) {
            return;
          }

          await module.init();
          searchFnRef.current = module.default;
        } catch {
          searchFnRef.current = null;
        }

        setLoading(false);
      } catch {
        if (cancelled) {
          return;
        }

        setError('Failed to load search index. Please try again later.');
        setLoading(false);
      }
    }

    void loadSearchIndex();

    return () => {
      cancelled = true;
    };
  }, []);

  function fallbackSearch(value: string): SearchResult[] {
    const normalizedQuery = value.toLowerCase();
    const scored = allDocsRef.current
      .map(document => {
        let score = 0;

        if (document.title.toLowerCase().includes(normalizedQuery)) {
          score += 10;
        }

        if (document.category.toLowerCase().includes(normalizedQuery)) {
          score += 5;
        }

        if (document.body.toLowerCase().includes(normalizedQuery)) {
          score += 1;
        }

        return {document, score};
      })
      .filter(result => result.score > 0)
      .sort((left, right) => right.score - left.score);

    return scored.map(result => result.document);
  }

  const runSearch = useCallback(async (value: string) => {
    const trimmedValue = value.trim();

    if (!trimmedValue) {
      setResults([]);
      return;
    }

    if (!searchFnRef.current) {
      setResults(fallbackSearch(trimmedValue));
      return;
    }

    try {
      const nextResults = await searchFnRef.current(trimmedValue);

      if (nextResults.length > 0) {
        setResults(nextResults);
        return;
      }

      setResults(fallbackSearch(trimmedValue));
    } catch {
      setResults(fallbackSearch(trimmedValue));
    }
  }, []);

  useEffect(() => {
    if (loading) {
      return;
    }

    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    debounceRef.current = setTimeout(() => {
      void runSearch(query);

      const url = new URL(window.location.href);
      const trimmedQuery = query.trim();

      if (trimmedQuery) {
        url.searchParams.set('q', trimmedQuery);
      } else {
        url.searchParams.delete('q');
      }

      window.history.replaceState(null, '', url.toString());
    }, 300);

    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, [loading, query, runSearch]);

  return (
    <div className={styles.searchContainer}>
      <div className={styles.inputWrapper}>
        <svg
          className={styles.searchIcon}
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          aria-hidden="true"
        >
          <circle cx="11" cy="11" r="8" />
          <path d="M21 21l-4.35-4.35" />
        </svg>
        <input
          type="text"
          className={styles.searchInput}
          placeholder="Search documentation..."
          value={query}
          onChange={event => setQuery(event.target.value)}
          autoFocus
        />
      </div>

      {error && <div className={styles.statusError}>{error}</div>}

      {!loading && !error && query.trim() && results.length === 0 && (
        <div className={styles.resultCount}>No results found for &ldquo;{query.trim()}&rdquo;</div>
      )}

      {results.length > 0 && (
        <div className={styles.results}>
          <p className={styles.resultCount}>
            {results.length} result{results.length === 1 ? '' : 's'} found
          </p>

          {results.map(result => (
            <Link key={`${result.href}:${result.title}`} to={result.href} className={styles.resultCard}>
              <div className={styles.resultHeader}>
                <span className={styles.resultTitle}>{result.title}</span>
                <span className={styles.resultCategory}>{result.category}</span>
              </div>

              {result.body && <p className={styles.resultBody}>{result.body}</p>}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

export default function SearchPage(): React.JSX.Element {
  const {siteConfig} = useDocusaurusContext();

  return (
    <Layout>
      <Head>
        <title>Search | {siteConfig.title}</title>
        <meta name="description" content={`Search ${siteConfig.title} documentation`} />
      </Head>

      <BrowserOnly fallback={<div />}>
        {() => <SearchContent />}
      </BrowserOnly>
    </Layout>
  );
}