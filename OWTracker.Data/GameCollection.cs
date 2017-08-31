using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace OWTracker.Data
{
    public class GameCollection
    {
        public List<Game> Games;

        public GameCollection(IEnumerable<Game> games, bool desc, bool calcDifference = true)
        {
            Games = games.ToList();
            Descending = desc;
            if (calcDifference) CalculateSkillRatingDifference();
        }

        public IEnumerable<Game> GamesWithoutDecay => Games.Where(x => x.Map != "SR Decay");
        public short High => Games.Any(x => x.SkillRating > 0) ? Games.Where(x => x.SkillRating > 0).Max(x => x.SkillRating) : (short)0;
        public short Low => Games.Any(x => x.SkillRating > 0) ? Games.Where(x => x.SkillRating > 0).Min(x => x.SkillRating) : (short)0;
        public short Average => Games.Any(x => x.SkillRating > 0) ? (short)Games.Where(x => x.SkillRating > 0).Average(x => x.SkillRating) : (short)0;
        public int GamesPlayed => GamesWon + GamesLost + GamesTied;
        public int GamesWon => Games.Count(x => x.GameWon == true);
        public int GamesLost => GamesWithoutDecay.Count(x => x.GameWon == false);
        public int GamesTied => Games.Count(x => x.GameWon == null);
        public double WinPercentage => Math.Round((double)GamesWon / (GamesWon + GamesLost) * 100d, 2);

        public WinStats BestMap => MapStatistics.OrderByDescending(x => x.Value.SkillRatingChange).FirstOrDefault().Value;
        public WinStats MostPlayedMap => MapStatistics.OrderByDescending(x => x.Value.GamesPlayed).FirstOrDefault().Value;
        public WinStats MostPlayedHero => HeroStatistics.OrderByDescending(x => x.Value.GamesPlayed).FirstOrDefault().Value;
        public WinStats BestHero => HeroStatistics.OrderByDescending(x => x.Value.SkillRatingChange).FirstOrDefault().Value;

        public short MostRecentSR => Games.FirstOrDefault(x => x.SkillRating > 0)?.SkillRating ?? 0;
        public int ChangeFromFirst => Games.FirstOrDefault(x => x.SkillRating > 0) != null && Games.LastOrDefault(x => x.SkillRating > 0) != null ? Games.First(x => x.SkillRating > 0).SkillRating - Games.Last(x => x.SkillRating > 0).SkillRating : 0;

        public int Count => Games.Count;
        private bool Descending { get; }

        public IEnumerable<string> SeasonComboBoxSource => Games.Select(x => x.Season).Distinct().Select(s => $"SEASON {s}");

        public Dictionary<string, WinStats> MapStatistics
        {
            get
            {
                var stats = new Dictionary<string, WinStats>();

                foreach (Game g in GamesWithoutDecay)
                {
                    if (g.Map == null) continue;
                    if (!stats.ContainsKey(g.Map)) stats[g.Map] = new WinStats(g.Map);

                    if (g.GameWon == true) stats[g.Map].GamesWon++;
                    else if (g.GameWon == false) stats[g.Map].GamesLost++;
                    else if (g.GameWon == null) stats[g.Map].GamesTied++;

                    if (g.SkillRatingDifference.HasValue && g.SkillRating > 0)
                        stats[g.Map].SkillRatingChange += g.SkillRatingDifference.Value;
                }

                return stats;
            }
        }

        public Dictionary<string, WinStats> HeroStatistics
        {
            get
            {
                var stats = new Dictionary<string, WinStats>();

                foreach (Game g in GamesWithoutDecay)
                {
                    if (g.Heroes != null)
                    {
                        foreach (string hero in g.HeroesList)
                        {
                            if (!stats.ContainsKey(hero)) stats[hero] = new WinStats(hero);
                            if (g.GameWon == true) stats[hero].GamesWon++;
                            if (g.GameWon == false) stats[hero].GamesLost++;
                            if (g.GameWon == null) stats[hero].GamesTied++;

                            if (g.SkillRatingDifference.HasValue && g.SkillRating > 0)
                                stats[hero].SkillRatingChange += g.SkillRatingDifference.Value;
                        }
                    }
                }

                return stats;
            }
        }

        public IEnumerable<string> HeroesComboBoxSource
        {
            get
            {
                var heroes = new List<string>();
                foreach (string hero in from g in Games from hero in g.HeroesList where !heroes.Contains(hero) select hero)
                    heroes.Add(hero);
                return heroes;
            }
        }

        public Game this[int index] => Games[index];

        private void CalculateSkillRatingDifference()
        {
            if (Games.Count < 2) return;

            for (int i = 0; i < Games.Count - 1; i++)
            {
                int index = Descending ? i : Games.Count - i - 1;
                int prevIndex = Descending ? i + 1 : Games.Count - i - 2;
                if (Games[index].Season != Games[prevIndex].Season || Games[prevIndex].SkillRating <= 0 || Games[index].SkillRating <= 0) continue;
                Games[index].SkillRatingDifference = Games[index].SkillRating - Games[prevIndex].SkillRating;
            }
        }

        public GameCollection OrderAscending()
        {
            Games = Descending ? Games.OrderBy(x => x.Date).ToList() : Games;
            return this;
        }

        public GameCollection OrderDescending()
        {
            Games = Descending ? Games : Games.OrderByDescending(x => x.Date).ToList();
            return this;
        }

        public GameCollection WhereSeasonIs(short season) => new GameCollection(Games.Where(x => x.Season == season), Descending, false);

        public string GetRecentStatusString(int hours)
        {
            try
            {
                int num = GamesWithoutDecay.Count(x => DateTimeOffset.Now - x.Date < TimeSpan.FromHours(hours));
                var games = GamesWithoutDecay.Where(x => DateTimeOffset.Now - x.Date < TimeSpan.FromHours(hours));
                if (num == 0) return "RECENT";
                int? skillRatingChange = games.Sum(x => x.SkillRatingDifference);
                int wins = games.Count(x => x.GameWon == true);
                int losses = games.Count(x => x.GameWon == false);
                int draws = games.Count(x => x.GameWon == null);
                return $"RECENT ({wins}-{losses}{(draws > 0 ? $"-{draws}" : "")}, {skillRatingChange:+#;-#;+0} SR LAST {hours} HOURS)";
            }
            catch
            {
                return "RECENT";
            }
        }

        private class GameDataPoint : IDataPointProvider
        {
            public double X { get; }
            public double Y { get; }

            public string GameText { get; }

            public GameDataPoint(Game g, double x, double y, bool showSrFromGame = true)
            {
                GameText = g.ToTooltipString(showSrFromGame);
                X = x;
                Y = y;
            }

            public DataPoint GetDataPoint() => new DataPoint(X, Y);
        }

        public PlotModel SkillRatingGraphModel()
        {
            var model = new PlotModel { Title = "Skill Rating" };

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Game" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Skill Rating", MajorGridlineThickness = 1, MajorGridlineStyle = LineStyle.Dash });

            short prevSeason = 0;
            var dataPoints = new List<IDataPointProvider>();
            for (int i = 0; i < Games.Count; i++)
            {
                if (i == 0)
                    prevSeason = Games[i].Season;
                else if (Games[i].Season != prevSeason)
                {
                    List<double> lines = model.Axes[0].ExtraGridlines?.ToList() ?? new List<double>();
                    lines.Add(Descending ? Games.Count - i + 0.5 : i + 0.5);
                    model.Axes[0].ExtraGridlines = lines.ToArray();
                    prevSeason = Games[i].Season;
                }

                short sr = Games[Descending ? Games.Count - i - 1 : i].SkillRating;
                if (sr > 0)
                    dataPoints.Add(new GameDataPoint(Games[Descending ? Games.Count - i - 1 : i], i + 1, sr, false));
            }

            var series = new LineSeries
            {
                Title = "Skill Rating",
                Color = OxyColor.FromRgb(140, 0, 255),
                MarkerFill = OxyColor.FromRgb(140, 0, 255),
                MarkerType = MarkerType.Circle,
                TrackerFormatString = "Game: {2}\n{3}: {4}\n{GameText}",
                ItemsSource = dataPoints,
                CanTrackerInterpolatePoints = false
            };

            model.Series.Add(series);

            return model;
        }

        public PlotModel WinStreakGraphModel()
        {
            var model = new PlotModel { Title = "Win Streak" };

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Game" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Win Streak", MajorGridlineThickness = 1, MajorGridlineStyle = LineStyle.Dash });

            int streak = 0;
            short prevSeason = 0;
            var dataPoints = new List<IDataPointProvider>();
            for (int i = 0; i < Games.Count; i++)
            {
                if (i == 0)
                    prevSeason = Games[i].Season;
                else if (Games[i].Season != prevSeason)
                {
                    List<double> lines = model.Axes[0].ExtraGridlines?.ToList() ?? new List<double>();
                    lines.Add(Descending ? Games.Count - i + 0.5 : i + 0.5);
                    model.Axes[0].ExtraGridlines = lines.ToArray();
                    prevSeason = Games[i].Season;
                }

                switch (Games[Descending ? Games.Count - i - 1 : i].GameWon)
                {
                    case true:
                        streak = streak >= 0 ? streak + 1 : 1;
                        break;
                    case false:
                        streak = streak <= 0 ? streak - 1 : -1;
                        break;
                    default:
                        streak = 0;
                        break;
                }

                dataPoints.Add(new GameDataPoint(Games[Descending ? Games.Count - i - 1 : i], i + 1, streak));
            }

            var series = new TwoColorLineSeries
            {
                Title = "Win Streak",
                Color = OxyColors.Green,
                Color2 = OxyColors.Red,
                MarkerFill = OxyColors.Black,
                MarkerType = MarkerType.Circle,
                Limit = 0,
                TrackerFormatString = "Game: {2}\n{3}: {4:+#;-#;+0}\n{GameText}",
                ItemsSource = dataPoints,
                CanTrackerInterpolatePoints = false
            };


            model.Series.Add(series);
            return model;
        }

        public PlotModel WinRateGraphModel()
        {
            var model = new PlotModel { Title = "Win Rate" };

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Game" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Win Rate", MajorGridlineThickness = 1, MajorGridlineStyle = LineStyle.Dash, ExtraGridlines = new[] { 50d } });

            int wins = 0;
            int losses = 0;
            short prevSeason = 0;
            var dataPoints = new List<IDataPointProvider>();
            for (int i = 0; i < Games.Count; i++)
            {
                if (i == 0)
                    prevSeason = Games[i].Season;
                else if (Games[i].Season != prevSeason)
                {
                    List<double> lines = model.Axes[0].ExtraGridlines?.ToList() ?? new List<double>();
                    lines.Add(Descending ? Games.Count - i + 0.5 : i + 0.5);
                    model.Axes[0].ExtraGridlines = lines.ToArray();
                    prevSeason = Games[i].Season;
                }

                if (Games[Descending ? Games.Count - i - 1 : i].GameWon == true) wins++;
                else if (Games[Descending ? Games.Count - i - 1 : i].GameWon == false) losses++;

                dataPoints.Add(new GameDataPoint(Games[Descending ? Games.Count - i - 1 : i], i + 1, 100d * wins / (wins + losses)));
            }

            var series = new TwoColorLineSeries
            {
                Title = "Win Rate",
                Color = OxyColors.Green,
                Color2 = OxyColors.Red,
                MarkerFill = OxyColors.Black,
                MarkerType = MarkerType.Circle,
                Limit = 50,
                TrackerFormatString = "Game: {2}\n{3}: {4:0.00}%\n{GameText}",
                ItemsSource = dataPoints,
                CanTrackerInterpolatePoints = false,
            };


            model.Series.Add(series);

            return model;
        }

        private class ResultsColumnItem : ColumnItem
        {
            public string Title { get; }
            public double Wins { get; }
            public double Losses { get; }
            public double Draws { get; }

            public double WinRate => Math.Round(Wins / (Wins + Losses) * 100d, 2);

            public ResultsColumnItem(string title, double wins, double losses, double draws, double value) : base(value)
            {
                Title = title;
                Wins = wins;
                Losses = losses;
                Draws = draws;
            }

            public const string TrackerFormat = "{Title}\nWins: {Wins}\nLosses: {Losses}\nDraws: {Draws}\nWin Rate: {WinRate}%";
        }

        public PlotModel DailyResultsGraphModel()
        {
            var model = new PlotModel { Title = "Game Results Per Day", LegendItemOrder = LegendItemOrder.Reverse };

            model.Axes.Add(new CategoryAxis { Position = AxisPosition.Bottom, Title = "Date", Angle = -90 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Games", ExtraGridlines = new[] { 0d }, ExtraGridlineThickness = 1 });

            DateTime prev = DateTime.Parse("Jan 1 1900");
            for (int i = 0; i < Games.Count; i++)
            {
                if (Games[Descending ? Games.Count - i - 1 : i].Date.Date != prev)
                {
                    (model.Axes[0] as CategoryAxis)?.Labels.Add(Games[Descending ? Games.Count - i - 1 : i].Date.Date.ToString("MMM d yyyy"));
                    prev = Games[Descending ? Games.Count - i - 1 : i].Date.Date;
                }
            }

            var winsSeries = new ColumnSeries { Title = "Wins", FillColor = OxyColors.Green, IsStacked = true, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var lossesSeries = new ColumnSeries { Title = "Losses", FillColor = OxyColors.Red, IsStacked = true, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var drawsSeries = new ColumnSeries { Title = "Draws", FillColor = OxyColors.Goldenrod, IsStacked = true, TrackerFormatString = ResultsColumnItem.TrackerFormat };

            foreach (string label in ((CategoryAxis)model.Axes[0]).Labels)
            {
                IEnumerable<Game> games = Games.Where(g => g.Date.Date == DateTime.Parse(label));
                Game[] enumerable = games as Game[] ?? games.ToArray();
                double gamesWon = enumerable.Count(x => x.GameWon == true);
                double gamesLost = enumerable.Count(x => x.GameWon == false);
                double gamesTied = enumerable.Count(x => x.GameWon == null);

                winsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesWon));
                lossesSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, -gamesLost));
                drawsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesTied));
            }

            model.Series.Add(lossesSeries);
            model.Series.Add(drawsSeries);
            model.Series.Add(winsSeries);

            return model;
        }

        public PlotModel MapResultsGraphModel()
        {
            var model = new PlotModel { Title = "Game Results Per Map" };

            model.Axes.Add(new CategoryAxis { Position = AxisPosition.Bottom, Title = "Map", Angle = -45 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Games", MajorGridlineStyle = LineStyle.Dash, MajorGridlineThickness = 1 });

            (model.Axes[0] as CategoryAxis)?.Labels.AddRange(MapStatistics.OrderBy(x => x.Key).Select(x => x.Key));

            var winsSeries = new ColumnSeries { Title = "Wins", FillColor = OxyColors.Green, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var lossesSeries = new ColumnSeries { Title = "Losses", FillColor = OxyColors.Red, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var drawsSeries = new ColumnSeries { Title = "Draws", FillColor = OxyColors.Goldenrod, TrackerFormatString = ResultsColumnItem.TrackerFormat };

            foreach (string label in ((CategoryAxis)model.Axes[0]).Labels)
            {
                IEnumerable<Game> games = Games.Where(g => g.Map == label);
                Game[] enumerable = games as Game[] ?? games.ToArray();
                double gamesWon = enumerable.Count(x => x.GameWon == true);
                double gamesLost = enumerable.Count(x => x.GameWon == false);
                double gamesTied = enumerable.Count(x => x.GameWon == null);

                winsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesWon));
                lossesSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesLost));
                drawsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesTied));
            }

            model.Series.Add(winsSeries);
            model.Series.Add(lossesSeries);
            model.Series.Add(drawsSeries);

            return model;
        }

        public PlotModel HeroResultsGraphModel()
        {
            var model = new PlotModel { Title = "Game Results Per Hero" };

            model.Axes.Add(new CategoryAxis { Position = AxisPosition.Bottom, Title = "Hero", Angle = -45 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Games", MajorGridlineStyle = LineStyle.Dash, MajorGridlineThickness = 1 });

            ((CategoryAxis)model.Axes[0]).Labels.AddRange(HeroStatistics.OrderBy(x => x.Key).Select(x => x.Key));

            var winsSeries = new ColumnSeries { Title = "Wins", FillColor = OxyColors.Green, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var lossesSeries = new ColumnSeries { Title = "Losses", FillColor = OxyColors.Red, TrackerFormatString = ResultsColumnItem.TrackerFormat };
            var drawsSeries = new ColumnSeries { Title = "Draws", FillColor = OxyColors.Goldenrod, TrackerFormatString = ResultsColumnItem.TrackerFormat };

            foreach (string label in ((CategoryAxis)model.Axes[0]).Labels)
            {
                IEnumerable<Game> games = Games.Where(g => g.HeroesList.Contains(label));
                Game[] enumerable = games as Game[] ?? games.ToArray();
                double gamesWon = enumerable.Count(x => x.GameWon == true);
                double gamesLost = enumerable.Count(x => x.GameWon == false);
                double gamesTied = enumerable.Count(x => x.GameWon == null);

                winsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesWon));
                lossesSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesLost));
                drawsSeries.Items.Add(new ResultsColumnItem(label, gamesWon, gamesLost, gamesTied, gamesTied));
            }

            model.Series.Add(winsSeries);
            model.Series.Add(lossesSeries);
            model.Series.Add(drawsSeries);

            return model;
        }


        public GameCollection PerformQuery(bool desc = true, short? season = null, short? gameWon = -1, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, short? skillRatingMin = null, short? skillRatingMax = null, List<string> heroes = null, string map = null, bool? attackFirst = null, byte? rounds = null, byte? bscore = null, byte? rscore = null, int? gameLengthMin = null, int? gameLengthMax = null, byte? groupSize = null, int? count = null)
        {
            var result = new GameCollection(
            Games.Where(
                 g => (season == null || g.Season == season) &&
                      (gameWon < 0 ||
                       g.GameWon ==
                       (gameWon == 0 ? false : gameWon == 1 ? true : (bool?)null)) &&
                      (startDate == null || g.Date >= startDate) &&
                      (endDate == null || g.Date <= endDate) &&
                      (skillRatingMin == null || g.SkillRating >= skillRatingMin) &&
                      (skillRatingMax == null || g.SkillRating <= skillRatingMax) &&
                      (heroes == null || heroes.All(x => g.HeroesList.Contains(x))) &&
                      (map == null || g.Map == map) &&
                      (attackFirst == null || g.AttackFirst == attackFirst) &&
                      (rounds == null || g.Rounds == rounds) &&
                      (bscore == null || (g.Score != null && g.Score.StartsWith(bscore.ToString()))) &&
                      (rscore == null || (g.Score != null && g.Score.EndsWith(rscore.ToString()))) &&
                      (gameLengthMin == null || g.GameLength >= gameLengthMin) &&
                      (gameLengthMax == null || g.GameLength <= gameLengthMax) &&
                      (groupSize == null || g.GroupSize == groupSize))
                 ?.Take(count ?? int.MaxValue),
            desc,
            false);

            result = desc ? result.OrderDescending() : result.OrderAscending();
            return result;
        }

        public IEnumerator<Game> GetEnumerator() => Games.GetEnumerator();
    }

    public class WinStats
    {
        public WinStats(string label)
        {
            Label = label;
        }

        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int GamesTied { get; set; }
        public int GamesPlayed => GamesWon + GamesLost + GamesTied;
        public double WinPercentage => Math.Round((double)GamesWon / (GamesWon + GamesLost) * 100d, 2);
        public int SkillRatingChange { get; set; }
        public string Label { get; }
    }
}