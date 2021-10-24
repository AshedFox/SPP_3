using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AssemblyBrowser.Commands;
using AssemblyBrowserLib;
using Microsoft.Win32;

namespace AssemblyBrowser.ViewModels
{
    public class AssemblyViewModel: INotifyPropertyChanged
    {
        private RelayCommand _openAssemblyCommand;

        public ObservableCollection<AssemblyTreeNode> NamespaceNodes { get; set; } = new();

        public RelayCommand OpenAssembly => 
            _openAssemblyCommand ??= new RelayCommand(_ =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "dll|*.dll"
                };
                openFileDialog.ShowDialog();

                if (openFileDialog.FileName != string.Empty)
                {
                    var assemblyParser = new AssemblyParser();
                    var root = assemblyParser.Parse(openFileDialog.FileName);

                    NamespaceNodes = new ObservableCollection<AssemblyTreeNode>(root.ChildNodes);
                    
                    OnPropertyChanged(nameof(NamespaceNodes));
                }
            });

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}