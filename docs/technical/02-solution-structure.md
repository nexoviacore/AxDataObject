## 2. Proposed solution structure

```text
AxData.sln
  src/
    AxData.Api/                 HTTP endpoints, authentication, filters
    AxData.Application/         use cases, orchestration, authorization
    AxData.Domain/              AxData model, rules, result/error types
    AxData.Infrastructure/      Redis, SQL, queues, outbox, file storage
    AxData.Integration.Axpert/  adapters for StructDef/Validate/Parser/AxGet/AxPut
    AxData.Peg/                 PEG application/domain, Redis state, persistence adapters
    AxData.Workers/             snapshot, outbox, print, notification workers
  tests/
    AxData.UnitTests/
    AxData.IntegrationTests/
    AxData.ContractTests/
    AxData.EndToEndTests/
```

AxData and PEG are separate modules but may be hosted by `AxData.Api` in the same process initially. Boundaries must not rely on direct mutation of each other's internal models.
