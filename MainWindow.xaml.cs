using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace eur_tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Window Events

        //True, wenn ungespeicherte Daten vorhanden
        bool hasUnsavedChanges { get; set; } = false;

        /// <summary>
        /// Methode wird aufgerufen um das Fenster im Hero zu verschieben
        /// </summary>
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Methode wird aufgerufen um das Fenster zu minimieren
        /// </summary>
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Methode wird aufgerufen um das Fenster zu schließen
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //Wenn ungespeicherte Daten vorhanden sind, frag ab ob er wirklich schließen möchte
            if (hasUnsavedChanges)
            {
                if (MessageBox.Show("Ungespeicherte Daten vorhanden. Wirklich schließen?", "Achtung", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.Close();
        }

        #endregion
    }
}