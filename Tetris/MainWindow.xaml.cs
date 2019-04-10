using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Globalization;

namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CultureInfo.DefaultThreadCurrentCulture = Constants.Culture;

            //Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
          }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Keyboard.Focus(Tetris);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        #region settings

        private void ResetSettings()
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

            Tetris.TankWidth = Properties.Settings.Default.TankWidth;
            Tetris.TankHeight = Properties.Settings.Default.TankHeight;
            Tetris.FigureSet = Properties.Settings.Default.FigureSet;
            Tetris.ColorSet = Properties.Settings.Default.ColorSet;

            Tetris.Initialize();
        }

        private void SaveSettings()
        {
            bool SettingsChanged = false;

            if (Properties.Settings.Default.TankWidth != Tetris.TankWidth)
            {
                Properties.Settings.Default.TankWidth = Tetris.TankWidth;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.TankHeight != Tetris.TankHeight)
            {
                Properties.Settings.Default.TankHeight = Tetris.TankHeight;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.FigureSet != Tetris.FigureSet)
            {
                Properties.Settings.Default.FigureSet = Tetris.FigureSet;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.ColorSet != Tetris.ColorSet)
            {
                Properties.Settings.Default.ColorSet = Tetris.ColorSet;
                SettingsChanged = true;
            }

            if (SettingsChanged) Properties.Settings.Default.Save();
        }

        #endregion

        #region menu

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            string Message = "Tetris 2019\n\n" + "https://www.facebook.com/andriy.chernavin\n\n" + "https://www.linkedin.com/in/andriychernavin";

            MessageBox.Show(Message, "About");
        }

        private void MenuItemReset_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
        }

        #endregion
    }

    static class Constants
    {
        public static CultureInfo Culture { get; }

        public static Random Random;

        static Constants()
        {
            Culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            Culture.NumberFormat.NumberDecimalSeparator = ".";
            Culture.NumberFormat.NumberGroupSeparator = " ";
            //Culture.NumberFormat.NumberGroupSizes = new int[] { 3 };

            Random = new Random((int)DateTime.Now.Ticks);
        }
    }
}
