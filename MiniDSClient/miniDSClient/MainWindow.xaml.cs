using System.Net.Http;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        public MainWindow()
        {
            InitializeComponent();
            StartAsync();
        }


        private async void StartAsync()
        {
            await LoadChannels();
            await _websocket.ConnectAsync(
                new Uri($"ws://localhost:8000/ws/{_token}/{_channelId}"),
                CancellationToken.None
            );
            await ReceiveMessages();


        }

        private ClientWebSocket _websocket = new ClientWebSocket();
        private string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJweXBhIiwiZXhwIjoxNzc4OTQzMTkwfQ.W4DydOTPGClktZbILUT855ettrwXLnQ_fIqkxOKN4qo";
        private int _channelId = 1;
        private HttpClient _httpClient = new HttpClient();

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
            catch
            {

            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var bytes = Encoding.UTF8.GetBytes(MessageInput.Text);
            await _websocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            MessageInput.Text = "";
        }

        private async Task LoadChannels()
        {
            var response = await _httpClient.GetStringAsync("http://localhost:8000/rooms/channels/1");
            var channels = JsonDocument.Parse(response).RootElement;

            foreach (var channel in channels.EnumerateArray())
            {
                var name = channel.GetProperty("name").GetString();
                var id = channel.GetProperty("id").GetInt32();
                ChannelsList.Items.Add(new ChannelItem{ Name = name, Id = id });
            }
        }


        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendButton_Click(sender, e);
        }


        private async void ChannelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelsList.SelectedItem is ChannelItem selected)
            {
                _channelId = selected.Id;

                try
                {
                    if (_websocket.State == WebSocketState.Open)
                    {
                        await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "switching channel", CancellationToken.None);
                    }
                }
                catch { }

                _websocket = new ClientWebSocket();
                MessagesList.Items.Clear();

                await _websocket.ConnectAsync(
                    new Uri($"ws://localhost:8000/ws/{_token}/{_channelId}"),
                    CancellationToken.None
                );

                _ = ReceiveMessages();
            }
        }

        private async void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите название канала", "Новый канал", "");
            if (string.IsNullOrEmpty(name)) return;

            var json = JsonSerializer.Serialize(new { name, server_id = 1 });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:8000/rooms/create_channel", content);

            ChannelsList.Items.Clear();
            await LoadChannels();
        }


        private async void DeleteChannelButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button == null) return;

            var clickedChannel = button.DataContext as ChannelItem;

            if (clickedChannel != null)
            {
                int index = clickedChannel.Id;
                var response = await _httpClient.DeleteAsync($"http://localhost:8000/rooms/delete_channels/{index}");
                ChannelsList.Items.Clear();
                await LoadChannels();
            }
        }
    }
}