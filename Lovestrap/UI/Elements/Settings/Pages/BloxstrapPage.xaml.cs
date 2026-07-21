using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lovestrap.UI.ViewModels.Settings;

namespace Lovestrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for LovestrapPage.xaml
    /// </summary>
    public partial class LovestrapPage
    {
        public LovestrapPage()
        {
            DataContext = new LovestrapViewModel();
            InitializeComponent();
        }
    }
}
