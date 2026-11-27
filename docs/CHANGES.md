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

## Stage 3: black redesign and extras
- True-black dark theme, brighter glass edges, hover rim glow and lift on cards, smoother eased animations.
- New monochrome liquid-glass orb logo and icon; animated orb pauses when its window is inactive, hidden or minimized.
- Roblox update log: records each Roblox install or update LXRStrap performs (version, time, channel); Home shows an unread banner.
- FastFlag profiles: save, apply, export and import named flag sets.
- Appearance: accent colour presets and custom colour, animation level (Full, Reduced, Off), interface size.
- Graphics tab lists every editable graphics-related flag in the catalog, marked when not fully verified.
- Backup and restore of settings, FastFlags and profiles; clear Roblox logs; schedule a clean Roblox reinstall.
- Not built: Robux purchase confirmation. Purchases happen inside Roblox; confirming them would mean intercepting or modifying the Roblox client, which LXRStrap does not do.

## Stage 4: new layout and motion
- Sidebar replaced by a floating glass dock at the top with a highlight that glides between pages.
- Home is now a bento layout: large hero with the orb and Play, plus a stack of quick tiles. The launch window is a centred vertical layout.
- Pages animate in with a staggered rise; windows fade in; hover glow and lift on cards; Animations setting still controls all of it.
- Orb animation pauses when its window is inactive, hidden, minimized, or Roblox is running (checked on focus/visibility changes, never on a timer).
- Optional "Higher Roblox priority" setting (off by default). It sets a normal Windows priority; LXRStrap does not claim FPS gains from it.

## Stage 5
- Cursor presets: LXR Glass, LXR Red and LXR Mono, each with arrow, text and shift-lock images (64x64, hotspot at the centre like Roblox's own cursors). The text and shift-lock slots are community-documented; if Roblox doesn't read them nothing changes.
- Red accent added. Cards and buttons get a mouse-following light and glass edge on hover.
- Smoothness: page transitions no longer animate the whole page and its blocks at once, moving blocks render from a GPU bitmap while they move, and the FastFlags page is built once instead of on every visit. This was not profiled on a real machine.
- Menus/textures: a button opens the installed Roblox textures folder so you can build your own mod pack. No file names (emote wheel etc.) are shipped because they could not be verified.

## Known limits
- Not compiled or run by the author yet (no Windows/.NET SDK available). First CI build is the first test.
- New UI is English only. Other locale files still contain old strings for removed screens.
- Tray menu windows (server info/history) still use the previous WPF UI look.

## Stage 6

- **Server location popup.** With "Look up server location" on, LXRStrap shows a normal Windows-style dialog ("Server location: ...") with an OK button when it sees you join a game. No tray clicking needed. If the lookup fails it stays quiet. The setting now works without restarting LXRStrap.
- **Modern cursor maker.** Customize > Cursors > "Make a modern cursor": pick White, Black, Red or Accent, pick a size (50% to 300%), press Create, then Apply. It draws a smooth, rounded Windows 11 style pointer, text beam and shift-lock ring. This is our own drawing in that style, not a copy of any specific third-party set. Not tested on a real Windows machine; whether Roblox uses the text-beam and shift-lock slots is unverified, the arrow is the sure one.

## Stage 7

- **Shift-lock cursor folder.** The shift-lock image now goes in Roblox's `textures` folder (not `Cursors`). Applying a pack also removes the misplaced copy older builds wrote.
- **Cursors redrawn** as tailless rounded Windows 11 style pointers plus a crosshair for shift lock, drawn at 4x and averaged down so edges stay smooth at every size.
- **Game Bar friendly shift-lock** (Customize > Cursors). Xbox Game Bar draws the cursor image shifted by half its size. A single image cannot be right for both Roblox (centre of the image) and Game Bar (corner of the image), so this option crops the shift-lock image as tight as possible to shrink the recording offset. It does not remove it. The page shows the expected offset in pixels.
- **Server location popup** now has a fallback trigger (20 s after the join started if the "joined" line never appears), an always-on-top window, and a **Test now** button in Settings that reads your newest Roblox log and shows the result.
- **New icon.** Black glass squircle with a white liquid bar crossing a red one (an X) and a drip.
