using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http.Headers;
using Xamarin.Forms.Xaml;
using App1.Models;

namespace App1
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        INotificationManager notificationManager;
        int notificationNumber = 0;
        HubConnection hubConnection;
        private Chat ChatPage;
        public Xamarin.Forms.Page CurrentPage { get; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public ObservableCollection<MessageData> Messages { get; }
        bool isBusy;
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    OnPropertyChanged("IsBusy");
                }
            }
        }
        bool isConnected;
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged("IsConnected");
                }
            }
        }
        public Command SendMessageCommand { get; }

        public ChatViewModel(Chat chat)
        {
            notificationManager = DependencyService.Get<INotificationManager>();
            ChatPage = chat;
            CookieContainer cookies = Cookies();
            hubConnection = new HubConnectionBuilder()
                .WithUrl("http://192.168.100.2:3000/chathub", options => {
                    options.Cookies = cookies;
                })
                .Build();
            Messages = new ObservableCollection<MessageData>();

            IsConnected = false;
            IsBusy = false;

            SendMessageCommand = new Command(async () => await SendMessage(), () => IsConnected);

            hubConnection.Closed += async (error) =>
            {
                SendLocalMessage(String.Empty, "Подключение закрыто...");
                IsConnected = false;
                await Task.Delay(5000);
                await Connect();
            };

            hubConnection.On<string>("ReceiveMessage", (json) =>
            {
                Dictionary<string, string> model = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if ((model["User1Id"] == (string)App.Current.Properties["toUser"] && 
                        model["User2Id"] == (string)App.Current.Properties["Username"]) ||
                    (model["User2Id"] == (string)App.Current.Properties["toUser"] || 
                        model["User1Id"] == (string)App.Current.Properties["Username"]))
                {
                    SendLocalMessage(model["User1Id"], model["Contect"]);
                }
                else
                {
                    AddNotification(model["User1Id"], model["Contect"]);
                }
            });
        }
        public async Task Connect()
        {
            if (IsConnected)
                return;
            try
            {
                await hubConnection.StartAsync();

                IsConnected = true;
            }
            catch (Exception ex)
            {
                SendLocalMessage(String.Empty, $"Ошибка подключения: {ex.Message}");
            }
        }
        public async Task Disconnect()
        {
            if (!IsConnected)
                return;

            await hubConnection.StopAsync();
            IsConnected = false;
            SendLocalMessage(String.Empty, "Вы покинули чат...");
        }
        async Task SendMessage()
        {
            try
            {
                IsBusy = true;
                await hubConnection.InvokeAsync("Xamarin", Message, (string)App.Current.Properties["toUser"], (string)App.Current.Properties["Token"]);
            }
            catch (Exception ex)
            {
                SendLocalMessage(String.Empty, $"Ошибка отправки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void SendLocalMessage(string user, string message)
        {
            if (ChatPage != null)
            {
                Messages.Add(new MessageData
                {
                    Message = message,
                    User = user
                });
                ChatPage.ScrollTo();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private CookieContainer Cookies()
        {
            Dictionary<string, string> responseJson = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)App.Current.Properties["Token"]);
            string token = responseJson["access_token"];
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            HttpClient client = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://192.168.100.2:3000/Mobile/GetIdentityToken?jwt=" + token);
            request.Method = HttpMethod.Post;
            request.Headers.Add("Accept", "application/json");
            HttpResponseMessage response = client.SendAsync(request).Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return cookies;
            }
            return null;
        }
        public void AddNotification(string user, string message)
        {
            notificationNumber++;
            string title = $"U've got new msg from #{user}. +#{notificationNumber}";
            notificationManager.ScheduleNotification(title, message);
        }
    }
}