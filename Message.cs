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
        public string name { set; get; }
        public string content { set; get; }
        public string head { set; get; }
        public Visibility fromMe { set; get; }
        public Visibility fromOther { set; get; }
        public string time { get; set; }
    }
}
