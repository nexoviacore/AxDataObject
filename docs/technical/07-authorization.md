## 7. Authorization matrix

The final matrix is driven by Axpert permission definitions; this table is the minimum service-level rule.

| Actor | Query | Set | Save | Approve/Reject/Return | Recall | Print |
|---|---:|---:|---:|---:|---:|---:|
| Initiator, permitted | Yes | Yes when editable | Yes when allowed | Only if also approver | Yes while pending | By permission |
| Current approver | Yes, permission-filtered | No | No direct save | Assigned task only | No unless also initiator | By explicit permission |
| Viewer with access | Read-only | No | No | No | No | By explicit permission |
| Unrelated user | No | No | No | No | No | No |

Authorization order:

1. Authenticate request.
2. Resolve tenant/instance context.
3. Load only by tenant-scoped ObjectId key.
4. Confirm the persisted initiator or current trusted task assignment required by the operation.
5. Apply transaction/DC/field/method permission.
6. Execute the operation.

Query/Get supplies the DataObject's DC/field map to the authorization port, so the production adapter can normalize fresh transaction/DC/field permissions without loading StructDef. Stored metadata is not permission authority: every request still performs the live permission lookup, and hidden nodes are omitted. Set and Save continue loading the current StructDef because they validate/evaluate business data and must confirm stored source-key/static-read-only flags. A role named `admin` creates no generic DataObject relationship or operation bypass; any future administrative operation requires its own trusted permission and policy.
