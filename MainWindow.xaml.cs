using BBBUG.COM.Model;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WebSocket4Net;
using WMPLib;

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
        WindowsMediaPlayer audio;
        public MainWindow()
        {
            InitializeComponent();
            startAnimation();
            //stopAnimation();
            if (Https.AccessToken =="" && true)
            {
                LoginWindow login = new LoginWindow();
                login.ShowDialog();
            }
            MessageList = new List<Message>();
            //
            //登录成功 获取房间信息
            GetRoomInfoAsync();
            GetMyInfo();
            GetServerTimeAsync();
            audio = new WindowsMediaPlayer();
            audio.PlayStateChange += Audio_PlayStateChange;
        }

        private void Audio_PlayStateChange(int NewState)
        {
            if(NewState == 1 || NewState == 8)
            {
                nowSongObject = null;
                show_song_name.Content = "歌曲加载中";
                show_song_user.Content = "";
            }
        }

        private async Task GetServerTimeAsync()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
            };
            JObject result = await Https.PostAsync("system/time", postData);
            timeDiff = Convert.ToInt32(result["data"]["time"]);
        }
        private void DeleteSong(object sender, MouseEventArgs args)
        {
            if (picked_list.SelectedItem != null)
            {
                DeleteSongAsync((Song)picked_list.SelectedItem);
            }
        }
        private async Task DeleteSongAsync(Song song)
        {
            //点歌
            Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"mid",song.mid },
                    {"room_id", roomId },
                };
            JObject result = await Https.PostAsync("song/remove", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
                getPickedSongData();
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
        private void DeleteMySong(object sender, MouseEventArgs args)
        {
            if (my_list.SelectedItem != null)
            {
                DeleteMySongAsync((Song)my_list.SelectedItem);
            }
        }
        private async Task DeleteMySongAsync(Song song)
        {
            //点歌
            Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"mid",song.mid },
                    {"room_id", roomId },
                };
            JObject result = await Https.PostAsync("song/deleteMySong", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
                getMySongData();
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
        private void PushSong(object sender, MouseEventArgs args)
        {
            if (picked_list.SelectedItem != null)
            {
                PushSongAsync((Song)picked_list.SelectedItem);
            }
        }
        private async Task PushSongAsync(Song song)
        {
            //点歌
            Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"mid",song.mid },
                    {"room_id", roomId },
                };
            JObject result = await Https.PostAsync("song/push", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
                getPickedSongData();
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
        private void PickMySong(object sender, MouseEventArgs args)
        {
            if (my_list.SelectedItem != null)
            {
                PickSongAsync(((Song)(my_list.SelectedItem)));
            }
        }
        private void PickSong(object sender, MouseEventArgs args)
        {
            if (song_list.SelectedItem != null)
            {
                PickSongAsync(((Song)(song_list.SelectedItem)));
            }
        }
        private async Task PickSongAsync(Song song)
        {
            //点歌
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"mid", song.mid },
                {"room_id", roomId },
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
        private void SearchSong(object sender,MouseEventArgs args)
        {
            SearchSongAsync();
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
                        song_list.Items.Clear();
                        for (int i = 0; i < ((JArray)(result["data"])).Count; i++)
                        {
                            song_list.Items.Add(new Song
                            {
                                mid = ((JArray)(result["data"]))[i]["mid"].ToString(),
                                name = ((JArray)(result["data"]))[i]["name"].ToString(),
                                pic = ((JArray)(result["data"]))[i]["pic"].ToString(),
                                singer = ((JArray)(result["data"]))[i]["singer"].ToString(),
                            }); 
                        }
                    };
                    song_list.Dispatcher.BeginInvoke(action_update_song);

                    Action action_update_song_no_data = () =>
                    {
                        if (song_list.Items.Count > 0)
                        {
                            song_list_nodata.Visibility = Visibility.Hidden;
                            song_list_nodata_tips.Content = "输入关键词搜索想听的歌曲吧";
                        }
                        else
                        {
                            song_list_nodata.Visibility = Visibility.Visible;
                            song_list_nodata_tips.Content = "没有搜索到你想要的歌曲,请重试";
                        }
                    };
                    picked_song_nodata.Dispatcher.BeginInvoke(action_update_song_no_data);

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
                userInfo = (JObject)result["data"];
                Console.WriteLine(userInfo["user_head"].ToString());
                my_head_img.Source = BitmapFrame.Create(new Uri(userInfo["user_head"].ToString(), false), BitmapCreateOptions.None, BitmapCacheOption.Default); ;
                if (userInfo["myRoom"].ToString().Equals("False"))
                {
                    enter_my_room.Text = "创建房间";
                }
                else
                {
                    enter_my_room.Text = "我的房间";
                }
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
                {"room_id", roomId },
                {"room_password", roomPassword },
            };
            roomInfo = (JObject)await Https.PostAsync("room/getRoomInfo", postData);
            if (roomInfo["code"].ToString().Equals(Https.CodeSuccess))
            {
                //获取房间成功
                roomInfo = (JObject)roomInfo["data"];
                message_list.Items.Clear();
                UpdateRoomUI();
                //GetRoomMessageHistory();
                GetRoomWebsocketAsync();
            }
            else if (roomInfo["code"].ToString().Equals(Https.CodeRedirect))
            {
                //需要输入密码
                //GetRoomInfoAsync();
            }
            else if (roomInfo["code"].ToString().Equals(Https.CodeRedirectForce))
            {
                //房间封禁
                AlertWindow alert = new AlertWindow();
                alert.ShowDialog();
                //跳转到888房间
                roomId = "888";
                GetRoomInfoAsync();
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
            Title = roomInfo["room_name"].ToString();
            room_id.Text = roomInfo["room_id"].ToString();
            room_title.Text = roomInfo["room_name"].ToString();
        }

        private async void GetRoomWebsocketAsync()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"channel", roomId },
                {"password", roomPassword },
            };
            JObject result = (JObject)await Https.PostAsync("room/getWebsocketUrl", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                Console.WriteLine("获取连接地址成功");
                //获取成功
                websocketUrl = "ws://websocket.bbbug.com/?account=" + result["data"]["account"].ToString() + "&channel=" + result["data"]["channel"].ToString() + "&ticket=" + result["data"]["ticket"].ToString();
                Console.WriteLine(websocketUrl);
                if (websocketConnected)
                {
                    wss.Close();
                }
                ConnectWebsocket();
            }
            else
            {
                //显示错误的提示信息
                AlertWindow alert = new AlertWindow();
                alert.showDialog(result["msg"].ToString());
                GetRoomInfoAsync();
            }
        }
        private async Task GetRoomMessageHistory() 
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"room_id", roomId },
                {"room_password", roomPassword },
            };
            JObject result = (JObject)await Https.PostAsync("message/getMessageList", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                for(int i = ((JArray)result["data"]).Count-1; i >=0; i--)
                {
                    MessageController(((JArray)(result["data"]))[i]["message_content"].ToString());
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
                if (!websocketConnected)
                {
                    wss = new WebSocket(websocketUrl);
                    wss.Opened += websocket_Opened;
                    wss.Error += websocket_Error;
                    wss.Closed += websocket_Closed;
                    wss.MessageReceived += websocket_MessageReceived;
                    wss.Open();
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
            websocketConnected = true;
            Console.WriteLine("链接成功");
        }
        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
            websocketReconnect();
            websocketConnected = false;
        }
        private void websocket_Closed(object sender, EventArgs e)
        {
            websocketReconnect();
            websocketConnected = false;
        }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            MessageController(e.Message);
        }
        JObject nowSongObject;
        private void MessageController(string message)
        {
            message = System.Web.HttpUtility.UrlDecode(message, System.Text.Encoding.UTF8);
            JObject result = JObject.Parse(message);
            Console.WriteLine(result["type"].ToString());
            if (message_list.Items.Count > 100)
            {
                Action action_update_message = () =>
                {
                    message_list.Items.RemoveAt(0);
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            if (result["type"].ToString().Equals("text"))
            {
                Action action_update_message = () =>
                {
                    message_list.Items.Add(new Message()
                    {
                        message_id = result["message_id"].ToString(),
                        message_type = result["type"].ToString(),
                        message_content = System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8),
                        user_name = result["user"]["user_name"].ToString(),
                        user_head = result["user"]["user_head"].ToString().Length < 5 ? "Images/nohead.jpg" : result["user"]["user_head"].ToString(),
                        fromMe = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Visible : Visibility.Hidden,
                        fromOther = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Hidden : Visibility.Visible,
                        fromSystem = Visibility.Hidden,
                        isPicture = result["type"].ToString().Equals("img") ? Visibility.Visible : Visibility.Hidden,
                        isText = result["type"].ToString().Equals("text") ? Visibility.Visible : Visibility.Hidden,
                        message_time = GetNowTimeFriendly(result["message_time"].ToString())
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("img"))
            {
                try { 
                    WebClient client = new WebClient();
                    HMACSHA1 hmacsha1 = new HMACSHA1();
                    string fileUrl = getStaticImage(System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8));
                    byte[] rstRes = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(fileUrl));
                    string shaString = System.Web.HttpUtility.UrlEncode(Convert.ToBase64String(rstRes)); 
                    if(!System.IO.File.Exists(Environment.CurrentDirectory + "/temp/" + shaString + ".jpg")) { 
                        client.DownloadFile(fileUrl, Environment.CurrentDirectory + "/temp/" + shaString + ".jpg");
                    }
                    Action action_update_message = () =>
                    {
                        message_list.Items.Add(new Message()
                        {
                            message_id = result["message_id"].ToString(),
                            message_type = result["type"].ToString(),
                            message_content = Environment.CurrentDirectory + "/temp/" + shaString + ".jpg",
                            user_name = result["user"]["user_name"].ToString(),
                            user_head = result["user"]["user_head"].ToString().Length < 5 ? "Images/nohead.jpg" : result["user"]["user_head"].ToString(),
                            fromMe = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Visible : Visibility.Hidden,
                            fromOther = (int)result["user"]["user_id"] == (int)userInfo["user_id"] ? Visibility.Hidden : Visibility.Visible,
                            fromSystem = Visibility.Hidden,
                            isPicture = result["type"].ToString().Equals("img") ? Visibility.Visible : Visibility.Hidden,
                            isText = result["type"].ToString().Equals("text") ? Visibility.Visible : Visibility.Hidden,
                            message_time = GetNowTimeFriendly(result["message_time"].ToString())
                        });
                        Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                        ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                        scrollViewer.ScrollToEnd();
                    };
                    message_list.Dispatcher.BeginInvoke(action_update_message);
                }
                catch(Exception e)
                {

                }
            }
            else if (result["type"].ToString().Equals("system"))
            {
                //系统消息
                Action action_update_message = () =>
                {
                    message_list.Items.Add(new Message()
                    {
                        fromMe = Visibility.Hidden,
                        fromOther = Visibility.Hidden,
                        fromSystem = Visibility.Visible,
                        message_content = System.Web.HttpUtility.UrlDecode(result["content"].ToString(), System.Text.Encoding.UTF8),
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("addSong"))
            {
                //系统消息
                Action action_update_message = () =>
                {
                    message_list.Items.Add(new Message()
                    {
                        fromMe = Visibility.Hidden,
                        fromOther = Visibility.Hidden,
                        fromSystem = Visibility.Visible,
                        message_content = System.Web.HttpUtility.UrlDecode(((JObject)(result["user"]))["user_name"].ToString(), System.Text.Encoding.UTF8) + " 点了一首 " + ((JObject)(result["song"]))["name"].ToString() + " (" + ((JObject)(result["song"]))["singer"].ToString()+")",
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("push"))
            {
                //系统消息
                Action action_update_message = () =>
                {
                    message_list.Items.Add(new Message()
                    {
                        fromMe = Visibility.Hidden,
                        fromOther = Visibility.Hidden,
                        fromSystem = Visibility.Visible,
                        message_content = System.Web.HttpUtility.UrlDecode(((JObject)(result["user"]))["user_name"].ToString(), System.Text.Encoding.UTF8) + " 将 " + ((JObject)(result["song"]))["name"].ToString() +" ("+ ((JObject)(result["song"]))["singer"].ToString() + ") 设为置顶候播",
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("removeSong"))
            {
                //系统消息
                Action action_update_message = () =>
                {
                    message_list.Items.Add(new Message()
                    {
                        fromMe = Visibility.Hidden,
                        fromOther = Visibility.Hidden,
                        fromSystem = Visibility.Visible,
                        message_content = System.Web.HttpUtility.UrlDecode(((JObject)(result["user"]))["user_name"].ToString(), System.Text.Encoding.UTF8) + " 移除了歌曲 " + ((JObject)(result["song"]))["name"].ToString() + "("+ ((JObject)(result["song"]))["singer"].ToString() + ")",
                    });
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(message_list, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                };
                message_list.Dispatcher.BeginInvoke(action_update_message);
            }
            else if (result["type"].ToString().Equals("playSong"))
            {
                nowSongObject = (JObject)result;
                Action action_update_song_name = () =>
                {
                    show_song_name.Content = ((JObject)(result["song"]))["name"] + "-" + ((JObject)(result["song"]))["singer"];
                };
                show_song_name.Dispatcher.BeginInvoke(action_update_song_name);
                Action action_update_song_user = () =>
                {
                    show_song_user.Content = "点歌人: " + System.Web.HttpUtility.UrlDecode(((JObject)(result["user"]))["user_name"].ToString(), System.Text.Encoding.UTF8);
                };
                show_song_user.Dispatcher.BeginInvoke(action_update_song_user);

                Action action_update_song_next = () =>
                {
                    if (Convert.ToInt32(userInfo["user_admin"]) == 1 || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(roomInfo["room_user"]) || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(nowSongObject["user"]["user_id"]))
                    {
                        button_next_song.ToolTip = "点击切歌";
                        button_next_song.Source = new BitmapImage(new Uri(@"Images/1_music82.png", UriKind.Relative));
                    }
                    else
                    {
                        button_next_song.ToolTip = "不喜欢,投一票";
                        button_next_song.Source = new BitmapImage(new Uri(@"Images/ios-heart-dislike.png", UriKind.Relative));
                    }
                };
                button_next_song.Dispatcher.BeginInvoke(action_update_song_next);


                int position = GetTimeStamp() - Convert.ToInt32(result["since"] ) - 2;
                if (position < 0)
                {
                    position = 0;
                }
                PlayMusicAsync(((JObject)(result["song"]))["mid"].ToString(),position);
            }
        }
        static int timeDiff = 0;
        public static int GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt32(ts.TotalSeconds) - timeDiff;
        }
        private async Task PlayMusicAsync(string mid,int position)
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
            };
            HttpResponseMessage result =  (HttpResponseMessage)await Https.PostMusicUrl("song/playurl?mid=" +mid , postData);
            audio.URL = result.Headers.Location.ToString();
            audio.controls.currentPosition = position;
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
                return url;
            }
            else
            {
                return "https://static.bbbug.com/uploads/" + url;
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
            message_list.SelectedIndex = (message_list.Items.Count - 1);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Image_Close_Clicked(object sender, RoutedEventArgs e)
        {
            Close();
            Environment.Exit(0);
        }

        private void Image_FullScreen_Click(object sender, RoutedEventArgs e)
        {
            if(WindowState == WindowState.Normal) 
            { 
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Image_MiniScreen_Clicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void Button_MessageSend_Clicked(object sender, RoutedEventArgs e)
        {
            string message = message_input.Text;
            if (message.Length > 0)
            {
                SendMessageToRoom(message, null);
                message_input.Text = "";
            }
        }
        private async void SendMessageToRoom(string message,JObject at)
        {
            //message = System.Web.HttpUtility.UrlEncode(System.Web.HttpUtility.UrlEncode(message, System.Text.Encoding.UTF8), System.Text.Encoding.UTF8);
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"to", roomId },
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
            ShowSelectRoomBox();
            searchSongBoxShow = true;
            ShowSearchSongBox();
            pickedSongBoxShow = true;
            ShowPickedSongBox();
            mySongBoxShow = true;
            ShowMySongBox();

            emoji_box.Visibility = Visibility.Hidden;
        }
        private void HideAllBoxClicked(object sender,MouseButtonEventArgs e)
        {
            hideAllBox();
        }
        private void ShowSelectRoomBoxClicked(object sender, MouseButtonEventArgs e)
        {
            pickedSongBoxShow = true;
            ShowPickedSongBox();

            searchSongBoxShow = true;
            ShowSearchSongBox();

            mySongBoxShow = true;
            ShowMySongBox();

            ShowSelectRoomBox();
        }
        DispatcherTimer selectRoomBoxTimer;
        private bool isLoadingRoomList = false;
        private void ShowSelectRoomBox()
        {
            if (isLoadingRoomList)
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
            isLoadingRoomList = true;
            if (!selectRoomBoxShow)
            {
                if (selectRoomBox.Margin.Right >= 10)
                {
                    selectRoomBoxTimer.Stop();
                    selectRoomBoxShow = true;
                    getHotRoomData();
                    isLoadingRoomList = false;
                }
                else
                {
                    selectRoomBox.Margin = new Thickness(10, 10, (selectRoomBox.Margin.Right + 30) > 10 ? 10 : (selectRoomBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (selectRoomBox.Margin.Right <= 0 - width - 50)
                {
                    selectRoomBoxTimer.Stop();
                    selectRoomBoxShow = false;
                    isLoadingRoomList = false;
                }
                else
                {
                    selectRoomBox.Margin = new Thickness(10, 10, (selectRoomBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (selectRoomBox.Margin.Right - 30), 10);
                }
            }
        }
        private void ShowPickedSongBoxClicked(object sender, MouseButtonEventArgs e)
        {
            selectRoomBoxShow = true;
            ShowSelectRoomBox();

            searchSongBoxShow = true;
            ShowSearchSongBox();

            mySongBoxShow = true;
            ShowMySongBox();

            ShowPickedSongBox();
        }
        DispatcherTimer pickedSongBoxTimer;
        private bool isLoadingPickedSong = false;
        private void ShowPickedSongBox()
        {
            if (isLoadingPickedSong)
            {
                return;
            }
            pickedSongBoxTimer = new DispatcherTimer();
            pickedSongBoxTimer.Interval = new TimeSpan(100000);   //时间间隔为20ms
            pickedSongBoxTimer.Tick += new EventHandler(pickedSongBoxAnimation);
            pickedSongBoxTimer.Start();
        }
        bool pickedSongBoxShow = false;
        public void pickedSongBoxAnimation(object sender, EventArgs e)
        {
            int width = 350;
            isLoadingPickedSong = true;
            if (!pickedSongBoxShow)
            {
                if (pickedSongBox.Margin.Right >= 10)
                {
                    pickedSongBoxTimer.Stop();
                    pickedSongBoxShow = true;
                    getPickedSongData();
                    isLoadingPickedSong = false;
                }
                else
                {
                    pickedSongBox.Margin = new Thickness(10, 10, (pickedSongBox.Margin.Right + 30) > 10 ? 10 : (pickedSongBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (pickedSongBox.Margin.Right <= 0 - width - 50)
                {
                    pickedSongBoxTimer.Stop();
                    pickedSongBoxShow = false;
                    isLoadingPickedSong = false;
                }
                else
                {
                    pickedSongBox.Margin = new Thickness(10, 10, (pickedSongBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (pickedSongBox.Margin.Right - 30), 10);
                }
            }
        }
        private void ShowSearchSongBoxClicked(object sender, MouseButtonEventArgs e)
        {
            selectRoomBoxShow = true;
            ShowSelectRoomBox();

            pickedSongBoxShow = true;
            ShowPickedSongBox();

            mySongBoxShow = true;
            ShowMySongBox();

            ShowSearchSongBox();
        }
        DispatcherTimer searchSongBoxTimer;
        private bool isLoadingSearchSongBox = false;
        private void ShowSearchSongBox()
        {
            if (isLoadingSearchSongBox)
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
            isLoadingSearchSongBox = true;
            if (!searchSongBoxShow)
            {
                if (searchSongBox.Margin.Right >= 10)
                {
                    searchSongBoxTimer.Stop();
                    searchSongBoxShow = true;
                    isLoadingSearchSongBox = false;
                }
                else
                {
                    searchSongBox.Margin = new Thickness(10, 10, (searchSongBox.Margin.Right + 30) > 10 ? 10 : (searchSongBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (searchSongBox.Margin.Right <= 0 - width - 50)
                {
                    searchSongBoxTimer.Stop();
                    searchSongBoxShow = false;
                    isLoadingSearchSongBox = false;
                }
                else
                {
                    searchSongBox.Margin = new Thickness(10, 10, (searchSongBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (searchSongBox.Margin.Right - 30), 10);
                }
            }
        }
        private void ShowMySongBoxClicked(object sender, MouseButtonEventArgs e)
        {
            selectRoomBoxShow = true;
            ShowSelectRoomBox();

            pickedSongBoxShow = true;
            ShowPickedSongBox();

            searchSongBoxShow = true;
            ShowSearchSongBox();

            ShowMySongBox();
        }
        DispatcherTimer mySongBoxTimer;
        private bool isLoadingMySongBox = false;
        private void ShowMySongBox()
        {
            if (isLoadingMySongBox)
            {
                return;
            }
            mySongBoxTimer = new DispatcherTimer();
            mySongBoxTimer.Interval = new TimeSpan(100000);   //时间间隔为20ms
            mySongBoxTimer.Tick += new EventHandler(mySongBoxAnimation);
            mySongBoxTimer.Start();
        }
        bool mySongBoxShow = false;
        public void mySongBoxAnimation(object sender, EventArgs e)
        {
            int width = 350;
            isLoadingMySongBox = true;
            if (!mySongBoxShow)
            {
                if (mySongBox.Margin.Right >= 10)
                {
                    mySongBoxTimer.Stop();
                    mySongBoxShow = true;
                    isLoadingMySongBox = false;
                    getMySongData();
                }
                else
                {
                    mySongBox.Margin = new Thickness(10, 10, (mySongBox.Margin.Right + 30) > 10 ? 10 : (mySongBox.Margin.Right + 30), 10);
                }
            }
            else
            {
                if (mySongBox.Margin.Right <= 0 - width - 50)
                {
                    mySongBoxTimer.Stop();
                    mySongBoxShow = false;
                    isLoadingMySongBox = false;
                }
                else
                {
                    mySongBox.Margin = new Thickness(10, 10, (mySongBox.Margin.Right - 30) < (0 - width - 50) ? (0 - width - 50) : (mySongBox.Margin.Right - 30), 10);
                }
            }
        }
        Visibility showDelete = Visibility.Hidden;
        public async void getPickedSongData()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"room_id",roomId }
            };
            JObject result = (JObject)await Https.PostAsync("song/songList", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                Action action_update_song_picked = () =>
                {
                    picked_list.Items.Clear();
                    for (int i = 0; i < ((JArray)(result["data"])).Count; i++)
                    {
                        if (Convert.ToInt32(userInfo["user_admin"]) == 1 || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(roomInfo["room_user"]) || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(result["data"][i]["user"]["user_id"]))
                        {
                            showDelete = Visibility.Visible;
                        }
                        else
                        {
                            showDelete = Visibility.Hidden;
                        }
                        picked_list.Items.Add(new Song
                        {
                            mid = ((JArray)(result["data"]))[i]["song"]["mid"].ToString(),
                            name = ((JArray)(result["data"]))[i]["song"]["name"].ToString(),
                            singer = ((JArray)(result["data"]))[i]["song"]["singer"].ToString(),
                            pic = ((JArray)(result["data"]))[i]["song"]["pic"].ToString(),
                            showDelete = showDelete,
                            pickerName = System.Web.HttpUtility.UrlDecode(((JArray)(result["data"]))[i]["user"]["user_name"].ToString(), System.Text.Encoding.UTF8),
                        }); ;
                    }
                };
                picked_list.Dispatcher.BeginInvoke(action_update_song_picked);
                Action action_update_song_picked_no_data = () =>
                {
                    if (picked_list.Items.Count > 0)
                    {
                        picked_song_nodata.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        picked_song_nodata.Visibility = Visibility.Visible;
                    }
                };
                picked_song_nodata.Dispatcher.BeginInvoke(action_update_song_picked_no_data);


            }
            else if (result["code"].ToString().Equals(Https.CodeLogin))
            {
                //需要登录
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
        public async void getMySongData()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"room_id",roomId },
                //{"order","recent" },
                {"per_page","50" }
            };
            JObject result = (JObject)await Https.PostAsync("song/mySongList", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {
                Action action_update_song_my = () =>
                {
                    my_list.Items.Clear();
                    for (int i = 0; i < ((JArray)(result["data"])).Count; i++)
                    {
                        my_list.Items.Add(new Song
                        {
                            mid = ((JArray)(result["data"]))[i]["mid"].ToString(),
                            name = ((JArray)(result["data"]))[i]["name"].ToString(),
                            singer = ((JArray)(result["data"]))[i]["singer"].ToString(),
                            pic = ((JArray)(result["data"]))[i]["pic"].ToString(),
                            played = ((JArray)(result["data"]))[i]["played"].ToString(),
                        }); ;
                    }
                };
                my_list.Dispatcher.BeginInvoke(action_update_song_my);
                Action action_update_song_my_no_data = () =>
                {
                    if (my_list.Items.Count > 0)
                    {
                        my_song_nodata.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        my_song_nodata.Visibility = Visibility.Visible;
                    }
                };
                my_song_nodata.Dispatcher.BeginInvoke(action_update_song_my_no_data);


            }
            else if (result["code"].ToString().Equals(Https.CodeLogin))
            {
                //需要登录
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
                        room_online = "(" + result[i]["room_online"].ToString() + ")",
                        user_head = result[i]["user_head"].ToString().Length < 5 ? "Images/nohead.jpg" : result[i]["user_head"].ToString(),
                        showOnline = (int)result[i]["room_online"] > 0 ? Visibility.Visible : Visibility.Hidden,
                        user_name = System.Web.HttpUtility.UrlDecode(result[i]["user_name"].ToString(), System.Text.Encoding.UTF8)
                    });
                }
                //room_list.Items.Clear();
                room_list.ItemsSource = RoomList;
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
        private void SelectRoomChanged(object sender, MouseButtonEventArgs e)
        {
            Room room = (Room)((ListBox)e.Source).SelectedItem;
            roomId = room.room_id;
            GetRoomInfoAsync();
            selectRoomBoxShow = true;
            ShowSelectRoomBox();
        }

        private void MessageInputKeydown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string message = message_input.Text;
                if (message.Length > 0)
                {
                    SendMessageToRoom(message, null);
                    message_input.Text = "";
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

        private void PassSong(object sender, MouseButtonEventArgs e)
        {
            if (nowSongObject != null)
            {
                PassSongAsync();
            }
            else
            {
                wss.Send("getNowSong");
            }
        }
        private async Task PassSongAsync()
        {
            Dictionary<string, string> postData = new Dictionary<string, string>()
                {
                    {"mid",nowSongObject["song"]["mid"].ToString() },
                {"room_id",roomId }
                };
            JObject result = await Https.PostAsync("song/pass", postData);
            if (result["code"].ToString().Equals(Https.CodeSuccess))
            {

                if (Convert.ToInt32(userInfo["user_admin"]) == 1 || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(roomInfo["room_user"]) || Convert.ToInt32(userInfo["user_id"]) == Convert.ToInt32(nowSongObject["user"]["user_id"]))
                {
                    getPickedSongData();
                }
                else
                {
                    AlertWindow alert = new AlertWindow();
                    alert.showDialog(result["msg"].ToString());
                    audio.controls.stop();
                    nowSongObject = null;
                }
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

        private void SearchSongTextBoxKeydown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                SearchSongAsync();
            }
        }

        private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void EnterMyRoomClicked(object sender, MouseButtonEventArgs e)
        {
            if (enter_my_room.Text.Equals("我的房间"))
            {
                roomId = userInfo["myRoom"]["room_id"].ToString();
                GetRoomInfoAsync();
            }
            else
            {
                new AlertWindow().showDialog("创建房间功能即将上线,你可以先在PC端创建房间后再玩耍~");
            }
        }

        private void MyHeadImageClicked(object sender, MouseButtonEventArgs e)
        {
            new AlertWindow().showDialog("个人中心即将上线，敬请期待!");
        }

        private void SettingClicked(object sender, MouseButtonEventArgs e)
        {
            new AlertWindow().showDialog("系统设置正在开发中，敬请期待！");
        }

        private void ShowEmojiBox(object sender, MouseButtonEventArgs e)
        {
            if (emoji_box.Visibility == Visibility.Hidden) { 
                emoji_box.Visibility = Visibility.Visible;
            }
            else
            {
                emoji_box.Visibility = Visibility.Hidden;
            }
        }

        private async void EmojiSendClickedAsync(object sender, MouseButtonEventArgs e)
        {
            string message = "https://cdn.bbbug.com/images/emoji/" + ((Image)e.Source).Source.ToString().Replace("pack://application:,,,/BBBUG音乐聊天室;component/Images/Emojis/", "");
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"to", roomId },
                {"type", "img" },
                {"msg", message },
                {"resource", message },
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
    }
}
