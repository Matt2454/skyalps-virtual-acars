using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using FSUIPC;

namespace SkyAlpsACARS;

public partial class MainWindow : Window
{
    private SkyAlpsTracker? _tracker;
    private DispatcherTimer? _updateTimer;
    private bool _isLoggedIn;
    public bool IsLoggedIn => _isLoggedIn;

    private const string SUPABASE_URL = "https://rqcyqgyhemxcpxsmidmo.supabase.co";
    private const string SUPABASE_KEY = "sb_publishable_6Midoeb4_59O3jJKDXWELg_L2BKGLjb";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await SupabaseClient.InitializeAsync(SUPABASE_URL, SUPABASE_KEY);
            txtLoginStatus.Text = "Supabase connected. Please login.";
            txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
        }
        catch (Exception ex)
        {
            txtLoginStatus.Text = $"Supabase error: {ex.Message}";
            txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        }

        _tracker = new SkyAlpsTracker();
        _tracker.FlightDataUpdated += Tracker_FlightDataUpdated;
        _tracker.ConnectionStatusChanged += Tracker_ConnectionStatusChanged;
        _tracker.TakeoffDetected += Tracker_TakeoffDetected;
        _tracker.LandingDetected += Tracker_LandingDetected;

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _updateTimer?.Stop();
        _tracker?.Dispose();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        _tracker?.UpdateData();
    }

    private void Tracker_FlightDataUpdated(object? sender, FlightDataEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtAirspeed.Text = e.Airspeed.ToString("F0");
            txtAltitude.Text = e.Altitude.ToString("F0");
            txtVS.Text = e.VerticalSpeed.ToString("F0");
            txtLatitude.Text = e.Latitude.ToString("F6");
            txtLongitude.Text = e.Longitude.ToString("F6");
            txtTimestamp.Text = e.Timestamp.ToString("HH:mm:ss UTC");

            txtStatus.Text = e.Status.ToString().ToUpper();

            switch (e.Status)
            {
                case FlightStatus.OnGround:
                case FlightStatus.Landing:
                    txtStatus.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case FlightStatus.Takeoff:
                case FlightStatus.Climbing:
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                    break;
                case FlightStatus.Cruise:
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A90D9"));
                    break;
                case FlightStatus.Descending:
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                    break;
            }
        });
    }

    private void Tracker_ConnectionStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() =>
        {
            txtConnectionStatus.Text = status;
            txtFooter.Text = status;

            if (status.Contains("Connected"))
            {
                txtConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else if (status.Contains("disconnected") || status.Contains("Not connected"))
            {
                txtConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                _updateTimer?.Stop();
            }
            else
            {
                txtConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
            }
        });
    }

    private void Tracker_TakeoffDetected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtFooter.Text = $"Takeoff detected at {DateTime.UtcNow:HH:mm:ss} UTC";
        });
    }

    private void Tracker_LandingDetected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtFooter.Text = $"Landing detected at {DateTime.UtcNow:HH:mm:ss} UTC";
        });
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_tracker?.IsConnected == true)
        {
            _tracker.Disconnect();
            _updateTimer?.Stop();
            btnConnect.Content = "Connect to MSFS";
            return;
        }

        btnConnect.IsEnabled = false;
        btnConnect.Content = "Connecting...";

        bool success = _tracker!.Connect();

        btnConnect.IsEnabled = true;
        btnConnect.Content = success ? "Disconnect" : "Connect to MSFS";

        if (success)
        {
            _updateTimer?.Start();
        }
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string email = txtEmail.Text.Trim();
        string password = txtPassword.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            txtLoginStatus.Text = "Please enter email and password";
            txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            return;
        }

        btnLogin.IsEnabled = false;
        btnLogin.Content = "Logging in...";

        var (success, error) = await SupabaseClient.LoginAsync(email, password);

        btnLogin.IsEnabled = true;
        btnLogin.Content = "Login to SkyAlps";

        if (success)
        {
            _isLoggedIn = true;
            btnLogin.IsEnabled = false;
            btnLogout.IsEnabled = true;
            txtLoginStatus.Text = $"Logged in as {email}";
            txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
        }
        else
        {
            txtLoginStatus.Text = $"Login failed: {error}";
            txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        }
    }

    private async void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        await SupabaseClient.LogoutAsync();
        _isLoggedIn = false;
        btnLogin.IsEnabled = true;
        btnLogout.IsEnabled = false;
        txtLoginStatus.Text = "Logged out successfully";
        txtLoginStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
    }
}