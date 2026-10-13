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
