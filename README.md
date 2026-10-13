# AxData

This repository contains the new AxData solution, its design documentation, and the legacy ARM 11.4 source needed for integration and behavioral reference.

## Layout

- `AxData/` — the new .NET 8 solution, tests, and developer tools.
- `docs/` — architecture, technical contracts, decisions, slices, and progress.
- `ARM-11.4/` — legacy source retained without structural reorganization.
- `AGENTS.md` — repository working rules.

Generated output, machine-local settings, application settings, credentials, and the preserved legacy Git metadata are excluded from commits by the root `.gitignore`.

## Build and test

From `AxData/`, using already-restored dependencies:

```powershell
dotnet build --nologo -v q --no-restore
dotnet test --nologo -v q --no-restore
```

## Local configuration

Never commit connection strings or credentials. See `AxData/config/README.md` and copy the relevant example into `AxData/config/local/` for local-only use.

Cloud deployments must obtain secrets from the hosting platform's secret manager and expose them through environment variables.

## Commit and push workflow

- Keep each commit to one reviewed concern. Product work and imported ARM updates use separate commits.
- Codex prepares the change, runs the relevant no-restore build/tests, and reports the exact files and proposed
  commit message. The repository owner approves the boundary before Codex creates the local commit.
- The repository owner pushes by default. Codex pushes only when explicitly requested.
- Update `docs/PROGRESS.md` when a task finishes, `docs/CODEMAP.md` for new or moved types, and
  `docs/ARM-11.4-CHANGES.md` for every approved future change under `ARM-11.4/`.

The GitHub remote for this workspace is `https://github.com/nexoviacore/AxDataObject.git`. After the initial
commit is approved and created, publish it with:

```powershell
git push -u origin main
```

## Legacy ARM updates

The original nested Git metadata is preserved locally under the ignored `.legacy-git-metadata/` directory. This retains the legacy remotes, branches, and history without making `ARM-11.4/` an embedded repository.

Future updates should fetch into the preserved metadata, inspect the upstream diff, and then deliberately import
reviewed files or hunks into `ARM-11.4/`:

```powershell
git --git-dir=".legacy-git-metadata/ARM-11.4.git" fetch origin --prune
git --git-dir=".legacy-git-metadata/ARM-11.4.git" log --oneline Dev..origin/Dev
git --git-dir=".legacy-git-metadata/ARM-11.4.git" diff --name-status Dev..origin/Dev
git --git-dir=".legacy-git-metadata/ARM-11.4.git" --work-tree="ARM-11.4" status --short
```

Before importing, compare incoming changes with the local ARM working-tree differences. Import only the reviewed
scope, record future AxData-related ARM changes in `docs/ARM-11.4-CHANGES.md`, and run the affected build/tests.
Commit the reviewed import separately from AxData work. Do not run `git pull`, `checkout`, `reset`, or `merge`
through the preserved metadata against the parent working tree. Advance the preserved `Dev` baseline only after a
complete upstream release has been deliberately imported and verified; keep it unchanged for partial imports.

