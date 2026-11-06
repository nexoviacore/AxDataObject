# HTTP and Postman review pack

Read `AxData/tools/postman/README.md` before planning any slice that adds or changes an endpoint, request/response shape,
or AXD error code.

- Update `AxData/tools/postman/` in the same slice.
- Use request shapes from contract tests. Never invent a request shape.
- Use targeted `rg` searches for the request being changed; do not read the whole collection JSON.
- Add or update requests in the current milestone folder: a happy path and at least one negative.
- Assert HTTP status and AXD error code. Chain `objectId` and `version` through test scripts when applicable.
- Put non-HTTP steps such as Redis eviction, worker execution, or user switching in the README as manual steps.
- Never commit secrets, tenant connections, tokens, or real record IDs in the collection or environment template.
