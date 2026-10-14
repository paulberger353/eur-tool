using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using eur_tool.Transactiontypes;

namespace eur_tool.Views
{
    /// <summary>
    /// Tabellenansicht mit Einträgen, Filtern und Summen
    /// </summary>
    public partial class TabellenAnsicht : UserControl
    {
        //Deutsche Formatierung für Summen und Meldungen
        private static readonly CultureInfo de = CultureInfo.GetCultureInfo("de-DE");

        //Controller mit den Daten des gewählten Jahres
        private Controller? controller;

        //Wird ausgelöst, wenn ein aktiver Eintrag bearbeitet werden soll
        public event EventHandler<Buchung>? EditRequested;

        /// <summary>
        /// Konstruktor der Tabellenansicht
        /// Füllt die Filter und setzt die Standardsortierung
        /// </summary>
        public TabellenAnsicht()
        {
            InitializeComponent();

            //Filter füllen (die Events laufen hier noch ohne Controller ins Leere)
            cmbType.ItemsSource = new[] { "Alle", "Einnahmen", "Ausgaben" };
            cmbType.SelectedIndex = 0;
            cmbMonth.ItemsSource = new[] { "Ganzes Jahr" }.Concat(de.DateTimeFormat.MonthNames.Take(12)).ToList();
            cmbMonth.SelectedIndex = 0;

            //Standardsortierung nach Datum aufsteigend
            colDate.SortDirection = ListSortDirection.Ascending;
        }

        /// <summary>
        /// Methode wird nach der Jahresauswahl aufgerufen um die Ansicht mit dem Controller zu verbinden
        /// </summary>
        /// <param name="_controller">Controller mit den Daten des gewählten Jahres</param>
        public void Init(Controller _controller)
        {
            controller = _controller;
            Refresh();
        }

        /// <summary>
        /// Methode lädt Daten und Summen neu, Filter und Sortierung bleiben erhalten
        /// </summary>
        public void Refresh()
        {
            if (controller == null) return;

            int typeFilter = Math.Max(cmbType.SelectedIndex, 0);
            int month = Math.Max(cmbMonth.SelectedIndex, 0);
            bool includeInactive = chkInactive.IsChecked == true;

            //Historienspalten nur mit Häkchen anzeigen
            Visibility historyVisibility = includeInactive ? Visibility.Visible : Visibility.Collapsed;
            colId.Visibility = historyVisibility;
            colStatus.Visibility = historyVisibility;
            colCreatedAt.Visibility = historyVisibility;
            colDeactivatedAt.Visibility = historyVisibility;

            //Einträge laden, Sortierung der Spalte übernehmen
            var entries = controller.GetEntries(typeFilter, month, includeInactive);
            SetItemsKeepSort(entries);

            //Hinweis bei leerer Liste
            txtEmpty.Text = $"Keine Einträge für {PeriodName(month)}.";
            txtEmpty.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            //Summen hängen nur vom Monat ab
            var sums = controller.GetSums(month);
            runSumsText.Text = string.Format(de, "Summen {0}: Einnahmen {1:N2} € · Ausgaben {2:N2} € · Gewinn ", PeriodName(month), sums.einnahmen, sums.ausgaben);
            runSumsProfit.Text = string.Format(de, "{0:N2} €", sums.gewinn);
            runSumsCount.Text = sums.anzahl == 1 ? " · 1 Eintrag" : string.Format(de, " · {0} Einträge", sums.anzahl);
            SetProfitColor(runSumsProfit, sums.gewinn);

            UpdateButtons();
        }

        /// <summary>
        /// Methode gibt die ausgewählte Zeile zurück, wenn sie aktiv ist
        /// </summary>
        /// <returns>Aktiver Eintrag oder null</returns>
        private Buchung? SelectedActiveEntry()
        {
            return gridEntries.SelectedItem is Buchung entry && entry.isActive ? entry : null;
        }

        /// <summary>
        /// Methode gibt Bearbeiten und Löschen nur bei einer ausgewählten, aktiven Zeile frei
        /// </summary>
        private void UpdateButtons()
        {
            bool canEdit = SelectedActiveEntry() != null;
            btnEdit.IsEnabled = canEdit;
            btnDelete.IsEnabled = canEdit;
        }

