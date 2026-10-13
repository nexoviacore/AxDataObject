# Design index

Both design docs, split by section so a task reads only what it needs (about 100 to 1,500 tokens per file).
Open a file only if a slice block names it. Section numbers match the original documents.

## Architecture (`docs/architecture/`)

| File | Heading | ~tokens |
|------|---------|--------:|
| 00-front-matter.md | AxData Object — Architecture and Solution Design | 173 |
| 01-purpose.md | 1. Purpose | 160 |
| 02-existing-capabilities.md | 2. Existing Axpert capabilities to reuse | 225 |
| 03-goals.md | 3. Goals | 217 |
| 04-non-goals.md | 4. Non-goals for the initial implementation | 108 |
| 05-core-concepts.md | 5. Core concepts | 632 |
| 06-logical-architecture.md | 6. Logical Architecture | 1475 |
| 07-service-boundaries.md | 7. Service boundaries and deployment | 224 |
| 07.2-peg-module.md | 7.2 PEG Module | 238 |
| 07.3-initial-deployment.md | 7.3 Initial Deployment | 263 |
| 07.4-later-scaling.md | 7.4 Later Independent Scaling | 211 |
| 08-lifecycle.md | 8. Primary transaction lifecycle | 712 |
| 09-peg-lifecycle.md | 9. PEG Lifecycle | 1206 |
| 10-state-persistence.md | 10. State and Persistence Strategy | 806 |
| 11-reliability.md | 11. Transaction and Reliability Model | 205 |
| 12-security.md | 12. Security Model | 237 |
| 13-scalability.md | 13. Scalability and Performance Strategy | 285 |
| 14-observability.md | 14. Observability and Operations | 220 |
| 15-delivery.md | 15. Delivery Strategy | 225 |
| 16-success-criteria.md | 16. Definition of Architectural Success | 275 |

## Technical (`docs/technical/`)

| File | Heading | ~tokens |
|------|---------|--------:|
| 00-front-matter.md | AxData Object — Technical Design and Implementation Reference | 92 |
| 01-scope.md | 1. Scope | 129 |
| 02-solution-structure.md | 2. Proposed solution structure | 224 |
| 03-dependency-direction.md | 3. Dependency direction | 109 |
| 04-domain-model.md | 4. Core domain model | 837 |
| 05-axpert-adapters.md | 5. Existing Axpert adapter contracts | 336 |
| 06.1-api-general.md | 6.1 General | 191 |
| 06.2-initialize.md | 6.2 Initialize | 144 |
| 06.3-set.md | 6.3 Set | 316 |
| 06.4-query.md | 6.4 Query/Get | 142 |
| 06.5-submit.md | 6.5 Submit for PEG approval | 205 |
| 06.6-save.md | 6.6 Save business data | 362 |
| 06.7-print.md | 6.7 Print | 84 |
| 06.8-peg-tasks.md | 6.8 PEG task APIs | 247 |
| 07-authorization.md | 7. Authorization matrix | 257 |
| 08-redis.md | 8. Redis design | 472 |
| 09-set-algorithm.md | 9. Set processing algorithm | 271 |
| 10-fillgrid.md | 10. FillGrid behavior | 133 |
| 11-save-orchestration.md | 11. Save orchestration | 546 |
| 12-peg-consistency.md | 12. PEG processing and state consistency | 1099 |
| 13-database.md | 13. Database persistence | 551 |
| 14-snapshot-queue.md | 14. Snapshot queue | 168 |
| 15-files-afl.md | 15. Files, AFL, and downloads | 196 |
| 16-errors.md | 16. Error contract | 259 |
| 17-config.md | 17. Configuration | 174 |
| 18-serialization.md | 18. Serialization and compatibility | 160 |
| 19-structdef-cache.md | 19. Caching StructDef | 102 |
| 20-observability.md | 20. Observability implementation | 169 |
| 21-testing.md | 21. Testing strategy | 547 |
| 22-entry-criteria.md | 22. Implementation entry criteria | 207 |
