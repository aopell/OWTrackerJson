using System;
using System.Windows.Controls;
using System.Windows.Media;
using OWTracker.Data;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for SmallGameListItem.xaml
    /// </summary>
    public partial class SmallGameListItem : UserControl
    {
        public readonly long GameId;

        public SmallGameListItem(Game g) : this(g.GameID, g.SkillRating, g.GameWon, g.SkillRatingDifference, g.Map, g.Date, g.Season) { }

        private SmallGameListItem(long gameId, int rank, bool? win, int? change, string map, DateTimeOffset date, short season)
        {
            InitializeComponent();

            GameId = gameId;
            CurrentRank.Text = rank > 0 ? rank.ToString() : "PLMT";
            RankChange.Text = change?.ToString("(+#);(-#);(+0)") ?? "";
            Date.Text = $"SEASON {season} - {date.ToLocalTime().ToString("MMM d").ToUpper()}";
            Date.ToolTip = date.ToLocalTime().ToString("dddd MMMM d, h:mm:ss tt");
            Map.Text = map?.ToUpper() ?? "";

            RankIcon.Source = Config.GetImageForSkillRating(rank, true);

            if (rank < 1) SRText.Visibility = System.Windows.Visibility.Hidden;

            if (win == true) Background = new SolidColorBrush(Colors.DarkGreen);
            else if (win == false) Background = new SolidColorBrush(Colors.DarkRed);
            else Background = new SolidColorBrush(Color.FromRgb(133, 101, 20));
        }
    }
}