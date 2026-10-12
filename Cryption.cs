using System;
using System.Collections.Generic;
using System.Text;

namespace eur_tool
{
    public class Cryption
    {
        //Variable mit individuellem Schlüssel, der auf dem Benutzernamen basiert
        private static string key = Environment.UserName;

        /// <summary>
        /// Methode wird aufgerufen um Text zu verschlüsseln.
        /// </summary>
        /// <param name="input">Text zum Verschlüsseln</param>
        /// <returns>verschlüsselter Text</returns>
        public static string Encrypt(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] resultBytes = new byte[inputBytes.Length];

            // Text Byte für Byte mit dem Schlüssel per XOR verknüpfen
            for (int i = 0; i < inputBytes.Length; i++)
            {
                resultBytes[i] = (byte)(inputBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            // Als Base64-Text zurückgeben
            return Convert.ToBase64String(resultBytes);
        }

        /// <summary>
        /// Methode wird aufgerufen um Text zu entschlüsseln.
        /// </summary>
        /// <param name="input">Verschlüsselter Text</param>
        /// <returns>Entschlüsselter Text</returns>
        public static string Decrypt(string input)
        {
            // Base64-Text zurück in Bytes umwandeln
            byte[] inputBytes = Convert.FromBase64String(input);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] resultBytes = new byte[inputBytes.Length];

            // Zweites XOR hebt die Verschlüsselung wieder auf
            for (int i = 0; i < inputBytes.Length; i++)
            {
                resultBytes[i] = (byte)(inputBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            // Als normalen Text zurückgeben
            return Encoding.UTF8.GetString(resultBytes);
        }
    }
}
