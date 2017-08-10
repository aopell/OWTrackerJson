using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OWTracker.Data;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for ViewGames.xaml
    /// </summary>
    public partial class ViewGames : UserControl
    {
        private GameCollection games;
        private int itemsShown = 20;

        public ViewGames()
        {
            InitializeComponent();
            ErrorString.Visibility = Visibility.Hidden;
            MapSelect.ItemsSource = ((string[])Application.Current.Resources["Maps"]).ToList<object>();
            ResultSelect.ItemsSource = new List<object> { "WIN", "LOSS", "DRAW" };
            GroupSelect.ItemsSource = new List<object> { 1, 2, 3, 4, 5, 6 };
            games = Config.LoggedInUser.GetAllGames();
            HeroSelect.ItemsSource = Config.LoggedInUser.GetAllGames().HeroesComboBoxSource.ToList<object>();
            SeasonSelect.ItemsSource = games.SeasonComboBoxSource.ToList<object>();
        }

        private void UpdateGamesList(bool forceReplaceGames = false)
        {
            if (forceReplaceGames)
            {
                ClearAllButton_Click(null, null);
                games = Config.LoggedInUser.GetAllGames();
                MapSelect.ItemsSource = ((string[])Application.Current.Resources["Maps"]).ToList<object>();
                ResultSelect.ItemsSource = new List<object> { "WIN", "LOSS", "DRAW" };
                GroupSelect.ItemsSource = new List<object> { 1, 2, 3, 4, 5, 6 };

                HeroSelect.ItemsSource = Config.LoggedInUser.GetAllGames().HeroesComboBoxSource.ToList<object>();
                SeasonSelect.ItemsSource = games.SeasonComboBoxSource.ToList<object>();

                MapSelect.SelectNone();
                ResultSelect.SelectNone();
                GroupSelect.SelectNone();
                HeroSelect.SelectNone();
                SeasonSelect.SelectNone();
            }

            GamesList.ItemsSource = null;
            games = games ?? Config.LoggedInUser.GetAllGames();

            var items = UpdateItems(itemsShown);
            GamesList.ItemsSource = items;

            GamesWon.Text = games.GamesWon.ToString();
            GamesLost.Text = games.GamesLost.ToString();
            GamesTied.Text = games.GamesTied.ToString();
            GamesPlayed.Text = games.GamesPlayed.ToString();
            WinPercentage.Text = $"{games.WinPercentage}%";

            TotalRankChange.Text = games.Games.Sum(x => x.SkillRatingDifference)?.ToString("+#;-#;+0") ?? "--";
            TotalRankGained.Text = games.Games.Sum(x => x.SkillRatingDifference > 0 ? x.SkillRatingDifference : 0)?.ToString("+#;-#;+0") ?? "--";
            TotalRankLost.Text = games.Games.Sum(x => x.SkillRatingDifference < 0 ? x.SkillRatingDifference : 0)?.ToString("+#;-#;+0") ?? "--";

            double? avgRankChange = games.Games.Average(x => x.SkillRatingDifference);
            AverageRankChange.Text = avgRankChange?.ToString("+0.###;-0.###;+0") ?? "--";
            double? avgRankWon = games.Games.Where(x => x.GameWon == true).Average(x => x.SkillRatingDifference);
            AverageRankGain.Text = avgRankWon?.ToString("+0.###;-0.###;+0") ?? "--";
            double? avgRankLost = games.Games.Where(x => x.GameWon == false).Average(x => x.SkillRatingDifference);
            AverageRankLoss.Text = avgRankLost?.ToString("+0.###;-0.###;+0") ?? "--";

            AttackFirst.Text = games.Games.Count(x => x.AttackFirst == true).ToString();
            DefendFirst.Text = games.Games.Count(x => x.AttackFirst == false).ToString();
            double? avgRounds = games.Games.Average(x => x.Rounds);
            AverageRounds.Text = avgRounds == null ? "--" : Math.Round(avgRounds.Value, 1).ToString();
            double? avgLength = games.Games.Average(x => x.GameLength);
            AverageGameLength.Text = avgLength == null ? "-:--" : $"{(int)avgLength / 60:00}:{(int)avgLength % 60:00}";
            WinStats bestHero = games.BestHero;
            BestHero.Text = bestHero?.Label ?? "- - - - -";
            BestHeroTooltip.ToolTip = $"{bestHero?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{bestHero?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";
            WinStats bestMap = games.BestMap;
            BestMap.Text = bestMap?.Label ?? "- - - - -";
            BestMapToolTip.ToolTip = $"{bestMap?.WinPercentage.ToString() ?? "??"}% WIN RATE\n{bestMap?.SkillRatingChange.ToString("+#;-#;+0") ?? "??"} SKILL RATING";
        }

        private List<SmallGameListItem> UpdateItems(int count)
        {
            List<SmallGameListItem> items = GamesList.Items.Cast<SmallGameListItem>().ToList();
            items.AddRange(games.Games.Skip(GamesList.Items.Count).Take(count - GamesList.Items.Count).Select(g => new SmallGameListItem(g)));
            return items;
        }

        private void GamesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GamesList.SelectedItems.Count > 0)
            {
                var w = new AddGameWindow(((SmallGameListItem)GamesList.SelectedItem).GameId);
                w.ShowDialog();
                if (!w.Cancel)
                    UpdateGamesList();
            }
        }

        private async void FilterGamesButton_Click(object sender, RoutedEventArgs e)
        {
            await FilterGames();
        }

        public async Task FilterGames(bool forceReplaceGames = false)
        {
            await Task.Run(
            () =>
            {
                Dispatcher.Invoke(
                () =>
                {
                    try
                    {
                        var defaultBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                        MinSkillRating.BorderBrush = defaultBrush;
                        MaxSkillRating.BorderBrush = defaultBrush;
                        NumberOfRounds.BorderBrush = defaultBrush;
                        BlueTeamScore.BorderBrush = defaultBrush;
                        RedTeamScore.BorderBrush = defaultBrush;
                        MinSeconds.BorderBrush = defaultBrush;
                        MinMinutes.BorderBrush = defaultBrush;
                        MaxSeconds.BorderBrush = defaultBrush;
                        MaxMinutes.BorderBrush = defaultBrush;
                        ErrorString.Visibility = Visibility.Hidden;

                        var red = new SolidColorBrush(Colors.Red);
                        if (!string.IsNullOrEmpty(MinSkillRating.Text) && !short.TryParse(MinSkillRating.Text, out short _))
                        {
                            MinSkillRating.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if (!string.IsNullOrEmpty(MaxSkillRating.Text) && !short.TryParse(MaxSkillRating.Text, out short _))
                        {
                            MaxSkillRating.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if (!string.IsNullOrEmpty(NumberOfRounds.Text) && !byte.TryParse(NumberOfRounds.Text, out byte _))
                        {
                            NumberOfRounds.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if (!string.IsNullOrEmpty(BlueTeamScore.Text) && !byte.TryParse(BlueTeamScore.Text, out byte _))
                        {
                            BlueTeamScore.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if (!string.IsNullOrEmpty(RedTeamScore.Text) && !byte.TryParse(RedTeamScore.Text, out byte _))
                        {
                            RedTeamScore.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if ((!string.IsNullOrEmpty(MinMinutes.Text) || !string.IsNullOrEmpty(MinSeconds.Text)) && (!byte.TryParse(MinMinutes.Text, out byte _) || !byte.TryParse(MinSeconds.Text, out byte _)))
                        {
                            MinSeconds.BorderBrush = red;
                            MinMinutes.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }
                        if ((!string.IsNullOrEmpty(MaxMinutes.Text) || !string.IsNullOrEmpty(MaxSeconds.Text)) && (!byte.TryParse(MaxMinutes.Text, out byte _) || !byte.TryParse(MaxSeconds.Text, out byte _)))
                        {
                            MaxSeconds.BorderBrush = red;
                            MaxMinutes.BorderBrush = red;
                            ErrorString.Visibility = Visibility.Visible;
                        }

                        var selectedSeasons = SeasonSelect.SelectedItems?.Select(x => short.Parse(x.ToString().Split(' ')[1]));
                        var selectedResults = new List<bool?>();
                        if (ResultSelect.SelectedItems != null)
                        {
                            if (ResultSelect.SelectedItems.Contains("WIN")) selectedResults.Add(true);
                            if (ResultSelect.SelectedItems.Contains("LOSS")) selectedResults.Add(false);
                            if (ResultSelect.SelectedItems.Contains("DRAW")) selectedResults.Add(null);
                        }
                        var selectedHeroes = HeroSelect.SelectedItems?.Select(x => x.ToString());
                        var selectedMaps = MapSelect.SelectedItems?.Select(x => x.ToString());
                        var groupSizes = GroupSelect.SelectedItems?.Select(x => byte.Parse(x.ToString()));

                        games = new GameCollection(
                        from game in Config.LoggedInUser.GetGames(
                        startDate: StartDate.SelectedDate,
                        endDate: EndDate.SelectedDate,
                        skillRatingMin: short.TryParse(MinSkillRating.Text, out short s) ? s : (short?)null,
                        skillRatingMax: short.TryParse(MaxSkillRating.Text, out short m) ? m : (short?)null,
                        attackFirst: SideSelect.SelectedValue == null ? (bool?)null : SideSelect.SelectedValue as string == "ATTACK",
                        rounds: byte.TryParse(NumberOfRounds.Text, out byte n) ? n : (byte?)null,
                        bscore: byte.TryParse(BlueTeamScore.Text, out byte b) ? b : (byte?)null,
                        rscore: byte.TryParse(RedTeamScore.Text, out byte r) ? r : (byte?)null,
                        gameLengthMin: byte.TryParse(MinMinutes.Text, out byte lm) && byte.TryParse(MinSeconds.Text, out byte ls) ? lm * 60 + ls : (int?)null,
                        gameLengthMax: byte.TryParse(MaxMinutes.Text, out byte mm) && byte.TryParse(MaxSeconds.Text, out byte ms) ? mm * 60 + ms : (int?)null
                        ).Games
                        where
                        (SeasonSelect.NoneSelected || selectedSeasons.Contains(game.Season)) &&
                        (ResultSelect.NoneSelected || selectedResults.Contains(game.GameWon)) &&
                        (HeroSelect.NoneSelected || game.HeroesList != null && selectedHeroes.Any(x => game.HeroesList.Contains(x))) &&
                        (MapSelect.NoneSelected || game.Map != null && selectedMaps.Contains(game.Map)) &&
                        (GroupSelect.NoneSelected || game.GroupSize != null && groupSizes.Contains(game.GroupSize.Value)) &&
                        (string.IsNullOrWhiteSpace(NotesTextBox.Text) || (game.Notes != null && game.Notes.ToLower().Contains(NotesTextBox.Text.ToLower())))
                        select game,
                        true, false);

                        UpdateGamesList(forceReplaceGames);
                    }
                    catch (Exception ex)
                    {

                    }
                });
            });
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            games = null;
            UpdateGamesList();

            SeasonSelect.SelectNone();
            MapSelect.SelectNone();
            HeroSelect.SelectNone();
            ResultSelect.SelectNone();
            MinSkillRating.Text = "0000";
            MaxSkillRating.Text = "5000";
            StartDate.SelectedDate = null;
            EndDate.SelectedDate = null;
            SideSelect.SelectedIndex = -1;
            NumberOfRounds.Text = "";
            BlueTeamScore.Text = "";
            RedTeamScore.Text = "";
            GroupSelect.SelectNone();
            MinSeconds.Text = "";
            MaxSeconds.Text = "";
            MinMinutes.Text = "";
            MaxMinutes.Text = "";
            NotesTextBox.Text = "";

            var defaultBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
            MinSkillRating.BorderBrush = defaultBrush;
            MaxSkillRating.BorderBrush = defaultBrush;
            NumberOfRounds.BorderBrush = defaultBrush;
            BlueTeamScore.BorderBrush = defaultBrush;
            RedTeamScore.BorderBrush = defaultBrush;
            MinSeconds.BorderBrush = defaultBrush;
            MinMinutes.BorderBrush = defaultBrush;
            MaxSeconds.BorderBrush = defaultBrush;
            MaxMinutes.BorderBrush = defaultBrush;

            ErrorString.Visibility = Visibility.Hidden;

            itemsShown = 20;
        }

        private async void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    await FilterGames();
                    break;
                case Key.Delete:
                case Key.Back:
                    if (GamesList.SelectedItems.Count > 0 && MessageBox.Show($"Are you sure you want to delete {GamesList.SelectedItems.Count} items?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    {
                        var games = GamesList.SelectedItems.Cast<SmallGameListItem>().OrderByDescending(x => x.GameId).ToList();
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

        private void ResetSetting(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox)
                ((ComboBox)sender).SelectedIndex = -1;
            else if (sender is DatePicker)
                ((DatePicker)sender).SelectedDate = null;
            else
                (sender as MultiSelectComboBox)?.SelectNone();
            FilterGamesButton_Click(null, null);
        }

        private async void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            itemsShown = 20;
            await FilterGames();
        }

        private async void FilterTextChanged(object sender, TextChangedEventArgs e)
        {
            itemsShown = 20;
            await FilterGames();
        }

        private async void FilterFocusLost(object sender, RoutedEventArgs e)
        {
            itemsShown = 20;
            await FilterGames();
        }

        private void GamesList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0 && e.VerticalOffset > (e.ExtentHeight - e.ViewportHeight) * 0.95 && GamesList.Items.Count > 0 && itemsShown < games.Count)
            {
                itemsShown += 20;
                int last = GamesList.Items.Count;
                try
                {
                    UpdateGamesList();
                    GamesList.ScrollIntoView(GamesList.Items[last - 5]);
                }
                catch { }
            }
        }

        private async void MultiSelectFilterChanged(object sender, RoutedEventArgs e)
        {
            itemsShown = 20;
            await FilterGames();
        }
    }
}