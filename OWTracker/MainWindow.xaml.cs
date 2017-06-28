using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OWTracker.Data;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Icon = Properties.Resources.ow.GetSource();
        }

        public bool FirstShown { get; set; } = true;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Config.LoggedInUser = null;
            UsernameTextBox.Focus();

            if (Config.LocalDataSource)
            {
                PasswordLabel.Visibility = Visibility.Hidden;
                PasswordTextBox.Visibility = Visibility.Hidden;
            }

            if (FirstShown)
            {
                var loginCredentials = SettingsManager.GetSetting<(string username, string password)>("rememberLogin");

                if (loginCredentials.Success)
                {
                    if (!string.IsNullOrEmpty(loginCredentials.Value.username))
                    {
                        UsernameTextBox.Text = loginCredentials.Value.username;
                        PasswordTextBox.Focus();
                    }

                    if (!string.IsNullOrEmpty(loginCredentials.Value.password) && !string.IsNullOrEmpty(loginCredentials.Value.username))
                    {
                        try
                        {
                            LoggingIn.Visibility = Visibility.Visible;
                            UsernameTextBox.IsEnabled = false;
                            PasswordTextBox.IsEnabled = false;
                            Config.LoggedInUser = await LoggedInUser.Login(loginCredentials.Value.username, loginCredentials.Value.password);
                        }
                        catch
                        {
                            LoggingIn.Visibility = Visibility.Hidden;
                            UsernameTextBox.IsEnabled = true;
                            PasswordTextBox.IsEnabled = true;
                            MessageBox.Show("Automatic login failed");
                            return;
                        }

                        var w = new OverviewWindow();
                        w.Show();
                        Close();
                    }

                }

                var (s, b) = SettingsManager.GetSetting<bool>("updateOnStartup");
                if (!s || b)
                {
                    try //Check for Updates
                    {
                        var webclient = new WebClient();
                        string updateInfo = webclient.DownloadString(Config.UpdateUrl);

                        if (Assembly.GetExecutingAssembly().GetName().Version < Version.Parse(updateInfo.Split('-')[0]))
                            new UpdateAvailable().ShowDialog();
                    }
                    catch
                    {
                        // If update check failed, proceed without error
                    }
                }

                await Config.DataSource.GetReadOnlyUserInfoAsync("-"); // First API connection is slow, so doing one on startup to improve UX
            }
        }

        private async void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingIn.Visibility = Visibility.Visible;
            UsernameLabel.Content = "Username";
            UsernameLabel.Foreground = new SolidColorBrush(Colors.Black);
            PasswordLabel.Content = "Password";
            PasswordLabel.Foreground = new SolidColorBrush(Colors.Black);

            try
            {
                LoggedInUser loggedInUser = await LoggedInUser.Login(UsernameTextBox.Text, PasswordTextBox.Password);

                if (!loggedInUser.EditPermissions && MessageBox.Show($"Are you sure you want to open a read only version of {loggedInUser.Username}'s profile?", "Read Only", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    UsernameTextBox.Text = "";
                    PasswordTextBox.Password = "";
                    LoggingIn.Visibility = Visibility.Hidden;
                    return;
                }

                Config.LoggedInUser = loggedInUser;
                var w = new OverviewWindow();
                w.Show();
                Close();
            }
            catch (ArgumentException)
            {
                UsernameLabel.Content = "Username doesn't exist!";
                UsernameLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
            catch (Exception passwordException) when (passwordException is UnauthorizedAccessException || passwordException is AccessViolationException)
            {
                PasswordLabel.Content = "Incorrect password!";
                PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
            catch (Exception ex)
            {
                PasswordLabel.Content = "Login failed unexpectedly";
                PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
            }

            LoggingIn.Visibility = Visibility.Hidden;
            UsernameTextBox.Text = "";
            PasswordTextBox.Password = "";
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameLabel.Content = "Username";
            UsernameLabel.Foreground = new SolidColorBrush(Colors.Black);
            PasswordLabel.Content = "Password";
            PasswordLabel.Foreground = new SolidColorBrush(Colors.Black);

            if (UsernameTextBox.Text.Length < 3)
            {
                UsernameLabel.Content = "Username too short - 3 character minimum.";
                UsernameLabel.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            if (!Config.LocalDataSource && PasswordTextBox.Password.Length < 8)
            {
                UsernameLabel.Content = "Password too short - 8 character minimum";
                UsernameLabel.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            LoggedInUser registeredUser = await LoggedInUser.Register(UsernameTextBox.Text, PasswordTextBox.Password);

            UsernameTextBox.Text = "";
            PasswordTextBox.Password = "";

            if (registeredUser != null)
                MessageBox.Show("User created successfully");
            else
            {
                UsernameLabel.Content = "Username taken! Try another.";
                UsernameLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }
}