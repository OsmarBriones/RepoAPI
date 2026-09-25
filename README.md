# RepoAPI

Shared C# library for R.E.P.O. BepInEx mods: item lookup/spawning helpers, config plumbing, and other logic reused across multiple mods.

**This is not a standalone Thunderstore mod.** It has no `[BepInPlugin]` entry point and is never installed on its own — it is source code meant to be embedded into other mods.

## How mods consume this repo

Each consuming mod adds this repository as a git submodule and references the project directly, so edits here are picked up immediately by a normal build (no manual DLL copying):

```bash
git submodule add https://github.com/OsmarBriones/RepoAPI.git external/RepoAPI
```

```xml
<ItemGroup>
  <ProjectReference Include="external/RepoAPI/RepoAPI.csproj" />
</ItemGroup>
```

At publish time, the consuming mod's Release build merges `RepoAPI.dll` into its own DLL with ILRepack (`Internalize="true"`), so the shipped artifact is a single file with no separate RepoAPI dependency. See `METHODOLOGY.md` in the workspace root for the full convention.

## Contents

- `Items/` — item key lookup and weighted random selection (`ItemProvider`, `ItemKeysProvider`).
- `Game/` — reflection/enum helpers for reading game-side data.
- `ModConfig/` — generic config binding helpers.
- `Utils/` — small shared utilities.
- `ConfigurationController` — minimal config init/reload helper; call `ConfigurationController.Initialize(this.Config)` from the consuming mod's `Awake()`.
- `Patches/ReloadOnLevelStart` — Harmony patch on `EnemyDirector.Start` that reloads config at level start; picked up automatically by the consuming mod's `Harmony.PatchAll()`.

## Tests

`RepoAPI.Test/` is an MSTest project covering the pieces here in isolation (`dotnet test`).
