using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace BBBUG.COM
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public string roomId = "888";
        public string roomPassword = "";
        public JObject roomInfo;
        public ClientWebSocket wss;
        public string websocketUrl = "";
        public bool websocketForceStop = false;

        public MainWindow()
        {
            InitializeComponent();
            this.startAnimation();
            //this.stopAnimation();
            Console.WriteLine("xxxxxxx");
            if (Https.AccessToken =="" && false)
            {
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            //登录成功 获取房间信息
            //this.GetRoomInfoAsync();
            List<Message> MessageList = new List<Message>();
            for(int i = 0; i < 50; i++)
            {
                MessageList.Add(new Message()
                {
                    name = "Hamm",
                    content = "吃饭了吊毛!"+i+" "+i*7,
                    fromMe = i % 3 == 0 ? Visibility.Hidden : Visibility.Visible,
                    fromOther = i % 3 == 0 ? Visibility.Visible : Visibility.Hidden,
                    time = "5分钟前",
                    head = "https://oscimg.oschina.net/oscnet/up-f108e334b130164b027f8af5104545e8.jpg"
                });
            }
            this.message_list.Items.Clear();
            this.message_list.ItemsSource = MessageList;


        }
        private async Task GetRoomInfoAsync()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"room_id", this.roomId },
                {"room_password", this.roomPassword },
            };
            roomInfo = (JObject)await Https.PostAsync("room/getRoomInfo", postData);
            if (roomInfo["code"].ToString().Equals(Https.CodeSuccess))
            {
                //获取房间成功
                this.GetRoomWebsocketAsync();
                this.GetRoomMessageHistory();
            }
            else if (roomInfo["code"].ToString().Equals(Https.CodeRedirect))
            {
                //需要输入密码
                //this.GetRoomInfoAsync();
            }
            else if (roomInfo["code"].ToString().Equals(Https.CodeRedirectForce))
            {
                //房间封禁
                AlertWindow alert = new AlertWindow();
                alert.ShowDialog();
                //跳转到888房间
                this.roomId = "888";
                this.GetRoomInfoAsync();
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(roomInfo["msg"].ToString());
            }
        }
        private async Task GetRoomWebsocketAsync()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"channel", this.roomId },
                {"password", this.roomPassword },
            };
            JObject result = (JObject)await Https.PostAsync("room/getWebsocketUrl", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                Console.WriteLine("获取连接地址成功");
                //获取成功
                this.websocketUrl = "wss://websocket.bbbug.com/?account=" + result["data"]["account"].ToString() + "&channel=" + result["data"]["channel"].ToString() + "&ticket=" + result["data"]["ticket"].ToString();
                
                while (this.wss!=null && this.wss.State == WebSocketState.Open)
                {
                    this.wss.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                    Console.WriteLine(this.wss.State.ToString());
                    this.websocketForceStop = true;
                    Console.WriteLine("等待断开");
                    //等待断开
                }
                this.websocketForceStop = false;
                Console.WriteLine("断开重连中");
                this.ConnectWebsocket();
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
                this.GetRoomInfoAsync();
            }
        }
        private async Task GetRoomMessageHistory() 
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"room_id", this.roomId },
                {"room_password", this.roomPassword },
            };
            roomInfo = (JObject)await Https.PostAsync("message/getMessageList", postData);
            if (roomInfo["code"].ToString().Equals(Https.CodeSuccess))
            {
                Console.WriteLine(roomInfo.ToString());
                //获取成功
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(roomInfo["msg"].ToString());
            }
        }
        private async Task ConnectWebsocket()
        {
            using (wss)
            {
                Uri serverUri = new Uri(this.websocketUrl);
                this.wss = new ClientWebSocket();
                await this.wss.ConnectAsync(serverUri, CancellationToken.None);
                while (this.wss.State == WebSocketState.Open)
                {
                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[102400000]);
                    WebSocketReceiveResult result = await this.wss.ReceiveAsync(bytesReceived, CancellationToken.None);
                    Console.WriteLine(Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count));
                }
            }
        }

        private void startAnimation()
        {
            RotateTransform rtf = new RotateTransform();
            rtf.CenterX = Convert.ToDouble(50);
            rtf.CenterY = Convert.ToDouble(50);
            image_song_picture.RenderTransform = rtf;
            DoubleAnimation dbAscending = new DoubleAnimation(0, 360, new Duration
            (TimeSpan.FromSeconds(30)));
            dbAscending.RepeatBehavior = RepeatBehavior.Forever;
            rtf.BeginAnimation(RotateTransform.AngleProperty, dbAscending);
        }
        private void stopAnimation()
        {
            RotateTransform rtf = new RotateTransform();
            rtf.CenterX = Convert.ToDouble(50);
            rtf.CenterY = Convert.ToDouble(50);
            image_song_picture.RenderTransform = rtf;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Image_Close_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_FullScreen_Click(object sender, RoutedEventArgs e)
        {
            if(this.WindowState == WindowState.Normal) 
            { 
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Image_MiniScreen_Clicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }


        private void Button_MessageSend_Clicked(object sender, RoutedEventArgs e)
        {
            this.roomId = "10000";    
            this.GetRoomInfoAsync();
        }
    }
}
