using System.Globalization;
using System.IO;
using System.Windows;
using eur_tool.Transactiontypes;
using Microsoft.Win32;

namespace eur_tool
{
    public class Datastorage
    {
        //Schema und Speicherort der CSV-Datei
        private string schema = "Id;Type;Date;Amount;Category;Description;ReceiptNo;IsActive;CreatedAt;DeactivatedAt";
        private string legacySchema = "Type;Date;Netto;MwSt;Brutto;Category;Comment";
        private string dataFilepath;

        //Jahr der Datendatei
        public int year { get; }

        //Listen der Einahmen/Ausgaben (alle Zeilen, aktive und inaktive)
        private List<Einnahmen> einnahmen = new List<Einnahmen>();
        private List<Ausgaben> ausgaben = new List<Ausgaben>();

        //Nächste freie Id in der Jahresdatei
        private int nextId = 1;


        /// <summary>
        /// Konstruktor der Datastorage-Klasse
        /// Initialisiert Dateipfad und ließt Daten aus der Datei aus.
        /// </summary>
        /// <param name="_year">Das Jahr für die Daten</param>
        public Datastorage(int _year)
        {
            year = _year;
            dataFilepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "eur_tool", $"eur_data_{year}.csv");

            ReadDataFromFile();
        }

        /// <summary>
        /// Methode gibt die Buchungen sortiert nach Datum, Id und Entstehungszeitpunkt zurück
        /// </summary>
        /// <param name="includeInactive">True, wenn auch inaktive Zeilen geliefert werden sollen</param>
        /// <returns>Liste der Buchungen</returns>
        public List<Buchung> GetAll(bool includeInactive = false)
        {
            return AllEntries()
                .Where(b => includeInactive || b.isActive)
                .OrderBy(b => b.date)
                .ThenBy(b => b.id)
                .ThenBy(b => b.createdAt)
                .ToList();
        }

