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
using System.Windows.Shapes;

namespace MutationMode.UI
{
    /// <summary>
    /// Interaction logic for TraitsWindow.xaml
    /// </summary>
    public partial class TraitsWindow : Window
    {

        IEnumerable<BaseTraitSwap> items;
        List<TraitCheckListViewModel> traits;

        public List<TraitViewModel> SelectedTraits
        {
            get
            {
                if (this.traits == null)
                {
                    return new List<TraitViewModel>();
                }

                return this.traits
                    .Where(q => q.Checking)
                    .Select(q => q.Trait)
                    .ToList();
            }
        }

        private TraitsWindow()
        {
            InitializeComponent();
        }

        public TraitsWindow(IEnumerable<BaseTraitSwap> list)
            : this()
        {
            this.items = list;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.lstItems.ItemsSource = this.items;
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = (sender as ListView).SelectedItems;

            if (selectedItems != null)
            {
                this.traits = new List<TraitCheckListViewModel>();

                foreach (BaseTraitSwap selectedItem in selectedItems)
                {
                    this.traits.AddRange(selectedItem.OriginalTraits
                        .Select(q => new TraitCheckListViewModel()
                        {
                            Trait = q,
                            Checking = true,
                        }));
                }

                this.lstTraits.ItemsSource = this.traits;
            }
        }
    }



}
