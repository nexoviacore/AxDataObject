## 17. Configuration

Configuration is tenant/instance-aware where appropriate:

```text
AxData:Redis:Connection
AxData:Object:CompletedRetention
AxData:Object:RejectedRetention
AxData:Object:MaximumSerializedBytes
AxData:Object:MaximumGridRows
AxData:Concurrency:LockLease
AxData:Concurrency:LockWait
AxData:Files:MaximumInlineBytes
AxData:Downloads:LinkLifetime
AxData:Outbox:BatchSize
AxData:Outbox:RetryPolicy
AxData:StructDef:CacheTtl
AxData:Peg:PersistenceMode
AxData:Peg:StateTtl
```

Secrets must come from the platform's supported secret provider, not plain application settings committed to source control.

DataObject snapshot transport/worker/database settings are not active deployment requirements. Any future reactivation must document its complete configuration and health model in the approving slice.
