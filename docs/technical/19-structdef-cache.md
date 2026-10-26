## 19. Caching StructDef

Cache key:

```text
tenantId + transId + publishedDefinitionVersion
```

Requirements:

- StructDef is treated as immutable for a given version.
- Definition changes create/invalidate a versioned entry.
- An active AxData object continues with a compatible definition or follows a documented migration/restart rule.
- Do not use only TransId as the cache key in a multi-tenant system.

### 19.1 Legacy cache and operation runtime

The current legacy `StructDef.LoadStructure` cache is keyed by tenant schema plus TransId, has no visible definition-hash component, and returns a newly deserialized mutable StructDef with helpers reattached. AxTstructData uses that cache by default; current AxData bypasses it with `refreshCache: true`.

P1D-05A must prove Builder invalidation/coherency before AxData enables the legacy cache. The operation first reads the authoritative current definition hash and compares it with the AxData object's stored hash. A mismatch returns `AXD-CONC-003` and requires re-Init. If legacy cache coherency cannot be proven, AxData refreshes once per operation rather than once per submitted field. Authorization never relies on a cached StructDef object.

The cached definition is only an input for creating a fresh operation-scoped runtime. Mutable `StructDef`, `Validate`, and `Parser` instances are not shared between operations, tenants, users, or AxData objects.
