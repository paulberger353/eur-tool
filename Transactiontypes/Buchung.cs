using System.Globalization;

namespace eur_tool.Transactiontypes
{
    /// <summary>
    /// Basisklasse für Einnahmen und Ausgaben
    /// Enthält die fachlichen Felder und die Historie einer Buchung
    /// </summary>
    public abstract class Buchung
    {
        //Formate für Datum und Zeitstempel in der Datendatei
        internal const string DateFormat = "yyyy-MM-dd";
        internal const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss";

        //Fachliche Felder (vom Benutzer erfasst)
        public DateTime date { get; protected set; }
        public decimal amount { get; protected set; }
        public string category { get; protected set; } = "";     // bei Einnahmen immer ""
        public string description { get; protected set; } = "";
        public string receiptNo { get; protected set; } = "";

        //Historie (setzt ausschließlich Datastorage)
        public int id { get; private set; }                      // fortlaufend je Jahresdatei, gleich über alle Versionen einer Buchung
        public bool isActive { get; private set; } = true;
        public DateTime createdAt { get; private set; }          // Zeitpunkt, zu dem diese Version entstand
        public DateTime? deactivatedAt { get; private set; }     // null, solange aktiv
        public bool isReplaced { get; private set; }             // nur im Speicher: inaktiv durch Bearbeiten (true) oder Löschen (false)

        public abstract string typeCode { get; }  // "1" = Einnahme, "2" = Ausgabe
        public abstract string typeName { get; }  // "Einnahme" / "Ausgabe"
        public string statusName => isActive ? "Aktiv" : (isReplaced ? "Ersetzt" : "Gelöscht");

        /// <summary>
        /// Methode wird aufgerufen um die Historienfelder zu setzen (beim Hinzufügen bzw. Einlesen)
        /// </summary>
        /// <param name="_id">Id der Buchung</param>
        /// <param name="_isActive">Gibt an, ob diese Version aktiv ist</param>
        /// <param name="_createdAt">Zeitpunkt, zu dem diese Version entstand</param>
        /// <param name="_deactivatedAt">Zeitpunkt der Deaktivierung, null solange aktiv</param>
        internal void SetHistory(int _id, bool _isActive, DateTime _createdAt, DateTime? _deactivatedAt)
        {
            id = _id;
            isActive = _isActive;
            createdAt = _createdAt;
            deactivatedAt = _deactivatedAt;
            isReplaced = false;
        }

        /// <summary>
        /// Methode wird aufgerufen um diese Version inaktiv zu setzen
        /// </summary>
        /// <param name="_at">Zeitpunkt der Deaktivierung</param>
        /// <param name="_replaced">True beim Bearbeiten, false beim Löschen</param>
        internal void Deactivate(DateTime _at, bool _replaced)
        {
            isActive = false;
            deactivatedAt = _at;
            isReplaced = _replaced;
        }

        /// <summary>
        /// Methode wird beim Einlesen aufgerufen, wenn diese inaktive Version durch eine neuere ersetzt wurde
        /// </summary>
        internal void MarkReplaced()
        {
            isReplaced = true;
        }

        /// <summary>
        /// Methode wird nur beim Rollback in Datastorage aufgerufen, um eine Deaktivierung zurückzunehmen
        /// </summary>
        internal void UndoDeactivate()
        {
            isActive = true;
            deactivatedAt = null;
            isReplaced = false;
        }

        /// <summary>
        /// Methode wird aufgerufen um die Daten der Buchung als String zurückzugeben
        /// (Klartext vor der Verschlüsselung)
        /// </summary>
        /// <returns>Daten als String</returns>
        public string GetDataString()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;

            return string.Join(";",
                id.ToString(inv),
                typeCode,
                date.ToString(DateFormat, inv),
                amount.ToString("0.00", inv),
                category,
                description,
                receiptNo,
                isActive ? "1" : "0",
                createdAt.ToString(TimestampFormat, inv),
                deactivatedAt?.ToString(TimestampFormat, inv) ?? "");
        }
    }
}
