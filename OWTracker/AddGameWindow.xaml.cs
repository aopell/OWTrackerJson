using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OWTracker.Data;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for AddGameWindow.xaml
    /// </summary>
    public partial class AddGameWindow : Window
    {
        public bool Cancel = true;
        public long GameId = -1;
        private Game gameToUpdate;

        private Game mostRecentGame;
        public bool Update;

        private bool userChangedRounds = true;

        public AddGameWindow()
        {
            InitializeComponent();
            Icon = Properties.Resources.ow.GetSource();

            SeasonNumber.Text = Config.LoggedInUser.MostRecentGame?.Season.ToString() ?? "?";
            NewSkillRating.Text = Config.LoggedInUser.GetAllGames()?.MostRecentSR.ToString() ?? "?";
        }

        public AddGameWindow(long gameId) : this()
        {
            GameId = gameId;
            Update = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Config.LoggedInUser.EditPermissions) AddGameButton.IsEnabled = false;

            if (!Update)
            {
                NewSkillRating.Focus();
                NewSkillRating.SelectAll();
                mostRecentGame = Config.LoggedInUser.MostRecentGame;

                SkillRatingChange.Visibility = Visibility.Visible;
                GameOutcome.Visibility = Visibility.Visible;

                SkillRatingChange.Foreground = new SolidColorBrush(Colors.Gold);
                GameOutcome.Foreground = new SolidColorBrush(Colors.Gold);
                GameOutcome.Content = "DRAW";

                if (mostRecentGame == null || mostRecentGame.SkillRating == 0)
                {
                    PlacementCheckBox.Visibility = Visibility.Visible;
                    PlacementCheckBox.IsChecked = true;
                }
            }
            else
            {
                Title = "Update Game";
                AddGameButton.Content = "UPDATE";

                gameToUpdate = Config.LoggedInUser.GetGameById(GameId);
                List<Game> games = Config.LoggedInUser.GetAllGames().Games;
                mostRecentGame = games.SkipWhile(x => x != gameToUpdate).Skip(1).Take(1).FirstOrDefault();
                if (gameToUpdate == null)
                {
                    MessageBox.Show($"Game not found (GameID = {GameId})", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
                else
                {
                    if (gameToUpdate.Map == Application.Current.Resources["SRDecayString"] as string) AddGameButton.IsEnabled = false;

                    SeasonNumber.Text = gameToUpdate.Season.ToString();
                    NewSkillRating.Text = gameToUpdate.SkillRating.ToString();
                    TierIcon.Source = Config.GetImageForSkillRating(gameToUpdate.SkillRating);

                    GameOutcome.Visibility = Visibility.Visible;
                    if (gameToUpdate.SkillRatingDifference.HasValue)
                    {
                        SkillRatingChange.Text = gameToUpdate.SkillRatingDifference.Value.ToString("(+#);(-#);(+0)");
                        SkillRatingChange.Visibility = Visibility.Visible;
                    }

                    if (gameToUpdate.GameWon == true)
                    {
                        SkillRatingChange.Foreground = new SolidColorBrush(Colors.Green);
                        GameOutcome.Foreground = new SolidColorBrush(Color.FromRgb(4, 232, 252));
                        GameOutcome.Content = "VICTORY";
                    }
                    else if (gameToUpdate.GameWon == false)
                    {
                        SkillRatingChange.Foreground = new SolidColorBrush(Colors.DarkRed);
                        GameOutcome.Foreground = new SolidColorBrush(Colors.DarkRed);
                        GameOutcome.Content = "DEFEAT";
                    }
                    else
                    {
                        SkillRatingChange.Foreground = new SolidColorBrush(Colors.Gold);
                        GameOutcome.Foreground = new SolidColorBrush(Colors.Gold);
                        GameOutcome.Content = "DRAW";
                    }

                    if (gameToUpdate.SkillRating < 1)
                    {
                        PlacementCheckBox.Visibility = Visibility.Visible;
                        PlacementCheckBox.IsChecked = true;
                    }

                    SelectedMap.SelectedValue = gameToUpdate.Map ?? "";
                    StartingSide.SelectedIndex = !gameToUpdate.AttackFirst.HasValue ? -1 : gameToUpdate.AttackFirst == true ? 0 : 1;
                    RoundNumber.Text = gameToUpdate.Rounds?.ToString() ?? "";
                    BlueTeamScore.Text = gameToUpdate.BlueTeamScore?.ToString() ?? "";
                    RedTeamScore.Text = gameToUpdate.RedTeamScore?.ToString() ?? "";
                    TimeMinutes.Text = gameToUpdate.Minutes?.ToString() ?? "";
                    TimeSeconds.Text = gameToUpdate.Seconds?.ToString("00") ?? "";
                    GroupSize.SelectedIndex = gameToUpdate.GroupSize - 1 ?? -1;
                    NotesBox.Text = gameToUpdate.Notes ?? "";

                    string[] heroes = gameToUpdate.Heroes?.Split(',') ?? new string[0];
                    foreach (string h in heroes)
                        HeroesList.SelectedItems.Add(h.Trim());
                }
            }
            userChangedRounds = false;
        }

        private async void AddGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (!short.TryParse(SeasonNumber.Text, out short season) || season < 0)
            {
                MessageBox.Show("Please provide a valid number for season");
                return;
            }
            short sr = 0;
            if (PlacementCheckBox.IsChecked != true && (!short.TryParse(NewSkillRating.Text, out sr) || sr > 5000 || sr < 1))
            {
                MessageBox.Show("Please provide a valid number for skill rating");
                return;
            }
            if (!byte.TryParse(RoundNumber.Text, out byte rounds) && RoundNumber.Text != "")
            {
                MessageBox.Show("Please provide a valid number for rounds or no value");
                return;
            }
            bool scoreBValid = short.TryParse(BlueTeamScore.Text, out short bscore) && bscore >= 0;
            bool scoreRValid = short.TryParse(RedTeamScore.Text, out short rscore) && rscore >= 0;
            if (BlueTeamScore.Text != "" && RedTeamScore.Text != "" && (!scoreBValid || !scoreRValid))
            {
                MessageBox.Show("Please provide a valid number for score or no value");
                return;
            }
            bool minsValid = short.TryParse(TimeMinutes.Text, out short mins) && mins >= 0;
            bool secsValid = short.TryParse(TimeSeconds.Text, out short secs) && secs >= 0 && secs < 60;
            if (TimeMinutes.Text != "" && TimeSeconds.Text != "" && (!minsValid || !secsValid))
            {
                MessageBox.Show("Please provide a valid number for match time or no value");
                return;
            }

            if (!Update && (mostRecentGame == null || season != mostRecentGame.Season) && MessageBox.Show($"Are you sure you want to create Season {SeasonNumber.Text}{(NewSkillRating.Text.ToLower() != "plmt" ? $" with placement rating {NewSkillRating.Text}" : "")}?", "New Season", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.No) return;

            if (mostRecentGame != null && season != mostRecentGame.Season)
                await Config.LoggedInUser.UpdateCompetitivePoints(Config.LoggedInUser.CompetitivePoints + Config.GetCompetitivePointsForSkillRating(Config.LoggedInUser.GetGamesBySeason(mostRecentGame.Season).High) ?? 0);

            if (Update)
            {
                switch (MessageBox.Show($"Are you sure you want to update this game?", "Update Game", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No))
                {
                    case MessageBoxResult.No:
                        Cancel = true;
                        Close();
                        break;
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }

            var heroes = new List<string>();
            heroes.AddRange(HeroesList.SelectedItems.Cast<string>());

            bool placement = false;
            bool? placementWon = null;
            if (mostRecentGame == null || PlacementCheckBox.IsChecked == true || mostRecentGame.SkillRating == 0)
            {
                placement = true;
                MessageBoxResult result = MessageBox.Show("Hey! This is a placement game and it is impossible to determine if you won by your skill rating. Did you win this game?\n\nYes: You won\nNo: You lost\nCancel: You tied", "Placement Game Result", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.None);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        placementWon = true;
                        break;
                    case MessageBoxResult.No:
                        placementWon = false;
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                    default:
                        return;
                }
            }

            Hide();

            if (Update)
            {
                Config.SetBusyStatus("Updating game");

                var game = new Game
                {
                    UserID = Config.LoggedInUser.UserId,
                    GameID = GameId,
                    Season = season,
                    Date = gameToUpdate.Date,
                    SkillRating = sr,
                    GameWon = placement ? placementWon : mostRecentGame != null && sr - mostRecentGame.SkillRating > 0 ? true : mostRecentGame != null && sr - mostRecentGame.SkillRating < 0 ? false : (bool?)null,
                    Heroes = heroes.Count > 0 ? string.Join(",", heroes) : null,
                    Map = SelectedMap.SelectedItem as string,
                    AttackFirst = string.IsNullOrEmpty(StartingSide.Text) ? null : StartingSide.Text == "ATTACK" ? true : StartingSide.Text == "DEFEND" ? false : (bool?)null,
                    Rounds = RoundNumber.Text == "" ? null : (byte?)rounds,
                    Score = scoreBValid && scoreRValid ? $"{bscore}-{rscore}" : null,
                    GameLength = minsValid && secsValid ? (int?)(mins * 60 + secs) : null,
                    GroupSize = byte.TryParse(GroupSize.Text, out byte b) ? (byte?)b : null,
                    Notes = NotesBox.Text.Trim() == "" ? null : NotesBox.Text.Trim()
                };
                await Config.LoggedInUser.UpdateGame(game);
                Config.SetFinishedStatus("Updated");
            }
            else
            {
                Config.SetBusyStatus("Adding game");
                await Config.LoggedInUser.AddGame(
                new Game
                {
                    UserID = Config.LoggedInUser.UserId,
                    Season = season,
                    Date = DateTimeOffset.Now,
                    SkillRating = sr,
                    GameWon = placement ? placementWon : mostRecentGame != null && sr - mostRecentGame.SkillRating > 0 ? true : mostRecentGame != null && sr - mostRecentGame.SkillRating < 0 ? false : (bool?)null,
                    Heroes = heroes.Count > 0 ? string.Join(",", heroes) : null,
                    Map = SelectedMap.SelectedItem as string,
                    AttackFirst = string.IsNullOrEmpty(StartingSide.Text) ? null : StartingSide.Text == "ATTACK" ? true : StartingSide.Text == "DEFEND" ? false : (bool?)null,
                    Rounds = RoundNumber.Text == "" ? null : (byte?)rounds,
                    Score = scoreBValid && scoreRValid ? $"{bscore}-{rscore}" : null,
                    GameLength = minsValid && secsValid ? (int?)(mins * 60 + secs) : null,
                    GroupSize = byte.TryParse(GroupSize.Text, out byte b) ? (byte?)b : null,
                    Notes = NotesBox.Text.Trim() == "" ? null : NotesBox.Text.Trim()
                });
                Config.SetFinishedStatus("Added");
            }

            Config.SetBusyStatus("Refreshing games");
            await Config.Refresh();
            Config.SetFinishedStatus("Games refreshed");

            Cancel = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NewSkillRating_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SkillRatingChange == null) return;
            if (!int.TryParse(NewSkillRating.Text, out int sr)) return;

            TierIcon.Source = Config.GetImageForSkillRating(sr);

            bool validSeason = byte.TryParse(SeasonNumber.Text, out byte season);

            if (validSeason && mostRecentGame != null && mostRecentGame.Season == season && mostRecentGame.SkillRating > 0)
            {
                SkillRatingChange.Visibility = Visibility.Visible;
                GameOutcome.Visibility = Visibility.Visible;
                SkillRatingChange.Text = (sr - mostRecentGame.SkillRating).ToString("(+#);(-#);(+0)");

                if (sr > mostRecentGame.SkillRating)
                {
                    SkillRatingChange.Foreground = new SolidColorBrush(Colors.Green);
                    GameOutcome.Foreground = new SolidColorBrush(Color.FromRgb(4, 232, 252));
                    GameOutcome.Content = "VICTORY";
                }
                else if (sr < mostRecentGame.SkillRating)
                {
                    SkillRatingChange.Foreground = new SolidColorBrush(Colors.DarkRed);
                    GameOutcome.Foreground = new SolidColorBrush(Colors.DarkRed);
                    GameOutcome.Content = "DEFEAT";
                }
                else
                {
                    SkillRatingChange.Foreground = new SolidColorBrush(Colors.Gold);
                    GameOutcome.Foreground = new SolidColorBrush(Colors.Gold);
                    GameOutcome.Content = "DRAW";
                }
            }
        }

        private void SelectedMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartingSide.IsEnabled = true;

            if (((string[])Application.Current.Resources["ControlMaps"]).Contains(SelectedMap.SelectedValue))
            {
                StartingSide.SelectedIndex = 0;
                StartingSide.IsEnabled = false;
                RoundNumber.Text = "3";
            }

            EstimateRounds();
        }

        private void PlacementCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (PlacementCheckBox.IsChecked == true)
            {
                NewSkillRating.Text = "PLMT";
                NewSkillRating.IsEnabled = false;
                TierIcon.Visibility = Visibility.Hidden;
                SkillRatingChange.Visibility = Visibility.Hidden;
                GameOutcome.Visibility = Visibility.Hidden;
            }
            else
            {
                NewSkillRating.Text = "0000";
                NewSkillRating.IsEnabled = true;
                TierIcon.Visibility = Visibility.Visible;
                SkillRatingChange.Visibility = Visibility.Visible;
                GameOutcome.Visibility = Visibility.Visible;
            }
        }

        private void SeasonNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool validSeason = byte.TryParse(SeasonNumber.Text, out byte season);

            if (validSeason && mostRecentGame != null && mostRecentGame.Season != season)
            {
                PlacementCheckBox.Visibility = Visibility.Visible;
                PlacementCheckBox.IsChecked = true;
            }
        }

        private void Score_TextChanged(object sender, TextChangedEventArgs e)
        {
            EstimateRounds();
        }

        private void EstimateRounds()
        {
            byte bScore = 0;
            byte rScore = 0;
            byte.TryParse(BlueTeamScore.Text, out bScore);
            byte.TryParse(RedTeamScore.Text, out rScore);

            if (((string[])Application.Current.Resources["ControlMaps"]).Contains(SelectedMap.SelectedValue))
            {
                RoundNumber.Text = (bScore + rScore).ToString();
            }
            else if (!userChangedRounds)
            {
                if (bScore == 0 && rScore == 0)
                {
                    RoundNumber.Text = "2";
                }
                else if (((string[])Application.Current.Resources["EscortMaps"]).Contains(SelectedMap.SelectedValue))
                {
                    RoundNumber.Text = ((int)(2 * Math.Max(Math.Ceiling(bScore / 3f), Math.Ceiling(rScore / 3f)))).ToString();
                }
                else if (((string[])Application.Current.Resources["AssaultMaps"]).Contains(SelectedMap.SelectedValue))
                {
                    RoundNumber.Text = ((int)(Math.Ceiling(bScore / 2f) + Math.Ceiling(rScore / 2f))).ToString();
                }
                else if (((string[])Application.Current.Resources["HybridMaps"]).Contains(SelectedMap.SelectedValue))
                {
                    RoundNumber.Text = ((int)(Math.Ceiling(bScore / 3d) + Math.Ceiling(rScore / 3d) + (bScore == rScore ? 1 : 0))).ToString();
                }
            }
        }

        private void RoundNumber_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            userChangedRounds = true;
        }
    }
}