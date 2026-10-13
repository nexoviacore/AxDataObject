## 16. Error contract

```json
{
  "status": "error",
  "code": "AXD-CONC-001",
  "message": "The AxData object was changed by another request.",
  "correlationId": "...",
  "currentVersion": 14,
  "errors": []
}
```

Suggested categories:

| Prefix | Category | Typical HTTP status |
|---|---|---:|
| `AXD-AUTH` | authentication/authorization | 401/403 |
| `AXD-NOTFOUND` | object/record not found or expired | 404/410 |
| `AXD-REQ` | malformed request/unknown DC or field | 400 |
| `AXD-VAL` | field/transaction validation | 422 |
| `AXD-CONC` | stale object/workflow version or active conflict | 409 |
| `AXD-IDEMP` | idempotency conflict | 409 |
| `AXD-PEG` | invalid workflow action/state | 409/422 |
| `AXD-SAVE` | save/mapping failure | 422/500 according to cause |
| `AXD-INFRA` | Redis/database/queue dependency | 503 |

Expected business/validation errors should not be logged as unhandled exceptions. Unexpected failures should return a safe message and correlation ID without leaking SQL, credentials, paths, or stack traces.
