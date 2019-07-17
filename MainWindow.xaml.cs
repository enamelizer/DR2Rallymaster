using DR2Rallymaster.Services;
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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// I started out intending for this to be a pure WPF MVVM app, but after spending
// far too long learning the framework and far too little time producing meaningful
// code, I am falling back to my WinForm ways in order to Get Stuff Done.
// Forgive me, for I have sinned.

namespace DR2Rallymaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        // This gets set when the login window is closed and used in the API calls
        public CookieContainer SharedCookieContainer { get; set; }

        // The control class for ClubInfo
        private ClubInfoService clubInfoService = null;

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

        private async void ClubSearch_Click(object sender, RoutedEventArgs e)
        {
            if (clubInfoService == null || String.IsNullOrWhiteSpace(clubId.Text))
            {
                // TODO error reporting
                return;
            }

            // do search
            searchProgressRing.IsActive = true;
            var clubInfoModel = await clubInfoService.GetClubInfo(clubId.Text);
            searchProgressRing.IsActive = false;

            if (clubInfoModel == null)
            {
                // TODO error reporting
            }

            clubNameLabel.Text = clubInfoModel.ClubName;
            clubDescLabel.Text = clubInfoModel.ClubDesc;

            for(int i=0; i < clubInfoModel.Championships.Length; i++)
            {
                var toggleButton = new ToggleButton();
                var content = String.Format("id: {0} - active: {1}", clubInfoModel.Championships[i].Id, clubInfoModel.Championships[i].IsActive.ToString());

                toggleButton.Content = content;
                ChampionshipItemControl.Items.Add(toggleButton);
            }
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
                clubInfoService = new ClubInfoService(SharedCookieContainer);
        }
    }
}
