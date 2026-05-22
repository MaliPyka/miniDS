using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace miniDSClient
{
    public partial class LoginWindow : Window
    {
        public string Username { get; private set; } = "";
        public string Token { get; private set; } = "";

        private HttpClient _http = new HttpClient();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            await DoRequest("http://localhost:8000/auth/login");
        }

        private async void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            await DoRequest("http://localhost:8000/auth/register");
        }

        private async System.Threading.Tasks.Task DoRequest(string url)
        {
            ErrorText.Text = "";
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Заполните все поля";
                return;
            }

            try
            {
                var body = JsonSerializer.Serialize(new { username, password });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ErrorText.Text = "Ошибка: " + json;
                    return;
                }

                var doc = JsonDocument.Parse(json);
                Token = doc.RootElement.GetProperty("access_token").GetString()!;
                Username = username;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка подключения: " + ex.Message;
            }
        }
    }
}