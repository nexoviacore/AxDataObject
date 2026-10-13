## 12. Security Model

- All requests use authenticated user and tenant/instance context supplied by trusted authentication middleware.
- `ObjectId` is opaque, unpredictable, and always checked against the authenticated tenant and applicable owner/initiator or current approver authorization.
- An approver receives only task-specific Get and approval permissions, not unrestricted Set/Save/Print access.
- The internal final-approval Save bypass is callable only through trusted application code; clients cannot submit a bypass flag.
- Hidden data is omitted rather than masked only in the UI.
- AFL definitions and credentials remain server-side and encrypted. Clients refer to an authorized AFL name.
- File references are validated against tenant, object, field, file type, and size policies.
- Download links are authenticated or signed, short-lived, and scoped to one artifact.
- Security-sensitive actions and workflow transitions are audited.
