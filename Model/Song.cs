using System.Windows;

namespace BBBUG.COM.Model
{
    class Song
    {
        public string mid { get; set; }
        public string name { get; set; }
        public string pic { get; set; }
        public string singer {get;set;}
        public string pickerName { get; set; }
        public string played { get; set; }
        public Visibility showDelete { get; set; }
    }
}
