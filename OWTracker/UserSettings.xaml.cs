using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class UserSettings : UserControl
    {
        public UserSettings()
        {
            InitializeComponent();
        }

        public async Task UpdateSettings()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(
                () =>
                {
                    Username.Text = Config.LoggedInUser.Username;

                    if (BattleTag != null && Config.LoggedInUser != null && Config.LoggedInUser.BattleTag != null)
                    {
                        BattleTag.Text = Config.LoggedInUser.BattleTag.Split('-')[0];
                        Discriminator.Text = Config.LoggedInUser.BattleTag.Split('-')[1].PadLeft(4, '0');
                    }

                    UserControl_Loaded(null, null);
                });
            });
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RecentHours.Text = SettingsManager.GetSetting<byte?>("recentHours").Value?.ToString() ?? "12";
            RecentRefresh.IsChecked = SettingsManager.GetBooleanSetting("recentRefresh") ?? true;
            UpdateOnStartup.IsChecked = SettingsManager.GetBooleanSetting("updateOnStartup") ?? true;
            UpdatePeriodically.IsChecked = SettingsManager.GetBooleanSetting("updatePeriodically") ?? false;

            var setting = SettingsManager.GetSetting<(string username, string password)>("rememberLogin");

            RememberUsername.IsChecked = setting.Success && !string.IsNullOrEmpty(setting.Value.username);
            SkipLogin.IsChecked = setting.Success && !string.IsNullOrEmpty(setting.Value.password);

            if (Config.LocalDataSource)
            {
                PasswordSettingsGrid.Visibility = Visibility.Hidden;
            }
        }

        private async void ImportGames_Click(object sender, RoutedEventArgs e)
        {
            new ImportGames().ShowDialog();
            await Config.Refresh();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings?", "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Config.SetBusyStatus("Resetting settings");
                SettingsManager.DeleteSettings();
                BattleTag.Text = "";
                Discriminator.Text = "";
                await Config.LoggedInUser.UpdateBattleTag(null);
                UserControl_Loaded(null, null);
                Config.SetFinishedStatus("Settings reset");
            }
        }

        private void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            try // Check for Updates
            {
                Config.SetBusyStatus("Checking for updates");
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                var webclient = new WebClient();
                string updateInfo = webclient.DownloadString("http://aopell.me/projects/OWTrackerVersion.txt");

                if (v >= Version.Parse(updateInfo.Split('-')[0])) Config.SetFinishedStatus("Up to date");
                else
                {
                    Config.SetFinishedStatus("Found update");
                    var uA = new UpdateAvailable();
                    uA.ShowDialog();
                }
            }
            catch
            {
                Config.SetFinishedStatus("Update check failed", true);
            }
        }

        private async void ChangeUsername_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Updating username");
            if (!await Config.LoggedInUser.ChangeUsername(Username.Text))
            {
                MessageBox.Show("Error changing username, try another instead.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Config.SetFinishedStatus("Username change failed", true);
            }
            else Config.SetFinishedStatus("Username changed");
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Changing password");
            if (!await Config.LoggedInUser.ChangePassword(OldPassword.Password, NewPassword.Password))
            {
                MessageBox.Show("Error changing password. Make sure current password is correct and that the new password is at least 8 characters.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Config.SetFinishedStatus("Password change failed", true);
            }
            else Config.SetFinishedStatus("Password changed");

            OldPassword.Clear();
            NewPassword.Clear();
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Saving settings");
            // Profile Settings
            if (BattleTag.Text.Length >= 3 && ushort.TryParse(Discriminator.Text, out ushort d))
            {
                await Config.LoggedInUser.UpdateBattleTag($"{BattleTag.Text}-{d:0000}");
            }
            else if (BattleTag.Text == "" && Discriminator.Text == "")
            {
                await Config.LoggedInUser.UpdateBattleTag(null);
            }
            else
            {
                MessageBox.Show("BattleTag wasn't in the correct format. Must be at least 3 characters. Discriminator must be a valid number.");
            }

            // General Settings
            SettingsManager.AddSetting("recentHours", RecentHours.Text);
            SettingsManager.AddSetting("recentRefresh", RecentRefresh.IsChecked);
            SettingsManager.AddSetting("updateOnStartup", UpdateOnStartup.IsChecked);
            SettingsManager.AddSetting("updatePeriodically", UpdatePeriodically.IsChecked);

            // Account Settings

            var loginSetting = SettingsManager.GetSetting<(string username, string password)>("rememberLogin");
            var value = loginSetting.Success ? loginSetting.Value : (username: "", password: "");

            value.username = RememberUsername.IsChecked == true ? Config.LoggedInUser.Username : "";

            if (SkipLogin.IsChecked == true && string.IsNullOrEmpty(value.password))
            {
                var dialog = new TextBoxDialog("Please enter password for verification. Please note this will store your password in plaintext in order to keep you logged in.", "CONFIRM", DialogType.PasswordEntry);
                while (true)
                {
                    dialog.ShowDialog();
                    if (dialog.Result != null)
                    {
                        try
                        {
                            Config.DataSource.Login(Config.LoggedInUser.Username, dialog.Result);
                            value.username = Config.LoggedInUser.Username;
                            value.password = SkipLogin.IsChecked == true ? dialog.Result : "";
                            break;
                        }
                        catch { }

                    }
                    else
                    {
                        SkipLogin.IsChecked = false;
                    }

                    dialog = new TextBoxDialog("Incorrect password, try again.", "CONFIRM", DialogType.PasswordEntry, Colors.Red);
                }
            }
            else if (SkipLogin.IsChecked == false && !string.IsNullOrEmpty(value.password))
                value.password = "";

            SettingsManager.AddSetting("rememberLogin", value);

            SettingsManager.SaveSettings();
            await Config.Refresh();
            Config.SetFinishedStatus("Settings saved");
        }

        private void MasterOverwatch_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://masteroverwatch.com/profile/pc/us/{Config.LoggedInUser.BattleTag}");
        }

        private void OverBuff_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://www.overbuff.com/players/pc/{Config.LoggedInUser.BattleTag}");
        }

        private async void UpdateOnlineProfiles_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Updating online profiles");
            if (Config.LoggedInUser.BattleTag != null)
            {
                var c = new HttpClient();
                string masterOverwatchRefresh = $"https://masteroverwatch.com/profile/pc/us/{Config.LoggedInUser.BattleTag}/update";
                string overBuffRefresh = $"https://www.overbuff.com/players/pc/{Config.LoggedInUser.BattleTag}/refresh";
                await c.GetAsync(masterOverwatchRefresh);
                await c.PostAsync(overBuffRefresh, new StringContent(""));

                Config.SetFinishedStatus("Profiles updated");
            }
            else Config.SetFinishedStatus("Invalid BattleTag", true);
        }

        private void ExportGames_Click(object sender, RoutedEventArgs e)
        {
            Config.SetBusyStatus("Exporting games");
            var d = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "owtracker_backup",
                DefaultExt = ".txt",
                Filter = "Tab-separated values (.txt)|*.txt"
            };

            if (d.ShowDialog() == true)
            {
                try
                {
                    string file = d.FileName;
                    List<string> fileLines = Config.LoggedInUser.GetAllGames(false).Games.Select(x => x.ToString()).ToList();
                    fileLines.Insert(0, "GameID\tSeason\tDate\tSkillRating\tGameResult\tHeroes\tMap\tStartingSide\tRounds\tScore\tGameLength\tGroupSize\tNotes");
                    File.WriteAllLines(file, fileLines);
                    Config.SetFinishedStatus("Exported games");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                    Config.SetFinishedStatus("Export failed", true);
                }
            }
        }

        private void PlayOverwatch_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://playoverwatch.com/en-us/career/pc/us/{Config.LoggedInUser.BattleTag}");
        }

        private void ChangelogButton_Click(object sender, RoutedEventArgs e)
        {
            (new UpdateAvailable(true)).ShowDialog();
        }
    }
}