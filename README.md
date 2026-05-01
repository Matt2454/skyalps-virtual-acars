# SkyAlps Virtual ACARS

ACARS (Aircraft Communications Addressing and Reporting System) software for SkyAlps Virtual airline.

## Features

- **Simulator Connection**: Connects to Microsoft Flight Simulator via FSUIPC
- **Flight Data Tracking**: Real-time tracking of airspeed, altitude, vertical speed, latitude, and longitude
- **Flight Status Detection**: Automatic detection of takeoff and landing events
- **Portal Login**: Authentication via SkyAlps Virtual Supabase backend
- **Single File Deployment**: Self-contained executable for easy distribution

## Requirements

- Microsoft Flight Simulator (MSFS 2024) with FSUIPC installed and registered
- .NET 9.0 Runtime (included in self-contained build)

## Configuration

Before running the application, edit `MainWindow.xaml.cs` and replace the placeholder values:

```csharp
private const string SUPABASE_URL = "YOUR_SUPABASE_URL";
private const string SUPABASE_KEY = "YOUR_SUPABASE_ANON_KEY";
```

Replace with your actual Supabase project URL and anon key.

## Building

```bash
cd SkyAlpsACARS
dotnet build
```

## Publishing (Single File)

```bash
dotnet publish -c Release
```

The executable will be created in `bin/Release/net9.0-windows/win-x64/publish/`

## Project Structure

- `SkyAlpsTracker.cs` - FSUIPC data reading and flight status detection
- `SupabaseClient.cs` - Supabase authentication singleton
- `MainWindow.xaml` - WPF user interface
- `MainWindow.xaml.cs` - UI logic and event handlers

## FSUIPC Offsets Used

| Offset | Description |
|--------|-------------|
| 0x02BC | Airspeed (knots) |
| 0x0366 | On Ground status |
| 0x02C8 | Vertical Speed |
| 0x0560 | Player Latitude |
| 0x0568 | Player Longitude |
| 0x0574 | Altitude |

## License
