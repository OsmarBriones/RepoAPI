# RepoAPI — Local Agent Context

RepoAPI is an independent shared C# library for R.E.P.O. mods, not an
installable Thunderstore mod. It has no `[BepInPlugin]` entry point. Consumers
add it as a git submodule and compile only the source modules they need into
their own DLLs; see `README.md` for the module dependency graph.

## Working rules

- Build the library with `dotnet build RepoAPI.csproj`; run its isolated tests
  with `dotnet test RepoAPI.Test/RepoAPI.Test.csproj` when relevant.
- Treat public API, module boundaries, and behavior as cross-project changes.
  Identify consuming mods, communicate required source includes, and update
  this README whenever the module graph or consumption procedure changes.
- Commit shared changes in this repository first. Consumers update their own
  submodule pointers separately; do not modify a consumer incidentally.
- Preserve existing local modifications. Do not reset, discard, or overwrite
  unrelated work.
- Maintain this file and other affected documentation with implementation.

Before the first edit of every task, synchronize
`external/RepoKit` once and follow its synchronization gate. Read
its `VERSION.md`, `REPO_MODS_WORKSPACE.md`, and
`REPO_MODS_METHODOLOGY.md`; do not repeat that check during the same task
unless shared guidance changes.
