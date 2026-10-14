using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

            //Einträge laden, Sortierung der Spalte übernehmen
            var entries = controller.GetEntries(typeFilter, month, false);
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
