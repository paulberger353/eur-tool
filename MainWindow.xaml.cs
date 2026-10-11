using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace eur_tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller? controller;

        public MainWindow()
        {
            InitializeComponent();

            //Startscreen laden
            gridYearSelection.Visibility = Visibility.Visible;
            gridMainFrame.Visibility = Visibility.Hidden;
            cmbYear.ItemsSource = Enumerable.Range(2025, DateTime.Now.Year - 2024).Reverse();
            cmbYear.SelectedItem = DateTime.Now.Year;
        }

        #region Window Events

        //True, wenn ungespeicherte Daten vorhanden
        bool hasUnsavedChanges { get; set; } = false;

        /// <summary>
        /// Methode wird aufgerufen um das Fenster im Hero zu verschieben
        /// </summary>
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Methode wird aufgerufen um das Fenster zu minimieren
        /// </summary>
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Methode wird aufgerufen um das Fenster zu schließen
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //Wenn ungespeicherte Daten vorhanden sind, frag ab ob er wirklich schließen möchte
            if (hasUnsavedChanges)
            {
                if (MessageBox.Show("Ungespeicherte Daten vorhanden. Wirklich schließen?", "Achtung", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.Close();
        }

        #endregion

        #region Startscreen Events

        /// <summary>
        /// Methode wird nach der Jahr-Selektion aufgerufen, 
        /// um das EÜR-Tool zu starten
        /// </summary>
        private void btnConfirmYear_Click(object sender, RoutedEventArgs e)
        {
            //Prüfe ob Jahr ausgewählt ist
            if (cmbYear.SelectedItem is not int selectedYear)
            {
                return;
            }

            //Falls ja, mache MainFrame sichtbar und initialisiere Controller
            controller = new Controller(this, selectedYear);
            gridYearSelection.Visibility = Visibility.Collapsed;
            gridMainFrame.Visibility = Visibility.Visible;
        }

        #endregion
    }
}