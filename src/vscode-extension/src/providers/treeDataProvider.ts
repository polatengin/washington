import * as vscode from 'vscode';
import { CostReport, ResourceCostLine } from '../types';

export class CostTreeDataProvider implements vscode.TreeDataProvider<CostTreeItem> {
  private _onDidChangeTreeData = new vscode.EventEmitter<CostTreeItem | undefined | void>();
  readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

  private report: CostReport | undefined;

  update(report: CostReport): void {
    this.report = report;
    this._onDidChangeTreeData.fire();
  }

  getTreeItem(element: CostTreeItem): vscode.TreeItem {
    return element;
  }

  getChildren(element?: CostTreeItem): CostTreeItem[] {
    if (!this.report) {
      return [new CostTreeItem('No cost data yet. Run "Estimate File" command.', '')];
    }

    if (!element) {
      // Root: show total + resource items
      const items: CostTreeItem[] = [];

      items.push(new CostTreeItem(
        `Total: $${this.report.grandTotal.toFixed(2)} ${this.report.currency}/month`,
        '',
        vscode.TreeItemCollapsibleState.None
      ));

      for (const line of this.report.lines) {
        items.push(new CostTreeItem(
          line.resourceName,
          `$${line.monthlyCost.toFixed(2)}/mo`,
          vscode.TreeItemCollapsibleState.Collapsed,
          line
        ));
      }

      if (this.report.warnings.length > 0) {
        items.push(new CostTreeItem(
          `Warnings (${this.report.warnings.length})`,
          '',
          vscode.TreeItemCollapsibleState.Collapsed
        ));
      }

      return items;
    }

    // Child: show details for a resource
    if (element.resourceLine) {
      const line = element.resourceLine;
      return [
        new CostTreeItem(`Type: ${line.resourceType}`, ''),
        new CostTreeItem(`Location: ${line.location}`, ''),
        new CostTreeItem(`Details: ${line.pricingDetails}`, ''),
        new CostTreeItem(`Monthly Cost: $${line.monthlyCost.toFixed(2)}`, ''),
      ];
    }

    // Warnings children
    if (element.label?.toString().startsWith('Warnings') && this.report) {
      return this.report.warnings.map(w => new CostTreeItem(w, ''));
    }

    return [];
  }
}

class CostTreeItem extends vscode.TreeItem {
  constructor(
    public readonly label: string,
    public readonly description: string,
    public readonly collapsibleState: vscode.TreeItemCollapsibleState = vscode.TreeItemCollapsibleState.None,
    public readonly resourceLine?: ResourceCostLine
  ) {
    super(label, collapsibleState);
    this.description = description;
    if (resourceLine) {
      this.iconPath = new vscode.ThemeIcon('symbol-variable');
    }
  }
}
