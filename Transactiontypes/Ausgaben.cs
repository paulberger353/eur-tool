namespace eur_tool.Transactiontypes
{
    public class Ausgaben : Buchung
    {
        //Feste Kategorien für Ausgaben
        public static readonly string[] Kategorien =
        {
            "Wareneinkauf/Material",
            "Fremdleistungen",
            "Software & Lizenzen",
            "Hardware/GWG (bis 800 €)",
            "Telefon & Internet",
            "Bürobedarf & Porto",
            "Fortbildung & Fachliteratur",
            "Reisekosten",
            "Fahrtkosten",
            "Raumkosten/Arbeitszimmer",
            "Werbung & Marketing",
            "Versicherungen & Beiträge",
            "Bank- & Kontogebühren",
            "Sonstiges"
        };

        //Typ der Buchung
        public override string typeCode => "2";
        public override string typeName => "Ausgabe";

        /// <summary>
        /// Konstruktor der Ausgaben-Klasse
        /// Nimmt die Ausgabe mit dem gezahlten Betrag entgegen (keine USt)
        /// </summary>
        /// <param name="_date">Datum der Ausgabe</param>
        /// <param name="_amount">Gezahlter Betrag der Ausgabe</param>
        /// <param name="_category">Kategorie der Ausgabe</param>
        /// <param name="_description">Beschreibung der Ausgabe</param>
        /// <param name="_receiptNo">Belegnummer der Ausgabe</param>
        public Ausgaben(DateTime _date, decimal _amount, string _category, string _description, string _receiptNo)
        {
            date = _date;
            amount = _amount;
            category = _category;
            description = _description;
            receiptNo = _receiptNo;
        }

        /// <summary>
        /// Methode zum Laden der Ausgaben aus einer Datei
        /// </summary>
        /// <param name="filepath">Dateipfad</param>
        public Ausgaben(string filepath)
        {
            //Drag&Drop von CSV-Datei in die GUI, automatisches auslesen via AI-API oder Tool
        }
    }
}
