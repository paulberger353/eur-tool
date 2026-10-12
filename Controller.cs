using System;
using System.Collections.Generic;
using System.Text;

namespace eur_tool
{
    public class Controller
    {
        //Instanzen von MainWindow und der Datenklasse
        MainWindow mw;
        Datastorage data;

        //Gewähltes Jahr für die EÜR
        public int selectedYear { get; private set; }

        /// <summary>
        /// Konstruktur der Controller-Klasse
        /// Setzt lokal Werte und initialisiert MainFrame des Programms
        /// </summary>
        /// <param name="_mw">Instanz des MainWindows</param>
        /// <param name="_selectedYear">Ausgewähltes Jahr für die EÜR</param>
        public Controller(MainWindow _mw, int _selectedYear) 
        { 
            mw = _mw;
            selectedYear = _selectedYear;

            data = new Datastorage(selectedYear);
        }

        /// <summary>
        /// Startet den Datenexport.
        /// </summary>
        public void ExportData()
        {
            data.ExportData();
        }
    }
}
