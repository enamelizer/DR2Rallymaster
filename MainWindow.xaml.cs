using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace DR2Rallymaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public CookieContainer SharedCookieContainer { get; set; }
        private RacenetApiUtilities racenetApi = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeThemes();
        }

        private string currentTheme;
        private string currentAccent;
        private void InitializeThemes()
        {
            currentTheme = Properties.Settings.Default.BaseTheme;
            currentAccent = Properties.Settings.Default.Accent;
            ChangeThemeAndAccent(currentTheme, currentAccent);

            foreach (var theme in ThemeManager.AppThemes)
            {
                var menuItem = new MenuItem();
                menuItem.Header = theme.Name;
                menuItem.Click += MenuItem_Click;
                menuItemThemes.Items.Add(menuItem);
            }

            foreach (var accent in ThemeManager.Accents)
            {
                var menuItem = new MenuItem();
                menuItem.Header = accent.Name;
                menuItem.Click += MenuItem_Click;
                menuItemAccents.Items.Add(menuItem);
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.BaseTheme = currentTheme;
            Properties.Settings.Default.Accent = currentAccent;
            Properties.Settings.Default.Save();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            btnSettings.ContextMenu.IsOpen = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // cast the sender to MenuItem, get the header and pass it to the themer
            var header = ((MenuItem)sender).Header.ToString();

            if (!String.IsNullOrWhiteSpace(header))
            {
                if (header == "BaseLight" || header == "BaseDark")
                    ChangeTheme(header);
                else
                    ChangeAccent(header);
            }
        }

        private void ChangeTheme(string themeName)
        {
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(currentAccent), ThemeManager.GetAppTheme(themeName));
            currentTheme = themeName;
        }

        private void ChangeAccent(string accent)
        {
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(accent), ThemeManager.GetAppTheme(currentTheme));
            currentAccent = accent;
        }

        private void ChangeThemeAndAccent(string theme, string accent)
        {
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(accent), ThemeManager.GetAppTheme(theme));
        }

        private void ClubSearch_Click(object sender, RoutedEventArgs e)
        {
            racenetApi.GetClubInfo(clubId.Text);
        }

        // Display the Codies login window
        // Once it is closed, the login window will populate the SharedCookieContainer
        // Use this to create the racenet api utilities object
        // with the authentication cookies from the login session
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            var browserWindow = new CodiesLoginWindow();
            browserWindow.Owner = Application.Current.MainWindow;
            browserWindow.ShowDialog();

            if (SharedCookieContainer != null)
                racenetApi = new RacenetApiUtilities(SharedCookieContainer);
        }
    }
}
