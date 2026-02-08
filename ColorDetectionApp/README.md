# Bright Light Color Detection Application

A C# console application that uses real-time camera tracking or image processing to detect bright lights and creates image maps based on the detected points.

## Features

### Real-time Camera Mode (New!)
- **Color Selection at Startup**: Choose which color of light to track (Red, Green, Blue, Yellow, Cyan, Magenta, White, or Any bright light)
- **HSV Color-Based Detection**: Uses HSV color space for accurate color matching of light sources
- **Live Color-Specific Tracking**: Automatically finds and tracks lights matching the selected color in each camera frame
- **Circular Screenshot Capture**: Define a circular region in the center of the screen and capture only that area
  - Visual indicator showing the capture region with a cyan circle overlay
  - Adjustable circle size using 'o' and 'i' keys (20-500px, default: 150px)
  - Press 's' to capture a circular screenshot that only includes the area within the circle
  - Areas outside the circle are made transparent in the saved image
  - **Automatic Symbol Detection**: Each captured circular screenshot is automatically analyzed for symbols
- **Symbol Recognition**: Captures are automatically compared against template symbols
  - Template symbols stored in the `symbols/` directory
  - Uses OpenCV template matching to detect symbols in captured images
  - Results recorded in `detected_symbols.csv` with timestamp, image filename, detected symbols, and confidence scores
  - Generate example symbol templates with `dotnet run --generate-symbols`
- **Automatic PNG Export**: When light is not detected for a configurable timeout (default: 3.0s), the drawn path is automatically saved as a PNG file
  - **Symbol Detection on Export**: Each exported PNG is automatically analyzed for symbol matches
  - Results saved to `detected_symbols.csv` with detailed information
- **Adjustable No-Light Timeout**: Use '[' and ']' keys to change the timeout for automatic PNG export (0.5-30.0s, default: 3.0s)
- **Statistical Outlier Detection (NEW!)**: Automatically removes extreme outliers from tracked points before exporting
  - Uses robust statistical methods: IQR (Interquartile Range) and Modified Z-score with MAD (Median Absolute Deviation)
  - Hybrid approach combining multiple algorithms for optimal accuracy
  - Toggle on/off with 'x' key (enabled by default)
  - Displays outlier removal statistics when exporting
  - Ensures clean, accurate drawings by filtering out erratic jumps and noise
- **Circle-based Filtering**: Only accepts points within a configurable radius of the last tracked point, preventing erratic jumps
- **Adjustable Tracking Radius**: Use '+' and '-' keys to change the tracking circle size (10-500px, default: 100px)
- **Visual Tracking Circle**: Purple circle overlay shows the current tracking area around the last tracked point
- **Calibration Mode**: Press 'b' to calibrate and capture the baseline color of the brightest point
- **Color Detection**: Displays the RGB color values of the brightest point in real-time
- **Color Difference Analysis**: When calibrated, shows the color difference from the baseline (useful for detecting color changes when light passes through colored objects/boxes)
- **Accumulated Trail Visualization**: Displays all brightest points from previous frames as a trail
- **Connected Path Lines**: Lines connect consecutive tracked points to show the path of movement
- **Interactive Controls**: 
  - Press 'q' to quit
  - Press 'c' to clear the tracked points trail
  - Press 'b' to calibrate with the current brightest point's color
  - Press '+' to increase tracking radius by 10px
  - Press '-' to decrease tracking radius by 10px
  - Press ']' to increase no-light timeout by 0.5s
  - Press '[' to decrease no-light timeout by 0.5s
  - Press 'o' to increase capture circle size by 10px
  - Press 'i' to decrease capture circle size by 10px
  - Press 's' to take a circular screenshot
  - Press 'x' to toggle outlier detection on/off
  - Press 'f' to flip/mirror the camera display
- **Real-time Overlay**: Shows current camera feed with overlaid tracking information
- **Frame Statistics**: Displays count of tracked points, tracking radius, no-light timeout, capture circle radius, and calibration status

