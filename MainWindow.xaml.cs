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
            this.startAnimation();
            //this.stopAnimation();
            if (Https.AccessToken =="" && true)
            {
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            this.MessageList = new List<Message>();
            //
            //登录成功 获取房间信息
            this.GetRoomInfoAsync();
            this.GetMyInfo();

        }
        private void PickSong(object sender, MouseEventArgs args)
        {
            this.PickSongAsync();
        }
        private async Task PickSongAsync()
        {
            if (this.song_list.SelectedItem!=null)
            {
                //点歌
                Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"mid", ((Song)(this.song_list.SelectedItem)).mid },
                    {"room_id", this.roomId },
                };
                JObject result = (JObject)await Https.PostAsync("song/addSong", postData);
                if (result["code"].ToString().Equals(Https.CodeSuccess))
                {
                    AlertWindow alert = new AlertWindow();
                    alert.showDialog(result["msg"].ToString());
                }
                else if (result["code"].ToString().Equals(Https.CodeLogin))
                {
                    LoginWindow login = new LoginWindow();
                    login.ShowDialog();
                }
                else
                {
                    AlertWindow alert = new AlertWindow();
                    alert.showDialog(result["msg"].ToString());
                }
            }
        }
        private void SearchSong(object sender,MouseEventArgs args)
        {
            this.SearchSongAsync();
        }
        private async Task SearchSongAsync()
        {
            string keyword = search_song_keyword.Text.Trim();
            if (keyword.Length>0)
            {
                Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"keyword", keyword },
                };
                JObject result = (JObject)await Https.PostAsync("song/search", postData);
                if (result["code"].ToString().Equals(Https.CodeSuccess))
                {
                    Action action_update_song = () =>
                    {
                        this.song_list.Items.Clear();
                        for (int i = 0; i < ((JArray)(result["data"])).Count; i++)
                        {
                            this.song_list.Items.Add(new Song
                            {
                                mid = ((JArray)(result["data"]))[i]["mid"].ToString(),
                                name = ((JArray)(result["data"]))[i]["name"].ToString(),
                                pic = ((JArray)(result["data"]))[i]["pic"].ToString(),
                                singer = ((JArray)(result["data"]))[i]["singer"].ToString(),
                            }); ;
                        }
                    };
                    this.song_list.Dispatcher.BeginInvoke(action_update_song);
                }
                else if (result["code"].ToString().Equals(Https.CodeLogin))
                {
                    LoginWindow login = new LoginWindow();
                    login.ShowDialog();
                }
                else
                {
                    AlertWindow alert = new AlertWindow();
                    alert.showDialog(result["msg"].ToString());
                }
            }
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
                this.GetRoomMessageHistory();
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
                if (this.websocketConnected)
                {
                    this.wss.Close();
                }
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
                for(int i = ((JArray)result["data"]).Count-1; i >=0; i--)
                {
                    this.MessageController(((JArray)(result["data"]))[i]["message_content"].ToString());
                }
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
            this.websocketConnected = true;
            Console.WriteLine("链接成功");
        }
        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
            this.websocketReconnect();
            this.websocketConnected = false;
        }
        private void websocket_Closed(object sender, EventArgs e)
        {
            this.websocketReconnect();
            this.websocketConnected = false;
        }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            this.MessageController(e.Message);
        }
        private void MessageController(string message)
        {
            message = System.Web.HttpUtility.UrlDecode(message, System.Text.Encoding.UTF8);
            JObject result = JObject.Parse(message);
            Console.WriteLine(result["type"].ToString());
            if (this.message_list.Items.Count > 100)
            {
                Action action_update_message = () =>
                {
                    this.message_list.Items.RemoveAt(0);
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                this.message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            if (result["type"].ToString().Equals("text"))
            {
                //text消息
                Action action_update_message = () =>
                {
                    this.message_list.Items.Add(new Message()
                    {
                        message_id = result["message_id"].ToString(),
                        message_type = result["type"].ToString(),
                        message_content = System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8),
                        user_name = result["user"]["user_name"].ToString(),
                        user_head = result["user"]["user_head"].ToString().Length < 5 ? "Images/nohead.jpg" : result["user"]["user_head"].ToString(),
                        fromMe = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Visible : Visibility.Hidden,
                        fromOther = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Hidden : Visibility.Visible,
                        isPicture = result["type"].ToString().Equals("img") ? Visibility.Visible : Visibility.Hidden,
                        isText = result["type"].ToString().Equals("text") ? Visibility.Visible : Visibility.Hidden,
                        message_time = this.GetNowTimeFriendly(result["message_time"].ToString())
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                this.message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("img"))
            {
                //text消息
                Action action_update_message = () =>
                {
                    this.message_list.Items.Add(new Message()
                    {
                        message_id = result["message_id"].ToString(),
                        message_type = result["type"].ToString(),
                        message_content = this.getStaticImage(System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8)),
                        user_name = result["user"]["user_name"].ToString(),
                        user_head = result["user"]["user_head"].ToString().Length < 5 ? "Images/nohead.jpg" : result["user"]["user_head"].ToString(),
                        fromMe = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Visible : Visibility.Hidden,
                        fromOther = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Hidden : Visibility.Visible,
                        isPicture = result["type"].ToString().Equals("img") ? Visibility.Visible : Visibility.Hidden,
                        isText = result["type"].ToString().Equals("text") ? Visibility.Visible : Visibility.Hidden,
                        message_time = this.GetNowTimeFriendly(result["message_time"].ToString())
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                this.message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("playSong"))
            {
                Action action_update_song_name = () =>
                {
                    this.show_song_name.Content = ((JObject)(result["song"]))["name"] + "-" + ((JObject)(result["song"]))["singer"];
                };
                this.show_song_name.Dispatcher.BeginInvoke(action_update_song_name);
                Action action_update_song_user = () =>
                {
                    this.show_song_user.Content = "点歌人: " + ((JObject)(result["user"]))["user_name"];
                };
                this.show_song_user.Dispatcher.BeginInvoke(action_update_song_user);
            }
        }
        private string GetNowTimeFriendly(string timestamps)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = (Convert.ToInt64(timestamps) * 10000000);
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime targetDt = dtStart.Add(toNow);
            return targetDt.ToString("HH:mm");
        }
        private string getStaticImage(string url)
        {
            if(url.StartsWith("https://")|| url.StartsWith("https://")){

                Console.WriteLine(url);
                return url;
            }
            else
            {
                Console.WriteLine("https://api.bbbug.com/uploads/" + url);
                return "https://api.bbbug.com/uploads/" + url;
            }
        }
        private void startAnimation()
        {
            RotateTransform rtf = new RotateTransform();
            rtf.CenterX = Convert.ToDouble(12);
            rtf.CenterY = Convert.ToDouble(12);
            icon_song_player.RenderTransform = rtf;
            DoubleAnimation dbAscending = new DoubleAnimation(0, 360, new Duration
            (TimeSpan.FromSeconds(10)));
            dbAscending.RepeatBehavior = RepeatBehavior.Forever;
            rtf.BeginAnimation(RotateTransform.AngleProperty, dbAscending);
        }
        private void stopAnimation()
        {
            RotateTransform rtf = new RotateTransform();
            rtf.CenterX = Convert.ToDouble(12);
            rtf.CenterY = Convert.ToDouble(12);
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
        private void hideAllBox()
        {
            selectRoomBoxShow = true;
            this.ShowSelectRoomBox();
            searchSongBoxShow = true;
            this.ShowSearchSongBox();
        }
        private void HideAllBoxClicked(object sender,MouseButtonEventArgs e)
        {
            this.hideAllBox();
        }
        private void ShowSelectRoomBoxClicked(object sender, MouseButtonEventArgs e)
        {
            searchSongBoxShow = true;
            this.ShowSearchSongBox();
            this.ShowSelectRoomBox();
        }
        DispatcherTimer selectRoomBoxTimer;
        private bool isLoadingRoomList = false;
        private void ShowSelectRoomBox()
        {
            if (this.isLoadingRoomList)
            {
                return;
            }
            selectRoomBoxTimer = new DispatcherTimer();
            selectRoomBoxTimer.Interval = new TimeSpan(100000);   //时间间隔为20ms
            selectRoomBoxTimer.Tick += new EventHandler(selectRoomBoxAnimation);
            selectRoomBoxTimer.Start();
        }
        bool selectRoomBoxShow = false;
        public void selectRoomBoxAnimation(object sender, EventArgs e)
        {
            int width = 350;
            this.isLoadingRoomList = true;
            if (!selectRoomBoxShow)
            {
                if (this.selectRoomBox.Margin.Right >= 10)
                {
                    selectRoomBoxTimer.Stop();
                    selectRoomBoxShow = true;
                    this.getHotRoomData();
                    this.isLoadingRoomList = false;
                }
                else
                {
                    selectRoomBox.Margin = new Thickness(10, 10, (this.selectRoomBox.Margin.Right + 30) > 10 ? 10 : (this.selectRoomBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (this.selectRoomBox.Margin.Right <= 0 - width - 50)
                {
                    selectRoomBoxTimer.Stop();
                    selectRoomBoxShow = false;
                    this.isLoadingRoomList = false;
                }
                else
                {
                    selectRoomBox.Margin = new Thickness(10, 10, (this.selectRoomBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (this.selectRoomBox.Margin.Right - 30), 10);
                }
            }
        }
        private void ShowSearchSongBoxClicked(object sender, MouseButtonEventArgs e)
        {
            selectRoomBoxShow = true;
            this.ShowSelectRoomBox();
            this.ShowSearchSongBox();
        }
        DispatcherTimer searchSongBoxTimer;
        private bool isLoadingSearchSongBox = false;
        private void ShowSearchSongBox()
        {
            if (this.isLoadingSearchSongBox)
            {
                return;
            }
            searchSongBoxTimer = new DispatcherTimer();
            searchSongBoxTimer.Interval = new TimeSpan(100000);   //时间间隔为20ms
            searchSongBoxTimer.Tick += new EventHandler(searchSongBoxAnimation);
            searchSongBoxTimer.Start();
        }
        bool searchSongBoxShow = false;
        public void searchSongBoxAnimation(object sender, EventArgs e)
        {
            int width = 350;
            this.isLoadingSearchSongBox = true;
            if (!searchSongBoxShow)
            {
                if (this.searchSongBox.Margin.Right >= 10)
                {
                    searchSongBoxTimer.Stop();
                    searchSongBoxShow = true;
                    this.getHotRoomData();
                    this.isLoadingSearchSongBox = false;
                }
                else
                {
                    searchSongBox.Margin = new Thickness(10, 10, (this.searchSongBox.Margin.Right + 30) > 10 ? 10 : (this.searchSongBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (this.searchSongBox.Margin.Right <= 0 - width - 50)
                {
                    searchSongBoxTimer.Stop();
                    searchSongBoxShow = false;
                    this.isLoadingSearchSongBox = false;
                }
                else
                {
                    searchSongBox.Margin = new Thickness(10, 10, (this.searchSongBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (this.searchSongBox.Margin.Right - 30), 10);
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
                    Console.WriteLine(result[i]["user_head"].ToString() ?? "Imgaes/nohead.jpg");
                    RoomList.Add(new Room()
                    {
                        room_id = result[i]["room_id"].ToString(),
                        room_name = result[i]["room_name"].ToString(),
                        room_notice = result[i]["room_notice"].ToString() ?? "房间过于牛逼，于是就不写介绍了。",
                        room_online = "("+ result[i]["room_online"].ToString() + ")",
                        user_head = result[i]["user_head"].ToString().Length<5 ? "Images/nohead.jpg" : result[i]["user_head"].ToString(),
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
            this.selectRoomBoxShow = true;
            this.ShowSelectRoomBox();
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

        private void SearchSongTextBoxFocused(object sender, RoutedEventArgs e)
        {
            if (search_song_keyword.Text.Equals("输入歌手/歌名/专辑搜索..."))
            {
                search_song_keyword.Text = "";
            }
        }

        private void SearchSongTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (search_song_keyword.Text.Trim().Length == 0)
            {
                search_song_keyword.Text = "输入歌手/歌名/专辑搜索...";
            }
        }

        private void SendMessageTextBoxFocused(object sender, RoutedEventArgs e)
        {
            if (message_input.Text.Equals("说点什么吧..."))
            {
                message_input.Text = "";
            }
        }

        private void SendMessageTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (message_input.Text.Trim().Length == 0)
            {
                message_input.Text = "说点什么吧...";
            }
        }

    }
}
