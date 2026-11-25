## 20. Observability implementation

- Use structured logging scopes for correlation ID, tenant, ObjectId, TransId, and workflow task. AxData logs do not include object versions, session identifiers, tokens, bodies, or field values.
- Redact data values by default; whitelist safe metadata rather than blacklist secrets.
- Trace API -> Redis -> Axpert engine operations with safe identifiers; AxPut retains its separately documented ARM trace exception around SQL and Save.
- Emit metrics for Set field count/evaluation duration, serialized bytes, conflicts, validation errors, Save duration/rollbacks, PEG transitions, queue lag, and worker retries.
- Add health checks only for active dependencies, distinguishing readiness from liveness. Dormant DataObject snapshot transport/SQL/worker paths are not readiness dependencies.
