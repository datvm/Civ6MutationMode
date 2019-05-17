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
    /// Interaction logic for RandomOptionsWindow.xaml
    /// </summary>
    public partial class RandomOptionsWindow : Window
    {

        public bool RandomizeCiv { get; private set; }
        public bool RandomizeLeaders { get; private set; }
        public bool KeepOriginal { get; private set; }
        public int Seed { get; private set; }

        public RandomOptionsWindow()
        {
            InitializeComponent();
        }

        private void OnGenerateNewSeedButtonClick(object sender, RoutedEventArgs e)
        {
            this.txtSeed.Text = new Random().Next().ToString();
        }

        private void OnRandomizeButtonClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(this.txtSeed.Text, out var seed))
            {
                MessageBox.Show("Invalid seed number, please enter a positive integer smaller than 2 billion.", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.RandomizeCiv = this.chkRandomizeCivs.IsChecked == true;
            this.RandomizeLeaders = this.chkRandomizeLeaders.IsChecked == true;
            this.KeepOriginal = this.chkKeepOriginalAbilities.IsChecked == true;
            this.Seed = seed;

            this.DialogResult = true;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.OnGenerateNewSeedButtonClick(null, null);
        }
    }
}
