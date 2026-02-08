# test-uga-hacks11

## Bright Light Color Detection Application with Real-Time Shape Recognition

A C# application using OpenCV and SixLabors.ImageSharp that detects bright lights in real-time from a camera feed or processes static images. Now features **real-time shape detection** that recognizes shapes AS YOU DRAW them with high accuracy on hand-drawn and messy shapes.

### Key Features

- **🎨 REAL-TIME Shape Detection (NEW!)**: See shapes recognized WHILE you draw them
  - Live shape recognition in the camera feed as you draw
  - Displays detected shape name and confidence in real-time
  - Visual contour overlay showing detected shape boundaries
  - Adjustable detection frequency (every N frames) for performance
  - Toggle on/off with 'r' key
  - Adjust detection speed with 'd' (faster) and 'D' (slower) keys
  - Minimum 10 points required before detection starts
  - Works seamlessly with existing tracking features

- **🚀 Statistical Outlier Detection**: Automatic removal of extreme outliers from tracked points
  - Uses robust statistical methods (IQR and Modified Z-score with MAD)
  - Hybrid approach combining multiple algorithms for best results
  - Toggle on/off with 'x' key during tracking
  - Enabled by default to ensure clean, accurate drawings
  - Displays outlier removal statistics during export
  - Test with `dotnet run --test-outlier-detection`

- **🔧 Configurable Contour Approximation**: Advanced contour simplification with interactive adjustment
  - Configurable epsilon parameter for precise control
  - Multi-level visualization showing different approximation levels
  - Interactive mode for real-time parameter tuning
  - Reduces points while preserving shape characteristics
  - See [ColorDetectionApp/CONTOUR_APPROXIMATION.md](ColorDetectionApp/CONTOUR_APPROXIMATION.md) for details

- **🎯 Enhanced Shape Detection**: Advanced geometric analysis for accurate hand-drawn shape recognition
  - 92% accuracy on hand-drawn shapes (vs 43% with template matching)
  - Rotation and scale invariant
  - Works with messy, imperfect drawings
  - No training required - works immediately
  - Detects: circles, squares, triangles, stars, pentagons, hexagons, and more
  - Now integrated into real-time camera loop for live feedback
  - See [ENHANCED_SHAPE_DETECTION.md](ENHANCED_SHAPE_DETECTION.md) for details
  
- **Dual-Color Tracking Mode**: Choose two different colors to track with different export behaviors
  - Primary color: Normal tracking with PNG + CSV export on color switch
  - Secondary color: CSV-only export on switch back
- **Circular Screenshot Capture**: Define a circular region in the center of the screen and capture only that area as a screenshot
  - Adjustable circle size using 'o' and 'i' keys (default: 150px, range: 20-500px)
  - Visual indicator showing the capture region
  - Press 's' to capture a circular screenshot
  - **Automatic Symbol Detection**: Each captured screenshot is analyzed for symbols using **enhanced geometric detection**
  
- **Symbol Recognition and Tracking**: 
  - **Enhanced Detector**: Uses geometric feature analysis (circularity, vertices, convexity) for robust hand-drawn shape recognition
  - **Template Matching**: Compare images against customizable symbol templates in `symbols/` directory (fallback mode)
  - **Hybrid Approach**: Automatically uses best detection method
  - Records all detected symbols in a CSV file with timestamps and confidence scores
  - Generate example templates with `dotnet run --generate-symbols`
  - Test enhanced detector with `dotnet run --test-enhanced`
  - Analyze specific images with `dotnet run --analyze-shape <path>`
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

After selecting both colors, the application will track lights and detect shapes in real-time:
- **REAL-TIME SHAPE DETECTION**: Shapes are detected AS YOU DRAW with live visual feedback
  - Detected shape name and confidence displayed on screen
  - Green contour overlay shows the detected shape boundary
  - Press 'r' to toggle real-time detection on/off (default: ON)
  - Press 'd' to detect MORE frequently (every 10 frames, faster updates)
  - Press 'D' to detect LESS frequently (every 20 frames, better performance)
  - Requires minimum 10 tracked points before detection starts
