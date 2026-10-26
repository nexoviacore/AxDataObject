## 18. Serialization and compatibility

- Use an explicit `schemaVersion` in serialized AxData snapshots.
- Do not serialize runtime service objects, database connections, delegates, or credentials.
- Define deterministic converters for Axpert data types, dates, decimals, nulls, files, and binary references.
- Keep snapshots backward-readable across rolling deployments or provide an explicit migration/upcaster.
- Store StructDef version/reference rather than an unnecessary complete duplicate when the existing definition can be reloaded consistently. Whether a definition subset must be embedded for exact recovery requires confirmation.

### 18.1 Snapshot v3 target

- Continue storing only the immutable StructDef content-hash reference; a mismatch remains a fail-closed re-Init condition.
- Readers remain compatible with schema v1 and v2; all writes after P1D-05B use deterministic schema v3.
- Store container identity once per container and row identity, order, action, and nullable persisted record ID once per row.
- Preserve typed field values; do not reduce dates, decimals, nulls, or binary references to undifferentiated strings.
- For clean fields, serialize current value and optional current source ID only. Serialize original value/source ID only while dirty; dirty state must be derivable or consistency-validated during rehydration.
- Field names may be represented once per data container and referenced by deterministic ordinal in rows if P1D-05A proves this is required for the capacity envelope.
- MessagePack, compression, or multi-key partitioning are not assumed. They require measurement, dependency review, backward compatibility, and preservation of atomic Redis CAS/snapshot publication.
