using System;
using System.Collections.Generic;
using System.Text;

namespace eur_tool.Transactiontypes
{
    public class Ausgaben
    {
        //Variablen der Ausgaben-Klasse
        public DateTime date { get; private set; }
        public int netto { get; private set; }
        public int mwst { get; private set; }
        public int brutto { get; private set; }
        public string category { get; private set; }
        public string comment { get; private set; }




        /// <summary>
        /// Konstruktor der Ausgaben-Klasse
        /// Nimmt Ausgabe als Netto bzw Brutto entgegen
        /// </summary>
        /// <param name="_date">Datum der Ausgabe</param>
        /// <param name="_betrag">Netto/Brutto-Betrag der Ausgabe</param>
        /// <param name="isBrutto">Gibt an, ob der Betrag Brutto ist</param>
        public Ausgaben(DateTime _date, int _betrag, bool isBrutto = false)
        {
            date = _date;
            category = "";
            comment = "";

            if (isBrutto)
            {
                brutto = _betrag;
                netto = (int)Math.Round(brutto / 1.19);
                mwst = brutto - netto;
            }
            else
            {
                netto = _betrag;
                mwst = (int)Math.Round(netto * 0.19);
                brutto = netto + mwst;
            }
        }

        /// <summary>
        /// Methode zum Laden der Ausgaben aus einer Datei
        /// </summary>
        /// <param name="filepath">Dateipfad</param>
        public Ausgaben(string filepath)
        {
            //Drag&Drop von CSV-Datei in die GUI, automatisches auslesen via AI-API oder Tool
        }

        /// <summary>
        /// Methode wird aufgerufen um die Daten der Ausgabe als String zurückzugeben
        /// </summary>
        /// <returns>Daten als String</returns>
        public string GetDataString()
        {
            return $"2;{date};{netto};{mwst};{brutto};{category};{comment}";
        }
    }
}
