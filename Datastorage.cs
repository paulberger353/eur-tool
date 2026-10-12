using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using eur_tool.Transactiontypes;
using Microsoft.Win32;

namespace eur_tool
{
    public class Datastorage
    {
        //Schema und Speicherort der CSV-Datei
        private string schema = "Type;Date;Netto;MwSt;Brutto;Category;Comment";
        private string dataFilepath;

        //Listen der Einahmen/Ausgaben
        private List<Einnahmen> einnahmen;
        private List<Ausgaben> ausgaben;


        /// <summary>
        /// Konstruktor der Datastorage-Klasse
        /// Initialisiert Dateipfad und ließt Daten aus der Datei aus.
        /// </summary>
        /// <param name="year">Das Jahr für die Daten</param>
        public Datastorage(int year)
        {
            dataFilepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "eur_tool", $"eur_data_{year}.csv");

            ReadDataFromFile();
        }

        /// <summary>
        /// Methode wird aufgerufen um Daten aus der Datei zu lesen
        /// </summary>
        private void ReadDataFromFile()
        {
            //Listen initialisieren
            einnahmen = new List<Einnahmen>();
            ausgaben = new List<Ausgaben>();

            //Falls die Datei nicht existiert, erstelle sie und verlasse Methode
            if (!File.Exists(dataFilepath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilepath)!);
                File.Create(dataFilepath).Close();
                return;
            }

            //Falls doch lese aus und fülle entsprechend die Liste
            using (StreamReader sr = new StreamReader(dataFilepath))
            {
                //Prüfe Schmema in der Kopfzeile
                if (sr.ReadLine() != schema)
                {
                    throw new Exception("Unzulässiges Schema in der Datendatei. \n\nÜberprüfe:  '" + dataFilepath + "'");
                }


                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var parts = line.Split(';');

                    //Prüfe ob die Zeile korrekt ist
                    CheckLineformat(parts);

                    //Erstelle Einahmen oder Ausgaben Objekt und füge es der entsprechenden Liste hinzu
                    if (parts[0] == "1")
                    {
                        einnahmen.Add(new Einnahmen(DateTime.Parse(parts[1]), int.Parse(parts[2])));
                    }
                    else if (parts[0] == "2")
                    {
                        ausgaben.Add(new Ausgaben(DateTime.Parse(parts[1]), int.Parse(parts[2])));
                    }
                }
            }
        }

        /// <summary>
        /// Methode wird aufgerufen um Daten in die Datei zu schreiben
        /// </summary>
        private void WriteDataToFile(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                //Schreibe Schema in die Kopfzeile
                sw.WriteLine(schema);

                //Schreibe Einnahmen/Ausgaben in die Datei
                foreach (var e in einnahmen)
                {
                    sw.WriteLine(e.GetDataString());
                }

                foreach (var a in ausgaben)
                {
                    sw.WriteLine(a.GetDataString());
                }
            }
        }

        /// <summary>
        /// Methode wird aufgerufen, um eine Zeile nach dem Einlesen zu prüfen
        /// </summary>
        private void CheckLineformat(string[] parts)
        {
            //Bilde Zeile, und prüfe sie dann
            string line = string.Join(";", parts);


            if (parts.Length != 7)
            {
                throw new Exception("Unzulässige Zeile in der Datendatei. \n\n" + line);
            }
            else if (parts[0] != "1" && parts[0] != "2")
            {
                throw new Exception("Unzulässiger Typ in der Datendatei. \n\n" + line);
            }
            else if (!DateTime.TryParse(parts[1], out _))
            {
                throw new Exception("Unzulässiges Datum in der Datendatei. \n\n" + line);
            }
            else if (!double.TryParse(parts[2], out _))
            {
                throw new Exception("Unzulässiger Netto-Betrag in der Datendatei. \n\n" + line);
            }
            else if (!double.TryParse(parts[3], out _))
            {
                throw new Exception("Unzulässiger MwSt-Betrag in der Datendatei. \n\n" + line);
            }
            else if (!double.TryParse(parts[4], out _))
            {
                throw new Exception("Unzulässiger Brutto-Betrag in der Datendatei. \n\n" + line);
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
