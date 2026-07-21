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
        // SavedQualityLevel is the user-facing quality level (1-10). Roblox derives
        // GraphicsQualityLevel (1-21) from it at runtime, so we only write this one.
        public int GraphicsQuality
        {
            get => Int32.TryParse(RobloxGlobalSettings.GetProperty("SavedQualityLevel"), out int v) ? Math.Clamp(v, 1, 10) : 1;
            set => RobloxGlobalSettings.SetProperty("token", "SavedQualityLevel", value.ToString());
        }

        // Framerate limit is Roblox's own FramerateCap setting (int) in this file.
        public string FramerateLimit
        {
            get => RobloxGlobalSettings.GetProperty("FramerateCap") ?? "";
            set
            {
                if (Int32.TryParse(value, out int v))
                    RobloxGlobalSettings.SetProperty("int", "FramerateCap", v.ToString());
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

        // PreferredTextSize is a numeric token (0 = default, 1/2/3 = progressively larger)
        public IReadOnlyList<string> FontSizes { get; } = new[] { "Default", "1x", "2x", "3x" };

        public string SelectedFontSize
        {
            get => RobloxGlobalSettings.GetProperty("PreferredTextSize") switch
            {
                "1" => "1x",
                "2" => "2x",
                "3" => "3x",
                _ => "Default"
            };
            set => RobloxGlobalSettings.SetProperty("token", "PreferredTextSize", value switch
            {
                "1x" => "1",
                "2x" => "2",
                "3x" => "3",
                _ => "0"
            });
        }

        // ---- Other ----
        // Roblox stores a MouseSensitivity float plus first/third-person Vector2s; set all three.
        public string MouseSensitivity
        {
            get => RobloxGlobalSettings.GetProperty("MouseSensitivity")
                   ?? RobloxGlobalSettings.GetVector2X("MouseSensitivityFirstPerson")?.ToString("0.0#######", CultureInfo.InvariantCulture)
                   ?? "";
            set
            {
                if (Single.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float v))
                {
                    RobloxGlobalSettings.SetProperty("float", "MouseSensitivity", v.ToString("0.0#######", CultureInfo.InvariantCulture));
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
