using BBBUG.COM.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using System.Windows.Threading;
using WebSocket4Net;

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
        public WebSocket wss;
        public string websocketUrl = "";
        public bool websocketForceStop = false;
        public bool websocketConnected = false;
        public JObject userInfo;

        List<Message> MessageList;
        public MainWindow()
        {
            InitializeComponent();
            //this.startAnimation();
            //this.stopAnimation();
            if (Https.AccessToken =="" && true)
            {
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            //登录成功 获取房间信息
            this.GetRoomInfoAsync();
            this.GetMyInfo();
        }
        private async Task GetMyInfo()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
            };
            JObject result = (JObject)await Https.PostAsync("user/getMyInfo", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                //获取房间成功
                this.userInfo = (JObject)result["data"];
            }
            else if (result["code"].ToString().Equals(Https.CodeLogin))
            {
                //需要输入密码
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
            }
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
                this.roomInfo = (JObject)roomInfo["data"];
                this.UpdateRoomUI();
                //this.GetRoomMessageHistory();
                this.GetRoomWebsocketAsync();
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
        private async void UpdateRoomUI()
        {
            this.Title = this.roomInfo["room_name"].ToString();
            this.room_id.Text = this.roomInfo["room_id"].ToString();
            this.room_title.Text = this.roomInfo["room_name"].ToString();
        }

        private async void GetRoomWebsocketAsync()
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
                this.websocketUrl = "ws://websocket.bbbug.com/?account=" + result["data"]["account"].ToString() + "&channel=" + result["data"]["channel"].ToString() + "&ticket=" + result["data"]["ticket"].ToString();
                Console.WriteLine(this.websocketUrl);
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
            JObject result = (JObject)await Https.PostAsync("message/getMessageList", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                Console.WriteLine(result.ToString());
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
            while (true)
            {
                if (!this.websocketConnected)
                {
                    this.wss = new WebSocket(this.websocketUrl);
                    this.wss.Opened += websocket_Opened;
                    this.wss.Error += websocket_Error;
                    this.wss.Closed += websocket_Closed;
                    this.wss.MessageReceived += websocket_MessageReceived;
                    this.wss.Open();
                    break;
                }
            }
        }
        public void websocketReconnect()
        {
            Console.WriteLine("链接失败");
        }
        private void websocket_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("链接成功");
            this.MessageList = new List<Message>();
        }
        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
            this.websocketReconnect();
        }
        private void websocket_Closed(object sender, EventArgs e)
        {
            this.websocketReconnect();
        }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            this.MessageController(e.Message);
        }
        private void MessageController(string message)
        {
            message = System.Web.HttpUtility.UrlDecode(message, System.Text.Encoding.UTF8);
            JObject result = JObject.Parse(message);
            if (result["type"].ToString().Equals("text"))
            {
                //text消息
                Console.WriteLine(result.ToString());

                MessageList.Add(new Message()
                {
                    message_id = result["message_id"].ToString(),
                    message_content = System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8),
                    user_name = result["user"]["user_name"].ToString(),
                    user_head = result["user"]["user_head"].ToString(),
                    fromMe = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Visible : Visibility.Hidden,
                    fromOther = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Hidden : Visibility.Visible,
                });
                Action action_update_message = () =>
                {
                    this.message_list.ItemsSource = null;
                    this.message_list.ItemsSource = MessageList;
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                this.message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else
            {

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
            this.message_list.SelectedIndex = (this.message_list.Items.Count - 1);
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
            Environment.Exit(0);
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
            string message = this.message_input.Text;
            if (message.Length > 0)
            {
                this.SendMessageToRoom(message, null);
                this.message_input.Text = "";
            }
        }
        private async void SendMessageToRoom(string message,JObject at)
        {
            //message = System.Web.HttpUtility.UrlEncode(System.Web.HttpUtility.UrlEncode(message, System.Text.Encoding.UTF8), System.Text.Encoding.UTF8);
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"to", this.roomId },
                {"type", "text" },
                {"msg", message },
                {"where", "channel" },
            };
            JObject result = (JObject)await Https.PostAsync("message/send", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
            }
        }

        DispatcherTimer pickSongBoxTimer;
        private void ShowPickSongBoxClicked(object sender, MouseButtonEventArgs e)
        {
            this.ShowPickSongBox();
        }
        private bool isLoadingRoomList=false;
        private void ShowPickSongBox()
        {
            if (this.isLoadingRoomList)
            {
                return;
            }
            pickSongBoxTimer = new DispatcherTimer();
            pickSongBoxTimer.Interval = new TimeSpan(100000);   //时间间隔为20ms
            pickSongBoxTimer.Tick += new EventHandler(pickSongBoxAnimation);
            pickSongBoxTimer.Start();
        }
        bool pickSongBoxShow = false;
        public void pickSongBoxAnimation(object sender, EventArgs e)
        {
            int width = 350;
            this.isLoadingRoomList = true;
            if (!pickSongBoxShow) { 
                if (this.pickSongBox.Margin.Right >= 10)
                {
                    pickSongBoxTimer.Stop();
                    pickSongBoxShow = true;
                    this.getHotRoomData();
                    this.isLoadingRoomList = false;
                }
                else
                {
                    pickSongBox.Margin = new Thickness(10, 10, (this.pickSongBox.Margin.Right + 30)> 10?10: (this.pickSongBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (this.pickSongBox.Margin.Right <= 0- width - 50)
                {
                    pickSongBoxTimer.Stop();
                    pickSongBoxShow = false;
                    this.isLoadingRoomList = false;
                }
                else
                {
                    pickSongBox.Margin = new Thickness(10, 10, (this.pickSongBox.Margin.Right - 30) < (0- width- 50) ? (0 - width - 50) : (this.pickSongBox.Margin.Right - 30), 10);
                }
            }
        }
        public async void getHotRoomData()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
            };
            JObject roomList = (JObject)await Https.PostAsync("room/hotRooms", postData);
            if (roomList["code"].ToString().Equals(Https.CodeSuccess))
            {
                //获取房间成功
                JArray result = (JArray)roomList["data"];
                List<Room> RoomList = new List<Room>();
                for (int i = 0; i < result.Count; i++)
                {
                    RoomList.Add(new Room()
                    {
                        room_id = result[i]["room_id"].ToString(),
                        room_name = result[i]["room_name"].ToString(),
                        room_notice = result[i]["room_notice"].ToString() ?? "房间过于牛逼，于是就不写介绍了。",
                        room_online = "("+ result[i]["room_online"].ToString() + ")",
                        user_head = result[i]["user_head"].ToString(),
                        showOnline = (int)result[i]["room_online"] > 0 ? Visibility.Visible : Visibility.Hidden,
                        user_name = System.Web.HttpUtility.UrlDecode(result[i]["user_name"].ToString(), System.Text.Encoding.UTF8)
                    });
                }
                //this.room_list.Items.Clear();
                this.room_list.ItemsSource = RoomList;
            }
            else if (roomInfo["code"].ToString().Equals(Https.CodeLogin))
            {
                //需要登录
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(roomInfo["msg"].ToString());
            }
        }
        LoadingWindow loading;
        private void SelectRoomChanged(object sender, MouseButtonEventArgs e)
        {
            loading = new LoadingWindow();
            Room room = (Room)((ListBox)e.Source).SelectedItem;
            this.roomId = room.room_id;
            this.GetRoomInfoAsync();
            this.pickSongBoxShow = true;
            this.ShowPickSongBox();
        }

        private void MessageInputKeydown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string message = this.message_input.Text;
                if (message.Length > 0)
                {
                    this.SendMessageToRoom(message, null);
                    this.message_input.Text = "";
                }
            }
        }
    }
}
