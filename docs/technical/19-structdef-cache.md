## 19. Loading StructDef

Active DataObject state does not retain or enforce a StructDef version/hash. AxData does not query raw `tstructs.props`, calculate a content hash, or maintain a hash-keyed projected-definition cache.

Init/AxGet, Set, and Save load the current StructDef needed for metadata and behavior. Query/Get uses stored compact field metadata plus fresh authorization and does not load StructDef. Concrete mutable Axpert types remain inside Integration.Axpert; application-only and fake paths use the small neutral definition contract where required by the dependency boundary.

### 19.1 Legacy cache and operation runtime

The legacy `StructDef.LoadStructure` cache is keyed by tenant schema plus TransId and returns a newly deserialized mutable StructDef with helpers reattached. AxTstructData uses that cache by default; current AxData bypasses cache reads with `refreshCache: true`.

AxData loads one current mutable StructDef per real operation rather than preloading a second definition for a hash or cache check. Real Init uses that same instance for AxGet, Validate/Parser, and on-load processing, then constructs the clean DataObject once from the final runtime values. Real Set uses its operation-owned instance for neutral metadata, hydration, validation/dependencies, final readback, and FillGrid metadata/query preparation; the handler does not separately preload a projected definition. Authorization never relies on a cached mutable StructDef object.

Mutable `StructDef`, `Validate`, `Parser`, and `Utils` instances are request-owned and are not shared between operations, tenants, users, or AxData objects. Init follows AxTstructData's scoped-runtime model without the Set field-engine's process-wide gate; a concurrent-init contract verifies separate definitions and overlapping loads. Set holds the field-engine gate for its complete runtime, including FillGrid execution, so FillGrid no longer acquires a second runtime/gate in the production Set path.
