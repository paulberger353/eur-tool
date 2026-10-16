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

        //Eintrag, der gerade bearbeitet wird (null im Neu-Modus)
        private Buchung? editEntry;

        //Wird nach "Änderung speichern" oder "Abbrechen" im Bearbeitungsmodus ausgelöst
        public event EventHandler? EditFinished;

        //True, wenn Eingaben in der Maske stehen, die noch nicht gespeichert sind
        public bool HasPendingInput =>
            editEntry != null ||
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

            //Kategorie nur bei Ausgaben anzeigen
            Visibility categoryVisibility = isAusgabe ? Visibility.Visible : Visibility.Collapsed;
            lblCategory.Visibility = categoryVisibility;
            cmbCategory.Visibility = categoryVisibility;
            cmbCategory.ItemsSource = Ausgaben.Kategorien;

            editEntry = null;
            SetEditMode(false);
            ClearInputs(false);
        }

        /// <summary>
        /// Methode startet den Bearbeitungsmodus mit den Werten eines aktiven Eintrags
        /// </summary>
        /// <param name="entry">Zu bearbeitender, aktiver Eintrag</param>
        public void StartEdit(Buchung entry)
        {
            if (controller == null || !entry.isActive) return;

            editEntry = entry;
            ClearInputs(true);
            txtSuccess.Text = "";
            txtSuccess.Visibility = Visibility.Collapsed;

            //Felder aus dem Eintrag befüllen
            dpDate.SelectedDate = entry.date;
            txtAmount.Text = entry.amount.ToString("0.00", de);
            txtDescription.Text = entry.description;
            txtReceiptNo.Text = entry.receiptNo;

            if (isAusgabe)
            {
                //Unbekannte Kategorie (z. B. aus einer älteren Liste) vorübergehend ergänzen
                if (!Ausgaben.Kategorien.Contains(entry.category))
                {
                    cmbCategory.ItemsSource = Ausgaben.Kategorien.Append(entry.category).ToList();
                }

                cmbCategory.SelectedItem = entry.category;
            }

            SetEditMode(true);
        }

        /// <summary>
        /// Methode bricht den Bearbeitungsmodus ohne Änderung ab (löst kein Event aus)
        /// </summary>
        public void CancelEdit()
        {
            if (editEntry == null) return;

            EndEdit();
        }

        /// <summary>
        /// Methode beendet den Bearbeitungsmodus, leert die Felder und kehrt in den Neu-Modus zurück
        /// </summary>
        private void EndEdit()
        {
            editEntry = null;
            ClearInputs(false);
            SetEditMode(false);
        }

        /// <summary>
        /// Methode schaltet Überschrift und Buttons zwischen Neu- und Bearbeitungsmodus um
        /// </summary>
        /// <param name="isEdit">True im Bearbeitungsmodus</param>
        private void SetEditMode(bool isEdit)
        {
            string type = isAusgabe ? "Ausgabe" : "Einnahme";
            txtTitle.Text = isEdit ? $"{type} bearbeiten" : $"{type} erfassen";
            btnSave.Content = isEdit ? "Änderung speichern" : "Speichern";
            btnCancel.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn "Abbrechen" geklickt oder Esc gedrückt wird
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (editEntry == null) return;

            EndEdit();
            EditFinished?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Methode wird aufgerufen, wenn "Speichern" bzw. "Änderung speichern" geklickt oder Enter gedrückt wird
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            txtSuccess.Text = "";
            txtSuccess.Visibility = Visibility.Collapsed;

            if (!TryReadInput(out DateTime date, out decimal amount, out string category, out string description, out string receiptNo))
            {
                return;
            }

            Buchung entry = isAusgabe
                ? new Ausgaben(date, amount, category, description, receiptNo)
                : new Einnahmen(date, amount, description, receiptNo);

            if (editEntry != null)
            {
                SaveEdit(editEntry, entry);
                return;
            }

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
        /// Methode speichert die Änderung eines Eintrags als neue Version
        /// Ohne geändertes Feld wirkt sie wie "Abbrechen", damit keine leere neue Version entsteht
        /// </summary>
        /// <param name="oldEntry">Bisherige Version</param>
        /// <param name="newEntry">Neue Version aus den Eingaben</param>
        private void SaveEdit(Buchung oldEntry, Buchung newEntry)
        {
            bool unchanged =
                newEntry.date == oldEntry.date &&
                newEntry.amount == oldEntry.amount &&
                newEntry.category == oldEntry.category &&
                newEntry.description == oldEntry.description &&
                newEntry.receiptNo == oldEntry.receiptNo;

            if (!unchanged)
            {
                //Änderung bestätigen lassen, bei Nein bleibt die Maske im Bearbeitungsmodus
                string question = string.Format(de,
                    "Eintrag wirklich ändern?\n\nBisher: {0}\nNeu: {1}\n\nDie bisherige Fassung bleibt als „Ersetzt“ in der Historie (Häkchen „Inaktive anzeigen“).",
                    EntrySummary(oldEntry), EntrySummary(newEntry));

                if (MessageBox.Show(question, "Eintrag ändern", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                //Bei Fehlern bleiben alle Eingaben stehen
                if (!controller!.ReplaceEntry(oldEntry, newEntry))
                {
                    return;
                }
            }

            EndEdit();
            EditFinished?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Methode gibt einen Eintrag als kurze Zeile für Rückfragen zurück
        /// </summary>
        /// <param name="entry">Eintrag</param>
        /// <returns>z. B. "12.03.2026 · 49,90 € · Software & Lizenzen · Domain · RE-1"</returns>
        private static string EntrySummary(Buchung entry)
        {
            var parts = new List<string>
            {
                entry.date.ToString("dd.MM.yyyy", de),
                entry.amount.ToString("N2", de) + " €",
                entry.category,
                entry.description,
                entry.receiptNo
            };

            return string.Join(" · ", parts.Where(p => p != ""));
        }

        /// <summary>
        /// Methode prüft alle Eingaben, markiert ungültige Felder und zeigt alle Fehler auf einmal
        /// </summary>
        /// <returns>True, wenn alle Eingaben gültig sind</returns>
        private bool TryReadInput(out DateTime date, out decimal amount, out string category, out string description, out string receiptNo)
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

            //Kategorie prüfen (nur bei Ausgaben)
            category = "";
            if (isAusgabe)
            {
                if (cmbCategory.SelectedItem is string selectedCategory)
                {
                    category = selectedCategory;
                }
                else
                {
                    errors.Add("Kategorie fehlt.");
                    MarkError(cmbCategory);
                }
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
            cmbCategory.ClearValue(BorderBrushProperty);
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
            cmbCategory.SelectedIndex = -1;

            //Vorübergehend ergänzte Kategorie wieder entfernen
            if (cmbCategory.ItemsSource != Ausgaben.Kategorien)
            {
                cmbCategory.ItemsSource = Ausgaben.Kategorien;
            }

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
