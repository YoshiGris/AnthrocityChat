using agsXMPP;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnthrocityChat
{
    public class Items_Source
    {
        public class Login_Item
        {
            public string pseudo { get; set; }
            public string password { get; set; }
            public bool login_auto { get; set; }
        }

        public class Carte_Contact
        {
            public string Nickname { get; set; }
            public string Status_Type { get; set; }
            public string status_jabber { get; set; }
            public Jid jid_user { get; set; }
            public ObservableCollection<Message_Conv> list_conv { get; set; }
            public SolidColorBrush Color_Status { get; set; }
            public SolidColorBrush Background_Select_Color { get; set; }
            public System.Uri image_profil { get; set; }
            public string Real_Name { get; set; }
            public Visibility elipse_white_visibility { get; set; }

            public Brush Color_Background { get; set; }
            public bool can_chat { get; set; }
            public bool is_typing { get; set; }
            public string text_pause { get; set; }

            public string last_message { get; set; }
        }

        public class Message_Conv
        {
            public string message { get; set; }
            public string date { get; set; }
            public SolidColorBrush color_bg { get; set; }
            public Thickness margin { get; set; }
            public HorizontalAlignment align_message { get; set; }
        }
    }
}
