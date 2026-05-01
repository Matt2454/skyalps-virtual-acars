using FSUIPC;

namespace SkyAlpsACARS;

public enum FlightStatus
{
    OnGround,
    Takeoff,
    Climbing,
    Cruise,
    Descending,
    Landing
}

public class FlightDataEventArgs : EventArgs
{
    public FlightStatus Status { get; init; }
    public double Airspeed { get; init; }
    public double VerticalSpeed { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Altitude { get; init; }
    public bool IsOnGround { get; init; }
    public DateTime Timestamp { get; init; }
}

public class SkyAlpsTracker : IDisposable
{
    private Offset<int> _airspeed = new Offset<int>(0x02BC);
    private Offset<short> _onGround = new Offset<short>(0x0366);
    private Offset<int> _verticalSpeed = new Offset<int>(0x02C8);
    private Offset<long> _playerLatitude = new Offset<long>(0x0560);
    private Offset<long> _playerLongitude = new Offset<long>(0x0568);
    private Offset<int> _altitude = new Offset<int>(0x0574);

    private FlightStatus _currentStatus = FlightStatus.OnGround;
    private bool _previousOnGround = true;
    private DateTime? _takeoffTime;
    private bool _isConnected;
    private bool _disposed;

    public event EventHandler<FlightDataEventArgs>? FlightDataUpdated;
    public event EventHandler? TakeoffDetected;
    public event EventHandler? LandingDetected;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected => _isConnected;
    public FlightStatus CurrentStatus => _currentStatus;

    public bool Connect()
    {
        try
        {
            FSUIPCConnection.Open();
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(this, "Connected to MSFS");
            return true;
        }
        catch (FSUIPCException ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            if (FSUIPCConnection.IsOpen)
            {
                FSUIPCConnection.Close();
            }
        }
        catch { }
        _isConnected = false;
        ConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    public void UpdateData()
    {
        if (!_isConnected || _disposed) return;

        try
        {
            if (!FSUIPCConnection.IsOpen)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, "Simulator disconnected");
                return;
            }

            FSUIPCConnection.Process();

            double speedKnots = (double)_airspeed.Value / 128.0;
            bool isOnGround = _onGround.Value == 1;
            double vsPerMin = _verticalSpeed.Value * -60.0 * 3.28084 / 16384.0;
            double latitude = _playerLatitude.Value * 90.0 / 10000000.0;
            double longitude = _playerLongitude.Value * 360.0 / 10000000.0;
            double altitudeFeet = _altitude.Value / 100.0 * 3.28084;

            _currentStatus = DetermineFlightStatus(isOnGround, vsPerMin, altitudeFeet);

            if (isOnGround && !_previousOnGround)
            {
                LandingDetected?.Invoke(this, EventArgs.Empty);
                _currentStatus = FlightStatus.Landing;
                _takeoffTime = null;
            }
            else if (!isOnGround && _previousOnGround)
            {
                TakeoffDetected?.Invoke(this, EventArgs.Empty);
                _takeoffTime = DateTime.UtcNow;
            }

            _previousOnGround = isOnGround;

            FlightDataUpdated?.Invoke(this, new FlightDataEventArgs
            {
                Status = _currentStatus,
                Airspeed = speedKnots,
                VerticalSpeed = vsPerMin,
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitudeFeet,
                IsOnGround = isOnGround,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (FSUIPCException ex)
        {
            if (ex.FSUIPCErrorCode == FSUIPCError.FSUIPC_ERR_SENDMSG || 
                ex.FSUIPCErrorCode == FSUIPCError.FSUIPC_ERR_NOFS)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, "Simulator disconnected");
            }
        }
    }

    private FlightStatus DetermineFlightStatus(bool isOnGround, double vsPerMin, double altitudeFeet)
    {
        if (isOnGround)
            return FlightStatus.OnGround;

        if (altitudeFeet < 1000)
        {
            if (vsPerMin > 500)
                return FlightStatus.Climbing;
            return FlightStatus.Takeoff;
        }

        if (altitudeFeet > 25000)
            return FlightStatus.Cruise;

        if (vsPerMin < -500)
            return FlightStatus.Descending;

        if (vsPerMin < 100 && vsPerMin > -100)
            return FlightStatus.Cruise;

        return _currentStatus;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        GC.SuppressFinalize(this);
    }
}