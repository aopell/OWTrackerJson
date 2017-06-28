using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace OWTracker
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += OnException;
        }

        private static void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            string windowsVersion = "Unknown";
            try
            {
                var v = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                         select x.GetPropertyValue("Caption")).FirstOrDefault();
                windowsVersion = v != null ? v.ToString() : "Unknown";
            }
            catch { }

            StringBuilder error = new StringBuilder();
            error.AppendLine("===========================================");
            error.AppendLine("                CRASH REPORT               ");
            error.AppendLine("===========================================");
            error.AppendLine();
            error.AppendLine($"Date: {DateTimeOffset.Now}");
            error.AppendLine($"Application Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            error.AppendLine($"Status Text: {Config.Window?.StatusText?.Text ?? "null"}");
            error.AppendLine($"Username: {Config.LoggedInUser?.Username ?? "null"}");
            error.AppendLine($"UserID: {Config.LoggedInUser?.UserId.ToString() ?? "null"}");
            error.AppendLine($"BattleTag: {Config.LoggedInUser?.BattleTag ?? "null"}");
            error.AppendLine($"Edit Permissions: {Config.LoggedInUser?.EditPermissions.ToString() ?? "null"}");
            error.AppendLine($"Windows Version: {windowsVersion}");
            error.AppendLine();
            error.AppendLine("===========================================");
            error.AppendLine("               EXCEPTION INFO              ");
            error.AppendLine("===========================================");
            error.AppendLine();
            error.AppendLine($"Exception: {ex.GetType().Name}");
            error.AppendLine($"Exception Source: {ex.Source}");
            error.AppendLine($"Exception Location: {ex.TargetSite}");
            error.AppendLine($"Exception Messages:");
            error.AppendLine(GetExceptionMessages(ex));
            error.AppendLine();
            error.AppendLine("===========================================");
            error.AppendLine("                STACK TRACE                ");
            error.AppendLine("===========================================");
            error.AppendLine();
            error.AppendLine(ex.StackTrace);

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title}\\crashes";
            if (Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string fileName = folderPath + "\\crash-report-{DateTimeOffset.Now.ToFileTime()}.txt";
            File.WriteAllText(fileName, error.ToString());
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.PostAsync("https://hastebin.com/documents", new StringContent(error.ToString())).Result;
                    JObject json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    System.Diagnostics.Process.Start("https://hastebin.com/" + json["key"].Value<string>() + ".txt");
                }
            }
            catch { }
            MessageBox.Show("An unexpected error occurred. Please report the crash using the generated crash report file or hastebin link.");
            System.Diagnostics.Process.Start(fileName);
        }

        private static string GetExceptionMessages(Exception e, string msgs = "")
        {
            if (e == null) return string.Empty;
            if (msgs == "") msgs = e.Message;
            if (e.InnerException != null)
                msgs += $"\r\n| {e.InnerException.GetType().Name}: {GetExceptionMessages(e.InnerException)}";
            return msgs;
        }
    }
}