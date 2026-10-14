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
        private Controller controller;

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

            //Falls ja, initialisiere Controller (lädt die Jahresdatei)
            try
            {
                controller = new Controller(this, selectedYear);
            }
            catch (Exception ex)
            {
                //Bei Fehlern auf der Jahresauswahl bleiben
                MessageBox.Show($"Daten für {selectedYear} konnten nicht geladen werden.\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Ansichten mit dem Controller verbinden
            maskeEinnahmen.Init(controller, false);
            maskeAusgaben.Init(controller, true);

            //Mache MainFrame sichtbar
            gridYearSelection.Visibility = Visibility.Collapsed;
            gridMainFrame.Visibility = Visibility.Visible;

            //Klicke Standartmäßig Einnahmen
            SetActiveSection(btnEinnahmen);

        }

        /// <summary>
        /// Methode wird aufgerufen, wenn ein Button in der Sidebar geklickt wird
        /// </summary>
        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                //Bei Export-Button direkt Datenexport starten
                if (button == btnExport)
                {
                    controller.ExportData();
                }
                else
                {
                    SetActiveSection(button);
                }
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um einzelnen Grids im MainFrame zu steuern
        /// </summary>
        /// <param name="activeButton">Button des jeweiligen Grids</param>
        private void SetActiveSection(Button activeButton)
        {
            gridEinnahmen.Visibility = activeButton == btnEinnahmen ? Visibility.Visible : Visibility.Collapsed;
            gridAusgaben.Visibility = activeButton == btnAusgaben ? Visibility.Visible : Visibility.Collapsed;
            gridTabelle.Visibility = activeButton == btnTabelle ? Visibility.Visible : Visibility.Collapsed;
            gridExport.Visibility = activeButton == btnExport ? Visibility.Visible : Visibility.Collapsed;
            gridPlaceholder.Visibility = activeButton == btnPlaceholder ? Visibility.Visible : Visibility.Collapsed;

            btnEinnahmen.Tag = activeButton == btnEinnahmen ? "Active" : null;
            btnAusgaben.Tag = activeButton == btnAusgaben ? "Active" : null;
            btnTabelle.Tag = activeButton == btnTabelle ? "Active" : null;
            btnExport.Tag = activeButton == btnExport ? "Active" : null;
            btnPlaceholder.Tag = activeButton == btnPlaceholder ? "Active" : null;
        }
    }

    #endregion

    #region MainFrame Events

    #endregion
}
