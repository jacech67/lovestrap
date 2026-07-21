using Lovestrap.Enums.FlagPresets;

namespace Lovestrap
{
    public class FastFlagManager : JsonManager<Dictionary<string, object>>
    {
        private Dictionary<string, object> OriginalProp = new();

        public override string ClassName => nameof(FastFlagManager);

        public override string LOG_IDENT_CLASS => ClassName;

        public override string FileName => "ClientAppSettings.json";

        public override string FileLocation => Path.Combine(Paths.Modifications, "ClientSettings", FileName);

        public bool Changed => !OriginalProp.SequenceEqual(Prop);

        public static IReadOnlyDictionary<string, string> PresetFlags = new Dictionary<string, string>
        {
            { "Rendering.ManualFullscreen", "FFlagHandleAltEnterFullscreenManually" },
            { "Rendering.DisableScaling", "DFFlagDisableDPIScale" },
            { "Rendering.MSAA", "FIntDebugForceMSAASamples" },

            { "Rendering.TextureQuality.OverrideEnabled", "DFFlagTextureQualityOverrideEnabled" },
            { "Rendering.TextureQuality.Level", "DFIntTextureQualityOverride" },
            // Mesh detail: CSG/mesh level-of-detail switching distances (lower = meshes lose detail / disappear)
            { "Rendering.MeshDetail.L0", "DFIntCSGLevelOfDetailSwitchingDistance" },
            { "Rendering.MeshDetail.L12", "DFIntCSGLevelOfDetailSwitchingDistanceL12" },
            { "Rendering.MeshDetail.L23", "DFIntCSGLevelOfDetailSwitchingDistanceL23" },
            { "Rendering.MeshDetail.L34", "DFIntCSGLevelOfDetailSwitchingDistanceL34" },

            // FRM (frame-rate manager) quality level override
            { "Rendering.FRMQuality", "DFIntDebugFRMQualityLevelOverride" },

            // rendering mode / graphics API preference
            { "Rendering.Mode.Vulkan", "FFlagDebugGraphicsPreferVulkan" },
            { "Rendering.Mode.D3D11", "FFlagDebugGraphicsPreferD3D11" },
            { "Rendering.Mode.OpenGL", "FFlagDebugGraphicsPreferOpenGL" },

            // simple on/off rendering presets
            { "Rendering.GraySky", "FFlagDebugSkyGray" },
            { "Rendering.PauseVoxelizer", "DFFlagDebugPauseVoxelizer" },
            { "Rendering.DisableGrass.MaxDistance", "FIntFRMMaxGrassDistance" },
            { "Rendering.DisableGrass.Detail", "FIntRenderGrassDetailStuds" },
        };

        // ---- Mesh detail (0-100 quality: 100 = full detail, 0 = meshes drop to lowest LOD / disappear) ----
        private static readonly Dictionary<string, int> MeshDetailBase = new()
        {
            { "Rendering.MeshDetail.L0", 125 },
            { "Rendering.MeshDetail.L12", 250 },
            { "Rendering.MeshDetail.L23", 500 },
            { "Rendering.MeshDetail.L34", 750 },
        };

        public const int MeshDetailMax = 100;

        public static IReadOnlyList<TextureMeshMode> TextureMeshModes { get; } = Enum.GetValues<TextureMeshMode>();

        // These older texture hacks are no longer accepted by the Roblox Player's local
        // FastFlag filter. Keep their names here so upgrades can remove stale values.
        private static readonly string[] ObsoleteTextureFlags =
        {
            "FIntDebugTextureManagerSkipMips",
            "DFIntPerformanceControlTextureQualityBestUtility",
            "DFIntTextureCompositorActiveJobs",
            "FIntTerrainArraySliceSize"
        };

        public TextureMeshMode GetTextureMeshMode()
        {
            if (GetPreset("Rendering.FRMQuality") == "1" &&
                MeshDetailBase.All(x => GetPreset(x.Key) == "0"))
                return TextureMeshMode.ZeroTextures;

            if (GetPreset("Rendering.TextureQuality.OverrideEnabled") == "True" &&
                GetPreset("Rendering.TextureQuality.Level") == "0")
                return TextureMeshMode.Blurry;

            return TextureMeshMode.Normal;
        }

