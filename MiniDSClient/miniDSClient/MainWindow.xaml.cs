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

namespace miniDSClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
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
        private string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJweXBhIiwiZXhwIjoxNzc4NjY5ODk0fQ.FWABasagKUN8RAfGO1q_yYMZZIjdAzQJK5rEMNa5gy0";
        private int _channelId = 1;
        private HttpClient _httpClient = new HttpClient();

        private async Task ReceiveMessages()
        {
            while (true)
            {
                var buffer = new byte[1024];
                var result = await _websocket.ReceiveAsync(buffer, CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessagesList.Items.Add(message);
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

        
    }
}