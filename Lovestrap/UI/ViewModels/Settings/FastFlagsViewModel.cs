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
            set => App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
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
            set => App.FastFlags.SetPreset("Rendering.PauseVoxelizer", value ? "True" : null);
        }

        public bool DisableGrass
        {
            get => App.FastFlags.GetPreset("Rendering.DisableGrass.MaxDistance") == "0";
            set => App.FastFlags.SetPreset("Rendering.DisableGrass", value ? "0" : null);
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
