using Microsoft.Win32;
using MutationMode.UI.Models;
using MutationMode.UI.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                case Operation.RequestModLocation:
                    this.RequestModLocation();
                    break;
                case Operation.AnnounceModAutoLocation:
                    MessageBox.Show($"We detected mod location at: {e.Params[0]}. If it's incorrect, please change it in the settings.", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case Operation.ModFolderNotWritable:
                    MessageBox.Show($"We detected the mod location at {e.Params[1]} but cannot write file there. We probably do not have the permission. If it's in Program Files folder, you may need" +
                        $"to run the app with Administrator permission. Error:\r\n{e.Params[0]}", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case Operation.ImportProfileWarning:
                    MessageBox.Show("Import Profile completed with some warnings:\r\n" + e.Params[0], this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);

                    break;
                default:
                    throw new InvalidOperationException("Unknown Operation: " + e.Operation);
            }
        }

        private void RequestModLocation()
        {
            var needHelp = MessageBox.Show("Hey we cannot find the mod folder location. It's likely you didn't install the game at default location. " +
                "We need you to give us the install location. Do you need help finding the location?", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (needHelp == MessageBoxResult.Yes)
            {
                Process.Start("https://github.com/datvm/Civ6MutationMode/wiki/Locate-Mod-folder-location");
            }

            var diagFolder = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = AppOptions.DefaultModLocation,
                Description = "Select Mutation Mod folder",
            };

            if (diagFolder.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                this.Close();
                return;
            }

            this.model.SetModLocationOverride(diagFolder.SelectedPath);
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

        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
        {
            this.model.ApplyModData();
        }

        private void OnSaveProfileButtonClick(object sender, RoutedEventArgs e)
        {
            var diagSave = new SaveFileDialog()
            {
                Title = "Choose a profile name to Export",
                Filter = "Mutation Profile|*.mpf",
                FileName = "profile.mpf",
            };

            if (diagSave.ShowDialog() != true)
            {
                return;
            }

            this.model.ExportProfile(diagSave.FileName);
        }

        private void OnOpenProfileButtonClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("All current changes will be overriden. Make sure you save it if you want first. Continue?", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            var diagOpen = new OpenFileDialog()
            {
                Title = "Choose profile to import",
                Filter = "Mutation Profile|*.mpf",
            };

            if (diagOpen.ShowDialog() != true)
            {
                return;
            }

            this.model.ImportProfile(diagOpen.FileName);
        }

        private void RemoveAllCivChanges(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to delete all ability changes to Civilizations?", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            this.model.RemoveAllCivChanges();
        }

        private void RemoveAllLeaderChanges(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to delete all ability changes to Leaders?", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            this.model.RemoveAllLeaderChanges();
        }

        private void OnRevertButtonClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This will revert the mod file to default so you can use the mod without this app. It will randomize the leader abilities whenever you start the game - similar to v1. " +
                "You can always use this app and choose 'Apply to Mod' later. Continue?", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            this.model.RevertToDefault();
        }
    }
}
