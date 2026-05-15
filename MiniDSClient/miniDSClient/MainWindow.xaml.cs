using System.Net.Http;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic;

namespace miniDSClient
{
    public class ChannelItem
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public override string ToString() => Name;
    }

    public partial class MainWindow : Window
    {
        private ClientWebSocket _websocket = new ClientWebSocket();
        private string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJweXBhIiwiZXhwIjoxNzc4OTYxNTU3fQ.3nqyIku3shFh1DZ09HsPVgoQwzN9qvfpHHK8W-AJT-Y";
        private int _channelId = 1;
        private HttpClient _httpClient = new HttpClient();
        private bool _isLoadingChannels = false;

        // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJweXBhIiwiZXhwIjoxNzc4OTYxNTU3fQ.3nqyIku3shFh1DZ09HsPVgoQwzN9qvfpHHK8W-AJT-Y pypa
        // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzdHJpbmciLCJleHAiOjE3Nzg5NTExNTZ9.J2lGzFvZvXP6HPJ5BjJb6t4_0fvECBEwff4A0eSPtCU string
        public MainWindow()
        {
            InitializeComponent();
            StartAsync();
        }

        private async void StartAsync()
        {
            await LoadChannels();
            if (ChannelsList.Items.Count > 0)
            {
                ChannelsList.SelectedIndex = 0;
            }
        }

        private async Task LoadChannels()
        {
            _isLoadingChannels = true;
            try
            {
                var response = await _httpClient.GetStringAsync($"http://localhost:8000/rooms/channels/1/{_token}");
                var channels = JsonDocument.Parse(response).RootElement;
                ChannelsList.Items.Clear();
                foreach (var channel in channels.EnumerateArray())
                {
                    var name = channel.GetProperty("name").GetString();
                    var id = channel.GetProperty("id").GetInt32();
                    ChannelsList.Items.Add(new ChannelItem { Name = name, Id = id });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каналов: {ex.Message}");
            }
            finally
            {
                _isLoadingChannels = false;
            }
        }

        private async Task ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    var buffer = new byte[1024];
                    var result = await _websocket.ReceiveAsync(buffer, CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessagesList.Items.Add(message);
                }
            }
            catch { }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MessageInput.Text)) return;
            var bytes = Encoding.UTF8.GetBytes(MessageInput.Text);
            await _websocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            MessageInput.Text = "";
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendButton_Click(sender, e);
        }

        private bool _isConnecting = false;

        private async void ChannelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingChannels) return;
            if (_isConnecting) return;
            if (ChannelsList.SelectedItem is not ChannelItem selected) return;

            _isConnecting = true;
            _channelId = selected.Id;

            try
            {
                if (_websocket.State == WebSocketState.Open)
                    await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "switching channel", CancellationToken.None);
            }
            catch { }

            _websocket = new ClientWebSocket();
            MessagesList.Items.Clear();

            await _websocket.ConnectAsync(
                new Uri($"ws://localhost:8000/ws/{_token}/{_channelId}"),
                CancellationToken.None
            );

            _isConnecting = false;
            _ = ReceiveMessages();
        }

        private async void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите название канала", "Новый канал", "");
            if (string.IsNullOrEmpty(name)) return;

            var json = JsonSerializer.Serialize(new { name, server_id = 1, token = _token });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("http://localhost:8000/rooms/create_channel", content);
            await LoadChannels();
        }

        private async void DeleteChannelButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button == null) return;
            var clickedChannel = button.DataContext as ChannelItem;
            if (clickedChannel == null) return;

            await _httpClient.DeleteAsync($"http://localhost:8000/rooms/delete_channels/{clickedChannel.Id}");
            await LoadChannels();
        }


        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var clickedChannel = button.DataContext as ChannelItem;
            if (clickedChannel == null) return;

            string name = Interaction.InputBox("Введите имя пользователя", "Добавить пользователя", "");
            if (string.IsNullOrEmpty(name)) return;

            var json = JsonSerializer.Serialize(new { username = name, channel_id = clickedChannel.Id });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("http://localhost:8000/rooms/add_member", content);
        }
    }
}