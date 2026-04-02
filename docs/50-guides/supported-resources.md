---
title: Supported Resources
sidebar_position: 51
---

# Supported Resources

Washington currently ships with pricing mappers for 87 Azure resource types. Support is implemented in the CLI mapper registry and shared by the CLI, VS Code extension, and GitHub Action.

## Coverage by Category

| Category | Resource Types |
| --- | --- |
| **Compute** | Virtual Machines, Virtual Machine Scale Sets, Managed Disks, Batch Accounts, Spring Apps |
| **Containers** | AKS Managed Clusters, Container Registry, Container Apps, Container App Environments, Container Instances |
| **App Services** | App Service Plans, Function Apps, Static Web Apps |
| **Storage** | Storage Accounts, Azure NetApp Files |
| **Databases** | SQL Database, SQL Elastic Pools, SQL Managed Instances, Cosmos DB, Cosmos DB for MongoDB vCore, PostgreSQL Flexible Servers, MySQL Flexible Servers, MariaDB Servers |
| **Networking** | Public IP Addresses, Application Gateways, Load Balancers, Virtual Network Gateways, Azure Firewall, Firewall Policies, Private Endpoints, NAT Gateways, Virtual Networks, Network Interfaces, Network Security Groups, Route Tables, Private DNS Zones, DNS Zones, Traffic Manager, Bastion Hosts, DDoS Protection Plans, ExpressRoute Circuits, Front Door, Network Watcher |
| **Security** | Key Vault, Managed Identity, Recovery Services Vault, Defender for Cloud |
| **Messaging** | Event Hub, Service Bus, Event Grid, Notification Hubs |
| **AI / ML** | Cognitive Services, Machine Learning Workspaces, Azure AI Search, Bot Service |
| **Monitoring** | Log Analytics Workspaces, Application Insights, Azure Monitor Workspace, Azure Managed Grafana |
| **Integration** | API Management, Logic Apps, Data Factory, Azure Relay, Azure Communication Services, Azure API for FHIR |
| **Analytics** | Databricks Workspaces, Synapse Workspaces, Azure Data Explorer, Stream Analytics, HDInsight, Power BI Embedded |
| **Caching** | Azure Cache for Redis, Redis Enterprise |
| **Real-time** | SignalR Service |
| **IoT** | IoT Hub, Azure Digital Twins |
| **Config** | App Configuration |
| **Automation** | Automation Accounts |
| **Developer** | Dev Center, Azure Load Testing, DevTest Labs |
| **Virtual Desktop** | Azure Virtual Desktop |
| **Service Fabric** | Service Fabric Clusters |
| **Governance** | Microsoft Purview, Confidential Ledger |
| **Media & Maps** | Media Services, Azure Maps |

## What Supported Means

Supported does not mean that every billing nuance for a service is modeled.

In practice, support means:

- Washington can recognize the resource type.
- Washington can derive one or more pricing queries from the resource's SKU or properties.
- Washington can produce a monthly cost line item from the returned price records.

Some services have a simple one-to-one mapping. Others are approximated from the most relevant recurring meter for the chosen SKU.

## Unsupported Resources

When a resource type does not have a mapper yet, Washington does not fail the whole estimate. It emits a warning like this instead:

```text
⚠ No pricing mapper for Microsoft.Xyz/abc — skipped
```

That behavior is useful in mixed templates, but it also means your total can be incomplete if some resource types are not yet mapped.

## Pricing Assumptions

The current mapper set uses pay-as-you-go retail pricing by default.

- Spot and low-priority pricing are excluded from the default queries.
- Reserved Instance, Savings Plan, and contract-specific pricing are not modeled yet.
- If a region cannot be resolved from the template, the estimator falls back to `eastus`.

## Choosing Good Validation Templates

If you want to validate mapper coverage quickly, start with templates that contain:

- one resource type per file
- explicit `location` and `sku` values
- minimal ARM expression indirection around SKU and sizing properties

That makes it easier to confirm whether a cost line is missing because the resource is unsupported or because an expression could not be resolved.

## Related Reading

- [How Estimates Work](/guides/how-estimates-work)
- [Troubleshooting](/guides/troubleshooting)
