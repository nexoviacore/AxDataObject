## 7. Authorization matrix

The final matrix is driven by Axpert permission definitions; this table is the minimum service-level rule.

| Actor | Query | Set | Save | Approve/Reject/Return | Recall | Print |
|---|---:|---:|---:|---:|---:|---:|
| Owner/initiator, permitted | Yes | Yes when editable | Yes when allowed | Only if also approver | Yes while pending | By permission |
| Current approver | Yes, permission-filtered | No by default | No direct save | Assigned task only | No unless admin/initiator | By explicit permission |
| Viewer with access | Read-only | No | No | No | No | By explicit permission |
| Administrator | By admin policy | By admin policy | By admin policy | By admin policy | Yes | By admin policy |
| Unrelated user | No | No | No | No | No | No |

Authorization order:

1. Authenticate request.
2. Resolve tenant/instance context.
3. Load only by tenant-scoped ObjectId key.
4. Confirm owner/current task/admin relationship.
5. Apply transaction/DC/field/method permission.
6. Execute the operation.
