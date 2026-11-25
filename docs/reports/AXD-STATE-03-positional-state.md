# AXD-STATE-03 positional DataObject state

Measured on the same Windows/.NET 8 development host with the integration-test fixtures. The representative fixture covers typed values, source IDs, dirty originals, and active workflow state. The capacity fixture is 50 DCs × 70 rows × 100 fields (350,000 field instances). Capacity timings are the best of three deterministic round trips, which reduces host/JIT noise; allocation is measured on the serialization thread.

| Fixture | Metric | Before | Positional | Change |
| --- | ---: | ---: | ---: | ---: |
| Representative | Payload | 1,283 bytes | 1,079 bytes | -15.9% |
| Representative | Serialize | 0.163 ms | 0.104 ms | -36.2% |
| Representative | Deserialize | 36.651 ms | 0.253 ms | -99.3% |
| Capacity | Payload | 25,716,773 bytes | 15,972,279 bytes | -37.9% |
| Capacity | Serialize | 296.63 ms | 165.69 ms | -44.1% |
| Capacity | Deserialize | 1,592.94 ms | 605.99 ms | -62.0% |
| Capacity | Serialization allocation | 68,012,752 bytes | 35,147,200 bytes | -48.3% |

The compact layout stores deterministic field names once per DC, exact-length typed row values, positive source IDs only when present, and dirty baseline value/source pairs only while dirty. It removes `schemaVersion`, backward readers, persisted read-only/StructDef metadata, files, and `AxOpen`; current StructDef reconstructs behavior metadata after restore. Active or final-save-failed workflow state is retained, while inactive workflow history is omitted.

The Redis key and single-value atomic-write boundary are unchanged. Malformed ordinal maps, inconsistent dirty baselines, field-set drift, and versioned legacy payloads fail closed. Timing values are diagnostic rather than service-level guarantees; the executable tests enforce the 32 MiB payload limit, a 500 ms capacity serialization ceiling, and a 256 MiB serialization-allocation ceiling.

## AXD-STATE-03A corrective measurement

The same representative and 350,000-field fixtures were rerun after replacing `{kind,value}` wrappers and ordinal objects with native JSON scalars and exact pair arrays. `Store` is deterministic serialization; `Restore` is strict parse and domain rehydration. Values are a single quiet post-implementation run on the same development host.

| Fixture | Metric | STATE-03 positional | STATE-03A native/pairs | Change |
| --- | ---: | ---: | ---: | ---: |
| Representative | Payload | 1,079 bytes | 918 bytes | -14.9% |
| Representative | Store | 0.104 ms | 0.215 ms | +106.7% |
| Representative | Restore | 0.253 ms | 0.203 ms | -19.8% |
| Capacity | Payload | 15,972,279 bytes | 6,872,243 bytes | -57.0% |
| Capacity | Store | 165.69 ms | 264.29 ms | +59.5% |
| Capacity | Restore | 605.99 ms | 633.76 ms | +4.6% |
| Capacity | Store allocation | 35,147,200 bytes | 43,892,648 bytes | +24.9% |

The corrected payload remains below 32 MiB, Store remains below the enforced 500 ms ceiling, and Store allocation remains below 256 MiB. Timing differences are diagnostic host measurements; the wire-shape and safety assertions are the compatibility contract.

## AXD-STATE-03D locator-removal measurement

After removing persisted `keyField` and `keyValue`, the current representative fixture is 705 bytes and the 350,000-field capacity fixture is 6,872,126 bytes. The capacity payload is 117 bytes smaller than the STATE-03A measurement; intervening STATE-03B/03C changes also affect the representative wire shape, so its 705-byte result is recorded as a current baseline rather than attributed solely to locator removal. The same 32 MiB payload, 500 ms Store, and 256 MiB allocation limits continue to pass.

## AXD-STATE-03E packed-source/aligned-old-values measurement

After replacing the three sparse per-row arrays with `{name,sourceKey}` descriptors, packed source-key strings, and optional exact-length `oldValues`, the representative dirty fixture is 848 bytes. The 350,000-field clean/new-row capacity fixture is 7,007,126 bytes, 135,000 bytes (+2.0%) above the 03D baseline because each of its 5,000 DC field mappings now carries explicit source-key metadata once. A diagnostic run measured 354.78 ms Store, 435.98 ms Restore, and 41,350,464 Store-allocated bytes for the capacity fixture. The enforced 32 MiB payload, 500 ms Store, and 256 MiB allocation limits pass. The representative fixture now covers aligned unchanged positions and independent source-ID changes; timings remain host diagnostics rather than service-level guarantees.

## AXD-STATE-03F compact field-flag measurement

Replacing field descriptor objects with `[name,flags]` arrays while adding static read-only metadata reduced the representative dirty fixture from 848 to 711 bytes (-137 bytes, -16.2%). The 350,000-field capacity fixture fell from 7,007,126 to 6,892,126 bytes (-115,000 bytes, -1.6%). A diagnostic run measured 41.973 ms Store and 19.725 ms Restore for the representative fixture; the capacity fixture measured 291.26 ms Store, 807.18 ms Restore, and 41,160,744 Store-allocated bytes. The enforced 32 MiB payload, 500 ms Store, and 256 MiB allocation limits pass. These timings are host diagnostics; the compact shape, valid flags `0..3`, and strict reader behavior are the compatibility contract.