### Static Image Mode
- **Bright Light Detection**: Automatically identifies bright light sources in images
- **Color Analysis**: Uses HSV-based color detection to find high-brightness, low-saturation regions
- **Connected Path Visualization**: Lines connect consecutive detected points to show spatial relationships
- **Image Map Generation**: Creates two types of output:
  - **Detection Overlay**: Original image with bounding boxes (red), center points (blue), and connecting lines (green)
  - **Point Map**: Black background with detected light positions marked (cyan) and connecting lines (yellow)
- **Detailed Analysis**: Provides position, area, and bounding box information for each detected light

## Technical Details

- Built with .NET 8.0
- Uses OpenCvSharp4 for real-time camera capture and processing
- Uses SixLabors.ImageSharp for static image processing
- Pure managed code with native OpenCV bindings
- Real-time tracking algorithm:
  - Converts each frame to grayscale
  - Finds pixel with maximum brightness value
  - Tracks location if brightness >= 200 (0-255 scale)
  - Accumulates all points across frames
- Static detection algorithm:
  - Brightness threshold: 200 (0-255 scale)
  - Saturation threshold: < 100 (to detect white/bright lights)
  - Minimum region size: 50 pixels (to filter noise)
  - Uses flood-fill algorithm for region detection
- Symbol detection algorithm:
  - Uses OpenCV template matching with CCoeffNormed method
  - Compares captured images against templates in `symbols/` directory
  - Confidence threshold: 0.7 (70% match required)
  - Results recorded in `detected_symbols.csv`

## Usage

### Generate Symbol Templates (First Time Setup)

Before using symbol detection, generate example templates:

```bash
dotnet run --generate-symbols
```

This creates five example symbol templates in the `symbols/` directory:
- `circle.png` - A white circle on black background
- `square.png` - A white square on black background
- `triangle.png` - A white triangle on black background
- `star.png` - A white 5-pointed star on black background
- `cross.png` - A white cross/plus sign on black background

You can add your own symbol templates by placing PNG or JPEG images in the `symbols/` directory. Each template should:
- Contain a single, clear symbol
- Have good contrast (preferably white symbol on black background)
- Be reasonably sized (100x100 pixels works well)
- Be named descriptively (the filename becomes the symbol name in results)

### Real-time Camera Mode

```bash
dotnet run
```

When run without arguments, the application opens your default camera and tracks lights matching your selected color in real-time.

**Startup Color Selection:**

When you start the application, you'll be prompted to choose which color of light to track:
1. **Any bright light** - Tracks any bright light (original behavior, uses brightness only)
2. **Red** - Tracks red lights using HSV color detection
3. **Green** - Tracks green lights using HSV color detection
4. **Blue** - Tracks blue lights using HSV color detection
5. **Yellow** - Tracks yellow lights using HSV color detection
6. **Cyan** - Tracks cyan lights using HSV color detection
7. **Magenta** - Tracks magenta lights using HSV color detection
8. **White** - Tracks white lights using HSV color detection

Simply enter the number (1-8) corresponding to your choice. The application will then only track lights of that color.

**Controls:**
- **'q' or ESC**: Quit the application
- **'c'**: Clear all tracked points and start fresh
- **'b'**: Calibrate using the current brightest point's color as baseline
- **'+' or '='**: Increase tracking radius by 10 pixels (max: 500px)
- **'-' or '_'**: Decrease tracking radius by 10 pixels (min: 10px)
- **']' or '}'**: Increase no-light timeout by 0.5 seconds (max: 30.0s)
- **'[' or '{'**: Decrease no-light timeout by 0.5 seconds (min: 0.5s)

**What you'll see:**
- Live camera feed
- Current brightest point: Large green circle with outline (if within tracking radius) or red circle (if filtered out)
- Historical brightest points: Smaller cyan dots forming a trail
- Orange lines connecting consecutive points showing the movement path
- Purple circle around last tracked point showing the tracking radius
- Color information: RGB values of the current brightest point
- Color difference: When calibrated, shows how much the color has changed from baseline
- Status text: "Points tracked: X | Tracking radius: Ypx | Timeout: Zs" showing total count, current radius, and no-light timeout, plus "[CALIBRATED]" status
- Automatic PNG export: When light is not detected for the configured timeout, a PNG file is saved with format: `light_drawing_YYYYMMDD_HHMMSS.png`

