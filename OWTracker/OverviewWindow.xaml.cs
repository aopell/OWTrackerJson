using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
        private DispatcherTimer timer;

        private Dictionary<FrameworkElement, bool> visibleButtons = new Dictionary<FrameworkElement, bool>();

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (overwatchProcess == null && Config.LoggedInUser != null && Process.GetProcessesByName("Overwatch").Any())
            {
                Config.Settings.UpdateOnlineProfiles.IsEnabled = false;
                overwatchProcess = overwatchProcess ?? Process.GetProcessesByName("Overwatch").FirstOrDefault();

                if (overwatchProcess != null && !exitEventAttached)
                {
                    Config.SetFinishedStatus("Overwatch application detected");
                    overwatchProcess.EnableRaisingEvents = true;
                    overwatchProcess.Exited += OverwatchProcess_Exited;
                    exitEventAttached = true;
                }
            }
            if (updateCheckCounter % 30 == 0 && overwatchProcess == null && Config.LoggedInUser.BattleTag != null)
            {
                UpdateOnlineProfiles();
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
            await Dispatcher.Invoke(
            async () =>
            {
                Config.SetFinishedStatus("Overwatch application closed");
                Config.Settings.UpdateOnlineProfiles.IsEnabled = true;
                await Task.Delay(TimeSpan.FromMinutes(5));
                Config.SetBusyStatus("Updating online profiles");
                UpdateOnlineProfiles();
                Config.SetFinishedStatus("Online profiles updated");
                overwatchProcess = null;
                exitEventAttached = false;
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            visibleButtons.Add(LogOutButton, true);
            visibleButtons.Add(RefreshButton, true);
            visibleButtons.Add(AddGameButton, true);
            visibleButtons.Add(InfoButton, false);
            visibleButtons.Add(SkillRatingDecayButton, true);

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

            if (!Config.LoggedInUser.EditPermissions) visibleButtons[AddGameButton] = false;

            Config.SetBusyStatus("Loading games");

            timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            timer.Tick += Timer_Tick;
            Timer_Tick(timer, new EventArgs());
            timer.Start();

            await Task.Run(
            () =>
            {
                Dispatcher.Invoke(
                async () =>
                {
                    await Config.Refresh();
                    Config.SetFinishedStatus("Ready");
                });
            });
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
                        visibleButtons[SkillRatingDecayButton] = true;
                    else
                        visibleButtons[SkillRatingDecayButton] = false;

                    int marginOffset = 10;
                    foreach (var kvp in visibleButtons)
                    {
                        var button = kvp.Key;
                        if (kvp.Value)
                        {
                            button.Visibility = Visibility.Visible;
                            button.Margin = new Thickness(button.Margin.Left, button.Margin.Top, marginOffset, button.Margin.Bottom);
                            marginOffset += (int)button.Width;
                        }
                        else
                        {
                            button.Visibility = Visibility.Hidden;
                        }
                    }

                    StatusText.Margin = new Thickness(StatusText.Margin.Left, StatusText.Margin.Top, marginOffset + 10, StatusText.Margin.Bottom);
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
            timer?.Stop();
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
            const short skillRatingDecayAmount = 25;
            const short minSkillRatingDecayRank = 3000;

            Game game = Config.LoggedInUser.MostRecentGame;
            if (game == null) return;

            var dialog = new TextBoxDialog("Please enter your current rank to apply skill rating decay", "Confirm Rank", "Cancel", DialogType.TextEntry);
            dialog.ShowDialog();
            string result = dialog.Result;
            if (result == null) return;
            short sr;
            while (!short.TryParse(result, out sr) || sr != minSkillRatingDecayRank && (sr < minSkillRatingDecayRank || sr > game.SkillRating || (game.SkillRating - sr) % 25 != 0))
            {
                dialog = new TextBoxDialog($"Invalid skill rating amount. Decay can only exist in increments of {skillRatingDecayAmount} SR except for when decay reaches {minSkillRatingDecayRank}. Please enter a valid new skill rating.", "Confirm Rank", "Cancel", DialogType.TextEntry, System.Windows.Media.Colors.Red);
                dialog.ShowDialog();
                result = dialog.Result;
                if (result == null) return;
            }

            if (MessageBox.Show($"This will lower your skill rating to {sr}.", "Apply SR Decay?", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                Config.SetBusyStatus("Applying SR decay");
                await Config.LoggedInUser.AddGame(new Game(Config.LoggedInUser.UserId, game.Season, DateTimeOffset.Now, sr, false, null, Application.Current.Resources["SRDecayString"] as string, null, null, null, null, null, null));
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

        private void ViewGames_Unselect(object sender, RoutedEventArgs e)
        {
            Config.ViewGames.DisplayItems(20);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var colors = new[]
            {
                Colors.Red,
                Colors.White,
                System.Windows.Media.Color.FromArgb(255, 190, 130, 49),
                Colors.Yellow
            };

            Random random = new Random();

            InfoButton.Foreground = new SolidColorBrush(colors[random.Next(colors.Length)]);
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