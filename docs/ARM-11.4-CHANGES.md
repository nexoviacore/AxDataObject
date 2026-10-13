# ARM-11.4 change ledger

Record every explicitly approved change made under `ARM-11.4/`. AxData remains the preferred location for
new behavior; this ledger documents the exceptional shared-project changes and their blast radius.

## 2026-09-30 — P1B-02 fail-closed Parser evaluation

- Files: `ARM-11.4/AxValidate/Validate.cs`, `ARM-11.4/AxValidate/Helpers/ValidateHelper.cs`.
- Reason: the field-engine adapter cannot safely wrap code that ignores `AxParser.Evaluate(false)`, reads
  `.Value`, or silently retains a prior calculated value; doing so could accept a failed validation or reuse an
  invalid calculation result.
- Behavioral impact: validation expressions, field calculations, and tax formulas now fail closed when parsing fails. Public
  signatures are unchanged; existing `AxValidate` consumers such as `AxTstructData` may now receive a
  validation/calculation failure where malformed expressions previously continued with an empty or stale value.
- Verification: seven focused P1B-02 regression/contract tests pass; the serial AxData solution build and full
  test suite pass without restore. The host's default parallel solution build exits without diagnostics while
  traversing the legacy project graph, so verification used `-m:1`.
