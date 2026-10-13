## 14. Snapshot queue

After a completed Set, the API publishes:

```json
{
  "eventId": "...",
  "tenantId": "...",
  "objectId": "...",
  "objectVersion": 13,
  "snapshot": { "...": "complete AxData state" },
  "persistedAt": "2026-08-31T09:00:00Z"
}
```

The consumer performs a version-guarded upsert: a snapshot with version 12 cannot overwrite stored version 13. Duplicate event IDs are harmless.

If publishing the queue message directly after Redis update is not reliable enough, use a Redis stream with persistence or a small durable snapshot outbox. The selected guarantee is a clarification item because the current AxPersist transport has not yet been provided.