- When switching from **primary to secondary**: Exports PNG + CSV
- When switching from **secondary to primary**: Exports CSV only
- The current brightest point is shown as a large green circle (if within tracking radius)
- All previously tracked points are shown as smaller cyan dots
- Orange lines connect consecutive points to show the path
- A purple circle shows the tracking radius around the last tracked point
- A cyan circle in the center shows the circular screenshot capture region
- RGB color values and active color are displayed on screen
- Press 'b' to calibrate and capture baseline color for comparison
- Press '+' to increase tracking radius, '-' to decrease (default: 100px)
- Press '[' to decrease no-light timeout, ']' to increase (default: 3.0s)
- Press 'o' to increase capture circle size, 'i' to decrease (default: 150px)
- Press 's' to take a circular screenshot of the capture region
- Press 'x' to toggle outlier detection on/off (default: ON)
- Press 'f' to flip/mirror the camera display
- Press 'F11' to toggle fullscreen mode
- Press 'q' to quit
- Press 'c' to clear the tracked points
- When light is not detected for the configured timeout, a PNG image is automatically saved

#### Static Image Mode
```bash
cd ColorDetectionApp
dotnet run <image_path>
```

This will process a static image and generate output files showing detected bright lights.

#### Shape Detection Testing
```bash
cd ColorDetectionApp

# Test enhanced shape detector
dotnet run --test-enhanced

# Analyze a specific image
dotnet run --analyze-shape path/to/image.png

# Analyze with custom epsilon for contour approximation
dotnet run --analyze-shape path/to/image.png 0.02

# Generate sample shapes for testing
dotnet run --generate-symbols
```

#### Contour Approximation Testing
```bash
cd ColorDetectionApp

# Get contour approximation info for an image
dotnet run --contour-info path/to/image.png

# Get contour info with custom epsilon
dotnet run --contour-info path/to/image.png 0.06

# Visualize approximation at multiple levels
dotnet run --test-contour-approx path/to/image.png

# Interactive mode - adjust epsilon in real-time
dotnet run --interactive-contour path/to/image.png
```

#### Outlier Detection Testing
```bash
cd ColorDetectionApp

# Run outlier detection test suite
dotnet run --test-outlier-detection
```

### Documentation

- **[REALTIME_SHAPE_DETECTION.md](REALTIME_SHAPE_DETECTION.md)** - ⭐ **NEW! Real-time shape detection guide** (learn how to use live shape recognition)
- **[ENHANCED_SHAPE_DETECTION.md](ENHANCED_SHAPE_DETECTION.md)** - ⭐ **Comprehensive guide to enhanced shape detection** (recommended read)
- **[ColorDetectionApp/CONTOUR_APPROXIMATION.md](ColorDetectionApp/CONTOUR_APPROXIMATION.md)** - ⭐ **Complete guide to contour approximation feature** (recommended for advanced shape analysis)
- **[SHAPE_DETECTION_MODEL_RESEARCH.md](SHAPE_DETECTION_MODEL_RESEARCH.md)** - Research on various shape detection models and recommendations
- **[IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)** - Step-by-step implementation guide for ML models (optional advanced usage)
- [ColorDetectionApp/README.md](ColorDetectionApp/README.md) - Detailed usage instructions and technical documentation
- [IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md) - Folder-based training system documentation
- [SYMBOL_DETECTION_CHANGES.md](SYMBOL_DETECTION_CHANGES.md) - Template matching feature changes

### Sample Output

#### Real-time Camera Mode
- Displays live camera feed with overlay
- **REAL-TIME SHAPE DETECTION**: Shows detected shape name and confidence as you draw
- **Green contour overlay**: Visual boundary around detected shape
- Current brightest point highlighted in green (if within tracking radius) or red (if filtered out)
- Trail of all brightest points from previous frames in cyan
- Orange lines connecting consecutive points
- Purple circle showing the tracking radius around the last tracked point
- RGB color information displayed on screen
- Color difference from baseline (when calibrated)
- Frame counter showing number of points tracked, current tracking radius, and no-light timeout
- Calibration status indicator
- Outlier detection status indicator (ON/OFF)
- Real-time detection status (ON/OFF) and frequency
- Automatic PNG export when light is not detected for the configured timeout
- Outlier removal statistics displayed in console when exporting

#### Static Image Mode
The application detects bright lights and provides:
- Position coordinates (x, y)
- Area in pixels
- Bounding box dimensions
- Visual overlays with detection markers and connecting lines
- Point map for spatial analysis with path visualization