        /// <summary>
        /// Methode wird aufgerufen um eine neue Buchung hinzuzufügen und sofort zu speichern
        /// </summary>
        /// <param name="entry">Neue Buchung</param>
        public void Add(Buchung entry)
        {
            if (Contains(entry))
            {
                throw new InvalidOperationException("Die Buchung ist bereits gespeichert.");
            }

            DateTime now = Now();
            int previousNextId = nextId;

            AddToList(entry);
            entry.SetHistory(nextId++, true, now, null);

            try
            {
                SaveToFile();
            }
            catch
            {
                //Rollback: Speicher wieder auf den Stand der Datei bringen
                RemoveFromList(entry);
                nextId = previousNextId;
                throw;
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um eine Buchung zu ändern und sofort zu speichern
        /// Die alte Version bleibt als inaktive Zeile erhalten, die neue bekommt dieselbe Id
        /// </summary>
        /// <param name="oldEntry">Bisherige, aktive Version</param>
        /// <param name="newEntry">Neue Version</param>
        public void Replace(Buchung oldEntry, Buchung newEntry)
        {
            if (oldEntry.GetType() != newEntry.GetType())
            {
                throw new ArgumentException("Alte und neue Buchung müssen vom gleichen Typ sein.");
            }

            if (!oldEntry.isActive || !Contains(oldEntry))
            {
                throw new InvalidOperationException("Nur aktive, gespeicherte Buchungen können geändert werden.");
            }

            if (Contains(newEntry))
            {
                throw new InvalidOperationException("Die neue Buchung ist bereits gespeichert.");
            }

            DateTime now = Now();

            oldEntry.Deactivate(now, true);
            newEntry.SetHistory(oldEntry.id, true, now, null);
            AddToList(newEntry);

            try
            {
                SaveToFile();
            }
            catch
            {
                //Rollback: neue Version entfernen, alte wieder aktiv setzen
                RemoveFromList(newEntry);
                oldEntry.UndoDeactivate();
                throw;
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um eine Buchung zu löschen und sofort zu speichern
        /// Die Zeile wird nur inaktiv gesetzt und bleibt in der Datei
        /// </summary>
        /// <param name="entry">Zu löschende, aktive Buchung</param>
        public void Remove(Buchung entry)
        {
            if (!entry.isActive || !Contains(entry))
            {
                throw new InvalidOperationException("Nur aktive, gespeicherte Buchungen können gelöscht werden.");
            }

            entry.Deactivate(Now(), false);

            try
            {
                SaveToFile();
            }
            catch
            {
                //Rollback: Buchung wieder aktiv setzen
                entry.UndoDeactivate();
                throw;
            }
        }

        /// <summary>
        /// Methode gibt die aktuelle Zeit sekundengenau zurück (wie in der Datei gespeichert)
        /// </summary>
        /// <returns>Aktuelle Zeit ohne Millisekunden</returns>
        private static DateTime Now()
        {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        }

        /// <summary>
        /// Methode prüft, ob genau dieses Objekt bereits in einer der Listen steht
        /// </summary>
        /// <param name="entry">Buchung</param>
        /// <returns>True, wenn enthalten</returns>
        private bool Contains(Buchung entry)
        {
            return AllEntries().Any(b => ReferenceEquals(b, entry));
        }

        /// <summary>
        /// Methode fügt die Buchung der passenden Liste hinzu
        /// </summary>
        /// <param name="entry">Buchung</param>
        private void AddToList(Buchung entry)
        {
            if (entry is Einnahmen e)
            {
                einnahmen.Add(e);
            }
            else if (entry is Ausgaben a)
            {
                ausgaben.Add(a);
            }
            else
            {
                throw new ArgumentException("Unbekannter Buchungstyp.");
            }
        }

        /// <summary>
        /// Methode entfernt die Buchung aus ihrer Liste (nur für den Rollback)
        /// </summary>
        /// <param name="entry">Buchung</param>
        private void RemoveFromList(Buchung entry)
        {
            if (entry is Einnahmen e)
            {
                einnahmen.Remove(e);
            }
            else if (entry is Ausgaben a)
            {
                ausgaben.Remove(a);
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um Daten aus der Datei zu lesen
        /// </summary>
        private void ReadDataFromFile()
        {
            //Falls die Datei nicht existiert, Ordner anlegen, Kopfzeile schreiben und Methode verlassen
            if (!File.Exists(dataFilepath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilepath)!);
                SaveToFile();
                return;
            }

            //True, wenn die Datei leer ist oder nur die alte Kopfzeile enthält
            bool rewriteHeader = false;

            //Falls doch lese aus und fülle entsprechend die Liste
            using (StreamReader sr = new StreamReader(dataFilepath))
            {
                string? header = sr.ReadLine();
                string? raw;
                int lineNo = 1;

                if (header == null)
                {
                    //0-Byte-Datei: wird mit neuer Kopfzeile überschrieben
                    rewriteHeader = true;
                }
                else if (header == legacySchema)
                {
                    //Alte Kopfzeile: nur ohne Datenzeilen automatisch ersetzen
                    while ((raw = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            throw new Exception("Datendatei im alten Format mit Daten gefunden. Die Daten werden nicht automatisch umgewandelt. \n\nBitte prüfen:  '" + dataFilepath + "'");
                        }
                    }

                    rewriteHeader = true;
                }
                else if (header != schema)
                {
                    throw new Exception("Unzulässiges Schema in der Datendatei. \n\nÜberprüfe:  '" + dataFilepath + "'");
                }
                else
                {
                    while ((raw = sr.ReadLine()) != null)
                    {
                        lineNo++;

                        //Leerzeilen überspringen
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            continue;
                        }

                        string line;
                        try
                        {
                            line = Cryption.Decrypt(raw);
                        }
                        catch (FormatException)
                        {
                            throw new Exception($"Zeile {lineNo} der Datendatei kann nicht entschlüsselt werden. \n\nÜberprüfe:  '{dataFilepath}'");
                        }

                        var parts = line.Split(';');

                        //Prüfe ob die Zeile korrekt ist
                        CheckLineformat(parts, lineNo);

                        //Erstelle Einahmen oder Ausgaben Objekt und füge es der entsprechenden Liste hinzu
                        AddFromLine(parts);
                    }
                }
            }

            if (rewriteHeader)
            {
                SaveToFile();
                return;
            }

            CheckHistory();
        }

        /// <summary>
        /// Methode wird aufgerufen, um aus einer geprüften Zeile ein Objekt zu erzeugen und der Liste hinzuzufügen
        /// </summary>
        /// <param name="parts">Felder der Zeile</param>
        private void AddFromLine(string[] parts)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;

            int id = int.Parse(parts[0], inv);
            DateTime date = DateTime.ParseExact(parts[2], Buchung.DateFormat, inv);
            decimal amount = decimal.Parse(parts[3], NumberStyles.Number, inv);
            bool isActive = parts[7] == "1";
            DateTime createdAt = DateTime.ParseExact(parts[8], Buchung.TimestampFormat, inv);
            DateTime? deactivatedAt = isActive ? null : DateTime.ParseExact(parts[9], Buchung.TimestampFormat, inv);

            if (parts[1] == "1")
            {
                var entry = new Einnahmen(date, amount, parts[5], parts[6]);
                entry.SetHistory(id, isActive, createdAt, deactivatedAt);
                einnahmen.Add(entry);
            }
            else
            {
                var entry = new Ausgaben(date, amount, parts[4], parts[5], parts[6]);
                entry.SetHistory(id, isActive, createdAt, deactivatedAt);
                ausgaben.Add(entry);
            }
        }

        /// <summary>
        /// Methode wird nach dem Einlesen aufgerufen, um die Historie zu prüfen,
        /// ersetzte Versionen zu markieren und die nächste Id zu bestimmen
        /// </summary>
        private void CheckHistory()
        {
            var all = AllEntries().ToList();

            foreach (var group in all.GroupBy(b => b.id))
            {
                //Je Id höchstens eine aktive Zeile, alle Zeilen vom gleichen Typ
                if (group.Count(b => b.isActive) > 1 || group.Select(b => b.typeCode).Distinct().Count() > 1)
                {
                    throw new Exception($"Inkonsistente Historie für Id {group.Key} in der Datendatei. \n\nBitte prüfen:  '{dataFilepath}'");
                }

                //Inaktive Zeile wurde ersetzt, wenn eine andere Version genau bei ihrer Deaktivierung entstand
                foreach (var entry in group.Where(b => !b.isActive))
                {
                    if (group.Any(o => o != entry && o.createdAt == entry.deactivatedAt))
                    {
                        entry.MarkReplaced();
                    }
                }
            }

            nextId = all.Count > 0 ? all.Max(b => b.id) + 1 : 1;
        }

        /// <summary>
        /// Methode wird aufgerufen um Daten verschlüsselt in die Datei zu schreiben
        /// </summary>
        /// <param name="filepath">Zieldatei</param>
        private void WriteDataToFile(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                //Schreibe Schema in die Kopfzeile
                sw.WriteLine(schema);

                //Schreibe verschlüsselt alle Zeilen (aktiv und inaktiv), sortiert nach Id und Entstehungszeitpunkt
                foreach (var entry in AllEntries().OrderBy(b => b.id).ThenBy(b => b.createdAt))
                {
                    sw.WriteLine(Cryption.Encrypt(entry.GetDataString()));
                }
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um die Daten in die eigene Datendatei zu speichern
        /// Schreibt zuerst in eine temporäre Datei und ersetzt dann die Datendatei
        /// </summary>
        private void SaveToFile()
        {
            string tmpFilepath = $"{dataFilepath}.tmp";

            try
            {
                WriteDataToFile(tmpFilepath);
                File.Move(tmpFilepath, dataFilepath, true);
            }
            catch
            {
                //Temporäre Datei nicht liegen lassen
                try { File.Delete(tmpFilepath); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Methode gibt alle Einnahmen und Ausgaben in einer Aufzählung zurück
        /// </summary>
        /// <returns>Alle Zeilen (aktiv und inaktiv)</returns>
        private IEnumerable<Buchung> AllEntries()
        {
            return einnahmen.Cast<Buchung>().Concat(ausgaben);
        }

        /// <summary>
        /// Methode wird aufgerufen, um eine Zeile nach dem Einlesen zu prüfen
        /// </summary>
        /// <param name="parts">Felder der Zeile</param>
        /// <param name="lineNo">Zeilennummer in der Datei</param>
        private void CheckLineformat(string[] parts, int lineNo)
        {
            //Bilde Zeile, und prüfe sie dann
            string line = string.Join(";", parts);
            string where = $"in Zeile {lineNo} der Datendatei. \n\n" + line + "\n\nÜberprüfe:  '" + dataFilepath + "'";
            CultureInfo inv = CultureInfo.InvariantCulture;

            if (parts.Length != 10)
            {
                throw new Exception("Unzulässige Zeile " + where);
            }
            else if (!int.TryParse(parts[0], NumberStyles.None, inv, out int id) || id <= 0)
            {
                throw new Exception("Unzulässige Id " + where);
            }
            else if (parts[1] != "1" && parts[1] != "2")
            {
                throw new Exception("Unzulässiger Typ " + where);
            }
            else if (!DateTime.TryParseExact(parts[2], Buchung.DateFormat, inv, DateTimeStyles.None, out DateTime date))
            {
                throw new Exception("Unzulässiges Datum " + where);
            }
            else if (date.Year != year)
            {
                throw new Exception($"Datum außerhalb des Jahres {year} " + where);
            }
            else if (!decimal.TryParse(parts[3], NumberStyles.Number, inv, out decimal amount) || amount <= 0)
            {
                throw new Exception("Unzulässiger Betrag " + where);
            }
            else if (parts[7] != "1" && parts[7] != "0")
            {
                throw new Exception("Unzulässiger Status " + where);
            }
            else if (!DateTime.TryParseExact(parts[8], Buchung.TimestampFormat, inv, DateTimeStyles.None, out DateTime createdAt))
            {
                throw new Exception("Unzulässiger Erfassungszeitpunkt " + where);
            }
            else if (parts[7] == "1" && parts[9] != "")
            {
                throw new Exception("Unzulässiger Deaktivierungszeitpunkt bei aktiver Zeile " + where);
            }
            else if (parts[7] == "0" && (!DateTime.TryParseExact(parts[9], Buchung.TimestampFormat, inv, DateTimeStyles.None, out DateTime deactivatedAt) || deactivatedAt < createdAt))
            {
                throw new Exception("Unzulässiger Deaktivierungszeitpunkt " + where);
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um die Daten zu exportieren
        /// </summary>
        public void ExportData()
        {
            //Frage ab ob er sicher ist, dass er unverschlüsselte Daten exportieren möchte
            if (MessageBox.Show("Möchten Sie die Daten wirklich unverschlüsselt als CSV-Datei exportieren?",
                "Achtung!", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Speicherort und Dateiname auswählen",
                Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = Path.GetFileName(dataFilepath),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                WriteDataToFile(dialog.FileName);
                MessageBox.Show("Die Daten wurden erfolgreich exportiert.", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Die Daten konnten nicht exportiert werden.\n\n{ex.Message}", "Export fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
