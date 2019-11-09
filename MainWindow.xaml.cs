using DR2Rallymaster.Services;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.Win32;
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
            statusLabel.Visibility = Visibility.Visible;

            var clubInfoModel = await clubInfoService.GetClubInfo(clubId.Text);

            searchProgressRing.IsActive = false;
            statusLabel.Visibility = Visibility.Hidden;

            // clear the UI before populating it
            ClearUi();

            if (clubInfoModel == null)
            {
                clubNameLabel.Text = "Error";
                clubDescLabel.Text = "Racenet didn't return any data.";
                return;
            }

            // populate club title and desc
            clubNameLabel.Text = clubInfoModel.ClubInfo.Club.Name;
            clubDescLabel.Text = clubInfoModel.ClubInfo.Club.Description;


            // populate club info datagrid (there is a much better way to do it than this, this is so ugly)
            if (clubInfoModel.ClubInfo != null && clubInfoModel.ClubInfo.Club != null)
            {
                try
                {
                    var club = clubInfoModel.ClubInfo.Club;
                    //clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("Club Info: ", ""));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("id", club.Id));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("hasActiveChampionship", club.HasActiveChampionship.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("hasFutureChampionship", club.HasFutureChampionship.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("memberCount", club.MemberCount.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("clubAccessType", club.ClubAccessType));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("pendingInvites", clubInfoModel.ClubInfo.PendingInvites.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("", ""));

                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("Current User Data: ", ""));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("isMember", club.IsMember.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("role", clubInfoModel.ClubInfo.Role.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("hasAskedToJoin", club.HasBeenInvitedToJoin.ToString()));
                    clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("hasBeenInvitedToJoin", club.HasBeenInvitedToJoin.ToString()));

                    if (clubInfoModel.ClubInfo.Permissions != null)
                    {
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canEditClubSettings", clubInfoModel.ClubInfo.Permissions.CanEditClubSettings.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canDisbandClub", clubInfoModel.ClubInfo.Permissions.CanDisbandClub.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canCancelChampionship", clubInfoModel.ClubInfo.Permissions.CanCancelChampionship.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canAcceptOrDenyJoinRequest", clubInfoModel.ClubInfo.Permissions.CanAcceptOrDenyJoinRequest.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canCreateChampionship", clubInfoModel.ClubInfo.Permissions.CanCreateChampionship.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canPromoteToAdmin", clubInfoModel.ClubInfo.Permissions.CanPromoteToAdmin.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canPromoteToOwner", clubInfoModel.ClubInfo.Permissions.CanPromoteToOwner.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canDemoteToAdmin", clubInfoModel.ClubInfo.Permissions.CanDemoteToAdmin.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canDemoteToPlayer", clubInfoModel.ClubInfo.Permissions.CanDemoteToPlayer.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("canKickMember", clubInfoModel.ClubInfo.Permissions.CanKickMember.ToString()));
                    }


                    if (club.MyChampionshipProgress != null)
                    {
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("", ""));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("Current User Championship Progress:", ""));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("eventCount", club.MyChampionshipProgress.EventCount.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("finishedCount", club.MyChampionshipProgress.FinishedCount.ToString()));
                        clubInfoDataGrid.Items.Add(new KeyValuePair<string, string>("completedCount", club.MyChampionshipProgress.CompletedCount.ToString()));
                    }

                    clubInfoTab.IsSelected = true;
                }
                catch (Exception exp)
                {
                    // TODO error handling, there was probably a null ref error somewhere
                    //throw;
                }
            }

            // populate championships list
            for (int i=0; i < clubInfoModel.Championships.Length; i++)
            {
                var championshipId = clubInfoModel.Championships[i].Id;
                var content = String.Format("id: {0} - active: {1}", championshipId, clubInfoModel.Championships[i].IsActive.ToString());
                championshipsListBox.Items.Add(new KeyValuePair<string, string>(championshipId, content));
            }
        }

        // Called when a championship is selected
        private void ChampionshipListBox_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                // disable the export button
                btnGetStageResults.Visibility = Visibility.Hidden;

                if (championshipsListBox.SelectedItem == null)
                    return;

                // get the championship ID
                var championshipId = ((KeyValuePair<string, string>)championshipsListBox.SelectedItem).Key;
                if (championshipId == null || String.IsNullOrWhiteSpace(championshipId))
                    return; // TODO error handling

                // clear event listbox and championship info datagird
                eventsListBox.Items.Clear();
                championshipInfoDataGrid.Items.Clear();

                // populate championship info datagrid
                var championshipMetaData = clubInfoService.GetChampionshipMetadata(championshipId);
                if (championshipMetaData == null)
                    return; // TODO error handling

                championshipInfoDataGrid.Items.Add(new KeyValuePair<string, string>("id", championshipMetaData.Id));
                championshipInfoDataGrid.Items.Add(new KeyValuePair<string, string>("name", championshipMetaData.Name));
                championshipInfoDataGrid.Items.Add(new KeyValuePair<string, string>("isActive", championshipMetaData.IsActive.ToString()));
                championshipInfoTab.IsSelected = true;

                // populate event listbox
                var eventsMetadata = championshipMetaData.Events.ToList();

                foreach (var eventMeta in eventsMetadata)
                {
                    var eventId = eventMeta.Id;
                    var content = String.Format("{0} - {1}", eventMeta.CountryName, eventMeta.EventStatus);
                    eventsListBox.Items.Add(new KeyValuePair<string, string>(eventId, content));
                }
            }
            catch (Exception exp)
            {
                // TODO error handling
                //throw;
            }
        }

        private void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // if there is no selected event, exit
            if (championshipsListBox.SelectedItem == null || eventsListBox.SelectedItem == null)
                return;

            try
            {
                // get the championship ID
                var championshipId = ((KeyValuePair<string, string>)championshipsListBox.SelectedItem).Key;
                if (championshipId == null || String.IsNullOrWhiteSpace(championshipId))
                    return; // TODO error handling

                // get the event ID
                var eventId = ((KeyValuePair<string, string>)eventsListBox.SelectedItem).Key;
                if (eventId == null || String.IsNullOrWhiteSpace(eventId))
                    return; // TODO error handling

                // clear event info datagrid
                eventInfoDataGrid.Items.Clear();

                // populate event info datagrid
                var eventMetaData = clubInfoService.GetEventMetadata(championshipId, eventId);
                if (eventMetaData == null)
                    return; // TODO error handling

                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("id", eventMetaData.Id));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("countryId", eventMetaData.CountryId));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("countryName", eventMetaData.CountryName));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("locationId", eventMetaData.LocationId));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("locationName", eventMetaData.LocationName));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("firstStageRouteId", eventMetaData.FirstStageRouteId));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("firstStageConditions", eventMetaData.FirstStageConditions));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("hasParticipated", eventMetaData.HasParticipated.ToString()));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("eventStatus", eventMetaData.EventStatus));
                eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("eventTime", eventMetaData.EventTime));

                if (eventMetaData.EntryWindow != null)
                {
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("", ""));
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("entryWindow", "(Times are UTC)"));
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("start", eventMetaData.EntryWindow.Start.ToUniversalTime().ToString()));
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("open", eventMetaData.EntryWindow.Open.ToUniversalTime().ToString()));
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("close", eventMetaData.EntryWindow.Close.ToUniversalTime().ToString()));
                    eventInfoDataGrid.Items.Add(new KeyValuePair<string, string>("end", eventMetaData.EntryWindow.End.ToUniversalTime().ToString()));
                }

                eventInfoTab.IsSelected = true;

                // enable the export button
                btnGetStageResults.Visibility = Visibility.Visible;
            }
            catch(Exception exp)
            {
                // TODO error handling
                //throw;
            }
        }

        private async void GetStageResults_Click(object sender, RoutedEventArgs e)
        {
            // if there is no selected event, exit
            if (championshipsListBox.SelectedItem == null || eventsListBox.SelectedItem == null)
                return;

            // get the championship ID
            var championshipId = ((KeyValuePair<string, string>)championshipsListBox.SelectedItem).Key;
            if (championshipId == null || String.IsNullOrWhiteSpace(championshipId))
                return; // TODO error handling

            // get the event ID
            var eventId = ((KeyValuePair<string, string>)eventsListBox.SelectedItem).Key;
            if (eventId == null || String.IsNullOrWhiteSpace(eventId))
                return; // TODO error handling

            // restore the last selected path
            var lastSelectedExportPath = Properties.Settings.Default["LastSelectedExportPath"] as string;
            if (string.IsNullOrWhiteSpace(lastSelectedExportPath))
                lastSelectedExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // ask the user where to save
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = lastSelectedExportPath;
            saveFileDialog.Filter = "csv files (*.csv)|*.csv";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                searchProgressRing.IsActive = true;
                statusLabel.Visibility = Visibility.Visible;

                try
                {
                    await clubInfoService.SaveStageDataToCsv(championshipId, eventId, saveFileDialog.FileName);
                }
                catch (Exception exp)
                {

                    MessageBox.Show(exp.Message);
                }

                searchProgressRing.IsActive = false;
                statusLabel.Visibility = Visibility.Hidden;

                // save the last selected path
                Properties.Settings.Default["LastSelectedExportPath"] = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                Properties.Settings.Default.Save();
            }
        }

        // Clears all UI elements
        private void ClearUi()
        {
            clubNameLabel.Text = "";
            clubDescLabel.Text = "";
            championshipsListBox.Items.Clear();
            eventsListBox.Items.Clear();
            clubInfoDataGrid.Items.Clear();
            championshipInfoDataGrid.Items.Clear();
            eventInfoDataGrid.Items.Clear();
        }

        // Display the Codies login window
        // Once it is closed, the login window will populate the SharedCookieContainer
        // Use this to create the racenet api utilities object
        // with the authentication cookies from the login session
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            // clear cookie container
            SharedCookieContainer = null;

            var browserWindow = new CodiesLoginWindow();
            browserWindow.Owner = Application.Current.MainWindow;
            browserWindow.ShowDialog();

            if (SharedCookieContainer != null)
                clubInfoService = new ClubInfoService(SharedCookieContainer);

            // TODO: we should wait for the GetInitialState call to allow the user to proceed
            // TODO show indication that user is logged in
        }
    }
}
