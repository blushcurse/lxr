namespace Bloxstrap.LXR.Flags
{
    /// <summary>
    /// The curated list of FastFlags LXRStrap knows about. Every entry is based on the flags that community
    /// documentation reports as being on Roblox's allowlist for local client configuration (see docs/FASTFLAGS.md).
    /// Nothing here is invented: where the purpose or range of a flag is not documented, the entry says so.
    /// </summary>
    public static class FlagCatalog
    {
        private const string AllowlistSource = "Roblox allowlist announcement, as summarised by community guides (Fishstrap flag guide, Voidstrap, iTechGuides).";
        private const string AppliedAtLaunch = "Written to ClientAppSettings.json and read by Roblox when it starts. Roblox can change its allowlist between releases, so re-check after big updates.";

        public static readonly string[] CategoryOrder =
        {
            "Graphics", "Lighting", "Rendering", "Performance", "Textures", "Interface", "Other"
        };

        public static readonly IReadOnlyList<FlagDefinition> All = Build();

        private static readonly Dictionary<string, FlagDefinition> _byName = All.ToDictionary(x => x.Name, StringComparer.Ordinal);

        public static FlagDefinition? Find(string name) => _byName.TryGetValue(name, out var def) ? def : null;

        public static FlagDefinition CreateCustom(string name) => new()
        {
            Name = name,
            Title = name,
            Kind = FlagValueKind.Text,
            Status = FlagStatus.Unverified,
            Categories = new[] { "Other" },
            Tags = new[] { "custom", "unverified" },
            Summary = "This flag is not in LXRStrap's verified list.",
            Effect = "Unknown. Roblox only honours flags on its allowlist and silently ignores the rest.",
            Performance = "Unknown.",
            Visual = "Unknown.",
            Compatibility = "Not verified. If Roblox ignores it nothing happens; LXRStrap cannot tell you whether it is working.",
            Confidence = "Unverified"
        };

        private static List<FlagDefinition> Build()
        {
            const string csgSummary = "Sets the distance at which CSG models (unions and negated parts) switch between levels of detail.";
            const string csgEffect = "Changing the distance changes when those models swap to a simpler or more detailed version.";
            const string csgPerf = "Inferred: switching to simpler models sooner usually costs less to draw; later switching costs more. Not benchmarked by LXRStrap.";
            const string csgVisual = "Inferred: distant unions may look blockier if they switch to simpler models sooner.";

            return new List<FlagDefinition>
            {
                new()
                {
                    Name = "FIntDebugForceMSAASamples", Title = "Anti-aliasing samples (MSAA)", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Rendering", "Performance" },
                    Tags = new[] { "msaa", "anti-aliasing", "antialiasing", "edges", "jagged", "quality", "fps" },
                    Summary = "Sets the number of MSAA (multisample anti-aliasing) samples used by the DirectX renderer.",
                    Effect = "Higher values smooth jagged edges more. 1 means one sample per pixel, which is effectively no multisampling.",
                    Performance = "Higher sample counts generally cost more GPU time and video memory; lower values can help weaker GPUs. Not benchmarked by LXRStrap.",
                    Visual = "Smoother edges on geometry at higher values.",
                    Compatibility = "Listed values are 1, 2, 4 and 8. " + AppliedAtLaunch,
                    Options = new[] { "1", "2", "4", "8" },
                    Confidence = "Documented values; performance notes are general MSAA behaviour",
                },
                new()
                {
                    Name = "DFIntTextureQualityOverride", Title = "Texture quality level", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Textures", "Graphics", "Performance" },
                    Tags = new[] { "texture", "textures", "resolution", "vram", "memory", "quality" },
                    Summary = "Sets the texture resolution: 0 = default, 1 = low, 2 = medium, 3 = maximum.",
                    Effect = "Forces the chosen texture resolution instead of letting Roblox pick it from your graphics setting. LXRStrap also turns on the matching override switch automatically.",
                    Performance = "Lower levels use less video memory; level 3 uses the most. Not benchmarked by LXRStrap.",
                    Visual = "Lower levels look blurrier up close; level 3 is the sharpest.",
                    Compatibility = AppliedAtLaunch,
                    Options = new[] { "0", "1", "2", "3" },
                    Companions = new() { ["DFFlagTextureQualityOverrideEnabled"] = "True" },
                },
                new()
                {
                    Name = "DFFlagTextureQualityOverrideEnabled", Title = "Texture quality override switch", Kind = FlagValueKind.Bool, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Textures", "Graphics" },
                    Tags = new[] { "texture", "textures", "override", "quality" },
                    Summary = "Turns on the manual texture resolution override. The level itself comes from the texture quality level flag.",
                    Effect = "On its own this does nothing useful; use the texture quality level setting, which sets both flags for you.",
                    Performance = "See the texture quality level flag.",
                    Visual = "See the texture quality level flag.",
                    Compatibility = AppliedAtLaunch,
                },
                new()
                {
                    Name = "DFIntCSGLevelOfDetailSwitchingDistance", Title = "CSG detail distance", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "csg", "lod", "level of detail", "union", "distance", "fps" },
                    Summary = csgSummary, Effect = csgEffect, Performance = csgPerf, Visual = csgVisual,
                    Compatibility = "Accepted range 0–10000 per community documentation. " + AppliedAtLaunch,
                    Min = 0, Max = 10000, Confidence = "Documented range; effects inferred",
                },
                new()
                {
                    Name = "DFIntCSGLevelOfDetailSwitchingDistanceL12", Title = "CSG detail distance (quality 1–2)", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "csg", "lod", "level of detail", "union", "distance", "fps" },
                    Summary = "Like the CSG detail distance, but for the switch between graphics quality levels 1 and 2.",
                    Effect = csgEffect, Performance = csgPerf, Visual = csgVisual,
                    Compatibility = "Accepted range 0–10000 per community documentation. " + AppliedAtLaunch,
                    Min = 0, Max = 10000, Confidence = "Documented range; effects inferred",
                },
                new()
                {
                    Name = "DFIntCSGLevelOfDetailSwitchingDistanceL23", Title = "CSG detail distance (quality 2–3)", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "csg", "lod", "level of detail", "union", "distance", "fps" },
                    Summary = "Like the CSG detail distance, but for the switch between graphics quality levels 2 and 3.",
                    Effect = csgEffect, Performance = csgPerf, Visual = csgVisual,
                    Compatibility = "Accepted range 0–10000 per community documentation. " + AppliedAtLaunch,
                    Min = 0, Max = 10000, Confidence = "Documented range; effects inferred",
                },
                new()
                {
                    Name = "DFIntCSGLevelOfDetailSwitchingDistanceL34", Title = "CSG detail distance (quality 3–4)", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "csg", "lod", "level of detail", "union", "distance", "fps" },
                    Summary = "Like the CSG detail distance, but for the switch between graphics quality levels 3 and 4.",
                    Effect = csgEffect, Performance = csgPerf, Visual = csgVisual,
                    Compatibility = "Accepted range 0–10000 per community documentation. " + AppliedAtLaunch,
                    Min = 0, Max = 10000, Confidence = "Documented range; effects inferred",
                },
                new()
                {
                    Name = "DFFlagDebugPauseVoxelizer", Title = "Pause voxel lighting", Kind = FlagValueKind.Bool, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Lighting", "Graphics", "Performance" },
                    Tags = new[] { "voxel", "voxelizer", "lighting", "light", "shadows", "fps" },
                    Summary = "Disables the voxel lighting calculations.",
                    Effect = "Lighting that depends on voxel data may look different or stop updating.",
                    Performance = "Inferred: skipping the calculation can reduce CPU/GPU work in voxel-lit places. Not benchmarked by LXRStrap.",
                    Visual = "Inferred: lighting can look flatter or stale.",
                    Compatibility = AppliedAtLaunch,
                    Confidence = "Documented purpose; effects inferred",
                },
                new()
                {
                    Name = "FFlagDebugSkyGray", Title = "Solid gray sky", Kind = FlagValueKind.Bool, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Lighting" },
                    Tags = new[] { "sky", "skybox", "stars", "gray", "grey" },
                    Summary = "Makes the skybox a solid gray and removes the stars.",
                    Effect = "The sky in every game becomes flat gray.",
                    Performance = "Not documented.",
                    Visual = "Flat gray sky, no stars or custom sky textures.",
                    Compatibility = AppliedAtLaunch,
                },
                new()
                {
                    Name = "FFlagDebugGraphicsPreferD3D11", Title = "Prefer DirectX 11", Kind = FlagValueKind.Bool, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Rendering", "Graphics" },
                    Tags = new[] { "directx", "d3d11", "dx11", "renderer", "api", "graphics api" },
                    Summary = "Asks Roblox to use the DirectX 11 graphics API.",
                    Effect = "Roblox renders through DirectX 11 when it can.",
                    Performance = "Depends on your GPU and drivers. Not benchmarked by LXRStrap.",
                    Visual = "Should look the same; this changes the API, not the settings.",
                    Compatibility = "Only one renderer flag should be on; LXRStrap turns the others off. " + AppliedAtLaunch,
                    ExclusiveGroup = "renderer",
                },
                new()
                {
                    Name = "FFlagDebugGraphicsPreferVulkan", Title = "Prefer Vulkan", Kind = FlagValueKind.Bool, Status = FlagStatus.Uncertain,
                    Categories = new[] { "Rendering", "Graphics" },
                    Tags = new[] { "vulkan", "renderer", "api", "graphics api" },
                    Summary = "Asks Roblox to use the Vulkan graphics API.",
                    Effect = "Roblox renders through Vulkan when it can.",
                    Performance = "Unknown. Depends on GPU, drivers and Roblox's Vulkan support.",
                    Visual = "Should look the same; may differ if the renderer is not fully supported.",
                    Compatibility = "On the allowlist, but whether it works well on your PC is not verified. If Roblox fails to start or looks wrong, reset this flag. Only one renderer flag should be on. " + AppliedAtLaunch,
                    ExclusiveGroup = "renderer",
                    Confidence = "Allowlisted; real-world behaviour unverified",
                },
                new()
                {
                    Name = "FFlagDebugGraphicsPreferOpenGL", Title = "Prefer OpenGL", Kind = FlagValueKind.Bool, Status = FlagStatus.Uncertain,
                    Categories = new[] { "Rendering", "Graphics" },
                    Tags = new[] { "opengl", "renderer", "api", "graphics api" },
                    Summary = "Asks Roblox to use the OpenGL graphics API.",
                    Effect = "Roblox renders through OpenGL when it can.",
                    Performance = "Unknown.",
                    Visual = "Unknown.",
                    Compatibility = "On the allowlist, but support on Windows is not verified and may vary per Roblox version. If anything goes wrong, reset this flag. Only one renderer flag should be on. " + AppliedAtLaunch,
                    ExclusiveGroup = "renderer",
                    Confidence = "Allowlisted; real-world behaviour unverified",
                },
                new()
                {
                    Name = "FFlagHandleAltEnterFullscreenManually", Title = "Manual Alt+Enter fullscreen", Kind = FlagValueKind.Bool, Status = FlagStatus.Managed,
                    Categories = new[] { "Interface" },
                    Tags = new[] { "fullscreen", "alt enter", "window", "display" },
                    Summary = "Lets the Roblox engine handle Alt+Enter fullscreen itself instead of Windows.",
                    Effect = "Controls how Alt+Enter switches between windowed and fullscreen.",
                    Performance = "Not documented.",
                    Visual = "Not documented.",
                    Compatibility = AppliedAtLaunch,
                    LockedReason = "LXRStrap keeps this flag set to False (inherited from Bloxstrap) so fullscreen behaves normally. It is restored automatically if changed.",
                },
                new()
                {
                    Name = "DFFlagDisableDPIScale", Title = "Disable Windows DPI scaling", Kind = FlagValueKind.Bool, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Interface" },
                    Tags = new[] { "dpi", "scaling", "scale", "display", "blurry", "ui", "window" },
                    Summary = "Disables Windows DPI scaling for the Roblox window.",
                    Effect = "Roblox ignores the Windows display scale. Useful if the window looks blurry or the wrong size at scaling above 100%.",
                    Performance = "Not documented.",
                    Visual = "The window may render smaller on a scaled display.",
                    Compatibility = AppliedAtLaunch,
                },
                new()
                {
                    Name = "FIntGrassMovementReducedMotionFactor", Title = "Grass movement amount", Kind = FlagValueKind.Int, Status = FlagStatus.Allowlisted,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "grass", "terrain", "motion", "animation", "wind", "fps" },
                    Summary = "Sets how much grass animates. 0 turns grass movement off, 100 is the largest value.",
                    Effect = "Lower values make terrain grass move less.",
                    Performance = "Inferred: less animation may reduce work. Not benchmarked by LXRStrap.",
                    Visual = "Grass looks still at 0.",
                    Compatibility = "Accepted range 0–100 per community documentation. " + AppliedAtLaunch,
                    Min = 0, Max = 100, Confidence = "Documented range; effects inferred",
                },
                new()
                {
                    Name = "FIntFRMMaxGrassDistance", Title = "Maximum grass distance", Kind = FlagValueKind.Int, Status = FlagStatus.Uncertain,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "grass", "terrain", "distance", "frm", "fps" },
                    Summary = "On the allowlist, but no official description or value range has been published.",
                    Effect = "By its name it likely limits how far grass is drawn. This is inferred, not confirmed.",
                    Performance = "Unknown.", Visual = "Unknown.",
                    Compatibility = "Value range unknown. Try small changes and check the result in-game; reset the flag if anything looks wrong. " + AppliedAtLaunch,
                    Confidence = "Allowlisted; purpose inferred from the name",
                },
                new()
                {
                    Name = "FIntFRMMinGrassDistance", Title = "Minimum grass distance", Kind = FlagValueKind.Int, Status = FlagStatus.Uncertain,
                    Categories = new[] { "Graphics", "Performance" },
                    Tags = new[] { "grass", "terrain", "distance", "frm", "fps" },
                    Summary = "On the allowlist, but no official description or value range has been published.",
                    Effect = "By its name it likely sets a lower bound for grass draw distance. This is inferred, not confirmed.",
                    Performance = "Unknown.", Visual = "Unknown.",
                    Compatibility = "Value range unknown. Try small changes and check the result in-game; reset the flag if anything looks wrong. " + AppliedAtLaunch,
                    Confidence = "Allowlisted; purpose inferred from the name",
                },
                new()
                {
                    Name = "DFIntDebugFRMQualityLevelOverride", Title = "Quality level override (FRM)", Kind = FlagValueKind.Int, Status = FlagStatus.Uncertain,
                    Categories = new[] { "Performance", "Graphics" },
                    Tags = new[] { "frm", "quality", "level", "override", "fps", "performance" },
                    Summary = "On the allowlist, but no official description or value range has been published.",
                    Effect = "By its name it likely overrides the automatic graphics quality level. This is inferred, not confirmed.",
                    Performance = "Unknown.", Visual = "Unknown.",
                    Compatibility = "Value range unknown. Try small changes and check the result in-game; reset the flag if anything looks wrong. " + AppliedAtLaunch,
                    Confidence = "Allowlisted; purpose inferred from the name",
                },

                // ---- Commonly shared flags that are NOT honoured. Listed so searches for "fps" etc. explain why. ----
                Unsupported("DFIntTaskSchedulerTargetFps", "FPS cap (task scheduler)", "Used to be the usual way to change the FPS cap.",
                    "Reported as removed from Roblox's allowlist, so the client ignores it. Use the frame rate setting inside Roblox instead.",
                    "fps", "framerate", "frame rate", "cap", "limit", "unlock"),
                Unsupported("FFlagTaskSchedulerLimitTargetFpsTo2402", "FPS limit (2402)", "Related to the old FPS cap flags.",
                    "Reported as removed from Roblox's allowlist.", "fps", "framerate", "cap", "limit"),
                Unsupported("FFlagGameBasicSettingsFramerateCap5", "Framerate cap setting", "Related to the old FPS cap flags.",
                    "Reported as removed from Roblox's allowlist.", "fps", "framerate", "cap", "limit"),
                Unsupported("FFlagDebugDisplayFPS", "Show FPS counter", "Used to show an FPS counter on screen.",
                    "Not on the current allowlist according to community guides, so it is ignored. Roblox has its own FPS display (Shift+F5).",
                    "fps", "counter", "display", "overlay"),
                Unsupported("FStringDebugGraphicsPreferredGPUName", "Preferred GPU name", "Used to choose a GPU by name.",
                    "Reported as blocked by Roblox.", "gpu", "graphics card", "renderer"),
                Unsupported("FLogNetwork", "Network logging", "Used for network debug logging.",
                    "Reported as blocked by Roblox.", "network", "networking", "latency", "ping", "logging"),
                Unsupported("FFlagMovePreRender", "Pre-render movement", "Sometimes shared as a latency tweak.",
                    "Reported as blocked by Roblox.", "latency", "input", "network", "networking"),
            };
        }

        private static FlagDefinition Unsupported(string name, string title, string summary, string reason, params string[] tags) => new()
        {
            Name = name,
            Title = title,
            Kind = FlagValueKind.Text,
            Status = FlagStatus.Unsupported,
            Categories = tags.Contains("network") ? new[] { "Other", "Performance" } : new[] { "Performance", "Other" },
            Tags = tags.Append("unsupported").Append("removed").ToArray(),
            Summary = summary,
            Effect = "None. " + reason,
            Performance = "No effect, because the client ignores the flag.",
            Visual = "No effect.",
            Compatibility = reason + " LXRStrap will not apply it.",
            Confidence = "Reported by community guides; the original Roblox post could not be fetched",
            LockedReason = reason + " LXRStrap will not apply it.",
        };
    }
}
