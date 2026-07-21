using System.Globalization;

namespace Lovestrap.UI.ViewModels.Settings
{
    public class GlobalSettingsViewModel : NotifyPropertyChangedViewModel
    {
        // Prevent Roblox from overwriting the file
        public bool SetReadOnly
        {
            get => RobloxGlobalSettings.IsReadOnly;
            set => RobloxGlobalSettings.SetReadOnly(value);
        }

        // ---- Rendering and Graphics ----
        public int GraphicsQuality
        {
            get => Int32.TryParse(RobloxGlobalSettings.GetProperty("SavedQualityLevel"), out int v) ? Math.Clamp(v, 1, 10) : 1;
            set
            {
                RobloxGlobalSettings.SetProperty("token", "SavedQualityLevel", value.ToString());
                RobloxGlobalSettings.SetProperty("int", "GraphicsQualityLevel", value.ToString());
            }
        }

        // Framerate limit is the DFIntTaskSchedulerTargetFps fast flag
        public string FramerateLimit
        {
            get => App.FastFlags.GetValue("DFIntTaskSchedulerTargetFps") ?? "";
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    App.FastFlags.SetValue("DFIntTaskSchedulerTargetFps", null);
                else if (Int32.TryParse(value, out int v))
                    App.FastFlags.SetValue("DFIntTaskSchedulerTargetFps", v);
            }
        }

        // ---- User Interface and Layout ----
        public double Transparency
        {
            get => Double.TryParse(RobloxGlobalSettings.GetProperty("PreferredTransparency"), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 1;
            set => RobloxGlobalSettings.SetProperty("float", "PreferredTransparency", value.ToString("0.0###", CultureInfo.InvariantCulture));
        }

        public bool ReducedMotion
        {
            get => RobloxGlobalSettings.GetProperty("ReducedMotion") == "true";
            set => RobloxGlobalSettings.SetProperty("bool", "ReducedMotion", value ? "true" : "false");
        }

        public IReadOnlyList<string> FontSizes { get; } = new[] { "Default", "Large", "Larger", "Largest" };

        public string SelectedFontSize
        {
            get => RobloxGlobalSettings.GetProperty("PreferredTextSize") is string s && FontSizes.Contains(s) ? s : "Default";
            set => RobloxGlobalSettings.SetProperty("token", "PreferredTextSize", value);
        }

        // ---- Other ----
        public string MouseSensitivity
        {
            get => RobloxGlobalSettings.GetVector2X("MouseSensitivityFirstPerson")?.ToString("0.0#######", CultureInfo.InvariantCulture) ?? "";
            set
            {
                if (Single.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float v))
                {
                    RobloxGlobalSettings.SetVector2("MouseSensitivityFirstPerson", v);
                    RobloxGlobalSettings.SetVector2("MouseSensitivityThirdPerson", v);
                }
            }
        }

        public bool VREnabled
        {
            get => RobloxGlobalSettings.GetProperty("VREnabled") == "true";
            set => RobloxGlobalSettings.SetProperty("bool", "VREnabled", value ? "true" : "false");
        }
    }
}
