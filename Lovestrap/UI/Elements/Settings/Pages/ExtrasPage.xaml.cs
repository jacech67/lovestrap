using Lovestrap.UI.ViewModels.Settings;

namespace Lovestrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ExtrasPage.xaml
    /// </summary>
    public partial class ExtrasPage
    {
        public ExtrasPage()
        {
            DataContext = new ExtrasViewModel();
            InitializeComponent();
        }
    }
}
