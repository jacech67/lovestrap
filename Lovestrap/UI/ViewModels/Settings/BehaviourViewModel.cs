namespace Lovestrap.UI.ViewModels.Settings
{
    public class BehaviourViewModel : NotifyPropertyChangedViewModel
    {
        public bool ConfirmLaunches
        {
            get => App.Settings.Prop.ConfirmLaunches;
            set => App.Settings.Prop.ConfirmLaunches = value;
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
        }

        public bool CloseCrashHandler
        {
            get => App.Settings.Prop.CloseRobloxCrashHandler;
            set => App.Settings.Prop.CloseRobloxCrashHandler = value;
        }

        public bool RobloxUpgrades
        {
            get => App.Settings.Prop.RobloxUpgradesEnabled;
            set => App.Settings.Prop.RobloxUpgradesEnabled = value;
        }

        public bool StaticRobloxDirectory
        {
            get => App.Settings.Prop.UseStaticRobloxVersionDirectory;
            set
            {
                App.Settings.Prop.UseStaticRobloxVersionDirectory = value;
                App.State.Prop.ForceReinstall = true;
                OnPropertyChanged(nameof(StaticRobloxDirectory));
                OnPropertyChanged(nameof(ForceRobloxReinstallation));
            }
        }

        public string RobloxChannel
        {
            get => App.Settings.Prop.RobloxChannel;
            set
            {
                string channel = value?.Trim().ToLowerInvariant() ?? "";
                App.Settings.Prop.RobloxChannel = Regex.IsMatch(channel, "^[a-z0-9_-]+$")
                    ? channel
                    : Lovestrap.RobloxInterfaces.Deployment.DefaultChannel;
            }
        }

        public string InstalledPlayerVersionGuid => String.IsNullOrEmpty(App.PlayerState.Prop.VersionGuid)
            ? "Not installed"
            : App.PlayerState.Prop.VersionGuid;

        public bool IsRobloxInstallationMissing => !App.IsPlayerInstalled && !App.IsStudioInstalled;

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }
    }
}
