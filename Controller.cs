using System.Globalization;
using System.Windows;
using eur_tool.Transactiontypes;

namespace eur_tool
{
    //Summen eines Monats für die Monatsübersicht
    public record MonthSum(string month, decimal einnahmen, decimal ausgaben, decimal gewinn);

    public class Controller
    {
        //Instanzen von MainWindow und der Datenklasse
        MainWindow mw;
        Datastorage data;

        //Gewähltes Jahr für die EÜR
        public int selectedYear { get; private set; }

        /// <summary>
        /// Konstruktur der Controller-Klasse
        /// Setzt lokal Werte und initialisiert MainFrame des Programms
        /// </summary>
        /// <param name="_mw">Instanz des MainWindows</param>
        /// <param name="_selectedYear">Ausgewähltes Jahr für die EÜR</param>
        public Controller(MainWindow _mw, int _selectedYear)
        {
            mw = _mw;
            selectedYear = _selectedYear;

            data = new Datastorage(selectedYear);
        }

        /// <summary>
        /// Startet den Datenexport.
        /// </summary>
        public void ExportData()
        {
            data.ExportData();
        }

        /// <summary>
        /// Methode wird aufgerufen um eine neue Buchung zu speichern
        /// </summary>
        /// <param name="entry">Neue Buchung</param>
        /// <returns>True, wenn gespeichert</returns>
        public bool AddEntry(Buchung entry)
        {
            try
            {
                data.Add(entry);
                return true;
            }
            catch (Exception ex)
            {
                ShowSaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um eine Buchung durch eine geänderte Version zu ersetzen
        /// </summary>
        /// <param name="oldEntry">Bisherige Version</param>
        /// <param name="newEntry">Neue Version</param>
        /// <returns>True, wenn gespeichert</returns>
        public bool ReplaceEntry(Buchung oldEntry, Buchung newEntry)
        {
            try
            {
                data.Replace(oldEntry, newEntry);
                return true;
            }
            catch (Exception ex)
            {
                ShowSaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um eine Buchung zu löschen (inaktiv setzen)
        /// </summary>
        /// <param name="entry">Zu löschende Buchung</param>
        /// <returns>True, wenn gespeichert</returns>
        public bool DeleteEntry(Buchung entry)
        {
            try
            {
                data.Remove(entry);
                return true;
            }
            catch (Exception ex)
            {
                ShowSaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// Methode gibt die gefilterten Buchungen zurück
        /// </summary>
        /// <param name="typeFilter">0 = alle, 1 = Einnahmen, 2 = Ausgaben</param>
        /// <param name="month">0 = ganzes Jahr, sonst 1 bis 12</param>
        /// <param name="includeInactive">True, wenn auch inaktive Zeilen geliefert werden sollen</param>
        /// <returns>Liste der Buchungen</returns>
        public List<Buchung> GetEntries(int typeFilter, int month, bool includeInactive)
        {
            return data.GetAll(includeInactive)
                .Where(b => typeFilter == 0 || (typeFilter == 1 && b is Einnahmen) || (typeFilter == 2 && b is Ausgaben))
                .Where(b => month == 0 || b.date.Month == month)
                .ToList();
        }

        /// <summary>
        /// Methode gibt die Summen der aktiven Buchungen eines Monats bzw. des Jahres zurück
        /// </summary>
        /// <param name="month">0 = ganzes Jahr, sonst 1 bis 12</param>
        /// <returns>Einnahmen, Ausgaben, Gewinn und Anzahl der Buchungen</returns>
        public (decimal einnahmen, decimal ausgaben, decimal gewinn, int anzahl) GetSums(int month)
        {
            var entries = GetEntries(0, month, false);
            decimal einnahmen = entries.OfType<Einnahmen>().Sum(b => b.amount);
            decimal ausgaben = entries.OfType<Ausgaben>().Sum(b => b.amount);

            return (einnahmen, ausgaben, einnahmen - ausgaben, entries.Count);
        }

        /// <summary>
        /// Methode gibt die Summen der aktiven Buchungen je Monat zurück (immer 12 Zeilen)
        /// </summary>
        /// <returns>Liste mit einer Zeile pro Monat</returns>
        public List<MonthSum> GetMonthlySums()
        {
            string[] monthNames = CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.MonthNames;
            var result = new List<MonthSum>();

            for (int month = 1; month <= 12; month++)
            {
                var sums = GetSums(month);
                result.Add(new MonthSum(monthNames[month - 1], sums.einnahmen, sums.ausgaben, sums.gewinn));
            }

            return result;
        }

        /// <summary>
        /// Methode zeigt eine Fehlermeldung, wenn das Speichern fehlgeschlagen ist
        /// </summary>
        /// <param name="ex">Aufgetretene Exception</param>
        private void ShowSaveError(Exception ex)
        {
            MessageBox.Show($"Speichern fehlgeschlagen.\n\n{ex.Message}", "Speichern fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
