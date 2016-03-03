using agsXMPP;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace AnthrocityChat
{
    public class GlobalVariables
    {
        private static CookieContainer cookieContainer = new CookieContainer();
        private static HttpClientHandler clienthandler = new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true, CookieContainer = cookieContainer };
        private static HttpClient client = new HttpClient(clienthandler);
        public HttpClient httpclient
        {
            get { return client; }
            set { client = value; }
        }
        public HttpClientHandler Client_Handler
        {
            get { return clienthandler; }
            set { clienthandler = value; }
        }
        public CookieContainer Cookie_Container
        {
            get { return cookieContainer; }
            set { cookieContainer = value; }
        }

        private static XmppClientConnection xmpp_ = new XmppClientConnection();
        public XmppClientConnection Xmpp
        {
            get { return xmpp_; }
            set { xmpp_ = value; }
        }


        /*
            ANTHROCITY
        */
        private static string ID_Temp_ = "";
        public string ID_User
        {
            get { return ID_Temp_; }
            set { ID_Temp_ = value; }
        }

        private static string User_Name_ = "";
        public string Username
        {
            get { return User_Name_; }
            set { User_Name_ = value; }
        }

        private static System.Uri Url_Avatar_;
        public System.Uri URL_Avatar
        {
            get { return Url_Avatar_; }
            set { Url_Avatar_ = value; }
        }

        private static string Password_ = "";
        public string Password
        {
            get { return Password_; }
            set { Password_ = value; }
        }

        private static string JID_Talk_Temp_ = "";
        public string JID_Talk
        {
            get { return JID_Talk_Temp_; }
            set { JID_Talk_Temp_ = value; }
        }

        /*
            NOTIFS
        */

        private static string Notif_Username_ = "";
        public string Notif_Username
        {
            get { return Notif_Username_; }
            set { Notif_Username_ = value; }
        }

        private static string Text_Notif_ = "";
        public string Text_Notif
        {
            get { return Text_Notif_; }
            set { Text_Notif_ = value; }
        }

        private static Dictionary<string, System.Uri> dico_image = new Dictionary<string, System.Uri>();
        public Dictionary<string, System.Uri> Dico_Images_Profil
        {
            get { return dico_image; }
            set { dico_image = value; }
        }
    }
}