        public void SetTextureMeshMode(TextureMeshMode mode)
        {
            // Clear every flag owned by this combined preset before applying the selected mode.
            foreach (var pair in MeshDetailBase)
                SetValue(PresetFlags[pair.Key], null);

            SetValue(PresetFlags["Rendering.FRMQuality"], null);
            SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], null);
            SetValue(PresetFlags["Rendering.TextureQuality.Level"], null);
            ClearObsoleteTextureFlags();

            if (mode == TextureMeshMode.Normal)
                return;

            SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], "True");
            SetValue(PresetFlags["Rendering.TextureQuality.Level"], "0");

            if (mode == TextureMeshMode.Blurry)
            {
                // Lowest supported texture quality while keeping recognizable geometry.
                SetValue(PresetFlags["Rendering.FRMQuality"], "6");
                SetValue(PresetFlags["Rendering.MeshDetail.L0"], "20");
                SetValue(PresetFlags["Rendering.MeshDetail.L12"], "10");
                SetValue(PresetFlags["Rendering.MeshDetail.L23"], "5");
                SetValue(PresetFlags["Rendering.MeshDetail.L34"], "0");
                return;
            }

            // Strongest downgrade currently accepted by the Roblox Player: minimum texture
            // quality, minimum frame-manager quality and immediate lowest mesh LOD.
            SetValue(PresetFlags["Rendering.FRMQuality"], "1");
            SetValue(PresetFlags["Rendering.MeshDetail.L0"], "0");
            SetValue(PresetFlags["Rendering.MeshDetail.L12"], "0");
            SetValue(PresetFlags["Rendering.MeshDetail.L23"], "0");
            SetValue(PresetFlags["Rendering.MeshDetail.L34"], "0");
        }

        /// <summary>
        /// Applies the highest-quality settings that Roblox currently permits through local
        /// client configuration. This is an RTX-like quality preset, not hardware ray tracing.
        /// </summary>
        public void SetRtxMode(bool enabled)
        {
            // Clear every setting owned by the quality preset first. This also makes disabling
            // the preset return these controls to Roblox defaults instead of leaving stale values.
            SetValue(PresetFlags["Rendering.MSAA"], null);
            SetValue(PresetFlags["Rendering.FRMQuality"], null);
            SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], null);
            SetValue(PresetFlags["Rendering.TextureQuality.Level"], null);
            SetValue(PresetFlags["Rendering.GraySky"], null);
            SetValue(PresetFlags["Rendering.PauseVoxelizer"], null);
            SetPreset("Rendering.DisableGrass", null);

            foreach (var pair in MeshDetailBase)
                SetValue(PresetFlags[pair.Key], null);

            ClearObsoleteTextureFlags();

            if (!enabled)
                return;

            // Roblox's highest local texture override, 4x MSAA, and maximum frame-manager
            // quality. Mesh LOD, grass, sky and voxel lighting remain at the game's defaults.
            SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], "True");
            SetValue(PresetFlags["Rendering.TextureQuality.Level"], "3");
            SetValue(PresetFlags["Rendering.MSAA"], "4");
            SetValue(PresetFlags["Rendering.FRMQuality"], "21");
        }

        public bool GetMeshDetailEnabled() => GetPreset("Rendering.MeshDetail.L12") is not null;

        public void SetMeshDetailEnabled(bool enabled)
        {
            if (enabled)
                SetMeshDetail(GetMeshDetail());        // materialise the flags at the current level
            else
            {
                foreach (var pair in MeshDetailBase)   // remove them
                    SetValue(PresetFlags[pair.Key], null);

                SetValue(PresetFlags["Rendering.FRMQuality"], null);
                SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], null);
                SetValue(PresetFlags["Rendering.TextureQuality.Level"], null);
                ClearObsoleteTextureFlags();
            }
        }

        public void SetMeshDetail(int quality)
        {
            quality = Math.Clamp(quality, 0, MeshDetailMax);
            double factor = quality / (double)MeshDetailMax;

            foreach (var pair in MeshDetailBase)
                SetValue(PresetFlags[pair.Key], (int)Math.Round(pair.Value * factor));

            if (quality >= MeshDetailMax)
            {
                // Full bar means Roblox defaults: no forced texture, mip or render downgrade.
                SetValue(PresetFlags["Rendering.FRMQuality"], null);
                SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], null);
                SetValue(PresetFlags["Rendering.TextureQuality.Level"], null);
                ClearObsoleteTextureFlags();
            }
            else
            {
                // FRM controls general MeshPart/geometry quality. 1 is Roblox's lowest level.
                int renderQuality = 1 + (int)Math.Round(factor * 19);
                SetValue(PresetFlags["Rendering.FRMQuality"], renderQuality);

                SetValue(PresetFlags["Rendering.TextureQuality.OverrideEnabled"], "True");
                SetValue(PresetFlags["Rendering.TextureQuality.Level"], "0");

                ClearObsoleteTextureFlags();
            }
        }

        private void ClearObsoleteTextureFlags()
        {
            foreach (string flag in ObsoleteTextureFlags)
                SetValue(flag, null);
        }

        private void ClearParserPoisoningIxpOverrides()
        {
            // Roblox 0.730 repeatedly reports a malformed filtered-settings document when a
            // local *_IXPValue override is present. Such keys are experiment assignments, not
            // ordinary player FastFlags, so remove them before writing ClientAppSettings.json.
            foreach (string key in Prop.Keys.Where(x => x.EndsWith("_IXPValue", StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                App.Logger.WriteLine("FastFlagManager::Compatibility", $"Removing incompatible local IXP override '{key}'");
                Prop.Remove(key);
            }
        }

        public int GetMeshDetail()
        {
            string? raw = GetPreset("Rendering.MeshDetail.L12");

            if (raw is null || !Int32.TryParse(raw, out int value))
                return MeshDetailMax;

            return Math.Clamp((int)Math.Round((double)value / MeshDetailBase["Rendering.MeshDetail.L12"] * MeshDetailMax), 0, MeshDetailMax);
        }

        // ---- Rendering mode (graphics API) ----
        public void SetRenderingMode(RenderingMode mode)
        {
            // clear all preference flags first
            SetValue(PresetFlags["Rendering.Mode.Vulkan"], null);
            SetValue(PresetFlags["Rendering.Mode.D3D11"], null);
            SetValue(PresetFlags["Rendering.Mode.OpenGL"], null);

            switch (mode)
            {
                case RenderingMode.Vulkan: SetValue(PresetFlags["Rendering.Mode.Vulkan"], "True"); break;
                case RenderingMode.D3D11:  SetValue(PresetFlags["Rendering.Mode.D3D11"], "True"); break;
                case RenderingMode.OpenGL: SetValue(PresetFlags["Rendering.Mode.OpenGL"], "True"); break;
            }
        }

        public RenderingMode GetRenderingMode()
        {
            if (GetPreset("Rendering.Mode.Vulkan") == "True") return RenderingMode.Vulkan;
            if (GetPreset("Rendering.Mode.D3D11")  == "True") return RenderingMode.D3D11;
            if (GetPreset("Rendering.Mode.OpenGL") == "True") return RenderingMode.OpenGL;
            return RenderingMode.Default;
        }

        public static IReadOnlyDictionary<MSAAMode, string?> MSAAModes => new Dictionary<MSAAMode, string?>
        {
            { MSAAMode.Default, null },
            { MSAAMode.x1, "1" },
            { MSAAMode.x2, "2" },
            { MSAAMode.x4, "4" }
        };

        public static IReadOnlyDictionary<TextureQuality, string?> TextureQualityLevels => new Dictionary<TextureQuality, string?>
        {
            { TextureQuality.Default, null },
            { TextureQuality.Level0, "0" },
            { TextureQuality.Level1, "1" },
            { TextureQuality.Level2, "2" },
            { TextureQuality.Level3, "3" },
        };

        // all fflags are stored as strings
        // to delete a flag, set the value as null
        public void SetValue(string key, object? value)
        {
            const string LOG_IDENT = "FastFlagManager::SetValue";

            if (value is null)
            {
                if (Prop.ContainsKey(key))
                    App.Logger.WriteLine(LOG_IDENT, $"Deletion of '{key}' is pending");

                Prop.Remove(key);
            }
            else
            {
                if (Prop.ContainsKey(key))
                {
                    if (key == Prop[key].ToString())
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"Changing of '{key}' from '{Prop[key]}' to '{value}' is pending");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Setting of '{key}' to '{value}' is pending");
                }

                Prop[key] = value.ToString()!;
            }
        }

        // this returns null if the fflag doesn't exist
        public string? GetValue(string key)
        {
            // check if we have an updated change for it pushed first
            if (Prop.TryGetValue(key, out object? value) && value is not null)
                return value.ToString();

            return null;
        }

        public void SetPreset(string prefix, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
                SetValue(pair.Value, value);
        }

        public void SetPresetEnum(string prefix, string target, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
            {
                if (pair.Key.StartsWith($"{prefix}.{target}"))
                    SetValue(pair.Value, value);
                else
                    SetValue(pair.Value, null);
            }
        }

        public string? GetPreset(string name)
        {
            if (!PresetFlags.ContainsKey(name))
            {
                App.Logger.WriteLine("FastFlagManager::GetPreset", $"Could not find preset {name}");
                Debug.Assert(false, $"Could not find preset {name}");
                return null;
            }

            return GetValue(PresetFlags[name]);
        }

        public T GetPresetEnum<T>(IReadOnlyDictionary<T, string> mapping, string prefix, string value) where T : Enum
        {
            foreach (var pair in mapping)
            {
                if (pair.Value == "None")
                    continue;

                if (GetPreset($"{prefix}.{pair.Value}") == value)
                    return pair.Key;
            }

            return mapping.First().Key;
        }

        public override void Save()
        {
            ClearParserPoisoningIxpOverrides();

            // convert all flag values to strings before saving

            foreach (var pair in Prop)
                Prop[pair.Key] = pair.Value.ToString()!;

            base.Save();

            DeployToInstalledPlayerVersions();

            // clone the dictionary
            OriginalProp = new(Prop);
        }

        /// <summary>
        /// Copies the saved FastFlag configuration into every installed Roblox Player version.
        /// Roblox reads most FastFlags at startup, but deployment itself happens immediately.
        /// </summary>
        public void DeployToInstalledPlayerVersions()
        {
            const string LOG_IDENT = "FastFlagManager::DeployToInstalledPlayerVersions";

            if (!File.Exists(FileLocation))
                return;

            var versionRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Paths.Versions,
                Path.Combine(Paths.LocalAppData, "Roblox", "Versions")
            };

            foreach (string versionsRoot in versionRoots.Where(Directory.Exists))
            {
                foreach (string versionDir in Directory.EnumerateDirectories(versionsRoot))
                {
                    try
                    {
                        if (!File.Exists(Path.Combine(versionDir, $"{App.RobloxPlayerAppName}.exe")))
                            continue;

                        string clientSettingsDir = Path.Combine(versionDir, "ClientSettings");
                        string destination = Path.Combine(clientSettingsDir, FileName);

                        Directory.CreateDirectory(clientSettingsDir);
                        Filesystem.AssertReadOnly(destination);
                        File.Copy(FileLocation, destination, true);

                        App.Logger.WriteLine(LOG_IDENT, $"Deployed FastFlags to '{versionDir}'");
                    }
                    catch (Exception ex)
                    {
                        // One stale or locked version directory must not prevent the others updating.
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }
            }
        }

        public override bool Load(bool alertFailure = true)
        {
            bool result = base.Load(alertFailure);

            ClearParserPoisoningIxpOverrides();

            // clone the dictionary
            OriginalProp = new(Prop);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");

            return result;
        }
    }
}
