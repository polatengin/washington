import * as assert from 'assert';
import { CostReport, ResourceCostLine } from '../../types';

suite('Types', () => {
  test('CostReport structure is correct', () => {
    const report: CostReport = {
      lines: [
        {
          resourceType: 'Microsoft.Compute/virtualMachines',
          resourceName: 'test-vm',
          pricingDetails: 'Standard_D2s_v3 @ $0.0960/hr × 730 hrs',
          hourlyCost: 0.096,
          monthlyCost: 70.08,
        },
      ],
      grandTotal: 70.08,
      currency: 'USD',
      warnings: ['⚠ No pricing mapper for Microsoft.Network/networkInterfaces - skipped'],
    };

    assert.strictEqual(report.lines.length, 1);
    assert.strictEqual(report.grandTotal, 70.08);
    assert.strictEqual(report.currency, 'USD');
    assert.strictEqual(report.warnings.length, 1);
  });

  test('ResourceCostLine structure is correct', () => {
    const line: ResourceCostLine = {
      resourceType: 'Microsoft.Storage/storageAccounts',
      resourceName: 'mystorage',
      pricingDetails: 'StorageV2 Standard_LRS Hot ~100 GB',
      hourlyCost: 0.0071,
      monthlyCost: 5.2,
    };

    assert.strictEqual(line.resourceType, 'Microsoft.Storage/storageAccounts');
    assert.strictEqual(line.resourceName, 'mystorage');
    assert.ok(line.monthlyCost > 0);
  });
});
