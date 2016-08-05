using agsXMPP;
using HtmlAgilityPack;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using agsXMPP.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AnthrocityChat.Pages
{

    public partial class Login_Window : MetroWindow
    {
        bool notsave = false; GlobalVariables variables = new GlobalVariables();

        public Login_Window()
        {
            InitializeComponent();
        }

        //Fonction du bouton pour se connecter
        private void login_button_Click(object sender, RoutedEventArgs e)
        {
            login_button.IsEnabled = false; grid_connect.Visibility = Visibility.Visible;  ring_connect.IsActive = true;
            GetID();
        }

        //Cette fonction permet de se connecter à Anthrocity
        async void GetID()
        {
            Text_Connect.Text = "Je vérifie ton ID Anthrocity";
            HttpResponseMessage response = await variables.httpclient.GetAsync("https://www.anthrocity.net/user/login/");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            //Je parse pour get la p'tite key...
            HtmlDocument htmlDocument_account = new HtmlDocument();
            htmlDocument_account.LoadHtml(responseString);
            string key = htmlDocument_account.DocumentNode.SelectSingleNode("//input[@name='core[security_token]']").Attributes["value"].Value;

            //On se connecte...
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("core[security_token]", key),
                new KeyValuePair<string, string>("val[login]", Username_Box.Text),
                new KeyValuePair<string, string>("val[password]", MDP_Box.Password)
            };

            response = await variables.httpclient.PostAsync("https://www.anthrocity.net/user/login/", new FormUrlEncodedContent(values));
            response.EnsureSuccessStatusCode();
            responseString = await response.Content.ReadAsStringAsync();

            if(responseString.Contains("menu5")) //Si le code source contient "menu5", alors on s'est bien connecté !
            {
                htmlDocument_account.LoadHtml(responseString);
                variables.Username = htmlDocument_account.DocumentNode.SelectSingleNode("//div[@class='user_display_name']").Descendants("a").FirstOrDefault().InnerText;
                variables.ID_User = htmlDocument_account.DocumentNode.SelectSingleNode("//div[@class='user_display_name']").Descendants("a").FirstOrDefault().Attributes["href"].Value.Replace("https://www.anthrocity.net/", "").Replace("/", "");
                variables.Password = MDP_Box.Password;

                response = await variables.httpclient.GetAsync("https://www.anthrocity.net/" + variables.ID_User + "/");
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync();

                HtmlDocument htmlDocument_account_b = new HtmlDocument();
                htmlDocument_account_b.LoadHtml(responseString);
                variables.URL_Avatar = new System.Uri(htmlDocument_account_b.DocumentNode.DescendantsAndSelf("a").FirstOrDefault(o => o.GetAttributeValue("href", "").Contains("https://www.anthrocity.net/photo/album/profile/")).Descendants("img").FirstOrDefault().Attributes["data-src"].Value, UriKind.Absolute);

                if(!notsave)
                {
                    Items_Source.Login_Item login_item = new Items_Source.Login_Item { password = "blblblblbl", pseudo = Username_Box.Text, login_auto = (bool)AutoConnect_Switch.IsChecked };
                    string json_login = JsonConvert.SerializeObject(login_item);
                    System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\anthro_chat.json", json_login);

                    Properties.Settings.Default.Password = Protect(MDP_Box.Password);
                    Properties.Settings.Default.Save();
                }

                Jid jid_user = new Jid(variables.ID_User + "@anthrocity.net");

                Text_Connect.Text = "Connexion...";
                variables.Xmpp.Username = variables.ID_User;
                variables.Xmpp.Resource = "MiniClient";
                variables.Xmpp.Password = MDP_Box.Password;
                variables.Xmpp.Server = "anthrocity.net"; 
                variables.Xmpp.ClientLanguage = "fr";
                variables.Xmpp.AutoPresence = true;
                variables.Xmpp.SocketConnectionType = SocketConnectionType.Bosh;
                variables.Xmpp.ConnectServer = "https://www.anthrocity.net/http-bind/";
                variables.Xmpp.KeepAlive = true; variables.Xmpp.UseSSL = true;
                
                variables.Xmpp.AutoResolveConnectServer = false; variables.Xmpp.AutoRoster = true;

                new Window_ContactChat().Show();
                this.Close();
            }
            else //Sinon, on avertit l'utilisateur qu'il y a un problème de connexion !
            { MessageBox.Show("Vérifie tes identifiants grrr !"); login_button.IsEnabled = true; grid_connect.Visibility = Visibility.Collapsed; ring_connect.IsActive = false; }
        }

        //Ici, on charge le fichier JSON qui permet d'enregistrer les informations du logiciel (ex: les logins)
        private void login_button_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\anthro_chat.json"))
            {
                using (StreamReader sr = new StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Replace("file:/", "").Replace(@"//", "")) + @"\anthro_chat.json"))
                {
                    String line = sr.ReadToEnd(); string mdp = Properties.Settings.Default.Password;
                    Items_Source.Login_Item deserialized_login = JsonConvert.DeserializeObject<Items_Source.Login_Item>(line);
                    Username_Box.Text = deserialized_login.pseudo;
                    try
                    { MDP_Box.Password = Unprotect(Properties.Settings.Default.Password); }
                    catch
                    { mdp = "null"; }
                    
                    if (deserialized_login.login_auto && mdp != "null")
                    {
                        Username_Box.IsEnabled = false; MDP_Box.IsEnabled = false;
                        AutoConnect_Switch.IsChecked = true; notsave = true;
                        grid_connect.Visibility = Visibility.Visible; ring_connect.IsActive = true;
                        GetID();
                    }
                }

            }

        }

        public static string Protect(string str)
        {
            byte[] entropy = Encoding.ASCII.GetBytes(Assembly.GetExecutingAssembly().FullName);
            byte[] data = Encoding.ASCII.GetBytes(str);
            string protectedData = Convert.ToBase64String(ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser));
            return protectedData;
        }

        public static string Unprotect(string str)
        {
            byte[] protectedData = Convert.FromBase64String(str);
            byte[] entropy = Encoding.ASCII.GetBytes(Assembly.GetExecutingAssembly().FullName);
            string data = Encoding.ASCII.GetString(ProtectedData.Unprotect(protectedData, entropy, DataProtectionScope.CurrentUser));
            return data;
        }


    }
}
