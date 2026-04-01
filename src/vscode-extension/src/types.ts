export interface CostReport {
  lines: ResourceCostLine[];
  grandTotal: number;
  warnings: string[];
}

export interface ResourceCostLine {
  resourceType: string;
  resourceName: string;
  pricingDetails: string;
  monthlyCost: number;
}