### Static Image Mode

```bash
dotnet run <image_path>
```

Processes a static image file and generates output images with detected bright regions.

### Example Output - Real-time Camera Mode

```
Bright Light Color Detection and Mapping
========================================

Select the color of light to track:
1. Any bright light (default)
2. Red
3. Green
4. Blue
5. Yellow
6. Cyan
7. Magenta
8. White

Enter your choice (1-8): 3

Starting real-time camera tracking for Green light...
Press 'q' to quit, 'c' to clear tracked points

Camera opened successfully!
Resolution: 640x480
Tracking color: Green
Press 'b' to calibrate with brightest point color
Press '+' to increase tracking radius, '-' to decrease
Press '[' to decrease no-light timeout, ']' to increase

[Real-time window displays with overlaid points and connecting lines]

Calibrated! Baseline color: B=245 G=248 R=250

Total points tracked: 543
Camera tracking stopped.
```

### Example Output - Static Image Mode

```
Bright Light Color Detection and Mapping
========================================

Processing image: sample_input.png
Image size: 640x480
Found 4 bright light region(s)
  Light #1:
    Position: (499, 99)
    Area: 1296 pixels
    Bounding Box: (482, 82) - 35x35
  Light #2:
    Position: (399, 199)
    Area: 676 pixels
    Bounding Box: (387, 187) - 25x25
...

Output files created:
  Detection overlay: ./sample_input_detected.png
  Point map: ./sample_input_point_map.png

Processing complete!
```

## Output Files

The application generates two output images:

1. **`<input>_detected.png`**: Original image with detection annotations
   - Red bounding boxes around each bright region
   - Blue center points marking the centroid of each region
   - Green lines connecting consecutive points

2. **`<input>_point_map.png`**: Black canvas with detected point locations
   - Cyan circles marking the center of each detected light
   - Yellow lines connecting consecutive points
   - Useful for spatial analysis and mapping

3. **`detected_symbols.csv`**: Symbol detection results (created/updated automatically)
   - **Timestamp**: Date and time when the symbol detection was performed
   - **ImageFilename**: Name of the analyzed image file
   - **DetectedSymbols**: Semicolon-separated list of detected symbol names (or "None")
   - **Confidence**: Semicolon-separated list of confidence scores (0.000-1.000)
   - **Count**: Number of symbols detected in the image
   
   Example CSV content:
   ```
   Timestamp,ImageFilename,DetectedSymbols,Confidence,Count
   2024-02-07 18:15:23,circular_screenshot_20240207_181523.png,circle;star,0.892;0.754,2
   2024-02-07 18:16:45,light_drawing_20240207_181645.png,None,N/A,0
   ```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run [image_path]
