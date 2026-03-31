import * as assert from 'assert';
import * as vscode from 'vscode';
import { CostTreeDataProvider } from '../../providers/treeDataProvider';
import { CostReport } from '../../types';

suite('TreeDataProvider', () => {
  test('shows placeholder when no data', () => {
    const provider = new CostTreeDataProvider();
    const children = provider.getChildren();
    assert.ok(children.length > 0);
    assert.ok(children[0].label?.toString().includes('No cost data'));
  });

  test('shows total and resources after update', () => {
    const provider = new CostTreeDataProvider();
    const report: CostReport = {
      lines: [
        {
          resourceType: 'Microsoft.Compute/virtualMachines',
          resourceName: 'test-vm',
          location: 'eastus',
          pricingDetails: 'Standard_D2s_v3 @ $0.0960/hr × 730 hrs',
          monthlyCost: 70.08,
        },
        {
          resourceType: 'Microsoft.Storage/storageAccounts',
          resourceName: 'mystorage',
          location: 'eastus',
          pricingDetails: 'StorageV2 Standard_LRS Hot ~100 GB',
          monthlyCost: 5.2,
        },
      ],
      grandTotal: 75.28,
      currency: 'USD',
      warnings: [],
    };

    provider.update(report);
    const children = provider.getChildren();

    // Total line + 2 resources = 3 items (no warnings section)
    assert.strictEqual(children.length, 3);
    assert.ok(children[0].label?.toString().includes('75.28'));
    assert.ok(children[1].label?.toString().includes('test-vm'));
    assert.ok(children[2].label?.toString().includes('mystorage'));
  });

  test('shows warnings section when present', () => {
    const provider = new CostTreeDataProvider();
    const report: CostReport = {
      lines: [],
      grandTotal: 0,
      currency: 'USD',
      warnings: ['⚠ No pricing mapper for Microsoft.Network/networkInterfaces — skipped'],
    };

    provider.update(report);
    const children = provider.getChildren();

    // Total + warnings section
    assert.ok(children.length >= 2);
    const warningsItem = children.find(c => c.label?.toString().startsWith('Warnings'));
    assert.ok(warningsItem, 'Warnings item should be shown');
  });

  test('resource children show details', () => {
    const provider = new CostTreeDataProvider();
    const report: CostReport = {
      lines: [
        {
          resourceType: 'Microsoft.Compute/virtualMachines',
          resourceName: 'test-vm',
          location: 'eastus',
          pricingDetails: 'Standard_D2s_v3',
          monthlyCost: 70.08,
        },
      ],
      grandTotal: 70.08,
      currency: 'USD',
      warnings: [],
    };

    provider.update(report);
    const children = provider.getChildren();
    // Get the resource item (index 1 after total)
    const resourceItem = children[1];
    const details = provider.getChildren(resourceItem);

    assert.ok(details.length === 4);
    assert.ok(details.some(d => d.label?.toString().includes('Type:')));
    assert.ok(details.some(d => d.label?.toString().includes('Location:')));
    assert.ok(details.some(d => d.label?.toString().includes('Details:')));
    assert.ok(details.some(d => d.label?.toString().includes('Monthly Cost:')));
  });

  test('fires onDidChangeTreeData when updated', () => {
    const provider = new CostTreeDataProvider();
    let fired = false;
    provider.onDidChangeTreeData(() => { fired = true; });

    const report: CostReport = {
      lines: [],
      grandTotal: 0,
      currency: 'USD',
      warnings: [],
    };

    provider.update(report);
    assert.ok(fired, 'onDidChangeTreeData should fire after update');
  });
});
