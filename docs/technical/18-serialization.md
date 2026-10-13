## 18. Serialization and compatibility

- Use an explicit `schemaVersion` in serialized AxData snapshots.
- Do not serialize runtime service objects, database connections, delegates, or credentials.
- Define deterministic converters for Axpert data types, dates, decimals, nulls, files, and binary references.
- Keep snapshots backward-readable across rolling deployments or provide an explicit migration/upcaster.
- Store StructDef version/reference rather than an unnecessary complete duplicate when the existing definition can be reloaded consistently. Whether a definition subset must be embedded for exact recovery requires confirmation.
