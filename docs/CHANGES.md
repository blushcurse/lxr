# LXRStrap change log
- Identity: `ProjectName=LXRStrap`, assembly `LXRStrap.exe`, install dir `%LOCALAPPDATA%\LXRStrap`, uninstall key, shortcuts, mutex names all separate from Bloxstrap. `RootNamespace` kept as `Bloxstrap` on purpose (resource names, attribution).
- Privacy: see PRIVACY.md.
- Strings: English UI strings rebranded. Other locales still say "Bloxstrap" (to be addressed).
- Version reset to 1.0.0.
- Attribution: MIT license kept, LXRStrap modification line added; About > Licenses still credits Bloxstrap.
Roblox protocol handlers (`roblox://`) are registered by whichever launcher installed last; installing LXRStrap takes them over, uninstalling restores Roblox's.

## Stage 2: redesign and features
- New UI built from scratch (`Bloxstrap/LXR`): own design system, dark and light palettes, Mica backdrop on Windows 11 (solid fallback on Windows 10), live theme switching, shared motion.
- Replaced every window: launch/install/update window (real progress only), installer, main window (Home, FastFlags, Customization, Settings, About), message, error, connectivity and uninstall dialogs, add-flag dialog.
- FastFlags explorer with search, categories, detail panel, validation, autosave. Uncertain and unsupported flags are labelled.
- Cursor manager, graphics options (allowlisted flags only), mod packs with path and extension validation.
- Autosave: debounced and atomic, skips writes when nothing changed, recovers from a `.bak`.
- Removed: all old bootstrapper styles, custom-theme/bootstrapper editor, language selector, old settings pages, supporter/contributor pages, release-notes auto-open, upstream update check, telemetry.
- New app icon.

## Known limits
- Not compiled or run by the author yet (no Windows/.NET SDK available). First CI build is the first test.
- New UI is English only. Other locale files still contain old strings for removed screens.
- Tray menu windows (server info/history) still use the previous WPF UI look.
