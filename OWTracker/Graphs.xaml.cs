using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OWTracker.Data;
using OxyPlot;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for Graphs.xaml
    /// </summary>
    public partial class Graphs : UserControl
    {
        private IEnumerable<short> selectedSeasons = new List<short>();
        private IEnumerable<string> selectedMaps = new List<string>();
        private IEnumerable<string> selectedHeroes = new List<string>();

        public Graphs()
        {
            InitializeComponent();
        }

        public async Task UpdateGraphs(bool updateComboBoxes = false)
        {
            await Task.Run(
            () =>
            {
                Dispatcher.Invoke(
                () =>
                {
                    GameCollection games = Config.LoggedInUser.GetAllGames();

                    if (updateComboBoxes)
                    {
                        SeasonSelect.ItemsSource = games.SeasonComboBoxSource.ToList<object>();
                        MapSelect.ItemsSource = ((string[])Application.Current.Resources["Maps"]).ToList<object>();
                        HeroSelect.ItemsSource = games.HeroesComboBoxSource.OrderBy(x => x).ToList<object>();
                        SeasonSelect.SelectAll();
                        MapSelect.SelectAll();
                        HeroSelect.SelectAll();
                    }

                    PlotModel model;
                    var g = new GameCollection(
                        from game in games.Games
                        where
                        selectedSeasons.Contains(game.Season) &&
                        (selectedMaps.Count() == MapSelect.ItemsSource.Count || selectedMaps.Contains(game.Map)) &&
                        (selectedHeroes.Count() == HeroSelect.ItemsSource.Count || selectedHeroes.Any(x => game.HeroesList.Contains(x)))
                        select game
                        , true);

                    switch (ChartType.SelectedIndex)
                    {
                        case 1:
                            model = g.WinStreakGraphModel();
                            break;
                        case 2:
                            model = g.WinRateGraphModel();
                            break;
                        case 3:
                            model = g.DailyResultsGraphModel();
                            break;
                        case 4:
                            model = g.MapResultsGraphModel();
                            break;
                        case 5:
                            model = g.HeroResultsGraphModel();
                            break;
                        default:
                            model = g.SkillRatingGraphModel();
                            break;
                    }

                    Chart.Model = model;
                });
            });
        }

        private async void SeasonSelect_SelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            selectedSeasons = SeasonSelect.SelectedItems.Select(x => short.Parse(x.ToString().Split(' ')[1]));
            await UpdateGraphs();
        }

        private async void ChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateGraphs();
        }

        private async void MapSelect_SelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            selectedMaps = MapSelect.SelectedItems.Select(x => x.ToString());
            await UpdateGraphs();
        }

        private async void HeroSelect_SelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            selectedHeroes = HeroSelect.SelectedItems.Select(x => x.ToString());
            await UpdateGraphs();
        }

        private void ComboBox_RightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ComboBox box)
                box.SelectedIndex = 0;
            else
                (sender as MultiSelectComboBox)?.SelectAll();
        }
    }
}