        /// <summary>
        /// Methode fordert die Bearbeitung des ausgewählten, aktiven Eintrags an
        /// </summary>
        private void EditSelected()
        {
            Buchung? entry = SelectedActiveEntry();
            if (controller == null || entry == null) return;

            EditRequested?.Invoke(this, entry);
        }

        /// <summary>
        /// Methode löscht nach Rückfrage den ausgewählten, aktiven Eintrag (er wird inaktiv)
        /// </summary>
        private void DeleteSelected()
        {
            Buchung? entry = SelectedActiveEntry();
            if (controller == null || entry == null) return;

            string question = string.Format(de,
                "Eintrag wirklich löschen?\n\n{0:dd.MM.yyyy} · {1} · {2:N2} € · {3}\n\nEr bleibt als „Gelöscht“ in der Historie (Häkchen „Inaktive anzeigen“).",
                entry.date, entry.typeName, entry.amount, entry.description);

            if (MessageBox.Show(question, "Eintrag löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            controller.DeleteEntry(entry);
            Refresh();
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn sich die Auswahl in der Tabelle ändert
        /// </summary>
        private void gridEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null) return;

            UpdateButtons();
        }

        /// <summary>
        /// Methode wird bei Doppelklick aufgerufen und wirkt wie Bearbeiten (nur auf einer aktiven Zeile)
        /// </summary>
        private void gridEntries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (controller == null) return;

            //Nur auswerten, wenn der Doppelklick auf einer Zeile landet, nicht auf dem Spaltenkopf
            if (e.OriginalSource is not DependencyObject source ||
                ItemsControl.ContainerFromElement(gridEntries, source) is not DataGridRow row)
            {
                return;
            }

            if (row.Item is Buchung entry && entry.isActive)
            {
                gridEntries.SelectedItem = entry;
                EditSelected();
            }
        }

        /// <summary>
        /// Methode fängt die Taste Entf ab und wirkt wie Löschen
        /// </summary>
        private void gridEntries_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (controller == null) return;

            if (e.Key == Key.Delete)
            {
                e.Handled = true;
                DeleteSelected();
            }
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn "Bearbeiten" geklickt wird
        /// </summary>
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            EditSelected();
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn "Löschen" geklickt wird
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelected();
        }

        /// <summary>
        /// Methode setzt die Einträge und übernimmt die aktuelle Sortierung der Spalten
        /// </summary>
        /// <param name="entries">Anzuzeigende Einträge</param>
        private void SetItemsKeepSort(List<Buchung> entries)
        {
            DataGridColumn? sortColumn = gridEntries.Columns.FirstOrDefault(c => c.SortDirection != null);
            ListSortDirection? direction = sortColumn?.SortDirection;

            gridEntries.ItemsSource = entries;

            if (sortColumn != null && direction != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(gridEntries.ItemsSource);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(sortColumn.SortMemberPath, direction.Value));

                //Gleiche Werte stabil nach Datum, Nr. und Erfassungszeitpunkt ordnen
                view.SortDescriptions.Add(new SortDescription(nameof(Buchung.date), ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(nameof(Buchung.id), ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(nameof(Buchung.createdAt), ListSortDirection.Ascending));
                sortColumn.SortDirection = direction;
            }
        }

        /// <summary>
        /// Methode gibt den Namen des Zeitraums zurück, z. B. "März 2026" oder "2026"
        /// </summary>
        /// <param name="month">0 = ganzes Jahr, sonst 1 bis 12</param>
        /// <returns>Name des Zeitraums</returns>
        private string PeriodName(int month)
        {
            int year = controller?.selectedYear ?? DateTime.Today.Year;
            return month == 0 ? year.ToString(de) : $"{de.DateTimeFormat.GetMonthName(month)} {year}";
        }

        /// <summary>
        /// Methode färbt einen negativen Gewinn rot
        /// </summary>
        /// <param name="run">Textstück mit dem Gewinn</param>
        /// <param name="profit">Gewinn</param>
        private void SetProfitColor(System.Windows.Documents.Run run, decimal profit)
        {
            if (profit < 0)
            {
                run.Foreground = (Brush)FindResource("BrushError");
            }
            else
            {
                run.ClearValue(System.Windows.Documents.TextElement.ForegroundProperty);
            }
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn ein Filter geändert wird, und aktualisiert sofort
        /// </summary>
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            Refresh();
        }
    }
}