```

## Dependencies

- .NET 8.0 SDK
- OpenCvSharp4 4.10.0 (for real-time camera tracking)
- Platform-specific OpenCV runtime packages (automatically selected based on your OS):
  - **Linux**: OpenCvSharp4.runtime.linux-x64
  - **Windows**: OpenCvSharp4.runtime.win
  - **macOS**: OpenCvSharp4.runtime.osx-x64
- SixLabors.ImageSharp 3.1.12 (for static image processing)
- SixLabors.ImageSharp.Drawing 2.1.7

**Note**: The OpenCV runtime packages are automatically selected based on your platform when you run `dotnet restore` or `dotnet build`. The project file uses MSBuild conditions to ensure the correct runtime is installed for your operating system.

## Use Cases

### Real-time Camera Mode
- **Color-specific light tracking** - Track specific colored lights (red, green, blue, etc.) in multi-colored environments
- **RGB LED tracking** - Track individual colors from RGB light sources
- **Colored laser pointer tracking** - Follow colored laser pointers with accuracy
- **Stage lighting analysis** - Track specific colored stage lights separately
- Tracking light sources in motion with color change detection
- Calibrating baseline light color for comparison
- Detecting when light passes through colored objects or filters
- Interactive light painting applications with automatic capture
- Light drawing/painting - automatically saves your artwork when you turn off the light
- Real-time quality control for lighting installations
- Interactive installations and art projects
- Educational demonstrations of light tracking and color analysis

### Static Image Mode
- Light source detection in photographs
- Analyzing stage/studio lighting setups
- Detecting reflections or glare in images
- Mapping LED/bulb positions
- Quality control for lighting installations
- Astronomy image processing (star detection)

## Algorithm Details

### Real-time Camera Tracking

The real-time tracking works in a simple, efficient manner:

1. **Color Selection**: User selects which color of light to track at startup (or "Any" for original brightness-based behavior)
2. **Frame Capture**: Captures frames from the camera at the camera's native framerate
3. **Color-Based Detection** (when specific color selected):
   - Converts frame to HSV color space for better color discrimination
   - Creates a color mask using predefined HSV ranges for the selected color
   - Combines color mask with brightness threshold (>= 200) for accurate detection
   - Only lights matching both color and brightness criteria are detected
4. **Brightness-Based Detection** (when "Any" selected):
   - Converts the color frame to grayscale for efficient brightness analysis
   - Uses OpenCV's MinMaxLoc to find the pixel with maximum brightness
5. **Distance-based Filtering**: Calculates the distance from the last tracked point to the current brightest point. Only accepts the point if it's within the configured tracking radius (default: 100px)
6. **Color Extraction**: Captures the BGR color values at the brightest point location
7. **Calibration Support**: Press 'b' to store the current color as a baseline reference
8. **Color Difference Calculation**: When calibrated, calculates Euclidean distance in BGR color space between current and baseline colors
9. **Threshold Filtering**: Only tracks points with brightness >= 200 (0-255 scale) to avoid noise
10. **Accumulation**: Stores all brightest points that pass the distance filter from each frame in a list
11. **Path Visualization**: 
   - Current brightest point: Large green filled circle (8px radius) with outline (10px radius) if within tracking radius, or red if filtered out
   - Historical points: Small cyan filled circles (3px radius)
   - Connecting lines: Orange lines (2px width) between consecutive points
   - Tracking circle: Purple circle (2px width) with radius equal to tracking radius around the last tracked point
   - Text overlays: Shows RGB color values, color difference (if calibrated), total count of tracked points, current tracking radius, and no-light timeout
12. **No-Light Detection & PNG Export**:
   - Tracks the time when light was last detected
   - If no bright point is detected for the configured timeout duration (default: 3.0s), automatically exports the tracked path to a PNG file
   - Exported image shows the complete drawing with yellow connecting lines and cyan point markers on a black background
   - Files are saved with timestamp: `light_drawing_YYYYMMDD_HHMMSS.png`
   - Export only happens once per drawing session until light is detected again

### Static Image Detection

The detection works in three phases:

1. **Scanning**: Iterates through all pixels looking for high brightness (RGB max > 200) and low saturation (RGB range < 100)
2. **Region Growing**: Uses flood-fill to find connected bright pixels forming coherent regions
3. **Filtering**: Removes small regions (< 50 pixels) that are likely noise
4. **Path Connection**: Draws lines connecting consecutive detected points in order of detection

Each detected region provides:
- Center position (centroid)
- Total pixel count (area)
- Bounding box coordinates

Visual outputs include:
- Detection overlay: Red bounding boxes, blue center points, green connecting lines
- Point map: Cyan point markers, yellow connecting lines

## Customization

You can adjust detection parameters in `Program.cs`:

```csharp
int brightnessThreshold = 200;  // Minimum brightness (0-255)
int saturationThreshold = 100;  // Maximum saturation difference
int minimumArea = 50;           // Minimum pixels to be considered a region
```

## License

This project is open source and available for educational and commercial use.
