# LXRStrap

A lightweight Roblox launcher for Windows with a FastFlags explorer, cursor and mod management, and a fully redesigned "liquid glass" interface (separate dark and light themes, system option, live switching).

LXRStrap is a modified version of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) by pizzaboxer and contributors, used under the MIT licence (see `LICENSE`). It is **not affiliated with Roblox Corporation or the Bloxstrap project**. It never modifies Roblox executables or bypasses Roblox protections.

## What's in it

- **Home**: launch Roblox or Studio, see what's active.
- **FastFlags**: searchable explorer with categories, a detail panel per flag (current/default value, effect, performance and visual notes, compatibility, how sure we are), toggles, reset, restore defaults, custom flags clearly marked unverified. See [docs/FASTFLAGS.md](docs/FASTFLAGS.md).
- **Customization**: cursor manager (drag and drop PNG/CUR/ICO/folder/ZIP, validation, preview, apply, backup and restore), graphics options built only from allowlisted flags, mod packs, a few classic presets and a custom font.
- **Settings**: General, Appearance, Performance, Privacy, Updates, Advanced. Everything autosaves (debounced, atomic writes). There are no Save buttons.
- **About**: version, diagnostics, credits and licence.

No telemetry. The network requests that remain are listed in [docs/PRIVACY.md](docs/PRIVACY.md).

## Build

Requires Windows and the .NET 6 SDK (see `global.json`).

```powershell
git clone --recurse-submodules https://github.com/blushcurse/lxr
cd lxr
dotnet restore
dotnet build -c Release --no-restore
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained false .\Bloxstrap\Bloxstrap.csproj
```

Output: `Bloxstrap\bin\Release\net6.0-windows\win-x64\publish\LXRStrap.exe`. The .NET 6 Desktop Runtime must be installed on the target machine.

The **CI (Release)** GitHub Actions workflow runs the same build on every push and uploads the published folder as an artifact.

## Status

The redesign was written without access to a Windows machine or the .NET SDK, so it has not yet been compiled or run by its author. Treat the first CI build as the first real test and report compile or runtime problems as issues. See [docs/CHANGES.md](docs/CHANGES.md) for what changed and what is known to be untested.
