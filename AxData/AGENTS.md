# AxData-local agent instructions

These instructions extend the repository-level `AGENTS.md` for all work under `AxData/`.

## Method documentation

- Add concise C# XML documentation to public contracts and to methods with non-obvious security, lifecycle,
  concurrency, persistence, parsing, serialization, or integration behavior.
- State what the method guarantees. Document input constraints, meaningful return states, side effects, expected
  failures, cancellation behavior, and required ordering when those details are not obvious from the signature.
- Use `<remarks>` for security-sensitive or ordering-sensitive invariants, such as tenant scoping, authorization
  before access, token-owned writes, and success checks before reading computed values.
- For straightforward private methods, prefer a precise name over a comment. Document a private method only when
  its algorithm, assumptions, or side effects would otherwise be easy to misunderstand.
- Do not restate the method name or narrate individual statements. Keep documentation synchronized with every
  behavior change.

