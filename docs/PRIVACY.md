# LXRStrap privacy notes

LXRStrap is derived from Bloxstrap 2.11.4 (commit 02db760). Everything below was checked against that source.

## Removed
- `App.SendStat` -> `GET https://services.bloxstraplabs.com/metrics/post` (install/uninstall/upgrade, Roblox channel, download failures)
- `App.SendLog` -> `POST .../metrics/post-exception` (full log upload on crash)
- `EnableAnalytics` setting, installer opt-out toggle, settings toggle, `WebEnvironment` developer option
- Upstream auto-update: LXRStrap does not check or download from the Bloxstrap repo. `App.UpdateRepository` is empty, so update checks are disabled until you set it to your own GitHub `owner/repo` (releases must contain `LXRStrap.exe`).
- Upstream legacy migrations (they touched upstream Bloxstrap Start Menu/registry paths).

## Still present (required for function)
- Roblox deployment/version/download hosts (`clientsettings*.roblox.com`, `setup*.rbxcdn.com`, `roblox-setup.cachefly.net`, S3 setup bucket): install and update Roblox.
- Roblox user/game/thumbnail APIs: used only by the optional activity/Discord features.

## Optional, still present (off unless you enable the feature)
- `ipinfo.io` (server location in game join notifications; sends the game server IP, not yours).
- Discord Rich Presence.
- `raw.githubusercontent.com` / `github.com` downloads (supporter images, emoji font) are fetched only when the matching page/feature is used.

Logs stay local in `%LOCALAPPDATA%\LXRStrap\Logs`.
