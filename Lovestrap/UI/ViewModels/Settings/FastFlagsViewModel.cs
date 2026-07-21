using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Lovestrap.Enums.FlagPresets;

namespace Lovestrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;

        public event EventHandler? RequestPageReloadEvent;
        
        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        // Lovestrap: always expose the FastFlag editor (upstream hid it unless Roblox Studio was installed)
        public Visibility CanShowFastFlagEditor => Visibility.Visible;

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA")).Key;
            set
            {
                DisablePerformanceOptimizerForManualChange();
                App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
            }
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        public IReadOnlyDictionary<TextureQuality, string?> TextureQualities => FastFlagManager.TextureQualityLevels;

        public TextureQuality SelectedTextureQuality
        {
            get => TextureQualities.Where(x => x.Value == App.FastFlags.GetPreset("Rendering.TextureQuality.Level")).FirstOrDefault().Key;
            set
            {
                DisablePerformanceOptimizerForManualChange();

                if (value == TextureQuality.Default)
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                    App.FastFlags.SetPreset("Rendering.TextureQuality.Level", TextureQualities[value]);
                }
            }
        }
        public bool EnableFastFlagInjector
        {
            get => App.Settings.Prop.EnableFastFlagInjector;
            set
            {
                App.Settings.Prop.EnableFastFlagInjector = value;

                if (value)
                    App.FastFlagInjector.Start();
                else
                    App.FastFlagInjector.Stop();
            }
        }

        public IReadOnlyList<TextureMeshMode> TextureMeshModes => FastFlagManager.TextureMeshModes;

        public TextureMeshMode SelectedTextureMeshMode
        {
            get => App.Settings.Prop.TextureMeshMode;
            set
            {
                DisablePerformanceOptimizerForManualChange();

                if (value != TextureMeshMode.Normal && App.Settings.Prop.RtxMode)
                {
                    App.Settings.Prop.RtxMode = false;
                    App.FastFlags.SetRtxMode(false);
                    OnPropertyChanged(nameof(RtxMode));
                }

                App.Settings.Prop.TextureMeshMode = value;
                App.FastFlags.SetTextureMeshMode(value);
                OnPropertyChanged(nameof(SelectedTextureMeshMode));
                NotifyRenderingPresetChanged();
            }
        }

        public bool RtxMode
        {
            get => App.Settings.Prop.RtxMode;
            set
            {
                if (value)
                    DisablePerformanceOptimizerForManualChange();

                App.Settings.Prop.RtxMode = value;

                if (value)
                {
                    App.Settings.Prop.TextureMeshMode = TextureMeshMode.Normal;
                    App.FastFlags.SetTextureMeshMode(TextureMeshMode.Normal);
                }

                App.FastFlags.SetRtxMode(value);
                OnPropertyChanged(nameof(RtxMode));
                OnPropertyChanged(nameof(SelectedTextureMeshMode));
                NotifyRenderingPresetChanged();
            }
        }

        public bool PerformanceOptimizer
        {
            get => App.Settings.Prop.PerformanceOptimizer;
            set
            {
                if (value == App.Settings.Prop.PerformanceOptimizer)
                    return;

                if (value)
                {
                    App.Settings.Prop.PerformanceOptimizerPreviousRtxMode = App.Settings.Prop.RtxMode;
                    App.Settings.Prop.PerformanceOptimizerPreviousFastFlags = App.FastFlags.CapturePerformanceOptimizerSettings();
                    App.Settings.Prop.PerformanceOptimizerPreviousFramerateCap = RobloxGlobalSettings.GetProperty("FramerateCap");
                    App.Settings.Prop.PerformanceOptimizerPreviousGraphicsQuality = null;
                    App.Settings.Prop.PerformanceOptimizerPreviousReducedMotion = RobloxGlobalSettings.GetProperty("ReducedMotion");

                    // Performance and maximum-quality mode are mutually exclusive.
                    App.Settings.Prop.RtxMode = false;
                    App.Settings.Prop.PerformanceOptimizer = true;
                    App.FastFlags.SetPerformanceOptimizer(true);
                    RobloxGlobalSettings.SetProperty("int", "FramerateCap", "999");
                    RobloxGlobalSettings.SetProperty("bool", "ReducedMotion", "true");
                }
                else
                {
                    App.FastFlags.SetPerformanceOptimizer(false, App.Settings.Prop.PerformanceOptimizerPreviousFastFlags);
                    RestoreGlobalProperty("int", "FramerateCap", App.Settings.Prop.PerformanceOptimizerPreviousFramerateCap);
                    if (App.Settings.Prop.PerformanceOptimizerPreviousGraphicsQuality is not null)
                        RestoreGlobalProperty("token", "SavedQualityLevel", App.Settings.Prop.PerformanceOptimizerPreviousGraphicsQuality);
                    RestoreGlobalProperty("bool", "ReducedMotion", App.Settings.Prop.PerformanceOptimizerPreviousReducedMotion);

                    App.Settings.Prop.PerformanceOptimizer = false;
                    App.Settings.Prop.RtxMode = App.Settings.Prop.PerformanceOptimizerPreviousRtxMode ?? false;
                    App.Settings.Prop.PerformanceOptimizerPreviousRtxMode = null;
                    App.Settings.Prop.PerformanceOptimizerPreviousFastFlags = null;
                    App.Settings.Prop.PerformanceOptimizerPreviousFramerateCap = null;
                    App.Settings.Prop.PerformanceOptimizerPreviousGraphicsQuality = null;
                    App.Settings.Prop.PerformanceOptimizerPreviousReducedMotion = null;
                }

                OnPropertyChanged(nameof(PerformanceOptimizer));
                OnPropertyChanged(nameof(RtxMode));
                NotifyRenderingPresetChanged();
            }
        }

        private static void RestoreGlobalProperty(string elementType, string name, string? previousValue)
        {
            if (previousValue is null)
                RobloxGlobalSettings.RemoveProperty(name);
            else
                RobloxGlobalSettings.SetProperty(elementType, name, previousValue);
        }

        private void DisablePerformanceOptimizerForManualChange()
        {
            if (App.Settings.Prop.PerformanceOptimizer)
            {
                PerformanceOptimizer = false;

                // The manual value the user is about to choose supersedes a restored RTX preset.
                App.Settings.Prop.RtxMode = false;
                OnPropertyChanged(nameof(RtxMode));
            }
        }

        private void NotifyRenderingPresetChanged()
        {
            OnPropertyChanged(nameof(SelectedMSAALevel));
            OnPropertyChanged(nameof(SelectedTextureQuality));
            OnPropertyChanged(nameof(MeshDetailEnabled));
            OnPropertyChanged(nameof(MeshDetailLevel));
            OnPropertyChanged(nameof(FRMQualityEnabled));
            OnPropertyChanged(nameof(FRMQualityLevel));
            OnPropertyChanged(nameof(GraySky));
            OnPropertyChanged(nameof(PauseVoxelizer));
            OnPropertyChanged(nameof(DisableGrass));
        }

        // ---- Geometry: Mesh detail ----
        public bool MeshDetailEnabled
        {
            get => App.FastFlags.GetMeshDetailEnabled();
            set
            {
                App.FastFlags.SetMeshDetailEnabled(value);
                OnPropertyChanged(nameof(MeshDetailEnabled));
            }
        }

        public int MeshDetailLevel
        {
            get => App.FastFlags.GetMeshDetail();
            set => App.FastFlags.SetMeshDetail(value);
        }

        // ---- FRM quality override ----
        public bool FRMQualityEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.FRMQuality") is not null;
            set
            {
                DisablePerformanceOptimizerForManualChange();
                App.FastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.FRMQuality"], value ? FRMQualityLevel : (object?)null);
                OnPropertyChanged(nameof(FRMQualityEnabled));
            }
        }

        public int FRMQualityLevel
        {
            get
            {
                string? raw = App.FastFlags.GetPreset("Rendering.FRMQuality");
                return raw is not null && Int32.TryParse(raw, out int v) ? Math.Clamp(v, 1, 21) : 1;
            }
            set
            {
                DisablePerformanceOptimizerForManualChange();
                if (FRMQualityEnabled)
                    App.FastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.FRMQuality"], Math.Clamp(value, 1, 21));
            }
        }

        // ---- Rendering mode ----
        public IReadOnlyList<RenderingMode> RenderingModes { get; } = Enum.GetValues<RenderingMode>();

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetRenderingMode();
            set => App.FastFlags.SetRenderingMode(value);
        }

        // ---- simple rendering toggles ----
        public bool GraySky
        {
            get => App.FastFlags.GetPreset("Rendering.GraySky") == "True";
            set => App.FastFlags.SetPreset("Rendering.GraySky", value ? "True" : null);
        }

        public bool PauseVoxelizer
        {
            get => App.FastFlags.GetPreset("Rendering.PauseVoxelizer") == "True";
            set
            {
                DisablePerformanceOptimizerForManualChange();
                App.FastFlags.SetPreset("Rendering.PauseVoxelizer", value ? "True" : null);
            }
        }

        public bool DisableGrass
        {
            get => App.FastFlags.GetPreset("Rendering.DisableGrass.MaxDistance") == "0";
            set
            {
                DisablePerformanceOptimizerForManualChange();
                App.FastFlags.SetPreset("Rendering.DisableGrass", value ? "0" : null);
            }
        }

        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;

            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else
                {
                    App.FastFlags.Prop = _preResetFlags!;
                    _preResetFlags = null;
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
