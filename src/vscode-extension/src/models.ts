interface ARMTemplateJson {
  $schema: string;
  contentVersion: string;
  resources: Resource[];
  parameters: { [key in string]: { type: string, defaultValue: string, maxValue: string, minValue: string } };
}

interface Resource {
  name: string;
  type: string;
  apiVersion: string;
  properties: any;
  location: string;
  [key: string]: any;
}

interface ResourceModel {
  startLineIndex: number;
  endLineIndex: number;
  name: string;
  type: string;
  properties: string;
  estimatedCost: string;
}
