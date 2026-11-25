## 18. Serialization and compatibility

- The active DataObject payload has no `schemaVersion` or other format discriminator. The reader accepts only the current positional shape; deployments must clear incompatible Redis objects rather than migrate or upcast them.
- Do not serialize runtime services, credentials, a StructDef binding/header, Init/AxGet `keyField`/`keyValue` locators, business types, files, or `AxOpen`.
- Store ordered `[name,flags]` field descriptors once per data container, including empty DCs. Flag bit `1` means static read-only and bit `2` means source key; only `0`, `1`, `2`, and `3` are valid. Every row contains an exact-length `values` array aligned to those ordinals. Non-source-key values remain JSON-native null/string/boolean/number scalars; `{kind,value}` wrappers are invalid.
- A source-key value is `null` or one packed string `<displayValue>~<non-negative Int64 sourceId>`. Split only a field whose stored source-key flag is set and only at the last tilde; source ID `0` means no resolved ID. Never split an ordinary field, even when its literal string ends in `~digits`.
- Remove `sourceIds`, `originals`, and `originalSourceIds`. An existing dirty row has an exact-length `oldValues` aligned with `values`: dirty positions hold the encoded baseline and unchanged positions repeat the encoded current value. Omit `oldValues` for clean/new rows and after a full revert.
- Store PEG workflow data only while a workflow is active or a final-approval save has failed. Omit inactive workflow history from active DataObject state.
- Fail closed on non-scalar values, duplicate container/row/field identities, malformed field descriptors or unknown flag bits, missing or extra aligned values, malformed/negative/overflowing packed source IDs, legacy sparse properties, misaligned `oldValues`, or `oldValues` with no changed position. Set and AxPut additionally fail closed when stored source-key/static-read-only flags differ from current StructDef.
- Preserve decimals as JSON numbers and date/binary/reference representations as JSON strings. Query/Get needs no business-type interpretation and does not load StructDef; mutation/Save paths use the current StructDef and never infer a type from a string's appearance.
- Keep the unchanged tenant-scoped Redis key and one compact value so create-only writes, token-owned Set writes, and AxPut claim/finalization remain atomic.

The measured representative and 350,000-field capacity results for this layout are recorded in `docs/reports/AXD-STATE-03-positional-state.md`.
