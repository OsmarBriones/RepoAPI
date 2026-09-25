# RepoAPI

Shared C# library for R.E.P.O. BepInEx mods: item lookup/spawning helpers, config plumbing, and other logic reused across multiple mods.

**This is not a standalone Thunderstore mod.** It has no `[BepInPlugin]` entry point and is never installed on its own — it is source code meant to be embedded into other mods.

## How mods consume this repo

Each consuming mod adds this repository as a git submodule, then compiles in only the source files it actually needs with `<Compile Include>` — not a project/assembly reference. There is no `RepoAPI.dll`: the code becomes part of the consuming mod's own DLL directly, and a normal rebuild picks up edits made here immediately (no manual DLL copying, no merge step).

```bash
git submodule add https://github.com/OsmarBriones/RepoAPI.git external/RepoAPI
```

```xml
<ItemGroup>
  <Compile Remove="external\**" />
  <EmbeddedResource Remove="external\**" />
  <None Remove="external\**" />

  <!-- pick only the module(s) you need, see "Contents" for the dependency graph -->
  <Compile Include="external\RepoAPI\Items\**\*.cs" />
  <Compile Include="external\RepoAPI\Game\**\*.cs" />
</ItemGroup>
```

See `METHODOLOGY.md` in the workspace root for the full convention, and `EnemyDrops/CLAUDE.md` for a real example that includes only `Items/ItemProvider.cs` + `Items/ItemKeysProvider.cs` + `Items/ItemName.cs` + `Game/**` (not the whole `Items/` folder, since `Item.cs`/`ItemNames.cs` pull in `Utils/` and aren't needed there).

## Contents

Modules are folders. Some depend on others — the compiler will error clearly on a missing type if you forget to include a dependency:

| Module | Provides | Depends on |
|---|---|---|
| `Items/` | `ItemProvider` (spawning), `ItemKeysProvider` (uniform/weighted random key selection), `ItemName` enum, `Item` | `Game/` |
| `Game/` | `GameKeyAttribute`, `GameKeyEnumExtensions` — reflection helpers mapping enum members to game key strings | — |
| `ModConfig/` | Generic config binding helpers | `Items/` (→ `Game/`) |
| `Utils/` | Small shared utilities (`EnumUtils`) | — |
| `ConfigurationController` (repo root) | Minimal config init/reload helper; call `Initialize(this.Config)` from the consuming mod's `Awake()` | — |
| `Patches/ReloadOnLevelStart` | Harmony patch on `EnemyDirector.Start` that reloads config at level start; picked up automatically by the consuming mod's `Harmony.PatchAll()` | `ConfigurationController` |

## Tests

`RepoAPI.Test/` is an MSTest project covering the pieces here in isolation (`dotnet test`).
