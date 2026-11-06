# Axpert parity and salvage

Read this reference only when a slice adapts existing Axpert behavior, diagnoses an equivalent-operation bug, or may
require an `ARM-11.4/` change. The retired `ARM-11.4/AxDataObject` source is not a parity authority.

- For Init, Set/validation, Query, Auth, Redis state, Save/GenMap/MdMap, or PEG, inspect the relevant item in
  `docs/SALVAGE.md`. Name its bucket and item in the plan. "Reuse with changes" identifies behavior to preserve and
  parts to rebuild; it is not permission to copy the old type wholesale.
- Find the existing working caller first. Search `ARM-11.4/` with `rg` for the specific symbol, method, or table; never
  perform open-ended directory reads or read a large legacy file in full.
- For Save/AxPut work, read `docs/contracts/axput-save-parity.md` first. If its recorded source fingerprints still
  match, reuse its trace without reopening the implementation. If a fingerprint changed, inspect only the changed
  file/symbol diff, refresh the trace and fingerprints, and do not restore or inspect the retired AxDataObject project.
- Include a **Parity trace** in the plan: state initialization, variable registration, and call order before the operation.
  List every deliberately skipped step and its reason.
- For bug fixes, state the evidence-backed root cause after comparing the working legacy path. Do not propose an ARM
  change until adapter parity is ruled out. If two causes fit, name both and the check that distinguishes them.
- If targeted inspection does not make another ARM project's behavior clear, summarize the evidence and ask the user.
  Do not fill the gap with an assumption.
