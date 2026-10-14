namespace eur_tool.Transactiontypes
{
    public class Einnahmen : Buchung
    {
        //Typ der Buchung
        public override string typeCode => "1";
        public override string typeName => "Einnahme";

        /// <summary>
        /// Konstruktor der Einnahmen-Klasse
        /// Nimmt die Einnahme mit dem gezahlten Betrag entgegen (keine USt)
        /// </summary>
        /// <param name="_date">Datum der Einnahme</param>
        /// <param name="_amount">Gezahlter Betrag der Einnahme</param>
        /// <param name="_description">Beschreibung der Einnahme</param>
        /// <param name="_receiptNo">Belegnummer der Einnahme</param>
        public Einnahmen(DateTime _date, decimal _amount, string _description, string _receiptNo)
        {
            date = _date;
            amount = _amount;
            description = _description;
            receiptNo = _receiptNo;
        }

        /// <summary>
        /// Methode zum Laden der Ausgaben aus einer Datei
        /// </summary>
        /// <param name="filepath">Dateipfad</param>
        public Einnahmen(string filepath)
        {
            //Drag&Drop von CSV-Datei in die GUI, automatisches auslesen via AI-API oder Tool
        }
    }
}
