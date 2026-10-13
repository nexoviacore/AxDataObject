# P1C-05 AxData-native FillGrid and P1C-04 HTTP walkthrough

Generated: 2026-10-08 15:07:16Z

Redis version: 8.2.10; Memurai version: 8.2.0.

## FillGrid characterization

| Tenant | TransId | Name | AddRows | Mode | Auto show | Firm bind | Multiselect | Parameters | Duplicate parameters | Mappings | SQL hash | Schema verified |
|---|---|---|---:|---|---|---|---|---:|---:|---:|---|---|
| uat2 | axdtf | batchone | 1 | Append | True | False | False | 0 | 0 | 2 | fadf116a5b4a1881fd0030fe07fdf58cf0a5df7ace79739b749f6214a3a9ffab | true |
| uat2 | axdtf | batchtwo | 2 | Replace | True | True | False | 1 | 1 | 2 | 154be859e083a4dab371084e4a42d7d1bef8343741509df8d91791f32364896a | true |
| uat2 | axdtf | batchthree | 3 | Append | True | True | False | 0 | 0 | 2 | fadf116a5b4a1881fd0030fe07fdf58cf0a5df7ace79739b749f6214a3a9ffab | true |

## HTTP walkthrough

| Case | TransId | State path | StructDef hash | updatedon | RecordId | Init version | Set version | Changed | Dependency changes | FillGrid changes | Errors | Defined dependencies | Worker acknowledged | Recovery version | D28 |
|---|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| existing-record | dtchk | AxGet | ae4022d3a0240b199dc185ca7cd263ebf3c672ae4e430b06b951512328a090b4 | 01/10/2026 22:12:18 | 100000000008013 | 1 | 2 | 4 | 4 | 0 | 0 | 2 | 1 | 2 | 409 AXD-CONC-003; durable recovery succeeded |
| new-state | axdtf | new | 9edc2390f32de82231c7989e3c29d2b4697ba516b1ee65897615805035bda015 | 08/10/2026 20:38:30 | - | 1 | 2 | 60 | 0 | 60 | 0 | 0 | 2 | 2 | 409 AXD-CONC-003; durable recovery succeeded |

Result: PASS. Real AddRows 1/2/3 metadata, multiple applicable definitions, duplicate-parameter normalization, parameterized PostgreSQL FillGrid execution, tenant-schema isolation, trusted identity, authorization, StructDef, AxGet/new state, Init, Get, Set, field evaluation, dependencies, Redis state, local SQL durability, exact-key eviction, recovery Get, and D28 fail-closed behavior completed.

No field values, key values, credentials, connection strings, or tokens are included in this report.
