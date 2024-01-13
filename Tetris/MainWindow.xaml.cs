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
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double InitialWindowWidth;
        double InitialWindowHeight;
        WindowState InitialWindowState;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();

            CultureInfo.DefaultThreadCurrentCulture = Constants.Culture;

            //Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            TopElement.MouseLeftButtonDown += new MouseButtonEventHandler(MoveWindow);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            this.Title = ((AssemblyTitleAttribute)assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;

            Tetris.Initialize();

            this.UpdateLayout();

            CompactWindow();

            InitialWindowWidth = this.Width;
            InitialWindowHeight = this.Height;
            InitialWindowState = this.WindowState;

            //Keyboard.Focus(Tetris);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal && e.ChangedButton == System.Windows.Input.MouseButton.Middle && e.ButtonState == MouseButtonState.Released)
            {
                CompactWindow();
            }
        }

        private void CompactWindow()
        {
            double Width = this.ActualWidth;
            double Height = this.ActualHeight;

            this.Width = this.ActualWidth - ((System.Windows.FrameworkElement)this.Content).ActualWidth + Tetris.ActualWidth;
            this.Height = this.ActualHeight - ((System.Windows.FrameworkElement)this.Content).ActualHeight + Tetris.ActualHeight;

            this.Left = this.Left + (Width - this.Width) / 2;
            this.Top = this.Top + (Height - this.Height) / 2;
        }

        void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        #region settings

        private void ResetSettings()
        {
            // сбрасываем настройки игры

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

            Tetris.TankWidth = Properties.Settings.Default.TankWidth;
            Tetris.TankHeight = Properties.Settings.Default.TankHeight;
            Tetris.FigureSet = Properties.Settings.Default.FigureSet;
            Tetris.ColorSet = Properties.Settings.Default.ColorSet;

            Tetris.Initialize();
        }

        private void LoadSettings()
        {
            if (Properties.Settings.Default.WindowLeft != 0) this.Left = Properties.Settings.Default.WindowLeft;
            if (Properties.Settings.Default.WindowTop != 0) this.Top = Properties.Settings.Default.WindowTop;
            if (Properties.Settings.Default.WindowWidth != 0) this.Width = Properties.Settings.Default.WindowWidth;
            if (Properties.Settings.Default.WindowHeight != 0) this.Height = Properties.Settings.Default.WindowHeight;
            if (Properties.Settings.Default.WindowMaximized) this.WindowState = WindowState.Maximized;
        }

        private void SaveSettings()
        {
            // сохраняем настройки игры

            bool SettingsChanged = false;

            // game settings

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

            // window position

            if (Properties.Settings.Default.WindowLeft != this.Left)
            {
                Properties.Settings.Default.WindowLeft = this.Left;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.WindowTop != this.Top)
            {
                Properties.Settings.Default.WindowTop = this.Top;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.WindowWidth != this.Width)
            {
                Properties.Settings.Default.WindowWidth = this.Width;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.WindowHeight != this.Height)
            {
                Properties.Settings.Default.WindowHeight = this.Height;
                SettingsChanged = true;
            }

            if (Properties.Settings.Default.WindowMaximized != (this.WindowState == WindowState.Maximized))
            {
                Properties.Settings.Default.WindowMaximized = true;
                SettingsChanged = true;
            }

            if (SettingsChanged) Properties.Settings.Default.Save();
        }

        #endregion

        #region menu

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string title = ((AssemblyTitleAttribute)assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
            string version = assembly.GetName().Version.ToString();

            string Message = title + " " + version + "\n\n"
                + "https://facebook.com/andriy.chernavin\n"
                + "https://linkedin.com/in/andriychernavin\n\n"
                + "keyboard controls:\n\n"
                + "left arrow / A - move left\n"
                + "right arrow / D - move right\n"
                + "up arrow / W - rotate\n"
                + "down arrow / S - drop\n"
                + "space - start or pause\n";

            MessageBox.Show(Message, "About");
        }

        private void MenuItemResetSettings_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
        }

        private void MenuItemResetWindow_Click(object sender, RoutedEventArgs e)
        {
            // сбрасываем настройки окна

            this.Width = InitialWindowWidth;
            this.Height = InitialWindowHeight;

            if (InitialWindowState != WindowState.Minimized) this.WindowState = InitialWindowState;
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
