using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using eur_tool.Transactiontypes;

namespace eur_tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Controller, wird nach der Jahresauswahl gesetzt
        private Controller? controller;

        public MainWindow()
        {
            InitializeComponent();

            //Startscreen laden
            gridYearSelection.Visibility = Visibility.Visible;
            gridMainFrame.Visibility = Visibility.Hidden;
            cmbYear.ItemsSource = Enumerable.Range(2025, DateTime.Now.Year - 2024).Reverse();
            cmbYear.SelectedItem = DateTime.Now.Year;

            //Ereignisse der Ansichten verbinden
            tabelle.EditRequested += tabelle_EditRequested;
            maskeEinnahmen.EditFinished += Maske_EditFinished;
            maskeAusgaben.EditFinished += Maske_EditFinished;
        }

        #region Window Events

        //True, wenn ungespeicherte Eingaben in einer Erfassungsmaske stehen
        bool hasUnsavedChanges => maskeEinnahmen.HasPendingInput || maskeAusgaben.HasPendingInput;

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
            //Rückfrage erfolgt in Window_Closing
            this.Close();
        }

        /// <summary>
        /// Methode wird vor dem Schließen aufgerufen (auch bei Alt+F4)
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            //Wenn ungespeicherte Eingaben vorhanden sind, frag ab ob er wirklich schließen möchte
            if (hasUnsavedChanges)
            {
                if (MessageBox.Show("Eingaben in der Erfassungsmaske sind noch nicht gespeichert. Wirklich schließen?", "Achtung",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
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
            Controller newController;
            try
            {
                newController = new Controller(this, selectedYear);
            }
            catch (Exception ex)
            {
                //Bei Fehlern auf der Jahresauswahl bleiben
                MessageBox.Show($"Daten für {selectedYear} konnten nicht geladen werden.\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            controller = newController;

            //Ansichten mit dem Controller verbinden
            maskeEinnahmen.Init(controller, false);
            maskeAusgaben.Init(controller, true);
            tabelle.Init(controller);

            //Mache MainFrame sichtbar
            gridYearSelection.Visibility = Visibility.Collapsed;
            gridMainFrame.Visibility = Visibility.Visible;

            //Klicke Standartmäßig Einnahmen
            SetActiveSection(btnEinnahmen);

        }

        #endregion

        #region MainFrame Events

        /// <summary>
        /// Methode wird aufgerufen, wenn ein Button in der Sidebar geklickt wird
        /// </summary>
        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

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
            //Bearbeitung in einer verlassenen Maske ohne Rückfrage abbrechen
            if (activeButton != btnEinnahmen)
            {
                maskeEinnahmen.CancelEdit();
            }

            if (activeButton != btnAusgaben)
            {
                maskeAusgaben.CancelEdit();
            }

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

            //Tabelle beim Aktivieren neu laden
            if (activeButton == btnTabelle)
            {
                tabelle.Refresh();
            }
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn in der Tabelle ein Eintrag bearbeitet werden soll
        /// Erst die Maske aktivieren, dann die Bearbeitung starten (SetActiveSection bricht Bearbeitungen ab)
        /// </summary>
        private void tabelle_EditRequested(object? sender, Buchung entry)
        {
            if (entry is Einnahmen)
            {
                SetActiveSection(btnEinnahmen);
                maskeEinnahmen.StartEdit(entry);
            }
            else if (entry is Ausgaben)
            {
                SetActiveSection(btnAusgaben);
                maskeAusgaben.StartEdit(entry);
            }
        }

        /// <summary>
        /// Methode wird nach "Änderung speichern" oder "Abbrechen" in einer Maske aufgerufen
        /// </summary>
        private void Maske_EditFinished(object? sender, EventArgs e)
        {
            SetActiveSection(btnTabelle);
        }

        #endregion
    }
}
