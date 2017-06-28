using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Linq;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for UpdateAvailable.xaml
    /// </summary>
    public partial class UpdateAvailable : Window
    {
        private string link;
        private bool justChangelog = false;

        public UpdateAvailable(bool changelog = false)
        {
            justChangelog = changelog;
            Icon = Properties.Resources.ow.GetSource();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChangelogWebBrowser.Url = new Uri(Config.ChangelogUrl);

            if (!justChangelog)
            {
                try
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    Title = $"Update Available - Current Version {v}";
                    var wc = new WebClient();
                    string updateInfo = wc.DownloadString(Config.UpdateUrl);
                    link = string.Join("-", updateInfo.Split('-').Skip(1));
                }
                catch
                {
                    // If update check fails, proceed without error
                }
            }
            else
            {
                Title = "Changelog";
                ProgressPercentage.Visibility = Visibility.Hidden;
                CancelButton.Visibility = Visibility.Hidden;
                UpdateButton.Visibility = Visibility.Hidden;
                Progress.Visibility = Visibility.Hidden;
                Browser.Margin = new Thickness(Browser.Margin.Left, Browser.Margin.Top, Browser.Margin.Right, 0);
            }
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Progress.Value = e.ProgressPercentage;
            ProgressPercentage.Text = $"{e.ProgressPercentage}%";
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var wc = new WebClient();
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            Progress.Minimum = 0;
            Progress.Maximum = 100;
            wc.DownloadFileAsync(new Uri(link), Path.GetTempPath() + "/OverwatchTrackerInstaller.exe");
        }

        private void Wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Process.Start(Path.GetTempPath() + "/OverwatchTrackerInstaller.exe", "/SILENT");
            Application.Current.Shutdown();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}