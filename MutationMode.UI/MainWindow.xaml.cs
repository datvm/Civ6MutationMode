using MutationMode.UI.Models;
using MutationMode.UI.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MutationMode.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        MainViewModel model;

        public MainWindow()
        {
            InitializeComponent();

            this.model = new MainViewModel();
            this.model.OperationRequested += this.OnOperationRequested;
        }

        private void OnOperationRequested(object sender, RequestOperationEventArgs e)
        {
            switch (e.Operation)
            {
                case Operation.RequestCacheLocation:
                    this.RequestCacheLocation();
                    break;
                default:
                    break;
            }
        }

        private void RequestCacheLocation()
        {
            MessageBox.Show("Cache files not found. Usually they should be in " +
                @"""Documents\my games\Sid Meier's Civilization VI\Cache"" and has " +
                @"""DebugGameplay.sqlite"" and ""DebugLocalization.sqlite"" files. " +
                "If you have moved it, please select the folder now.",
                this.Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);

            var diagFolder = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = AppOptions.DefaultCacheLocation,
                Description = "Select game Cache folder",
            };

            if (diagFolder.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                this.Close();
                return;
            }

            this.model.SetCacheLocationOverride(diagFolder.SelectedPath);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.model.Dispose();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.model.InitializeAsync();

            this.DataContext = this.model;
        }

        private void SelectItem(object sender)
        {
            this.model.SelectItem((sender as ListView).SelectedItem as BaseTraitSwap);
        }

        private void OnItemChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectItem(sender);
        }

        private void OnItemListGotFocus(object sender, RoutedEventArgs e)
        {
            this.SelectItem(sender);
        }

        private void OnAddTraitsButtonClick(object sender, RoutedEventArgs e)
        {
            var traitsWindow = new TraitsWindow(this.model.GetItemTraitListForSelectingItem());

            if (traitsWindow.ShowDialog() != true)
            {
                return;
            }

            this.model.AddTraitsToSelectingItem(traitsWindow.SelectedTraits);
        }

        private void OnRemoveSelectedTraitsButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.RemoveSelectedTraits();
        }

        private void OnAddOriginalButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.AddOriginalToSelecting();
        }

        private void OnSelectAllButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.SetSelectAllChanges(true);
        }

        private void OnSelectNoneButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.SetSelectAllChanges(false);
        }

        private void OnInvertSelectButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.SetSelectInvert();
        }
    }
}
