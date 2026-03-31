import * as assert from 'assert';
import { getConfig, WashingtonConfig } from '../../config';

suite('Config', () => {
  test('getConfig returns all settings', () => {
    const config: WashingtonConfig = getConfig();
    assert.ok(typeof config.currency === 'string');
    assert.ok(typeof config.defaultRegion === 'string');
    assert.ok(typeof config.cliPath === 'string');
    assert.ok(typeof config.estimateOnSave === 'boolean');
    assert.ok(typeof config.showCodeLens === 'boolean');
    assert.ok(typeof config.showStatusBar === 'boolean');
    assert.ok(typeof config.cacheTtlHours === 'number');
  });

  test('getConfig default values', () => {
    const config = getConfig();
    assert.strictEqual(config.currency, 'USD');
    assert.strictEqual(config.defaultRegion, 'eastus');
    assert.strictEqual(config.cliPath, '');
    assert.strictEqual(config.estimateOnSave, true);
    assert.strictEqual(config.showCodeLens, true);
    assert.strictEqual(config.showStatusBar, true);
    assert.strictEqual(config.cacheTtlHours, 24);
  });
});
