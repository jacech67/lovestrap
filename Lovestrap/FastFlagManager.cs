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
            { "Rendering.MeshDetail.L12", 250 },
            { "Rendering.MeshDetail.L23", 500 },
            { "Rendering.MeshDetail.L34", 750 },
        };

        public const int MeshDetailMax = 100;

        public bool GetMeshDetailEnabled() => GetPreset("Rendering.MeshDetail.L12") is not null;

        public void SetMeshDetailEnabled(bool enabled)
        {
            if (enabled)
                SetMeshDetail(GetMeshDetail());        // materialise the flags at the current level
            else
                foreach (var pair in MeshDetailBase)   // remove them
                    SetValue(PresetFlags[pair.Key], null);
        }

        public void SetMeshDetail(int quality)
        {
            quality = Math.Clamp(quality, 0, MeshDetailMax);
            double factor = quality / (double)MeshDetailMax;

            foreach (var pair in MeshDetailBase)
                SetValue(PresetFlags[pair.Key], (int)Math.Round(pair.Value * factor));
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
            // convert all flag values to strings before saving

            foreach (var pair in Prop)
                Prop[pair.Key] = pair.Value.ToString()!;

            base.Save();

            // clone the dictionary
            OriginalProp = new(Prop);
        }

        public override bool Load(bool alertFailure = true)
        {
            bool result = base.Load(alertFailure);

            // clone the dictionary
            OriginalProp = new(Prop);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");

            return result;
        }
    }
}
