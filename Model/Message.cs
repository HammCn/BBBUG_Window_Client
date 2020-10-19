using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BBBUG.COM
{
    class Message
    {
        public string message_id { set; get; }
        public string message_user { set; get; }
        public string user_name { set; get; }
        public string user_head { set; get; }
        public string message_content { set; get; }
        public Visibility fromMe { set; get; }
        public Visibility fromOther { set; get; }
        public string message_time { get; set; }
    }
}
