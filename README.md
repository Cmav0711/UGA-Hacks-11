# test-uga-hacks11

## Bright Light Color Detection Application

A C# application using OpenCV and SixLabors.ImageSharp that detects bright lights in real-time from a camera feed or processes static images.

### Features
- **Dual-Color Tracking Mode**: Choose two different colors to track with different export behaviors
  - Primary color: Normal tracking with PNG + CSV export on color switch
  - Secondary color: CSV-only export on switch back
- **Color Selection**: Choose specific light colors to track (Red, Green, Blue, Yellow, Cyan, Magenta, White, or Any)
- **Color-Based Detection**: Uses HSV color space for accurate color matching of light sources
- **Real-time Camera Tracking**: Tracks lights matching the selected color in each camera frame with calibration support
- **Automatic Export on Color Switch**: Saves appropriate files when switching between colors
- **Automatic PNG Export**: Saves a PNG image of the drawn path when light is not detected for a configurable timeout
- **Circle-based Point Filtering**: Only tracks points within a configurable radius of the last tracked point, filtering out erratic movements
- **Adjustable Tracking Radius**: Change the tracking circle size using '+' and '-' keys (default: 100px, range: 10-500px)
- **Adjustable No-Light Timeout**: Change the timeout for automatic PNG export using '[' and ']' keys (default: 3.0s, range: 0.5-30.0s)
- **Color Detection and Analysis**: Captures and displays RGB color values of bright points
- **Calibration Mode**: Establish baseline color to detect color changes when light passes through objects
- **Path Visualization**: Displays accumulated trail with lines connecting consecutive points
- **Static Image Processing**: Detects bright light sources using HSV color analysis
- Creates detection overlay with bounding boxes, center points, and connecting lines
- Generates point map visualization with connected paths
- Provides detailed statistics for each detected light

### Quick Start

#### Real-time Camera Mode (Default)
```bash
cd ColorDetectionApp
dotnet run
```

At startup, you'll be prompted to select TWO colors for dual-color tracking:
1. **PRIMARY color** - First selection for normal tracking (PNG + CSV export on switch)
2. **SECONDARY color** - Second selection for CSV-only tracking

**Available colors:**
- **1. Any bright light** - Tracks any bright light (original behavior)
- **2. Red** - Tracks red lights only
- **3. Green** - Tracks green lights only
- **4. Blue** - Tracks blue lights only
- **5. Yellow** - Tracks yellow lights only
- **6. Cyan** - Tracks cyan lights only
- **7. Magenta** - Tracks magenta lights only
- **8. White** - Tracks white lights only

After selecting both colors, the application will track lights and detect color switches:
- When switching from **primary to secondary**: Exports PNG + CSV
- When switching from **secondary to primary**: Exports CSV only
- The current brightest point is shown as a large green circle (if within tracking radius)
- All previously tracked points are shown as smaller cyan dots
- Orange lines connect consecutive points to show the path
- A purple circle shows the tracking radius around the last tracked point
- RGB color values and active color are displayed on screen
- Press 'b' to calibrate and capture baseline color for comparison
- Press '+' to increase tracking radius, '-' to decrease (default: 100px)
- Press '[' to decrease no-light timeout, ']' to increase (default: 3.0s)
- Press 'q' to quit
- Press 'c' to clear the tracked points
- When light is not detected for the configured timeout, a PNG image is automatically saved

#### Static Image Mode
```bash
cd ColorDetectionApp
dotnet run <image_path>
```

This will process a static image and generate output files showing detected bright lights.

### Documentation

See [ColorDetectionApp/README.md](ColorDetectionApp/README.md) for detailed usage instructions and technical documentation.

### Sample Output

#### Real-time Camera Mode
- Displays live camera feed with overlay
- Current brightest point highlighted in green (if within tracking radius) or red (if filtered out)
- Trail of all brightest points from previous frames in cyan
- Orange lines connecting consecutive points
- Purple circle showing the tracking radius around the last tracked point
- RGB color information displayed on screen
- Color difference from baseline (when calibrated)
- Frame counter showing number of points tracked, current tracking radius, and no-light timeout
- Calibration status indicator
- Automatic PNG export when light is not detected for the configured timeout

#### Static Image Mode
The application detects bright lights and provides:
- Position coordinates (x, y)
- Area in pixels
- Bounding box dimensions
- Visual overlays with detection markers and connecting lines
- Point map for spatial analysis with path visualization