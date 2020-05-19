using App1.Models;
using Java.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App1
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Chat : ContentPage
    {
        ChatViewModel viewModel;
        public Chat()
        {
            Title = "Я - "+ (string)App.Current.Properties["Username"] + " для " + (string)App.Current.Properties["toUser"];
            InitializeComponent();
            viewModel = new ChatViewModel(this);
            this.BindingContext = viewModel;

            Dictionary<string, string> responseJson = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)App.Current.Properties["Token"]);
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://192.168.100.2:3000/Mobile/GetListOfMessagges?jwt=" + responseJson["access_token"] + "&toUser=" + (string)App.Current.Properties["toUser"]);
            request.Method = HttpMethod.Post;
            request.Headers.Add("Accept", "application/json");
            HttpResponseMessage response = client.SendAsync(request).Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                HttpContent jsonRespone = response.Content;
                Dictionary<string,object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonRespone.ReadAsStringAsync().Result);
                JArray a = (JArray)json["messages"];
                foreach (JObject b in a)
                {
                    var z = b.ToObject<Dictionary<string,string>>();
                    MessageData message = new MessageData
                    {
                        Message = z["contect"],
                        User = z["user1Id"]
                    };
                    viewModel.Messages.Add(message);
                };
                var v = MessagesList.ItemsSource.Cast<object>().LastOrDefault();
                MessagesList.ScrollTo(v, ScrollToPosition.End, true);
            };
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.Connect();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel = null;
            await Navigation.PopAsync();
            App.Current.Properties["toUser"] = "";
        }
        public void ScrollTo()
        {
            var v = MessagesList.ItemsSource.Cast<object>().LastOrDefault();
            MessagesList.ScrollTo(v, ScrollToPosition.End, true);
        }
    }
}