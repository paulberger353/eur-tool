using System;
using System.Collections.Generic;
using System.Text;

namespace eur_tool
{
    public class Controller
    {
        //Aktuelle Instanz der MainWindow-Klasse
        MainWindow mw;

        //Gewähltes Jahr für die EÜR
        public int selectedYear { get; private set; }

        /// <summary>
        /// Konstruktur der Controller-Klasse
        /// Setzt lokal Werte und initialisiert Programm
        /// </summary>
        /// <param name="_mw">Instanz des MainWindows</param>
        /// <param name="_selectedYear">Ausgewähltes Jahr für die EÜR</param>
        public Controller(MainWindow _mw, int _selectedYear) 
        { 
            mw = _mw;
            selectedYear = _selectedYear;
        }
    }
}
