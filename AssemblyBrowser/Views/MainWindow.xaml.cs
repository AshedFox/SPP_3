using System.Windows;
using AssemblyBrowser.ViewModels;

namespace AssemblyBrowser.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new AssemblyViewModel();
            InitializeComponent();
        }
    }
}