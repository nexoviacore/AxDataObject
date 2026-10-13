## 17. Configuration

Configuration is tenant/instance-aware where appropriate:

```text
AxData:Redis:Connection
AxData:Object:InactivityTtl
AxData:Object:CompletedRetention
AxData:Object:RejectedRetention
AxData:Object:MaximumSerializedBytes
AxData:Object:MaximumGridRows
AxData:Concurrency:LockLease
AxData:Concurrency:LockWait
AxData:Snapshot:QueueName
AxData:Snapshot:MaximumLag
AxData:Files:MaximumInlineBytes
AxData:Downloads:LinkLifetime
AxData:Outbox:BatchSize
AxData:Outbox:RetryPolicy
AxData:StructDef:CacheTtl
AxData:Peg:PersistenceMode
AxData:Peg:StateTtl
```

Secrets must come from the platform's supported secret provider, not plain application settings committed to source control.
