using System;
using System.Collections.Generic;
using System.Text;

namespace eur_tool.Transactiontypes
{
    public class Einnahmen
    {
        //Variablen der Einnahmen-Klasse
        public DateTime date { get; private set; }
        public int netto { get; private set; }
        public int mwst { get; private set; }
        public int brutto { get; private set; }

        /// <summary>
        /// Konstruktor der Einnahmen-Klasse
        /// Nimmt Einnahme als Netto bzw Brutto entgegen
        /// </summary>
        /// <param name="_date">Datum der Einnahme</param>
        /// <param name="_betrag">Netto/Brutto-Betrag der Einnahme</param>
        /// <param name="isBrutto">Gibt an, ob der Betrag Brutto ist</param>
        public Einnahmen(DateTime _date, int _betrag, bool isBrutto = false)
        {
            date = _date;

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
        public Einnahmen(string filepath)
        {
            //Drag&Drop von CSV-Datei in die GUI, automatisches auslesen via AI-API oder Tool
        }

        /// <summary>
        /// Methode wird aufgerufen um die Daten der Einnahme als String zurückzugeben
        /// </summary>
        /// <returns>Daten als String</returns>
        public string GetDataString()
        {
            return $"1;{date};{netto};{mwst};{brutto};;";
        }
    }
}
