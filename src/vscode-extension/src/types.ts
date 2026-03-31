export interface CostReport {
  lines: ResourceCostLine[];
  grandTotal: number;
  currency: string;
  warnings: string[];
}

export interface ResourceCostLine {
  resourceType: string;
  resourceName: string;
  pricingDetails: string;
  monthlyCost: number;
}
