# FastFlags in LXRStrap

## How they work
FastFlags are written to `ClientAppSettings.json` (in LXRStrap's Modifications folder) and copied into Roblox's `ClientSettings` folder at launch. Roblox reads only flags on its own **allowlist**; everything else is ignored, and the allowlist can change with any Roblox release. LXRStrap cannot see whether Roblox honoured a flag.

## What the explorer shows
Every flag carries a status:

| Status | Meaning |
|---|---|
| Allowlisted | Named in the allowlist as reported by community sources. |
| Uncertain | Reported as allowlisted, but exact behaviour or valid range isn't documented. |
| Not supported | Known not to be honoured. Shown for information, never written. |
| Managed | Controlled by LXRStrap itself (read-only). |
| Unverified | Not in the catalog (custom or inherited). May do nothing. |

Descriptions marked "Inferred" are reasoning from the flag's name and general behaviour, not confirmed facts. No benchmarks were run; LXRStrap makes no FPS claims.

## Sources and limits
The catalog was assembled from community documentation of Roblox's allowlist and the upstream Bloxstrap flag lists. Roblox's original DevForum announcement of the allowlist could not be fetched while building this, so the catalog has not been checked against it directly. No flag was invented; where details are unknown the entry says so. Check `LXR/Flags/FlagCatalog.cs` for each entry.

## Adding flags
Use **Add custom** on the FastFlags page. Names are validated for format only. Custom flags are labelled Unverified.
