import React, { ChangeEvent, FormEvent, useState } from 'react';

import { playgroundFixtures } from '../generated/playgroundFixtures';

type ResourceCostLine = {
  resourceType: string;
  resourceName: string;
  pricingDetails: string;
  hourlyCost: number;
  monthlyCost: number;
};

type CostReport = {
  lines: ResourceCostLine[];
  grandTotal: number;
  warnings: string[];
};

const sampleTemplate = `param location string = 'eastus'

resource appPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'washington-playground-plan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'washingtonplaydemo'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}`;

const monthlyCurrency = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const hourlyCurrency = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 4,
  maximumFractionDigits: 4,
});

const defaultFixture = playgroundFixtures.find(fixture => fixture.id === 'showcase.bicep')
  ?? playgroundFixtures[0];

export default function Playground() {
  const [source, setSource] = useState(defaultFixture?.source ?? sampleTemplate);
  const [selectedFixtureId, setSelectedFixtureId] = useState(defaultFixture?.id ?? '');
  const [report, setReport] = useState<CostReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleFixtureChange(event: ChangeEvent<HTMLSelectElement>) {
    const fixtureId = event.target.value;
    setSelectedFixtureId(fixtureId);

    if (!fixtureId) {
      setReport(null);
      setError(null);
      setSource('');
      return;
    }

    const fixture = playgroundFixtures.find(candidate => candidate.id === fixtureId);
    if (!fixture) {
      return;
    }

    setReport(null);
    setError(null);
    setSource(fixture.source);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setIsSubmitting(true);
    setError(null);

    try {
      const response = await fetch('/api/estimate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ source }),
      });

      const payload = await response.json().catch(() => null);

      if (!response.ok) {
        throw new Error(typeof payload?.error === 'string' ? payload.error : 'Estimation failed.');
      }

      setReport(payload as CostReport);
    } catch (submissionError) {
      setReport(null);
      setError(submissionError instanceof Error ? submissionError.message : 'Estimation failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const resourceCount = report?.lines.length ?? 0;
  const warningCount = report?.warnings.length ?? 0;

  return (
    <section className="playground">
      <form onSubmit={handleSubmit}>
        <div className="playground__panelHeader">
          <h2>Template input</h2>

          <div className="playground__editorActions">
            <div className="playground__fixturePicker">
              <select
                id="playground-fixture"
                className="playground__select"
                onChange={handleFixtureChange}
                value={selectedFixtureId}
              >
                <option value="">Custom template</option>
                {playgroundFixtures.map(fixture => (
                  <option key={fixture.id} value={fixture.id}>
                    {fixture.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <textarea
          id="playground-source"
          className="playground__editor"
          value={source}
          onChange={event => {
            setSelectedFixtureId('');
            setSource(event.target.value);
          }}
          spellCheck={false}
          placeholder="resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = { ... }"
        />

        <div className="playground__helperRow">
          <p>⚠ Local modules and separate parameter files are not supported in the browser playground yet. Paste a self-contained template.</p>
          <button className="playground__button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Estimating...' : 'Estimate cost'}
          </button>
        </div>
      </form>

      <section className="playground__results">
        <div className="playground__summaryGrid">
          <article className="playground__summaryCard">
            <span>Estimated monthly total</span>
            <strong>{monthlyCurrency.format(report?.grandTotal ?? 0)}</strong>
          </article>
          <article className="playground__summaryCard">
            <span>Resources priced</span>
            <strong>{resourceCount}</strong>
          </article>
          <article className="playground__summaryCard">
            <span>Warnings</span>
            <strong>{warningCount}</strong>
          </article>
        </div>

        {error ? <div className="playground__errorBanner">{error}</div> : null}

        {report ? (
          <>
            {report.warnings.length > 0 ? (
              <div className="playground__warningPanel">
                <h3>Warnings</h3>
                <ul>
                  {report.warnings.map(warning => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            <table className="playground__table">
              <thead>
                <tr>
                  <th>Resource</th>
                  <th>Type</th>
                  <th>Details</th>
                  <th>Hourly</th>
                  <th>Monthly</th>
                </tr>
              </thead>
              <tbody>
                {report.lines.map(line => (
                  <tr key={`${line.resourceType}:${line.resourceName}`}>
                    <td>{line.resourceName}</td>
                    <td>{line.resourceType}</td>
                    <td>{line.pricingDetails}</td>
                    <td>{hourlyCurrency.format(line.hourlyCost)}</td>
                    <td>{monthlyCurrency.format(line.monthlyCost)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        ) : (
          <div className="playground__emptyState">
            <h3>No estimate yet</h3>
            <p>
              Paste a Bicep template on the left and run the estimator to see per-resource cost
              lines and the total monthly number here.
            </p>
          </div>
        )}
      </section>
    </section>
  );
}