using Livekit.Server.Sdk.Dotnet;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace miniDSClient
{
    public class ChannelItem
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public override string ToString() => Name;
    }

    public class VoiceParticipant
    {
        public string Name { get; set; }
        public string Letter { get; set; }
    }

    public partial class MainWindow : Window
    {
        private ClientWebSocket _websocket = new ClientWebSocket();
        private string _username = "string";
        private bool _muted = false;
        private string _token = "";
        private int _channelId = 1;
        private HttpClient _httpClient = new HttpClient();
        private bool _isLoadingChannels = false;
        private Room _voiceRoom = new Room();
        private bool _inVoice = false;


        // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJweXBhIiwiZXhwIjoxNzc5NTM0OTQ0fQ.7h4NpVeSkX8Nq_lzekiWNsVWFreyMmjosxovE001txM pypa
        // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzdHJpbmciLCJleHAiOjE3Nzk0NjU3ODN9.B9QPJboyZUH6jON2NrYFAd5gswkpiCbGDXGF8PAb8T8 string
        public MainWindow()
        {
            InitializeComponent();

            var login = new LoginWindow();
            if (login.ShowDialog() == true)
            {
                _username = login.Username;
                _token = login.Token;
                StartAsync();
            }
            else
            {
                Close();
            }
        }

        private async void StartAsync()
        {
            await LoadChannels();
            if (ChannelsList.Items.Count > 0)
            {
                ChannelsList.SelectedIndex = 0;
            }
            CurrentUsername.Text = _username;
            AvatarLetter.Text = _username[0].ToString().ToUpper();

            var env = await CoreWebView2Environment.CreateAsync(null, null,
                new CoreWebView2EnvironmentOptions(
                    "--unsafely-treat-insecure-origin-as-secure=http://localfiles --allow-running-insecure-content"
                ));
            await VoiceWebView.EnsureCoreWebView2Async(env);

            VoiceWebView.CoreWebView2.PermissionRequested += (s, e) =>
            {
                e.State = CoreWebView2PermissionState.Allow;
            };

            VoiceWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "localfiles",
                AppDomain.CurrentDomain.BaseDirectory,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );

            VoiceWebView.Source = new Uri("http://localfiles/voice.html");

            VoiceWebView.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                var message = e.TryGetWebMessageAsString();
                System.Diagnostics.Debug.WriteLine("VOICE MSG: " + message);

                if (message.StartsWith("ERROR:"))
                {
                    Dispatcher.Invoke(() => MessageBox.Show("Ошибка голоса: " + message.Replace("ERROR:", "")));
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    if (message == "CONNECTED")
                    {
                        VoiceButton.Content = "🔴";
                    }
                    else if (message == "DISCONNECTED")
                    {
                        VoiceButton.Content = "📞";
                    }
                    else if (message.StartsWith("JOINED:") || message.StartsWith("ALREADY:") || message.StartsWith("EVENT:"))
                    {
                        var name = message.StartsWith("JOINED:") ? message.Replace("JOINED:", "") :
                                   message.StartsWith("ALREADY:") ? message.Replace("ALREADY:", "") : null;

                        if (name != null)
                        {
                            if (VoiceParticipantsList.Items.Cast<VoiceParticipant>().Any(p => p.Name == name))
                                return;
                            VoiceParticipantsList.Items.Add(new VoiceParticipant
                            {
                                Name = name,
                                Letter = name[0].ToString().ToUpper()
                            });
                        }
                    }
                    else if (message.StartsWith("LEFT:"))
                    {
                        var name = message.Replace("LEFT:", "");
                        var item = VoiceParticipantsList.Items.Cast<VoiceParticipant>()
                            .FirstOrDefault(p => p.Name == name);
                        if (item != null)
                            VoiceParticipantsList.Items.Remove(item);
                    }
                });
            };
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
                    var buffer = new byte[4096];
                    var result = await _websocket.ReceiveAsync(buffer, CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message.StartsWith("MEMBERS:"))
                    {
                        var members = message.Replace("MEMBERS:", "").Split(',');
                        Dispatcher.Invoke(() =>
                        {
                            MembersCount.Text = $"{members.Length} members";
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() => MessagesList.Items.Add(message));
                    }
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
            ChannelTitle.Text = selected.Name;

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


        private void ChannelItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = (ListBoxItem)sender;
            var grid = FindVisualChild<Grid>(item);
            if (grid == null) return;
            foreach (var btn in grid.Children.OfType<Button>())
                btn.Opacity = 1;
        }

        private void ChannelItem_MouseLeave(object sender, MouseEventArgs e)
        {
            var item = (ListBoxItem)sender;
            var grid = FindVisualChild<Grid>(item);
            if (grid == null) return;
            foreach (var btn in grid.Children.OfType<Button>())
                btn.Opacity = 0;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private async void VoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_inVoice)
            {
                _inVoice = true;
                VoicePanelRow.Height = new GridLength(120);
                VoiceParticipantsList.Items.Clear();
                VoiceParticipantsList.Items.Add(new VoiceParticipant
                {
                    Name = _username,
                    Letter = _username[0].ToString().ToUpper()
                });

                var response = await _httpClient.GetAsync($"http://localhost:8000/voice/token/{_channelId}/{_username}");
                var json = await response.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var token = doc.RootElement.GetProperty("token").GetString();

                await VoiceWebView.CoreWebView2.ExecuteScriptAsync(
                    $"joinVoice('ws://localhost:7880', '{token}')");
            }
            else
            {
                _inVoice = false;
                VoicePanelRow.Height = new GridLength(0);
                VoiceParticipantsList.Items.Clear();
                await VoiceWebView.CoreWebView2.ExecuteScriptAsync("leaveVoice()");
                VoiceButton.Content = "📞";
            }
        }

        private async void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            _muted = !_muted;
            MuteButton.Content = _muted ? "🔇 Размут" : "🎤 Мут";
            await VoiceWebView.CoreWebView2.ExecuteScriptAsync(
                $"room.localParticipant.setMicrophoneEnabled({(_muted ? "false" : "true")})");
        }
    }
}