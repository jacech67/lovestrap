using System.Windows.Input;

using Lovestrap.UI.ViewModels.Settings;

namespace Lovestrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for GlobalSettingsPage.xaml
    /// </summary>
    public partial class GlobalSettingsPage
    {
        public GlobalSettingsPage()
        {
            DataContext = new GlobalSettingsViewModel();
            InitializeComponent();
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e) => e.Handled = !UInt32.TryParse(e.Text, out uint _);
    }
}
