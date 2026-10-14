using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using eur_tool.Transactiontypes;

namespace eur_tool.Views
{
    /// <summary>
    /// Erfassungsmaske für Einnahmen bzw. Ausgaben
    /// </summary>
    public partial class ErfassungMaske : UserControl
    {
        //Deutsche Formatierung für Meldungen
        private static readonly CultureInfo de = CultureInfo.GetCultureInfo("de-DE");

        //Controller und Art der Maske
        private Controller? controller;
        private bool isAusgabe;

        //True, wenn Eingaben in der Maske stehen, die noch nicht gespeichert sind
        public bool HasPendingInput =>
            !string.IsNullOrWhiteSpace(txtAmount.Text) ||
            !string.IsNullOrWhiteSpace(txtDescription.Text) ||
            !string.IsNullOrWhiteSpace(txtReceiptNo.Text);

        /// <summary>
        /// Konstruktor der Erfassungsmaske
        /// </summary>
        public ErfassungMaske()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Methode wird nach der Jahresauswahl aufgerufen um die Maske vorzubereiten
        /// </summary>
        /// <param name="_controller">Controller mit den Daten des gewählten Jahres</param>
        /// <param name="_isAusgabe">True für Ausgaben, false für Einnahmen</param>
        public void Init(Controller _controller, bool _isAusgabe)
        {
            controller = _controller;
            isAusgabe = _isAusgabe;

            //Datumsbereich auf das gewählte Jahr und höchstens heute begrenzen
            int year = controller.selectedYear;
            DateTime start = new DateTime(year, 1, 1);
            DateTime end = new DateTime(year, 12, 31) < DateTime.Today ? new DateTime(year, 12, 31) : DateTime.Today;
            dpDate.DisplayDateStart = start;
            dpDate.DisplayDateEnd = end < start ? start : end;

            txtTitle.Text = isAusgabe ? "Ausgabe erfassen" : "Einnahme erfassen";

            ClearInputs(false);
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn "Speichern" geklickt oder Enter gedrückt wird
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            txtSuccess.Text = "";
            txtSuccess.Visibility = Visibility.Collapsed;

            if (!TryReadInput(out DateTime date, out decimal amount, out string description, out string receiptNo))
            {
                return;
            }

            Buchung entry = isAusgabe
                ? new Ausgaben(date, amount, "", description, receiptNo)
                : new Einnahmen(date, amount, description, receiptNo);

            //Bei Fehlern bleiben alle Eingaben stehen
            if (!controller.AddEntry(entry))
            {
                return;
            }

            txtSuccess.Text = string.Format(de, "✓ Gespeichert: {0:dd.MM.yyyy} · {1:N2} € · {2}", entry.date, entry.amount, entry.description);
            txtSuccess.Visibility = Visibility.Visible;

            //Datum für die Serienerfassung behalten
            ClearInputs(true);
            txtAmount.Focus();
        }

        /// <summary>
        /// Methode prüft alle Eingaben, markiert ungültige Felder und zeigt alle Fehler auf einmal
        /// </summary>
        /// <returns>True, wenn alle Eingaben gültig sind</returns>
        private bool TryReadInput(out DateTime date, out decimal amount, out string description, out string receiptNo)
        {
            var errors = new List<string>();
            ClearErrors();

            //Datum prüfen
            date = DateTime.MinValue;
            DateTime? inputDate = ReadDate();
            int year = controller!.selectedYear;
            if (inputDate == null)
            {
                errors.Add("Datum fehlt oder ist ungültig.");
                MarkError(dpDate);
            }
            else if (inputDate.Value.Year != year)
            {
                errors.Add($"Datum liegt nicht im Jahr {year}.");
                MarkError(dpDate);
            }
            else if (inputDate.Value > DateTime.Today)
            {
                errors.Add("Datum liegt in der Zukunft. In der EÜR zählt der Zahlungstag.");
                MarkError(dpDate);
            }
            else
            {
                date = inputDate.Value;
            }

            //Betrag prüfen
            if (!InputValidation.TryParseAmount(txtAmount.Text, out amount, out string amountError))
            {
                errors.Add(amountError);
                MarkError(txtAmount);
            }

            //Texte prüfen
            string? descriptionError = InputValidation.CheckText(txtDescription.Text, "Beschreibung", true, 100);
            if (descriptionError != null)
            {
                errors.Add(descriptionError);
                MarkError(txtDescription);
            }

            string? receiptError = InputValidation.CheckText(txtReceiptNo.Text, "Belegnummer", false, 30);
            if (receiptError != null)
            {
                errors.Add(receiptError);
                MarkError(txtReceiptNo);
            }

            description = txtDescription.Text.Trim();
            receiptNo = txtReceiptNo.Text.Trim();

            if (errors.Count > 0)
            {
                txtError.Text = string.Join("\n", errors);
                txtError.Visibility = Visibility.Visible;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Methode liest das Datum aus dem DatePicker
        /// Nimmt auch einen eingetippten, noch nicht übernommenen Text
        /// </summary>
        /// <returns>Datum oder null, wenn leer oder ungültig</returns>
        private DateTime? ReadDate()
        {
            string text = dpDate.Text.Trim();

            if (text.Length == 0)
            {
                return null;
            }

            if (DateTime.TryParse(text, de, DateTimeStyles.None, out DateTime parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        /// <summary>
        /// Methode markiert ein ungültiges Feld mit rotem Rahmen
        /// </summary>
        /// <param name="control">Ungültiges Feld</param>
        private void MarkError(Control control)
        {
            control.BorderBrush = (System.Windows.Media.Brush)FindResource("BrushError");
        }

        /// <summary>
        /// Methode setzt Fehlermeldung und rote Rahmen zurück
        /// </summary>
        private void ClearErrors()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;

            dpDate.ClearValue(BorderBrushProperty);
            txtAmount.ClearValue(BorderBrushProperty);
            txtDescription.ClearValue(BorderBrushProperty);
            txtReceiptNo.ClearValue(BorderBrushProperty);
        }

        /// <summary>
        /// Methode leert die Eingabefelder
        /// </summary>
        /// <param name="keepDate">True, wenn das Datum stehen bleiben soll</param>
        private void ClearInputs(bool keepDate)
        {
            txtAmount.Text = "";
            txtDescription.Text = "";
            txtReceiptNo.Text = "";

            if (!keepDate && controller != null)
            {
                //Vorbelegung: heute, falls im gewählten Jahr, sonst 31.12.
                int year = controller.selectedYear;
                dpDate.SelectedDate = DateTime.Today.Year == year ? DateTime.Today : new DateTime(year, 12, 31);
            }

            ClearErrors();
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn die Maske sichtbar wird, und setzt den Fokus auf den Betrag
        /// </summary>
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                //Direktes Focus() greift bei einer gerade sichtbar gewordenen Maske nicht
                Dispatcher.BeginInvoke(() =>
                {
                    txtAmount.Focus();
                    Keyboard.Focus(txtAmount);
                }, DispatcherPriority.Input);
            }
        }
    }
}
