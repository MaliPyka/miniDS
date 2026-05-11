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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartAsync();
        }


        private async void StartAsync()
        {
            await _websocket.ConnectAsync(
                new Uri("ws://localhost:8000/ws/pedic"),
                CancellationToken.None
            );
            await ReceiveMessages();
        }

        private ClientWebSocket _websocket = new ClientWebSocket();

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


        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendButton_Click(sender, e);
        }



    }
}