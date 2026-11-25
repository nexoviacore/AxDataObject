## 14. Snapshot queue (dormant future use)

The API does not currently publish DataObject snapshots. The compiled future-use envelope is version-free:

```json
{
  "eventId": "...",
  "tenantId": "...",
  "objectId": "...",
  "snapshot": { "...": "complete AxData state" },
  "persistedAt": "2026-08-31T09:00:00Z"
}
```

If reactivated, the current dormant consumer/store uses last-delivery-wins replacement. That is not yet an approved production ordering guarantee.

Reactivation must approve publication atomicity, ordering, duplicate handling, retention, recovery authorization, health checks, and deployment together. Until then, the transport, worker, SQL table, and recovery service remain unwired.
