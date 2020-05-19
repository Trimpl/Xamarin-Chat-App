using Android.Webkit;
using App1.Models;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace App1
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        ChatViewModel viewModel;
        public MainPage()
        {
            if (App.Current.Properties.ContainsKey("Token"))
            {
                viewModel = new ChatViewModel(null);
            }
            InitializeComponent();
            var MyEntry = new Entry { Text = "I am an Entry" };
            if (App.Current.Properties.ContainsKey("Token"))
            {
                this.BindingContext = this;
                Dictionary<string, string> tokenJson = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)App.Current.Properties["Token"]);
                string token = tokenJson["access_token"];
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage();
                request.RequestUri = new Uri("http://192.168.100.2:3000/Mobile/GetListOfUsers?jwt=" + token);
                request.Method = HttpMethod.Post;
                request.Headers.Add("Accept", "application/json");
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    HttpContent json = response.Content;
                    List<IndexUser> list = JsonConvert.DeserializeObject<List<IndexUser>>(json.ReadAsStringAsync().Result);
                    listView.ItemsSource = list.Select(x => x.IdentityUser.UserName).ToList();
                    usernameEntry.IsVisible = false;
                    passwordEntry.IsVisible = false;
                    UsernameLabel.Text = tokenJson["username"];
                    UsernameLabel.FontSize = 30;
                    UsernameLabel.TextColor = Color.Black;
                    PasswordLabel.IsVisible = false;
                    buttonLogin.IsVisible = false;
                    buttonLogout.IsVisible = true;
                }
            }
            else
            {
                buttonLogout.IsVisible = false;
                ListLabel.Text = "";
            };
        }
        async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://192.168.100.2:3000/Mobile/Token?username=" + usernameEntry.Text + "&password=" + passwordEntry.Text);
            request.Method = HttpMethod.Post;
            request.Headers.Add("Accept", "application/json");
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                HttpContent token = response.Content;
                App.Current.Properties["Token"] = await token.ReadAsStringAsync();
                Dictionary<string, string> responseJson = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)App.Current.Properties["Token"]);
                App.Current.Properties["Username"] = responseJson["username"];
                HttpRequestMessage requestListOfUsers = new HttpRequestMessage();
                requestListOfUsers.RequestUri = new Uri("http://192.168.100.2:3000/Mobile/GetListOfUsers?jwt=" + responseJson["access_token"]);
                requestListOfUsers.Method = HttpMethod.Post;
                requestListOfUsers.Headers.Add("Accept", "application/json");
                HttpResponseMessage responseListOfUsers = client.SendAsync(requestListOfUsers).Result;
                if (responseListOfUsers.StatusCode == HttpStatusCode.OK)
                {
                    HttpContent json = responseListOfUsers.Content;
                    List<IndexUser> list = JsonConvert.DeserializeObject<List<IndexUser>>(json.ReadAsStringAsync().Result);
                    listView.ItemsSource = list.Select(x => x.IdentityUser.UserName).ToList();
                    usernameEntry.IsVisible = false;
                    passwordEntry.IsVisible = false;
                    UsernameLabel.Text = responseJson["username"];
                    UsernameLabel.FontSize = 30;
                    UsernameLabel.TextColor = Color.Black;
                    PasswordLabel.IsVisible = false;
                    buttonLogin.IsVisible = false;
                    buttonLogout.IsVisible = true;
                    ListLabel.Text = "Кому пишем";
                }
            }
            else
            {
                ListLabel.Text = "Что-то пошло не так";
            }
            await App.Current.SavePropertiesAsync();
        }

        async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            App.Current.Properties.Remove("Token");
            App.Current.Properties.Remove("Username");
            await App.Current.SavePropertiesAsync();
            listView.ItemsSource = null;
            usernameEntry.IsVisible = true;
            usernameEntry.Text = "";
            passwordEntry.IsVisible = true;
            passwordEntry.Text = "";
            UsernameLabel.Text = "Username";
            UsernameLabel.FontSize = 20;
            UsernameLabel.TextColor = Color.Black;
            PasswordLabel.IsVisible = true;
            PasswordLabel.FontSize = 20;
            PasswordLabel.TextColor = Color.Black;
            buttonLogin.IsVisible = true;
            buttonLogout.IsVisible = false;
            ListLabel.Text = "";
        } 

        private async void ChatToUser(object sender, ItemTappedEventArgs e)
        {
            App.Current.Properties["toUser"] = e.Item;
            await App.Current.SavePropertiesAsync();
            await Navigation.PushAsync(new Chat());
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (App.Current.Properties.ContainsKey("Token"))
            {
                await viewModel.Connect();
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (App.Current.Properties.ContainsKey("Token"))
            {
                await viewModel.Disconnect();
            }
        }
    }
}
