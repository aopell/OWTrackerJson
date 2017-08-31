using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OWTracker.Data;
using OWTracker.Properties;

namespace OWTracker
{
    public static class Config
    {
        public static LoggedInUser LoggedInUser = null;
        public static CareerOverview Overview;
        public static ViewGames ViewGames;
        public static Graphs Graphs;
        public static OverviewWindow Window;
        public static UserSettings Settings;

        public static readonly IAsyncDataProvider DataSource;
        public static readonly bool LocalDataSource;

        public static readonly string UpdateUrl;
        public static readonly string ChangelogUrl;

        static Config()
        {
            DataSource = new JsonDataProvider(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title}");
            ChangelogUrl = "http://aopell.me/projects/OWTrackerJsonChangelog.html";
            UpdateUrl = "http://aopell.me/projects/owjv.txt";
            LocalDataSource = DataSource.GetType().IsDefined(typeof(LocalDataProviderAttribute));
        }

        public static BitmapSource GetImageForSkillRating(int sr, bool showDash = false)
        {
            if (sr >= 4000) return Resources.grandmaster.GetSource();
            if (sr >= 3500) return Resources.master.GetSource();
            if (sr >= 3000) return Resources.diamond.GetSource();
            if (sr >= 2500) return Resources.platinum.GetSource();
            if (sr >= 2000) return Resources.gold.GetSource();
            if (sr >= 1500) return Resources.silver.GetSource();
            if (sr > 0) return Resources.bronze.GetSource();
            return showDash ? Resources.placement.GetSource() : null;
        }

        public static void SetBusyStatus(string message)
        {
            Window.StatusText.Foreground = new SolidColorBrush(Colors.White);
            if (Window != null)
                Window.StatusText.Text = $"⟳ {message}...";
        }

        public static async void SetFinishedStatus(string message, bool error = false)
        {
            if (Window != null)
            {
                string m = error ? $"✘ {message}" : $"✔ {message}";
                Window.StatusText.Text = m;
                Window.StatusText.Foreground = error ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.White);
                await Task.Delay(error ? 10000 : 5000);
                Window.StatusText.Text = Window.StatusText.Text == m ? "" : Window.StatusText.Text;
            }
        }

        public static int GetCompetitivePointsForSkillRating(int sr)
        {
            if (sr >= 4000) return 3000;
            if (sr >= 3500) return 2000;
            if (sr >= 3000) return 1200;
            if (sr >= 2500) return 800;
            if (sr >= 2000) return 400;
            return sr >= 1500 ? 200 : 100;
        }

        public static async Task Refresh()
        {
            await LoggedInUser.RefreshGamesAsync();
            await Overview.UpdateStatistics(true);
            await ViewGames.FilterGames(true);
            await Graphs.UpdateGraphs(true);
            await Settings.UpdateSettings();

            await Window.UpdateUI();
        }
    }
}