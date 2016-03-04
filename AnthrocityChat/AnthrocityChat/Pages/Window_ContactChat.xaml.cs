using agsXMPP;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using agsXMPP.protocol.iq.roster;
using System.Windows.Threading;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using System.Collections.ObjectModel;
using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.Win32;
using agsXMPP.protocol.extensions.chatstates;
using System.Net.NetworkInformation;

namespace AnthrocityChat.Pages
{

    public partial class Window_ContactChat : MetroWindow
    {
        GlobalVariables variables = new GlobalVariables(); XmppClientConnection xmpp = new XmppClientConnection();
        Dictionary<string, int> item_username = new Dictionary<string, int>(); Items_Source.Carte_Contact actual_profil = new Items_Source.Carte_Contact();
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon(); bool force_shutdown = false; bool disconnected = false;

        //Cette clé de registre permet d'enregistrer l'application au démarrage de Windows
        RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public Window_ContactChat()
        {
            InitializeComponent();
            Application.Current.MainWindow = this;
            Closing += Window_ContactChat_Closing;
            NetworkChange.NetworkAvailabilityChanged += myNetworkAvailabilityChangeHandler;
            nIcon.Click += NIcon_Click;
        }

        private void myNetworkAvailabilityChangeHandler(object sender, NetworkAvailabilityEventArgs e)
        {
            if(!e.IsAvailable)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    contact_list.Items.Clear();
                    variables.Xmpp.Open();
                    disconnected = true;
                }), DispatcherPriority.Background);
            }
        }

        private void NIcon_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true; this.nIcon.Visible = false;
            SystemCommands.RestoreWindow(this);
            this.Activate();
        }

        /*
            === Annule la fermeture de l'application et la transforme en notification dans la barre des tâches ===
        */
        private void Window_ContactChat_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            if(!force_shutdown)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                System.IO.Stream iconStream = Application.GetResourceStream(new System.Uri("pack://application:,,,/AnthrocityChat;component/Assets/Images/ico_AC.ico")).Stream;
                this.nIcon.Icon = new System.Drawing.Icon(iconStream);
                this.nIcon.Text = "Anthrocity Chat";
                this.nIcon.Visible = true; this.ShowInTaskbar = false;
            }
            else
            { e.Cancel = false; }

        }

        private void Finish_GetContacts(object sender)
        {

        }

        private void Contact(object sender, RosterItem item)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                variables.Xmpp.MessageGrabber.Add(new Jid(item.Jid), new BareJidComparer(), new MessageCB(MessageCallBack), null);
                //contact_list.Items.Add(new Items_Source.Carte_Contact { Nickname = item.Name, Status_Type = "test", Color_Status = new SolidColorBrush(Colors.DarkGreen), list_conv = new ObservableCollection<Items_Source.Message_Conv>(), elipse_white_visibility = Visibility.Visible, Color_Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99808080")) });
            }), DispatcherPriority.Background);
        }

        //Envoie au serveur XMPP que notre body is ready pour recevoir les contacts/messages ! 
        private void OnLogin(object sender)
        { variables.Xmpp.SendMyPresence(); }

        //Quand la fenêtre est chargée, elle charge les fonctions du client XMPP
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nickname_block.Text = variables.Username;
            variables.Xmpp.OnLogin += new ObjectHandler(OnLogin);
            variables.Xmpp.OnRosterItem += new XmppClientConnection.RosterHandler(Contact);
            variables.Xmpp.OnRosterEnd += new ObjectHandler(Finish_GetContacts);
            variables.Xmpp.OnPresence += new PresenceHandler(xmpp_OnPresence);
            variables.Xmpp.OnRosterEnd += new ObjectHandler(Get_End);
            variables.Xmpp.OnMessage += Xmpp_OnMessage;
            variables.Xmpp.OnXmppConnectionStateChanged += Xmpp_OnXmppConnectionStateChanged;

            try
            { variables.Xmpp.Open(); }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }

        }

        private void Xmpp_OnXmppConnectionStateChanged(object sender, XmppConnectionState state)
        {
            if(disconnected)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (disconnected && state == XmppConnectionState.Disconnected)
                        variables.Xmpp.Open();

                    if (state == XmppConnectionState.SessionStarted)
                        disconnected = false;

                }), DispatcherPriority.Background);

            }
        }

        //Cette fonction permet de savoir si un des utilisateurs est en train d'écrire un message ou non
        private void Xmpp_OnMessage(object sender, Message msg)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (msg.From.User == actual_profil.Nickname)
                {
                    switch(msg.Chatstate.ToString())
                    {
                        case "active":
                            IsTyping_Text.Visibility = Visibility.Collapsed;
                            break;

                        case "composing":
                            IsTyping_Text.Visibility = Visibility.Visible; IsTyping_Text.Text = msg.From.User + " est en train d'écrire...";
                            break;
                    }

                }

                foreach (Items_Source.Carte_Contact item in contact_list.Items)
                {
                    if (item.Nickname == msg.From.User)
                    {
                        switch (msg.Chatstate.ToString())
                        {
                            case "active":
                                item.is_typing = false;
                                break;

                            case "composing":
                                item.is_typing = true;
                                break;
                        }
                        break;
                    }
                }

            }), DispatcherPriority.Background);
        }

        //Après avoir reçu les contacts, le logiciel dit au serveur qu'il est présent !
        private void Get_End(object sender)
        { variables.Xmpp.SendMyPresence(); }

        //Cette fonction permet de recevoir les contacts disponibles ou bien les contacts qui ont mis à jour leurs statuts de connexion !
        private void xmpp_OnPresence(object sender, Presence pres)
        {
            Dispatcher.Invoke(new Action(() =>
            {

                if(pres.Type.ToString() == "available" && pres.From.User != variables.Xmpp.Username) //Si la personne est disponible, alors on la rajoute !
                {
                    bool create_or_not = true; string type_status = ""; string status_jabber = ""; SolidColorBrush brush = new SolidColorBrush(Colors.DarkGreen);
                    foreach (Items_Source.Carte_Contact item in contact_list.Items) //On vérifie si on a pas déjà rajouté le contact dans la liste des contacts
                    {
                        if (item.Nickname == pres.From.User)
                        {
                            create_or_not = false;
                            if (item.status_jabber != pres.Show.ToString()) //S'il a été déjà rajouté dans la liste, alors on met à jour son statut !
                            {
                                if(!item.can_chat)
                                {
                                    item.list_conv.Add(new Items_Source.Message_Conv { message = "- Le furs est là ! :) -", align_message = HorizontalAlignment.Center, margin = new Thickness(20, 0, 20, 0), color_bg = new SolidColorBrush(Colors.DarkGreen), date = DateTime.Now.ToString() });
                                    if (pres.From.User == actual_profil.Nickname)
                                    { box_send.IsEnabled = true; }
                                }

                                switch (pres.Show.ToString())
                                {
                                    case "NONE":
                                        item.Status_Type = "Disponible"; item.jid_user = pres.From; item.status_jabber = "NONE"; item.can_chat = true;
                                        item.Color_Status = new SolidColorBrush(Colors.DarkGreen);
                                        break;

                                    case "dnd":
                                        item.Status_Type = "Occupé"; item.jid_user = pres.From; item.status_jabber = "dnd"; item.can_chat = true;
                                        item.Color_Status = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF5722"));
                                        break;

                                    case "away":
                                        item.Status_Type = "Absent"; item.jid_user = pres.From; item.status_jabber = "away"; item.can_chat = true;
                                        item.Color_Status = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800"));
                                        break;
                                }
                                contact_list.Items.Refresh();
                            }
                            break;
                        }
                    }

                    if (create_or_not) //S'il n'as pas été rajouté, alors on le rajoute dans la liste !
                    {
                        switch(pres.Show.ToString())
                        {
                            case "NONE":
                                type_status = "Disponible"; status_jabber = "NONE";
                                brush = new SolidColorBrush(Colors.DarkGreen);
                                break;

                            case "dnd":
                                type_status = "Occupé"; status_jabber = "dnd";
                                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF5722"));
                                break;

                            case "away":
                                type_status = "Absent"; status_jabber = "away";
                                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800"));
                                break;
                        }
                        contact_list.Items.Add(new Items_Source.Carte_Contact { Nickname = pres.From.User, Status_Type = type_status, Color_Status = brush, jid_user = pres.From, list_conv = new ObservableCollection<Items_Source.Message_Conv>(), elipse_white_visibility = Visibility.Visible, Color_Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99808080")), status_jabber = status_jabber, can_chat = true, is_typing = false, text_pause = "" });
                        GetPhoto(pres.From);
                    }

                }
                else //Si elle n'est pas disponible, alors on vérifie si elle a été rajoutée avant à la liste de contacts disponible et donc, la supprimer
                {
                    foreach (Items_Source.Carte_Contact item in contact_list.Items)
                    {
                        if (item.Nickname == pres.From.User)
                        {
                            if(pres.From.User == actual_profil.Nickname) //Si on a été en discussion avec la personne et qu'elle s'est déconnecté, alors on avertit l'utilisateur
                            {
                                item.list_conv.Add(new Items_Source.Message_Conv { message = "- Le furs s'est déconnecté :( -", align_message = HorizontalAlignment.Center, margin = new Thickness(20, 0, 20, 0), color_bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99FF0000")), date = DateTime.Now.ToString() });
                                box_send.IsEnabled = false;
                            }

                            item.Status_Type = "Déconnecté"; item.status_jabber = "nope"; item.can_chat = false;
                            item.Color_Status = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD32F2F"));
                            contact_list.Items.Refresh();
                            //contact_list.Items.Remove(item);
                            break;
                        }
                    }

                }

            }), DispatcherPriority.Background);
        }

        private void contact_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(contact_list.SelectedItem != null)
            {
                actual_profil.text_pause = box_send.Text; box_send.Text = "";
                Items_Source.Carte_Contact publicitem = (sender as ListView).SelectedItem as Items_Source.Carte_Contact; grid_conv.Visibility = Visibility.Visible;
                actual_profil = publicitem; actual_profil.Color_Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99808080"));
                variables.JID_Talk = publicitem.jid_user; chat_listview.DataContext = this; chat_listview.ItemsSource = actual_profil.list_conv; chat_listview.Items.Refresh(); contact_list.Items.Refresh();
                box_send.Text = publicitem.text_pause;

                if (publicitem.is_typing)
                { IsTyping_Text.Visibility = Visibility.Visible; IsTyping_Text.Text = publicitem.Nickname + " est en train d'écrire..."; }
                else
                { IsTyping_Text.Visibility = Visibility.Collapsed; }

                if (!publicitem.can_chat)
                    box_send.IsEnabled = false;
                else
                    box_send.IsEnabled = true;

                if(chat_listview.Items.Count > 0)
                    chat_listview.ScrollIntoView(chat_listview.Items[chat_listview.Items.Count - 1]);

                if (this.ActualWidth < 300)
                    this.Width = 700;
            }
        }

        //Cette fonction s'éxecute quand l'utilisateur reçoit un message d'un autre utilisateur
        private void MessageCallBack(object sender, Message msg, object data)
        {
            if (msg.Body != null)
            {
                Dispatcher.Invoke(new Action(() =>
                {

                    foreach (Items_Source.Carte_Contact item in contact_list.Items) //On va chercher dans la liste de contact disponible, à qui appartient le message reçu
                    {
                        if (item.Nickname == msg.From.User) //Si l'auteur du message est égal à l'auteur de l'item, alors c'est lui !
                        {
                            if (msg.From.User != variables.ID_User)
                            {
                                item.list_conv.Add(new Items_Source.Message_Conv { message = msg.Body, align_message = HorizontalAlignment.Left, margin = new Thickness(0, 0, 20, 0), color_bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99808080")), date = DateTime.Now.ToString() });

                                if (actual_profil.Nickname != msg.From.User) //Si l'utilisateur n'est pas en discussion avec la personne qui a envoyé le message, alors son item contact sera "rouge" pour signaler un nouveau message
                                    item.Color_Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99FF0000"));

                                if (this.WindowState == WindowState.Minimized || !this.IsActive) //Si la fenêtre est minimisé ou si elle est en arrière-plan (non utilisé par l'utilisateur) alors...
                                {
                                    new FlashWindow(Application.Current).FlashApplicationWindow();

                                    if(status_text.Text != "Occupé") //Si l'utilisateur n'est pas occupé, alors il envoie la notification sur le bureau !
                                    { new Notification_Window(msg.From.User, msg.Body, new BitmapImage(item.image_profil)).Show(); }
                                }

                            }
                            break;
                        }
                    }

                    contact_list.Items.Refresh(); //On met à jour les vues
                    chat_listview.Items.Refresh();


                    if (chat_listview.Items.Count != 0) //Si le nombre de messages dans la liste des messages n'est pas égal à 0, alors la liste fait un focus sur le dernier message !
                        chat_listview.ScrollIntoView(chat_listview.Items[chat_listview.Items.Count - 1]);

                }), DispatcherPriority.Background);

            }
        }

        //Si l'utilisateur appuie sur le bouton...
        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        //Si l'utilisateur appuie sur la touche "entrée"...
        private void box_send_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && box_send.Text != "") //Il regarde si la touche est entrée et que le message n'est pas vide
            {
                SendMessage();
            }
        }

        void SendMessage()
        {
            //variables.Xmpp.Send permet d'envoyer un message au serveur XMPP avec le JID de l'utilisateur, le type du message et... le message en lui même !
            variables.Xmpp.Send(new Message(new Jid(variables.JID_Talk), MessageType.chat, box_send.Text));
            actual_profil.list_conv.Add(new Items_Source.Message_Conv { message = box_send.Text, align_message = HorizontalAlignment.Right, margin = new Thickness(20, 0, 0, 0), color_bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#990FA831")), date = DateTime.Now.ToString() });
            box_send.Text = "";
            chat_listview.Items.Refresh();
            chat_listview.ScrollIntoView(chat_listview.Items[chat_listview.Items.Count - 1]);
        }

        //Cette fonction permet de télécharger les avatars des utilisateurs dans la base de données de Anthrocity
        public async void GetPhoto(Jid jid)
        {
            //Ici, on met en place le HttpClient pour télécharger la page
            HttpResponseMessage response = await variables.httpclient.GetAsync("https://www.anthrocity.net/" + jid.User + "/");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            //Si le code de la page contient ceci, alors la page téléchargée est belle et bien un profil
            if(responseString.Contains("js_is_user_profile"))
            {
                HtmlDocument htmlDocument_account = new HtmlDocument();
                htmlDocument_account.LoadHtml(responseString); //HtmlAgilityPack charge le code source de la page

                foreach (Items_Source.Carte_Contact item in contact_list.Items) //Là, on va chercher dans les contacts connectés à qui appartient l'image de profil (via le JID fournis en argument)
                {
                    if (item.Nickname == jid.User) //Si le JID correspond au JID de l'item, alors c'est bien lui !
                    {
                        //Ici, on parse pour avoir l'URL de l'avatar de l'utilisateur
                        string image = htmlDocument_account.DocumentNode.DescendantsAndSelf("a").FirstOrDefault(o => o.GetAttributeValue("href", "").Contains("https://www.anthrocity.net/photo/album/profile/")).Descendants("img").FirstOrDefault().Attributes["data-src"].Value;

                        item.image_profil = new System.Uri(image, UriKind.Absolute); //On met l'image de profil à l'item

                        item.elipse_white_visibility = Visibility.Collapsed; //On enlève l'ellipse blanc par défaut
                        contact_list.Items.Refresh();
                        break;
                    }
                }
            }
        }

        //Si l'utilisateur clic sur le bouton pour éteindre l'application
        private void button_shutdown_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Tu veux vraiment éteindre Anthrocity Chat ?", "Anthrocity Chat", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    variables.Xmpp.OnClose += Xmpp_OnClose;
                    variables.Xmpp.Close();
                    break;

                case MessageBoxResult.No:
                    force_shutdown = false;
                    break;
            }
        }

        //Cette fonction est appelée lorsque l'utilisateur demande la fermeture complète de l'application et permet donc au client XMPP de se déconnecter proprement
        private void Xmpp_OnClose(object sender)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                force_shutdown = true;
                this.Close();
            }), DispatcherPriority.Background);
        }

        private void box_send_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Si le texte est nul, alors il désactive le bouton "envoyer"
            if (box_send.Text != "")
            {
                Send_Button.IsEnabled = true;
                variables.Xmpp.Send(new Message { To = new Jid(variables.JID_Talk), Chatstate = Chatstate.composing, Type = MessageType.chat });
            }
            else
            {
                Send_Button.IsEnabled = false;
                variables.Xmpp.Send(new Message { To = new Jid(variables.JID_Talk), Chatstate = Chatstate.active, Type = MessageType.chat });
            }
        }

        //Quand le component elipse_profil est chargé, alors il met en place l'avatar de l'utilisateur
        private void elipse_profil_Loaded(object sender, RoutedEventArgs e)
        { profile_image_elipse.ImageSource = new BitmapImage(variables.URL_Avatar); }

        //Ouvre le menu des paramètres
        private void button_settings_Click(object sender, RoutedEventArgs e)
        { show_settings.IsOpen = true; }

        //Ouvre le navigateur par défaut pour accéder à ma page Twitter
        private void Twitter_Button_Click(object sender, RoutedEventArgs e)
        { System.Diagnostics.Process.Start("https://twitter.com/Yoshi_Gris"); }

        /*
            === StartupLaunch_Switch voids START ===
        */

        private void StartupLaunch_Switch_Loaded(object sender, RoutedEventArgs e)
        {
            //L'application va détecter si elle est enregistrée au démarrage de Windows
            if (rkApp.GetValue("AC-Chat") == null)
            {
                StartupLaunch_Switch.IsChecked = false;
            }
            else
            {
                StartupLaunch_Switch.IsChecked = true;
            }
        }

        //Si l'utilisateur allume le switch, alors l'application est rajoutée au démarrage de Windows
        private void StartupLaunch_Switch_Checked(object sender, RoutedEventArgs e)
        { rkApp.SetValue("AC-Chat", System.Reflection.Assembly.GetExecutingAssembly().Location); }

        //Sinon, elle est enlevée du démarrage de Windows !
        private void StartupLaunch_Switch_Unchecked(object sender, RoutedEventArgs e)
        { rkApp.DeleteValue("AC-Chat", false); }

        /*
            === StartupLaunch_Switch voids END ===
        */

        private void chat_listview_SourceUpdated(object sender, DataTransferEventArgs e)
        { chat_listview.ScrollIntoView(chat_listview.Items[chat_listview.Items.Count - 1]); }

        private void Stackpanel_Status_MouseEnter(object sender, MouseEventArgs e)
        { Stackpanel_Status.Background = new SolidColorBrush(Color.FromArgb(255, 205, 205, 205)); }

        private void Stackpanel_Status_MouseLeave(object sender, MouseEventArgs e)
        { Stackpanel_Status.Background = new SolidColorBrush(Colors.White); }

        private void Stackpanel_Status_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (menu_status.Visibility == Visibility.Collapsed)
                menu_status.Visibility = Visibility.Visible;
            else
                menu_status.Visibility = Visibility.Collapsed;
        }

        /*
            Ces fonctions permettent de changer le statut de l'utilisateur
        */
        private void Button_Available_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            variables.Xmpp.Status = "En ligne"; variables.Xmpp.Show = ShowType.NONE;
            variables.Xmpp.SendMyPresence(); status_text.Text = "Disponible"; status_text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF008900")); ellipse_status.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF008900"));
            menu_status.Visibility = Visibility.Collapsed;
        }

        private void Button_Occupe_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            variables.Xmpp.Status = "Occupé"; variables.Xmpp.Show = ShowType.dnd;
            variables.Xmpp.SendMyPresence(); status_text.Text = "Occupé"; status_text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF5722")); ellipse_status.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF5722"));
            menu_status.Visibility = Visibility.Collapsed;
        }

        private void Button_Absent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            variables.Xmpp.Status = "Absent"; variables.Xmpp.Show = ShowType.away;
            variables.Xmpp.SendMyPresence(); status_text.Text = "Absent"; status_text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800")); ellipse_status.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800"));
            menu_status.Visibility = Visibility.Collapsed;
        }

        private void Github_Button_Click(object sender, RoutedEventArgs e)
        { System.Diagnostics.Process.Start("https://github.com/YoshiGris/AnthrocityChat"); }
    }
}
