using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BBBUG.COM
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public string access_token = "";
        public string user_account_value = "";
        public LoginWindow()
        {
            InitializeComponent();
            this.user_account_value = Config.GetValue("user_account");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }
        private async void DoLogin()
        {
            string user_account = this.user_account.Text.Trim();
            string user_password = this.user_password.Password.Trim();
            if (user_account == "" || user_password == "")
            {
                return;
            }
            this.button_login.IsEnabled = false;
            this.button_login.Content = "Loading...";
            Config.SetValue("user_account", user_account);
            Dictionary<string, string> postData = new Dictionary<string, string>()
            {
                {"user_account", user_account },
                { "user_password", user_password },
                {"plat","windows"},
                {"version","10000"}
            };
            JObject jo = (JObject)await Https.PostAsync("user/login", postData);
            this.button_login.IsEnabled = true;
            this.button_login.Content = "立即登录";
            if (jo["code"].ToString().Equals(Https.CodeSuccess))
            {
                AlertWindow alert = new AlertWindow();
                //alert.showDialog("", "登录成功");
                Https.AccessToken = (string)jo["data"]["access_token"];
                this.Close();
            }
            else
            {
                AlertWindow alert = new AlertWindow();
                alert.showDialog((string)jo["msg"], "登录失败");
            }
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            System.Environment.Exit(0);
        }

        private void LoginWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.user_account.Text = this.user_account_value;
            //this.user_password.Password = "";
        }

        private void passwordBoxKeydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoLogin();
            }
        }
    }
}
