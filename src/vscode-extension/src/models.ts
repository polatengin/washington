interface ARMTemplateJson {
  $schema: string;
  contentVersion: string;
  resources: Resource[];
  parameters: { [key in string]: { type: string, defaultValue: string, maxValue: string, minValue: string } };
}
