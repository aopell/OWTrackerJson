using System;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Icon = Properties.Resources.ow.GetSource();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            ((Run)VersionTextBlock.Inlines.FirstInline).Text = $"VERSION {v}";

            try // Check for Updates
            {
                var webclient = new WebClient();
                string updateInfo = webclient.DownloadString("http://aopell.me/projects/OWTrackerVersion.txt");

                if (v >= Version.Parse(updateInfo.Split('-')[0])) return;
                var uA = new UpdateAvailable();
                uA.ShowDialog();
            }
            catch
            {
                // Proceed silently if update check failed
            }
        }
    }
}