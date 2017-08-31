using System;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using OWTracker.Data;
using HtmlElement = System.Windows.Forms.HtmlElement;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for CareerOverview.xaml
    /// </summary>
    public partial class CareerOverview : UserControl
    {
        private short selectedSeason = -1;

        public CareerOverview()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Username.Text = Config.LoggedInUser.Username;
            if (!Config.LoggedInUser.EditPermissions)
            {
                CompetitivePoints.IsEnabled = false;
            }
        }

        public async Task UpdateStatistics(bool refreshIcon = false)
        {
            await Task.Run(
            () =>
            {
                Dispatcher.Invoke(
                async () =>
                {

                    RecentGamesList.Items.Clear();
                    Game mostRecent = Config.LoggedInUser.MostRecentGame;

                    if (refreshIcon)
                    {
                        await RefreshIcon();
                    }

                    if (mostRecent != null)
                    {
                        GameCollection recentGames = Config.LoggedInUser.GetRecentGames(11);

                        for (int i = 0; i < (recentGames.Count < 11 ? recentGames.Count : 10); i++)
                            RecentGamesList.Items.Add(new GameListItem(recentGames[i]));

                        GameCollection allGames = Config.LoggedInUser.GetAllGames();

                        if (selectedSeason < 0) selectedSeason = mostRecent.Season;

                        GameCollection season = allGames.WhereSeasonIs(selectedSeason);

                        CompetitivePoints.Text = Config.LoggedInUser?.CompetitivePoints?.ToString("0000") ?? "0000";
                        if (Config.LoggedInUser.CompetitivePoints != null)
                        {
                            int afterSeason = Config.GetCompetitivePointsForSkillRating(season.High);
                            CompetitivePoints.ToolTip = $"After season ends +{afterSeason}\nTotal of {(Config.LoggedInUser.CompetitivePoints + afterSeason)?.ToString("0000")}\nWins to 3000: {(Config.LoggedInUser.CompetitivePoints > 3000 ? 0 : Math.Ceiling((3000 - Config.LoggedInUser.CompetitivePoints.Value) / 10f))}\nWins to 3000 (at End of Season): {(Config.LoggedInUser.CompetitivePoints + afterSeason > 3000 ? 0 : Math.Ceiling((3000 - Config.LoggedInUser.CompetitivePoints.Value - afterSeason) / 10f))}";
                        }

                        if (season.Count == 0)
                        {
                            selectedSeason = mostRecent.Season;
                            season = allGames.WhereSeasonIs(selectedSeason);
                        }

                        CurrentSR.Text = season.MostRecentSR > 0 ? season.MostRecentSR.ToString() : "PLMT";
                        if ((season.Games.FirstOrDefault()?.Season ?? 0) < 6)
                        {
                            CurrentSRIcon.Source = Config.GetImageForSkillRating(season.High < 3500 ? season.High : season.MostRecentSR >= 3500 ? season.MostRecentSR : 3000);
                        }
                        else
                        {
                            CurrentSRIcon.Source = Config.GetImageForSkillRating(season.MostRecentSR);
                        }

                        ChangeFromBest.Text = (season.MostRecentSR - allGames.High).ToString("(+#);(-#);(-0)");
                        ChangeFromAverage.Text = (season.MostRecentSR - allGames.Average).ToString("(+#);(-#);(+0)");
                        ChangeFromWorst.Text = (season.MostRecentSR - allGames.Low).ToString("(+#);(-#);(+0)");

                        SeasonChangeFromBest.Text = (season.MostRecentSR - season.High).ToString("(+#);(-#);(-0)");
                        SeasonChangeFromAverage.Text = (season.MostRecentSR - season.Average).ToString("(+#);(-#);(+0)");
                        SeasonChangeFromWorst.Text = (season.MostRecentSR - season.Low).ToString("(+#);(-#);(+0)");

                        CareerHigh.Text = allGames.High > 0 ? allGames.High.ToString() : "PLMT";
                        BestTierIcon.Source = Config.GetImageForSkillRating(allGames.High);
                        CareerAverage.Text = allGames.Average > 0 ? allGames.Average.ToString() : "PLMT";
                        AverageTierIcon.Source = Config.GetImageForSkillRating(allGames.Average);
                        CareerLow.Text = allGames.Low > 0 ? allGames.Low.ToString() : "PLMT";
                        WorstTierIcon.Source = Config.GetImageForSkillRating(allGames.Low);
                        SeasonHigh.Text = season.High > 0 ? season.High.ToString() : "PLMT";
                        SeasonBestTierIcon.Source = Config.GetImageForSkillRating(season.High);
                        SeasonAverage.Text = season.Average > 0 ? season.Average.ToString() : "PLMT";
                        SeasonAverageTierIcon.Source = Config.GetImageForSkillRating(season.Average);
                        SeasonLow.Text = season.Low > 0 ? season.Low.ToString() : "PLMT";
                        SeasonWorstTierIcon.Source = Config.GetImageForSkillRating(season.Low);
                        GamesWon.Text = allGames.GamesWon.ToString();
                        GamesLost.Text = allGames.GamesLost.ToString();
                        GamesTied.Text = allGames.GamesTied.ToString();
                        GamesPlayed.Text = allGames.GamesPlayed.ToString();
                        WinPercentage.Text = allGames.WinPercentage + "%";
                        SeasonGamesWon.Text = season.GamesWon.ToString();
                        SeasonGamesLost.Text = season.GamesLost.ToString();
                        SeasonGamesTied.Text = season.GamesTied.ToString();
                        SeasonGamesPlayed.Text = season.GamesPlayed.ToString();
                        SeasonWinPercentage.Text = season.WinPercentage + "%";
                        WinStats bestMap = allGames.BestMap;
                        WinStats mostMap = allGames.MostPlayedMap;
                        WinStats seasonBestMap = season.BestMap;
                        WinStats seasonMostMap = season.MostPlayedMap;
                        WinStats bestHero = allGames.BestHero;
                        WinStats mostHero = allGames.MostPlayedHero;
                        WinStats seasonBestHero = season.BestHero;
                        WinStats seasonMostHero = season.MostPlayedHero;

                        BestMap.Text = bestMap?.Label ?? "- - - - -";
                        BestMapToolTip.ToolTip = $"{bestMap?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{bestMap?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";
                        MostPlayedMap.Text = mostMap?.Label ?? "- - - - -";
                        MostPlayedMapToolTip.ToolTip = $"{mostMap?.GamesPlayed.ToString() ?? "??"} GAMES PLAYED";

                        MostPlayedHero.Text = mostHero?.Label ?? "- - - - -";
                        MostPlayedHeroToolTip.ToolTip = $"{mostHero?.GamesPlayed.ToString() ?? "??"} GAMES PLAYED";
                        BestHero.Text = bestHero?.Label ?? "- - - - -";
                        BestHeroTooltip.ToolTip = $"{bestHero?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{bestHero?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";

                        SeasonBestMap.Text = seasonBestMap?.Label ?? "- - - - -";
                        SeasonBestMapToolTip.ToolTip = $"{seasonBestMap?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{seasonBestMap?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";
                        SeasonMostPlayedHero.Text = seasonMostHero?.Label ?? "- - - - -";
                        SeasonMostPlayedHeroToolTip.ToolTip = $"{seasonMostHero?.GamesPlayed.ToString() ?? "??"} GAMES PLAYED";

                        SeasonBestHero.Text = seasonBestHero?.Label ?? "- - - - -";
                        SeasonBestHeroToolTip.ToolTip = $"{seasonBestHero?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{seasonBestHero?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";
                        SeasonMostPlayedMap.Text = seasonMostMap?.Label ?? "- - - - -";
                        SeasonMostPlayedMapToolTip.ToolTip = $"{seasonMostMap?.GamesPlayed.ToString() ?? "??"} GAMES PLAYED";

                        ChangeFromPlacement.Text = season.ChangeFromFirst.ToString("(+#);(-#);(+0)");
                        ChangeFromPlacement.ToolTip = $"Placement rank {season.MostRecentSR - season.ChangeFromFirst}";

                        UpdateRecentGames();

                        CurrentLabel.Content = selectedSeason == mostRecent.Season ? "CURRENT" : "FINAL";

                        var c = new ComboBox { Width = 200, Height = 28, FontSize = 18, ItemsSource = allGames.SeasonComboBoxSource, SelectedValue = $"SEASON {selectedSeason}" };
                        c.SelectionChanged += SelectedSeason_Changed;
                        SeasonGroupBox.Header = c;
                    }
                    else
                    {
                        CurrentSR.Text = "- - - -";
                        CurrentSRIcon.Source = Config.GetImageForSkillRating(0);
                        CareerHigh.Text = "- - - -";
                        BestTierIcon.Source = Config.GetImageForSkillRating(0);
                        CareerAverage.Text = "- - - -";
                        AverageTierIcon.Source = Config.GetImageForSkillRating(0);
                        CareerLow.Text = "- - - -";
                        WorstTierIcon.Source = Config.GetImageForSkillRating(0);
                        SeasonHigh.Text = "- - - -";
                        SeasonBestTierIcon.Source = Config.GetImageForSkillRating(0);
                        SeasonAverage.Text = "- - - -";
                        SeasonAverageTierIcon.Source = Config.GetImageForSkillRating(0);
                        SeasonLow.Text = "- - - -";
                        SeasonWorstTierIcon.Source = Config.GetImageForSkillRating(0);
                        GamesWon.Text = "--";
                        GamesLost.Text = "--";
                        GamesTied.Text = "--";
                        GamesPlayed.Text = "--";
                        SeasonGamesPlayed.Text = "--";
                        WinPercentage.Text = "--%";
                        SeasonGamesWon.Text = "--";
                        SeasonGamesLost.Text = "--";
                        SeasonGamesTied.Text = "--";
                        SeasonWinPercentage.Text = "--%";
                        BestMap.Text = "- - - - - -";
                        BestHero.Text = "- - - - -";
                        SeasonBestMap.Text = "- - - - -";
                        SeasonBestHero.Text = "- - - - -";
                        ChangeFromPlacement.Text = "--";

                        SeasonGroupBox.Header = "SEASON";
                    }
                });
            });
        }

        private void Wb_Navigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            try
            {
                var browser = (System.Windows.Forms.WebBrowser)sender;
                bool showStatus = (bool)browser.Tag;
                var elements = browser.Document.GetElementsByTagName("img");

                foreach (HtmlElement element in elements.Cast<HtmlElement>().Where(element => element.GetAttribute("className") == "player-portrait"))
                {
                    PlayerIcon.Source = new BitmapImage(new Uri(element.GetAttribute("src")));
                    if (showStatus)
                    {
                        Config.SetFinishedStatus("Player icon updated");
                    }
                    return;
                }
                if (showStatus)
                {
                    Config.SetFinishedStatus("Icon not found, try again", error: true);
                }
            }
            catch
            {
                PlayerIcon.Source = Properties.Resources.default_avatar.GetSource();
                Config.SetFinishedStatus("Icon not found, try again", error: true);
            }
        }

        public void UpdateRecentGames()
        {
            GameCollection allGames = Config.LoggedInUser.GetAllGames();
            var (s, hours) = SettingsManager.GetSetting<byte>("recentHours");
            hours = (byte)(s ? hours : 12);
            RecentGamesGroupBox.Header = allGames.GetRecentStatusString(hours);
        }

        private async void SelectedSeason_Changed(object sender, SelectionChangedEventArgs e)
        {
            selectedSeason = short.TryParse((((ComboBox)sender).SelectedValue as string)?.Split(' ')[1], out selectedSeason) ? selectedSeason : (short)-1;
            await UpdateStatistics();
        }

        private async void RecentGamesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentGamesList.SelectedItems.Count > 0)
            {
                var w = new AddGameWindow(((GameListItem)RecentGamesList.SelectedItem).GameId);
                w.ShowDialog();
                if (!w.Cancel)
                    await Config.Refresh();
            }
        }

        private async void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                case Key.Back:
                    if (RecentGamesList.SelectedItems.Count > 0 && MessageBox.Show($"Are you sure you want to delete {RecentGamesList.SelectedItems.Count} items?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    {
                        var games = RecentGamesList.SelectedItems.Cast<GameListItem>().OrderByDescending(x => x.GameId).ToList();
                        for (int i = 0; i < games.Count; i++)
                        {
                            Config.SetBusyStatus($"Deleting game ({i + 1}/{games.Count})");
                            await Config.LoggedInUser.DeleteGame(games[i].GameId);
                        }
                    }
                    await Config.Refresh();
                    Config.SetFinishedStatus("Game(s) deleted");
                    break;
            }
        }

        private async void CompetitivePoints_LostFocus(object sender, RoutedEventArgs e)
        {
            await UpdateCompetitivePoints();
        }

        private async void CompetitivePoints_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await UpdateCompetitivePoints();
            }
        }

        private async Task UpdateCompetitivePoints()
        {
            Config.SetBusyStatus("Updating competitive points");
            if (int.TryParse(CompetitivePoints.Text, out int points) && points >= 0)
            {
                await Config.LoggedInUser.UpdateCompetitivePoints(points);
                CompetitivePoints.Text = Config.LoggedInUser?.CompetitivePoints?.ToString("0000") ?? "0000";
            }
            else
                CompetitivePoints.Text = Config.LoggedInUser?.CompetitivePoints?.ToString("0000") ?? "0000";

            if (Config.LoggedInUser?.MostRecentGame != null && Config.LoggedInUser.CompetitivePoints != null)
            {
                int afterSeason = Config.GetCompetitivePointsForSkillRating(Config.LoggedInUser.GetGamesBySeason(Config.LoggedInUser.MostRecentGame.Season).High);
                CompetitivePoints.ToolTip = $"After season ends +{afterSeason}\nTotal of {(Config.LoggedInUser.CompetitivePoints + afterSeason)?.ToString("0000")}";
            }

            Config.SetFinishedStatus("Competitive points updated");
        }

        private async void RefreshIcon_Click(object sender, RoutedEventArgs e)
        {
            await RefreshIcon(showStatus: true);
        }

        private async Task RefreshIcon(bool showStatus = false)
        {
            if (showStatus)
            {
                Config.SetBusyStatus("Updating player icon");
            }
            await Task.Run(
            () =>
            {
                Dispatcher.Invoke(
                () =>
                {
                    try
                    {
                        if (Config.LoggedInUser.BattleTag != null)
                        {
                            var wb = new System.Windows.Forms.WebBrowser();
                            wb.Navigated += Wb_Navigated;
                            wb.Navigate($"https://playoverwatch.com/en-us/career/pc/us/{Config.LoggedInUser.BattleTag}");
                            wb.Tag = showStatus;
                        }
                        else
                        {
                            if (showStatus)
                            {
                                Config.SetFinishedStatus("BattleTag not found", error: true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        PlayerIcon.Source = Properties.Resources.default_avatar.GetSource();
                        if (showStatus)
                        {
                            Config.SetFinishedStatus("Error finding player icon", error: true);
                        }
                    }
                });
            });
        }

        private void RefreshIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            RefreshIconButton.Visibility = Visibility.Visible;
        }

        private void RefreshIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            RefreshIconButton.Visibility = Visibility.Hidden;
        }
    }
}