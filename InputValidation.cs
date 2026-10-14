using System.Globalization;

namespace eur_tool
{
    public static class InputValidation
    {
        //Höchster zulässiger Betrag
        private const decimal maxAmount = 999999.99m;

        /// <summary>
        /// Methode wird aufgerufen um einen eingegebenen Betrag zu prüfen und umzuwandeln
        /// Erlaubt sind Ziffern und höchstens ein Dezimaltrenner (Komma oder Punkt)
        /// </summary>
        /// <param name="input">Eingabe aus dem Betragsfeld</param>
        /// <param name="amount">Umgewandelter Betrag (0 bei Fehler)</param>
        /// <param name="error">Fehlermeldung ("" bei Erfolg)</param>
        /// <returns>True, wenn der Betrag gültig ist</returns>
        public static bool TryParseAmount(string input, out decimal amount, out string error)
        {
            amount = 0;
            error = "";

            //Euro-Zeichen und Leerzeichen entfernen
            string text = new string((input ?? "").Replace("€", "").Where(c => !char.IsWhiteSpace(c)).ToArray());

            if (text.Length == 0)
            {
                error = "Betrag fehlt.";
                return false;
            }

            //Negatives Vorzeichen gesondert melden
            bool negative = text.StartsWith('-');
            if (negative)
            {
                text = text.Substring(1);
            }

            if (text.Length == 0 || text.Any(c => !char.IsAsciiDigit(c) && c != ',' && c != '.'))
            {
                error = "Betrag ist keine Zahl.";
                return false;
            }

            int separatorCount = text.Count(c => c == ',' || c == '.');
            if (separatorCount > 1)
            {
                error = "Betrag bitte ohne Tausenderpunkt eingeben, z. B. 1234,56.";
                return false;
            }

            string normalized = text.Replace(',', '.');
            int separatorIndex = normalized.IndexOf('.');
            if (separatorIndex >= 0 && normalized.Length - separatorIndex - 1 > 2)
            {
                error = "Betrag hat mehr als 2 Nachkommastellen.";
                return false;
            }

            if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
            {
                error = "Betrag ist keine Zahl.";
                return false;
            }

            if (negative || value <= 0)
            {
                error = "Betrag muss größer als 0 sein.";
                return false;
            }

            if (value > maxAmount)
            {
                error = "Betrag ist zu groß (max. 999.999,99 €).";
                return false;
            }

            amount = value;
            return true;
        }

        /// <summary>
        /// Methode wird aufgerufen um ein Textfeld zu prüfen (getrimmt)
        /// </summary>
        /// <param name="input">Eingabe aus dem Textfeld</param>
        /// <param name="fieldName">Name des Feldes für die Fehlermeldung</param>
        /// <param name="required">Gibt an, ob das Feld ausgefüllt sein muss</param>
        /// <param name="maxLength">Maximale Länge</param>
        /// <returns>null, wenn der Text gültig ist, sonst die Fehlermeldung</returns>
        public static string? CheckText(string input, string fieldName, bool required, int maxLength)
        {
            string text = (input ?? "").Trim();

            if (required && text.Length == 0)
            {
                return $"{fieldName} fehlt.";
            }

            if (text.Length > maxLength)
            {
                return $"{fieldName} ist zu lang (max. {maxLength} Zeichen).";
            }

            if (text.Contains(';'))
            {
                return $"{fieldName} darf kein Semikolon enthalten.";
            }

            if (text.Contains('\r') || text.Contains('\n'))
            {
                return $"{fieldName} darf keinen Zeilenumbruch enthalten.";
            }

            return null;
        }
    }
}
