using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OWTracker.Data;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for OverviewWindow.xaml
    /// </summary>
    public partial class OverviewWindow : Window
    {
        public OverviewWindow()
        {
            InitializeComponent();
            Icon = Properties.Resources.ow.GetSource();
        }

        private uint updateCheckCounter = 1;
        private Process overwatchProcess = null;
        private bool exitEventAttached = false;
        private bool logout = false;
        private bool foundUpdates = false;

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (updateCheckCounter++ % 5 == 0 && Config.LoggedInUser != null)
            {
                if (Process.GetProcessesByName("Overwatch").Any())
                {
                    overwatchProcess = overwatchProcess ?? Process.GetProcessesByName("Overwatch").FirstOrDefault();

                    if (overwatchProcess != null && !exitEventAttached)
                    {
                        overwatchProcess.Exited += OverwatchProcess_Exited;
                        exitEventAttached = true;
                    }
                }
                else if (Config.LoggedInUser.BattleTag != null)
                {
                    UpdateOnlineProfiles();
                }
            }
            if (updateCheckCounter % 60 == 0 && !foundUpdates && SettingsManager.GetBooleanSetting("updatePeriodically") == true)
            {
                Config.SetBusyStatus("Checking for updates");
                try // Check for Updates
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    var webclient = new WebClient();
                    string updateInfo = webclient.DownloadString(Config.UpdateUrl);

                    if (v < Version.Parse(updateInfo.Split('-')[0]))
                    {
                        Config.SetFinishedStatus("Update found");
                        foundUpdates = true;
                        var uA = new UpdateAvailable();
                        uA.ShowDialog();
                    }
                    else
                    {
                        Config.SetFinishedStatus("Up to date");
                    }
                }
                catch
                {
                    Config.SetFinishedStatus("Update check failed", true);
                }
            }
            if (SettingsManager.GetBooleanSetting("recentRefresh") != false)
                Config.Overview.UpdateRecentGames();
        }

        private static void UpdateOnlineProfiles()
        {
            try
            {
                Config.SetBusyStatus("Updating online profiles");
                var c = new HttpClient();
                string masterOverwatchRefresh = $"https://masteroverwatch.com/profile/pc/us/{Config.LoggedInUser.BattleTag}/update";
                string overBuffRefresh = $"https://www.overbuff.com/players/pc/{Config.LoggedInUser.BattleTag}/refresh";
                c.GetAsync(masterOverwatchRefresh);
                c.PostAsync(overBuffRefresh, new StringContent(""));
                Config.SetFinishedStatus("Online profiles updated");
            }
            catch
            {
                Config.SetFinishedStatus("Profile update failed", error: true);
            }
        }

        private async void OverwatchProcess_Exited(object sender, EventArgs e)
        {
            Config.SetFinishedStatus("Overwatch application closed");
            await Task.Delay(300000);
            UpdateOnlineProfiles();
            overwatchProcess = null;
            exitEventAttached = false;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Overwatch Competitive Game Tracker v{v.Major}.{v.Minor}{(v.Build != 0 ? $".{v.Build}" : "")}";

            // Load tabs
            Config.Overview = new CareerOverview();
            OverviewTab.Content = Config.Overview;
            Config.ViewGames = new ViewGames();
            AllGamesTab.Content = Config.ViewGames;
            Config.Graphs = new Graphs();
            GraphsTab.Content = Config.Graphs;
            Config.Window = this;
            Config.Settings = new UserSettings();
            SettingsTab.Content = Config.Settings;

            Config.SetBusyStatus("Loading games");

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            timer.Tick += Timer_Tick;
            timer.Start();

            await Config.Refresh();

            if (!Config.LoggedInUser.EditPermissions) AddGameButton.Visibility = Visibility.Hidden;
            Config.SetFinishedStatus("Ready");
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    new AboutWindow().ShowDialog();
                    break;
            }
        }

        public async Task UpdateUI()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(
                () =>
                {
                    if (Config.LoggedInUser.EditPermissions && Config.LoggedInUser.MostRecentGame != null && Config.LoggedInUser.MostRecentGame.SkillRating > 3000)
                        SkillRatingDecayButton.Visibility = Visibility.Visible;
                    else
                        SkillRatingDecayButton.Visibility = Visibility.Hidden;
                });
            });
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Refreshing games");
            await Config.Refresh();
            Config.SetFinishedStatus("Games refreshed");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Config.LoggedInUser = null;
            new MainWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen, FirstShown = false }.Show();
            logout = true;
            Close();
        }

        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            var a = new AddGameWindow();
            a.ShowDialog();
        }

        private async void SkillRatingDecayButton_Click(object sender, RoutedEventArgs e)
        {
            Game game = Config.LoggedInUser.MostRecentGame;
            if (game == null) return;
            if (MessageBox.Show($"This will lower your skill rating to {(game.SkillRating - 50 < 3000 ? 3000 : game.SkillRating - 50)}.", "Apply SR Decay?", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                Config.SetBusyStatus("Applying SR decay");
                await Config.LoggedInUser.AddGame(new Game(Config.LoggedInUser.UserId, game.Season, DateTimeOffset.Now, game.SkillRating - 50 < 3000 ? (short)3000 : (short)(game.SkillRating - 50), false, null, Application.Current.Resources["SRDecayString"] as string, null, null, null, null, null, null));
                await Config.Refresh();
                Config.SetFinishedStatus("SR decay applied");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!logout && Config.LoggedInUser.BattleTag != null)
                UpdateOnlineProfiles();
            logout = false;
        }
    }

    public static class ExtensionMethods
    {
        public static BitmapSource GetSource(this Bitmap bitmap) => Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        public static string ToFirstLetterUpper(this string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return s.First().ToString().ToUpper() + s.Substring(1).ToLower();
        }
    }
}