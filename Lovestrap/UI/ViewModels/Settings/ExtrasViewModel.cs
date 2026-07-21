using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Lovestrap.UI.ViewModels.Settings
{
    public class ExtrasViewModel : NotifyPropertyChangedViewModel
    {
        public const string HanimeUrl = "https://hanime.tv";
        public const string SpotifyPlaylistUrl = "https://open.spotify.com/playlist/10ZZ8itYGR6DDHDaeQDHhs?si=0ef4953432e64c84";

        public ICommand OpenHanimeCommand => new RelayCommand(() => Utilities.ShellExecute(HanimeUrl));
        public ICommand OpenSpotifyCommand => new RelayCommand(() => Utilities.ShellExecute(SpotifyPlaylistUrl));
    }
}
