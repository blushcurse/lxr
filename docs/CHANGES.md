# LXRStrap change log (stage 1)
- Identity: `ProjectName=LXRStrap`, assembly `LXRStrap.exe`, install dir `%LOCALAPPDATA%\LXRStrap`, uninstall key, shortcuts, mutex names all separate from Bloxstrap. `RootNamespace` kept as `Bloxstrap` on purpose (resource names, attribution).
- Privacy: see PRIVACY.md.
- Strings: English UI strings rebranded. Other locales still say "Bloxstrap" (to be addressed).
- Version reset to 1.0.0.
- Attribution: MIT license kept, LXRStrap modification line added; About > Licenses still credits Bloxstrap.
Roblox protocol handlers (`roblox://`) are registered by whichever launcher installed last; installing LXRStrap takes them over, uninstalling restores Roblox's.
