using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace http
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            InitializeWebView();
            SetFullScreen();
        }

        // ====== HÀM SỰ KIỆN ======

        private async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToUrl();
        }

        private void AddressBar_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Enter để gọi hàm navigate
            {
                _ = NavigateToUrl();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoBack) WebView.GoBack(); // Kiểm tra có quay lại đc ko
            else MessageBox.Show("No previous page in history.", "Back", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            if (WebView.CanGoForward) WebView.GoForward(); // Kiểm tra có điều hướng trang tiếp theo đc ko
            else MessageBox.Show("No next page in history.", "Forward", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            WebView.Reload();
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            const string homeUrl = "https://truonggiangphotocopy.github.io/home/"; // https://www.google.com
            UrlTextBox.Text = homeUrl;
            WebView.Source = new Uri(homeUrl);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Khi WebView hoàn tất việc tải trang, cập nhật thanh địa chỉ với URL hiện tại.
            if (WebView.Source != null) UrlTextBox.Text = WebView.Source.ToString();
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, EventArgs e)
        {
            WebView.CoreWebView2.Navigate("https://jetbrains.com"); // https://www.google.com // https://microsoft.com
            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ngăn chặn các phím tắt Ctrl + Key
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                (e.Key == Key.C || e.Key == Key.V || e.Key == Key.X || e.Key == Key.Z || e.Key == Key.A))
            {
                e.Handled = true;
            }
        }

        // ====== HÀM XỬ LÝ ======

        private async Task NavigateToUrl()
        {
            string url = UrlTextBox.Text;
            // Kiểm tra xem URL hợp lệ
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                try
                {
                    // Gửi yêu cầu HTTP GET và lấy nội dung header
                    var (_, headers) = await FetchHttpContentAsync(url);
                    DisplayHeaders(headers);
                    WebView.Source = uri;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Địa chỉ URL không hợp lệ!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<(string Content, IEnumerable<KeyValuePair<string, string>> Headers)> FetchHttpContentAsync(string url)
        {
            // Gửi yêu cầu HTTP GET đến URL và Thực hiện yêu cầu HTTP bất đồng bộ
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode(); // Nhận phản hồi từ server và lưu trữ nội dung phản hồi

            // Đọc toàn bộ file HTML dưới dạng string // Nội dung file HTML
            string content = await response.Content.ReadAsStringAsync(); 
            
            // Lấy và gộp header chính với header nội dung
            // // Chuyển đổi từng header thành một cặp Key-Value (JS    ON) dưới dạng string.
            var headers = response.Headers
                .Concat(response.Content.Headers)
                .Select(header => new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value)));

            return (content, headers);
        }

        private void DisplayHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            // Hiển thị nội dung header đã lấy đc
            HeadersListView.Items.Clear();
            foreach (var header in headers)
            {
                HeadersListView.Items.Add(new { header.Key, header.Value });
            }
        }

        private async void InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async(null); // Đảm bảo rằng WebView2 đã được khởi tạo

            var settings = WebView.CoreWebView2.Settings; // Truy cập vào cài đặt của WebView2
            settings.IsScriptEnabled = true; // Cho phép thực thi JavaScript - TranScript
            settings.IsWebMessageEnabled = true; // Cho phép giao tiếp hai chiều giữa ứng dụng và trang web thông qua tin nhắn web.

            // WebView.CoreWebView2InitializationCompleted += (s, e) =>
            // {
            //     WebView.CoreWebView2.WebResourceRequested += (sender, args) =>
            //     {
            //         
            //     };
            // };

            WebView.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true; // Chặn Right Mouse
            WebView.PreviewMouseMove += (s, e) => e.Handled = true; // Chặn Move Mouse
            WebView.PreviewMouseWheel += (s, e) => e.Handled = true; // Chặn Scroll Mouse

            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void SetFullScreen()
        {
            WindowStyle = WindowStyle.None; // Loại bỏ titlebar.
            WindowState = WindowState.Maximized;
            Topmost = true; // Chặn Alt + Tab // Giữ cửa sổ luôn ở trên cùng, không bị che khuất bởi các cửa sổ khác.
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            // Chặn phím tắt Ctrl + Key và F12 // keydown
            // Chặn Right Mouse // contextmenu
            const string script = @"
                document.addEventListener('keydown', function(event) {
                    if (event.ctrlKey && ['c', 'v', 'x', 'u'].includes(event.key)) {
                        event.preventDefault();
                    }
                    if (event.key === 'F12') {
                        event.preventDefault();
                    }
                });
                document.addEventListener('contextmenu', function(event) {
                    event.preventDefault();
                });
            ";
            WebView.CoreWebView2.ExecuteScriptAsync(script); // Thực thi script trong trang web đang hiển thị
        }
    }
}
