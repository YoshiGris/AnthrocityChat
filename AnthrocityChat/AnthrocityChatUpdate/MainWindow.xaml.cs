using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnthrocityChatUpdate
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpClient client = new HttpClient(); WebClient download_client = new WebClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\version.anthro"))
            {
                using (StreamReader sr = new StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\version.anthro"))
                {
                    try
                    {
                        String actual_version = sr.ReadToEnd();
                        HttpResponseMessage response = await client.GetAsync("http://furhub.yoshigris.fr/version.anthro");
                        response.EnsureSuccessStatusCode();
                        var version_site = await response.Content.ReadAsStringAsync();

                        if (int.Parse(actual_version) > int.Parse(version_site) || int.Parse(actual_version) == int.Parse(version_site))
                        {
                            Process.Start(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\AnthrocityChat.exe");
                            this.Close();
                        }
                        else
                        {
                            Directory.Delete(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\", true);
                            Update();
                        }
                    }
                    catch
                    {
                        Process.Start(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\AnthrocityChat.exe");
                        this.Close();
                    }

                }
            }
            else
            {
                Update();
            }
        }

        async void Update()
        {
            update_text.Text = "Téléchargement de la nouvelle version...";
            HttpResponseMessage response = await client.GetAsync("http://furhub.yoshigris.fr/version.anthro");
            response.EnsureSuccessStatusCode();
            var version = await response.Content.ReadAsStringAsync();

            download_client.DownloadFileCompleted += Download_client_DownloadFileCompleted;
            download_client.DownloadFileAsync(new Uri("http://furhub.yoshigris.fr/Download/Anthrocity_Chat_" + version + ".zip", UriKind.Absolute), System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\anthro.zip");

            File.WriteAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\version.anthro", version);
        }

        private void Download_client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            update_text.Text = "Installation en cours...";
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\");
            ZipFile.ExtractToDirectory(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\anthro.zip", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\" );

            Process.Start(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\soft\AnthrocityChat.exe");
            this.Close();
        }
    }
}
