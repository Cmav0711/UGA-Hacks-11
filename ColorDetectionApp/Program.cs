using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace ColorDetectionApp
{
    // Enum for predefined color options
    public enum LightColor
    {
        Any,      // Any bright light (original behavior)
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        White
    }

    // Enum for tracking modes
    public enum TrackingMode
    {
        Drawing,  // Drawing mode - draws lines and shapes
        Cursor    // Cursor mode - only tracks coordinates
    }

    class Program
    {
        // Real-time detection constants
        private const double MIN_REALTIME_CONFIDENCE = 0.40;  // Lower threshold for real-time feedback
        private const double MIN_SAVED_CONFIDENCE = 0.60;     // Higher threshold for saved results
        private const int MIN_POINTS_FOR_DETECTION = 10;       // Minimum tracked points before detection starts
        private const double ASSUMED_CAMERA_FPS = 30.0;        // Assumed framerate for detection interval calculations
        // Key codes for F11 - can vary by platform
        private const int F11_KEY_CODE_PRIMARY = 65478;
        private const int F11_KEY_CODE_ALTERNATE = 65470;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Bright Light Color Detection and Mapping");
            Console.WriteLine("========================================\n");

            // Check for special commands
            if (args.Length > 0 && args[0] == "--generate-symbols")
            {
                Console.WriteLine("Generating example symbol templates...");
                SymbolTemplateGenerator.GenerateExampleTemplates();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-symbols")
            {
                SymbolDetectionTest.RunTest();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-enhanced")
            {
                Console.WriteLine("Testing enhanced shape detector...");
                TestEnhancedShapeDetector();
                return;
            }

            if (args.Length > 0 && args[0] == "--analyze-shape")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dotnet run --analyze-shape <image_path> [epsilon_factor]");
                    Console.WriteLine("       epsilon_factor: optional, default 0.04 (e.g., 0.01, 0.02, 0.08)");
                    return;
                }
                
                double epsilonFactor = 0.04;
                if (args.Length >= 3)
                {
                    if (!double.TryParse(args[2], out epsilonFactor))
                    {
                        Console.WriteLine("Error: Invalid epsilon factor. Using default 0.04");
                        epsilonFactor = 0.04;
                    }
                }
                
                Console.WriteLine("\nAnalyzing shape in image...");
                Console.WriteLine(EnhancedShapeDetector.GetShapeAnalysisDetails(args[1], epsilonFactor));
                return;
            }

            if (args.Length > 0 && args[0] == "--test-contour-approx")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dotnet run --test-contour-approx <image_path>");
                    return;
                }
                Console.WriteLine("\nTesting contour approximation at multiple levels...");
                ContourApproximation.VisualizeApproximationLevels(args[1]);
                return;
            }

            if (args.Length > 0 && args[0] == "--test-contour-unit")
            {
                TestContourApproximation.RunTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-outlier-detection")
            {
                TestOutlierDetection.RunTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-dot-with-lines")
            {
                TestDotWithLines.RunTest();
                return;
            }

            if (args.Length > 0 && args[0] == "--contour-info")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dotnet run --contour-info <image_path> [epsilon_factor]");
                    return;
                }
                
                double epsilonFactor = 0.04;
                if (args.Length >= 3)
                {
                    if (!double.TryParse(args[2], out epsilonFactor))
                    {
                        Console.WriteLine("Error: Invalid epsilon factor. Using default 0.04");
                        epsilonFactor = 0.04;
                    }
                }
                
                Console.WriteLine(ContourApproximation.GetApproximationInfo(args[1], epsilonFactor));
                return;
            }

            if (args.Length > 0 && args[0] == "--interactive-contour")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dotnet run --interactive-contour <image_path>");
                    return;
                }
                ContourApproximation.InteractiveApproximation(args[1]);
                return;
            }

            if (args.Length == 0)
            {
                // Prompt user to select target light color
                LightColor selectedColor = PromptForColorSelection();
                
                Console.WriteLine($"\nStarting real-time camera tracking for {selectedColor} light...");
                Console.WriteLine("Press 'q' to quit, 'c' to clear tracked points\n");
                RunCameraTracking(selectedColor);
                return;
            }

            string imagePath = args[0];

            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Error: Image file '{imagePath}' not found.");
                return;
            }

            ProcessImage(imagePath);
        }

        static LightColor PromptForColorSelection()
        {
            Console.WriteLine("Select the color of light to track:");
            Console.WriteLine("1. Any bright light (default)");
            Console.WriteLine("2. Red");
            Console.WriteLine("3. Green");
            Console.WriteLine("4. Blue");
            Console.WriteLine("5. Yellow");
            Console.WriteLine("6. Cyan");
            Console.WriteLine("7. Magenta");
            Console.WriteLine("8. White");
            Console.Write("\nEnter your choice (1-8): ");
            
            string? input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input) || input == "1")
            {
                return LightColor.Any;
            }
            
            return input switch
            {
                "2" => LightColor.Red,
                "3" => LightColor.Green,
                "4" => LightColor.Blue,
                "5" => LightColor.Yellow,
                "6" => LightColor.Cyan,
                "7" => LightColor.Magenta,
                "8" => LightColor.White,
                _ => LightColor.Any
            };
        }

        static void RunCameraTracking(LightColor targetColor = LightColor.Any)
        {
            // List to store all brightest points from previous frames
            var brightestPoints = new List<OpenCvSharp.Point>();
            
            // Calibration data - stores baseline color for comparison
            Scalar? calibratedColor = null;
            
            // Tracking radius for filtering points (adjustable via +/- keys)
            int trackingRadius = 100;
            
            // No-light detection timeout in seconds (adjustable via [ and ] keys)
            double noLightTimeout = 3.0;
            const double DEFAULT_TIMEOUT = 3.0;
            double savedTimeout = DEFAULT_TIMEOUT; // Store timeout value when toggling
            
            // Track the last time light was detected
            DateTime? lastLightDetectedTime = null;
            bool imageExported = false;
            
            // Outlier detection enabled flag (toggle with 'x' key)
            bool outlierDetectionEnabled = true;
            
            // Circular screenshot capture region (adjustable via 'O' and 'I' keys)
            int captureCircleRadius = 150;
            
            // Camera flip state (adjustable via 'f' key)
            bool flipCamera = false;
            
            // Real-time shape detection variables
            bool realtimeDetectionEnabled = true;
            int detectionFrameInterval = 15; // Detect every N frames (adjustable via 'd' and 'D' keys)
            int frameCounter = 0;
            string currentDetectedShape = "none";
            double currentShapeConfidence = 0.0;
            OpenCvSharp.Point[] currentShapeContour = Array.Empty<OpenCvSharp.Point>();
            // Fullscreen state (adjustable via F11 key)
            bool isFullscreen = false;
            
            // Track whether line drawing is enabled (toggle with 'a' key)
            bool lineDrawingEnabled = false;
            
            // Current tracking mode (toggle with 'm' key)
            TrackingMode currentMode = TrackingMode.Drawing;
            
            // Open the default camera
            using (var capture = new VideoCapture(0))
            {
                if (!capture.IsOpened())
                {
                    Console.WriteLine("Error: Could not open camera. Make sure a camera is connected.");
                    return;
                }

                // Set camera properties for better performance
                capture.Set(VideoCaptureProperties.FrameWidth, 640);
                capture.Set(VideoCaptureProperties.FrameHeight, 480);

                Console.WriteLine($"Camera opened successfully!");
                Console.WriteLine($"Resolution: {capture.FrameWidth}x{capture.FrameHeight}");
                Console.WriteLine($"Tracking color: {targetColor}");
                Console.WriteLine("Press 'b' to calibrate with brightest point color");
                Console.WriteLine("Press '+' to increase tracking radius, '-' to decrease");
                Console.WriteLine("Press '[' to decrease no-light timeout, ']' to increase");
                Console.WriteLine("Press 'o' to increase capture circle size, 'i' to decrease");
                Console.WriteLine("Press 's' to take circular screenshot");
                Console.WriteLine("Press 'x' to toggle outlier detection (currently: ON)");
                Console.WriteLine("Press 'r' to toggle real-time shape detection (currently: ON)");
                Console.WriteLine("Press 'd' to decrease detection interval (more frequent), 'D' to increase (less frequent)");
                Console.WriteLine("Press 'a' to toggle line drawing on/off");
                Console.WriteLine("Press 'm' to toggle between Drawing and Cursor modes");
                Console.WriteLine("Press 'f' to flip/mirror camera");
                Console.WriteLine("Press 'F11' to toggle fullscreen mode");
                
                // Calculate center point once (frame dimensions don't change)
                var centerPoint = new OpenCvSharp.Point((int)capture.FrameWidth / 2, (int)capture.FrameHeight / 2);

                using (var frame = new Mat())
                using (var window = new Window($"Brightest Point Tracker ({targetColor}) - 'q' quit, 'c' clear, 's' screenshot, 'f' flip"))
                {
                    while (true)
                    {
                        // Capture frame from camera
                        capture.Read(frame);
                        
                        if (frame.Empty())
                        {
                            Console.WriteLine("Warning: Empty frame captured");
                            break;
                        }

                        // Flip the frame horizontally if flip mode is enabled
                        if (flipCamera)
                        {
                            Cv2.Flip(frame, frame, OpenCvSharp.FlipMode.Y); // FlipMode.Y flips horizontally (mirror effect)
                        }

                        // Find the brightest point in this frame
                        // If we have previous points, only search within the tracking circle
                        var searchCenter = brightestPoints.Count > 0 
                            ? brightestPoints[brightestPoints.Count - 1] 
                            : (OpenCvSharp.Point?)null;
                        var brightestPoint = FindBrightestPointWithColor(frame, targetColor, searchCenter, trackingRadius);
                        
                        if (brightestPoint.HasValue)
                        {
                            if (brightestPoints.Count == 0)
                            {
                                NetworkManager.GenerateNewSpellShapeID();
                            }

                            // Add to our collection (already filtered by circle search)
                            brightestPoints.Add(brightestPoint.Value);
                            
                            // Update last light detected time
                            lastLightDetectedTime = DateTime.Now;
                            imageExported = false;
                            
                            // Get the color at the brightest point
                            var color = GetColorAtPoint(frame, brightestPoint.Value);
                            
                            // Draw the current brightest point (large, bright green)
                            var pointColor = new Scalar(0, 255, 0);
                            Cv2.Circle(frame, brightestPoint.Value, 8, pointColor, -1);
                            Cv2.Circle(frame, brightestPoint.Value, 10, pointColor, 2);
                            
                            // Display color information
                            string colorInfo = $"Color: B={color.Val0:F0} G={color.Val1:F0} R={color.Val2:F0}";
                            Cv2.PutText(frame, colorInfo, new OpenCvSharp.Point(10, 60), 
                                       HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1);
                            
                            // If calibrated, show color difference
                            if (calibratedColor.HasValue)
                            {
                                double colorDiff = CalculateColorDifference(color, calibratedColor.Value);
                                string diffInfo = $"Color Diff: {colorDiff:F1}";
                                Cv2.PutText(frame, diffInfo, new OpenCvSharp.Point(10, 85), 
                                           HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1);
                            }

                            float normX = (float)brightestPoint.Value.X / (float)capture.FrameWidth;
                            float normY = (float)brightestPoint.Value.Y / (float)capture.FrameHeight;
                            NetworkManager.SendStreamPacket(normX, normY, currentMode == TrackingMode.Drawing);
                        }
                        

                        // Real-time shape detection (every N frames to avoid performance issues)
                        // Only detect shapes in Drawing mode
                        frameCounter++;
                        if (currentMode == TrackingMode.Drawing && 
                            realtimeDetectionEnabled && 
                            brightestPoints.Count >= MIN_POINTS_FOR_DETECTION && 
                            frameCounter % detectionFrameInterval == 0)
                        {
                            try
                            {
                                // Create a temporary image from tracked points
                                using var tempMat = CreateMatFromPoints(brightestPoints, 
                                    (int)capture.FrameWidth, (int)capture.FrameHeight);
                                
                                // Detect shape from the current drawing
                                var (shape, confidence, contour) = EnhancedShapeDetector.DetectShapeFromMat(tempMat);
                                
                                // Update current detection if confidence is reasonable
                                if (confidence > MIN_REALTIME_CONFIDENCE)
                                {
                                    currentDetectedShape = shape;
                                    currentShapeConfidence = confidence;
                                    currentShapeContour = contour;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Silently continue if detection fails
                                Console.WriteLine($"Real-time detection error: {ex.Message}");
                            }
                        }

                        // Draw lines connecting consecutive points (only in Drawing mode)
                        if (currentMode == TrackingMode.Drawing)
                        {
                            for (int i = 1; i < brightestPoints.Count; i++)
                            {
                                Cv2.Line(frame, brightestPoints[i - 1], brightestPoints[i], 
                                        new Scalar(255, 128, 0), 2);
                            }
                        }

                        // Draw detected shape contour if available (only in Drawing mode)
                        if (currentMode == TrackingMode.Drawing && 
                            currentShapeContour.Length > 0 && 
                            brightestPoints.Count >= MIN_POINTS_FOR_DETECTION)
                        {
                            // Draw the detected contour in green
                            Cv2.DrawContours(frame, new OpenCvSharp.Point[][] { currentShapeContour }, -1, 
                                new Scalar(0, 255, 0), 2);
                        }

                        // Draw all historical points (smaller, cyan)
                        foreach (var point in brightestPoints)
                        {
                            Cv2.Circle(frame, point, 3, new Scalar(255, 255, 0), -1);
                        }
                        
                        // Draw tracking radius circle around the last tracked point
                        if (brightestPoints.Count > 0)
                        {
                            var lastPoint = brightestPoints[brightestPoints.Count - 1];
                            Cv2.Circle(frame, lastPoint, trackingRadius, new Scalar(255, 0, 255), 2);
                        }
                        
                        // Draw circular screenshot capture region in the center of the screen
                        Cv2.Circle(frame, centerPoint, captureCircleRadius, new Scalar(0, 255, 255), 3); // Cyan circle

                        // Display frame count and point count
                        string info = $"Mode: {currentMode.ToString().ToUpper()} | Points: {brightestPoints.Count} | Radius: {trackingRadius}px | Timeout: {noLightTimeout:F1}s";
                        if (calibratedColor.HasValue)
                        {
                            info += " [CALIBRATED]";
                        }
                        if (outlierDetectionEnabled)
                        {
                            info += " [OUTLIER FILTER: ON]";
                        }
                        Cv2.PutText(frame, info, new OpenCvSharp.Point(10, 30), 
                                   HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 255, 255), 2);
                        
                        // Display real-time shape detection info (only in Drawing mode)
                        if (currentMode == TrackingMode.Drawing && realtimeDetectionEnabled && brightestPoints.Count >= MIN_POINTS_FOR_DETECTION)
                        {
                            string shapeInfo = $"Detected Shape: {currentDetectedShape.ToUpper()} (Confidence: {currentShapeConfidence:P0})";
                            Cv2.PutText(frame, shapeInfo, new OpenCvSharp.Point(10, 60), 
                                       HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 255, 0), 2);
                        }
                        else if (currentMode == TrackingMode.Drawing && realtimeDetectionEnabled)
                        {
                            string shapeInfo = $"Draw more points for shape detection ({brightestPoints.Count}/{MIN_POINTS_FOR_DETECTION})";
                            Cv2.PutText(frame, shapeInfo, new OpenCvSharp.Point(10, 60), 
                                       HersheyFonts.HersheySimplex, 0.6, new Scalar(128, 128, 128), 1);
                        }
                        
                        // Display capture circle radius
                        string captureInfo = $"Capture Circle Radius: {captureCircleRadius}px (press 's' to screenshot)";
                        Cv2.PutText(frame, captureInfo, new OpenCvSharp.Point(10, frame.Height - 20), 
                                   HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 255, 255), 2);

                        // Show the frame
                        window.ShowImage(frame);

                        // Check for key press
                        int key = Cv2.WaitKey(1);
                        if (key == 'q' || key == 'Q' || key == 27) // 'q' or ESC
                        {
                            break;
                        }
                        else if (key == 'c' || key == 'C')
                        {
                            brightestPoints.Clear();
                            Console.WriteLine("Cleared all tracked points");
                        }
                        else if (key == 'b' || key == 'B')
                        {
                            // Calibration: capture the color of the brightest point
                            if (brightestPoint.HasValue)
                            {
                                calibratedColor = GetColorAtPoint(frame, brightestPoint.Value);
                                Console.WriteLine($"Calibrated! Baseline color: B={calibratedColor.Value.Val0:F0} G={calibratedColor.Value.Val1:F0} R={calibratedColor.Value.Val2:F0}");
                            }
                            else
                            {
                                Console.WriteLine("No bright point found for calibration");
                            }
                        }
                        else if (key == '+' || key == '=')
                        {
                            trackingRadius += 10;
                            if (trackingRadius > 500) trackingRadius = 500;
                            Console.WriteLine($"Tracking radius increased to {trackingRadius}px");
                        }
                        else if (key == '-' || key == '_')
                        {
                            trackingRadius -= 10;
                            if (trackingRadius < 10) trackingRadius = 10;
                            Console.WriteLine($"Tracking radius decreased to {trackingRadius}px");
                        }
                        else if (key == '[' || key == '{')
                        {
                            noLightTimeout -= 0.5;
                            if (noLightTimeout < 0.5) noLightTimeout = 0.5;
                            savedTimeout = noLightTimeout; // Update saved value
                            Console.WriteLine($"No-light timeout decreased to {noLightTimeout:F1}s");
                        }
                        else if (key == ']' || key == '}')
                        {
                            noLightTimeout += 0.5;
                            if (noLightTimeout > 30.0) noLightTimeout = 30.0;
                            savedTimeout = noLightTimeout; // Update saved value
                            Console.WriteLine($"No-light timeout increased to {noLightTimeout:F1}s");
                        }
                        else if (key == 'o' || key == 'O')
                        {
                            captureCircleRadius += 10;
                            if (captureCircleRadius > 500) captureCircleRadius = 500;
                            Console.WriteLine($"Capture circle radius increased to {captureCircleRadius}px");
                        }
                        else if (key == 'i' || key == 'I')
                        {
                            captureCircleRadius -= 10;
                            if (captureCircleRadius < 20) captureCircleRadius = 20;
                            Console.WriteLine($"Capture circle radius decreased to {captureCircleRadius}px");
                        }
                        else if (key == 's' || key == 'S')
                        {
                            // Capture circular screenshot
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string filename = $"circular_screenshot_{timestamp}.png";
                            CaptureCircularScreenshot(frame, centerPoint, captureCircleRadius, filename);
                            Console.WriteLine($"Circular screenshot saved to: {filename}");
                        }
                        else if (key == 'x' || key == 'X')
                        {
                            // Toggle outlier detection
                            outlierDetectionEnabled = !outlierDetectionEnabled;
                            Console.WriteLine($"Outlier detection: {(outlierDetectionEnabled ? "ENABLED" : "DISABLED")}");
                        }
                        else if (key == 'f' || key == 'F')
                        {
                            // Toggle camera flip/mirror
                            flipCamera = !flipCamera;
                            Console.WriteLine($"Camera flip {(flipCamera ? "enabled" : "disabled")} - image is {(flipCamera ? "mirrored" : "normal")}");
                        }
                        else if (key == 'r' || key == 'R')
                        {
                            // Toggle real-time shape detection
                            realtimeDetectionEnabled = !realtimeDetectionEnabled;
                            Console.WriteLine($"Real-time shape detection: {(realtimeDetectionEnabled ? "ENABLED" : "DISABLED")}");
                            if (!realtimeDetectionEnabled)
                            {
                                currentDetectedShape = "none";
                                currentShapeConfidence = 0.0;
                                currentShapeContour = Array.Empty<OpenCvSharp.Point>();
                            }
                        }
                        else if (key == 'd' || key == 'D')
                        {
                            // Increase detection interval (less frequent detection)
                            if (key == 'D')
                            {
                                detectionFrameInterval += 5;
                                if (detectionFrameInterval > 60) detectionFrameInterval = 60;
                            }
                            else // 'd'
                            {
                                // Decrease detection interval (more frequent detection)
                                detectionFrameInterval -= 5;
                                if (detectionFrameInterval < 5) detectionFrameInterval = 5;
                            }
                            Console.WriteLine($"Detection frame interval: every {detectionFrameInterval} frames ({ASSUMED_CAMERA_FPS/detectionFrameInterval:F1} times per second)");
                        }
                        else if (key == 'a' || key == 'A')
                        {
                            // Toggle line drawing
                            lineDrawingEnabled = !lineDrawingEnabled;
                            Console.WriteLine($"Line drawing: {(lineDrawingEnabled ? "ENABLED" : "DISABLED")}");
                            
                            // Activate instant export effect by setting timeout to 0.0
                            noLightTimeout = 0.0;
                            Console.WriteLine($"No-light timeout set to 0.0s (instant export activated)");
                        }
                        else if (key == 'm' || key == 'M')
                        {
                            // Toggle between Drawing and Cursor modes
                            TrackingMode previousMode = currentMode;
                            currentMode = (currentMode == TrackingMode.Drawing) ? TrackingMode.Cursor : TrackingMode.Drawing;
                            
                            Console.WriteLine($"\nMode switched from {previousMode} to {currentMode}");
                            
                            // When switching FROM Drawing TO Cursor mode, export PNG + CSV
                            if (previousMode == TrackingMode.Drawing && currentMode == TrackingMode.Cursor && brightestPoints.Count > 0)
                            {
                                // Apply outlier detection if enabled
                                var pointsToExport = ApplyOutlierDetectionIfEnabled(brightestPoints, outlierDetectionEnabled);
                                
                                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                string pngFilename = $"drawing_mode_{timestamp}.png";
                                string csvFilename = $"drawing_mode_{timestamp}.csv";
                                
                                ExportDrawingToPng(pointsToExport, (int)capture.FrameWidth, (int)capture.FrameHeight, pngFilename);
                                ExportPointsToCsv(pointsToExport, csvFilename);
                                
                                Console.WriteLine($"Drawing mode export: PNG saved to {pngFilename}");
                                Console.WriteLine($"Drawing mode export: CSV saved to {csvFilename}");
                                
                                // Perform symbol detection on the exported image
                                DetectAndRecordSymbolsEnhanced(pngFilename);

                                NetworkManager.SendActionPacket(currentDetectedShape, pointsToExport, capture.FrameWidth, currentMode);
                                
                                // Clear points after mode switch
                                brightestPoints.Clear();
                                Console.WriteLine("Points cleared after mode switch");
                                
                                // Reset detection state
                                currentDetectedShape = "none";
                                currentShapeConfidence = 0.0;
                                currentShapeContour = Array.Empty<OpenCvSharp.Point>();
                            }
                            // When switching FROM Cursor TO Drawing mode, only export CSV
                            else if (previousMode == TrackingMode.Cursor && currentMode == TrackingMode.Drawing && brightestPoints.Count > 0)
                            {
                                // Apply outlier detection if enabled
                                var pointsToExport = ApplyOutlierDetectionIfEnabled(brightestPoints, outlierDetectionEnabled);
                                
                                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                string csvFilename = $"cursor_mode_{timestamp}.csv";
                                
                                ExportPointsToCsv(pointsToExport, csvFilename);
                                
                                Console.WriteLine($"Cursor mode export: CSV saved to {csvFilename}");
                                
                                // Clear points after mode switch
                                brightestPoints.Clear();
                                Console.WriteLine("Points cleared after mode switch");
                            }
                        }
                        else if (key == F11_KEY_CODE_PRIMARY || key == F11_KEY_CODE_ALTERNATE)
                        {
                            // Toggle fullscreen mode
                            isFullscreen = !isFullscreen;
                            if (isFullscreen)
                            {
                                Cv2.SetWindowProperty(window.Name, WindowPropertyFlags.Fullscreen, 1.0);
                                Console.WriteLine("Fullscreen mode: ENABLED");
                            }
                            else
                            {
                                Cv2.SetWindowProperty(window.Name, WindowPropertyFlags.Fullscreen, 0.0);
                                Console.WriteLine("Fullscreen mode: DISABLED");
                            }
                        }
                    }
                }

                Console.WriteLine($"\nTotal points tracked: {brightestPoints.Count}");
                Console.WriteLine("Camera tracking stopped.");
            }
        }

        static OpenCvSharp.Point? FindBrightestPoint(Mat frame, OpenCvSharp.Point? circleCenter = null, int circleRadius = 0)
        {
            // Convert to grayscale for easier brightness analysis
            using (var gray = new Mat())
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                
                Mat? mask = null;
                try
                {
                    // If we have a circle center, create a mask for the circular region
                    if (circleCenter.HasValue && circleRadius > 0)
                    {
                        mask = new Mat(gray.Size(), MatType.CV_8UC1, new Scalar(0));
                        // Draw a filled white circle on the mask
                        Cv2.Circle(mask, circleCenter.Value, circleRadius, new Scalar(255), -1);
                    }
                    
                    // Find the location of the maximum brightness (within the mask if provided)
                    double minVal, maxVal;
                    OpenCvSharp.Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(gray, out minVal, out maxVal, out minLoc, out maxLoc, mask);
                    
                    // Only return if brightness is above threshold (to avoid noise in dark scenes)
                    if (maxVal >= 230)
                    {
                        return maxLoc;
                    }
                }
                finally
                {
                    mask?.Dispose();
                }
            }
            
            return null;
        }

        static OpenCvSharp.Point? FindBrightestPointWithColor(Mat frame, LightColor targetColor, OpenCvSharp.Point? circleCenter = null, int circleRadius = 0)
        {
            // If tracking any color, use the original brightness-based method
            if (targetColor == LightColor.Any)
            {
                return FindBrightestPoint(frame, circleCenter, circleRadius);
            }

            // Convert frame to HSV for better color detection
            using (var hsvFrame = new Mat())
            {
                Cv2.CvtColor(frame, hsvFrame, ColorConversionCodes.BGR2HSV);
                
                Mat colorMask;
                
                // Special handling for red since it wraps around in HSV space
                if (targetColor == LightColor.Red)
                {
                    // Red is at both ends of the hue spectrum (0-10 and 170-180)
                    using (var lowerRedMask = new Mat())
                    using (var upperRedMask = new Mat())
                    {
                        Cv2.InRange(hsvFrame, new Scalar(0, 100, 180), new Scalar(10, 255, 255), lowerRedMask);
                        Cv2.InRange(hsvFrame, new Scalar(170, 100, 180), new Scalar(180, 255, 255), upperRedMask);
                        colorMask = new Mat();
                        Cv2.BitwiseOr(lowerRedMask, upperRedMask, colorMask);
                    }
                }
                else
                {
                    // Define color ranges in HSV space
                    var (lowerBound, upperBound) = GetColorRange(targetColor);
                    
                    // Create mask for the target color
                    colorMask = new Mat();
                    Cv2.InRange(hsvFrame, lowerBound, upperBound, colorMask);
                }
                
                try
                {
                    // Apply brightness threshold to the color mask
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                        using (var brightMask = new Mat())
                        {
                            // Create mask for bright pixels (>= 230)
                            Cv2.Threshold(gray, brightMask, 230, 255, ThresholdTypes.Binary);
                            
                            // Combine color and brightness masks
                            using (var combinedMask = new Mat())
                            {
                                Cv2.BitwiseAnd(colorMask, brightMask, combinedMask);
                                
                                // If we have a circle center, apply circular mask
                                if (circleCenter.HasValue && circleRadius > 0)
                                {
                                    using (var circleMask = new Mat(combinedMask.Size(), MatType.CV_8UC1, new Scalar(0)))
                                    {
                                        Cv2.Circle(circleMask, circleCenter.Value, circleRadius, new Scalar(255), -1);
                                        Cv2.BitwiseAnd(combinedMask, circleMask, combinedMask);
                                    }
                                }
                                
                                // Find the brightest point within the combined mask
                                double minVal, maxVal;
                                OpenCvSharp.Point minLoc, maxLoc;
                                Cv2.MinMaxLoc(gray, out minVal, out maxVal, out minLoc, out maxLoc, combinedMask);
                                
                                // Check if any pixel was found in the mask
                                if (maxVal > 0)
                                {
                                    return maxLoc;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    colorMask.Dispose();
                }
            }
            
            return null;
        }

        static (Scalar lower, Scalar upper) GetColorRange(LightColor color)
        {
            // HSV ranges for different colors
            // H: 0-180, S: 0-255, V: 0-255 in OpenCV
            // Note: Higher saturation and value thresholds for stricter, brighter color detection
            return color switch
            {
                // Red is handled separately in FindBrightestPointWithColor due to hue wraparound
                LightColor.Green => (new Scalar(50, 50, 50), new Scalar(80, 255, 255)),    // Green - stricter
                LightColor.Blue => (new Scalar(100, 80, 150), new Scalar(130, 255, 255)),   // Blue - stricter
                LightColor.Yellow => (new Scalar(20, 100, 180), new Scalar(40, 255, 255)),  // Yellow - stricter
                LightColor.Cyan => (new Scalar(80, 80, 150), new Scalar(100, 255, 255)),    // Cyan - stricter
                LightColor.Magenta => (new Scalar(140, 80, 150), new Scalar(170, 255, 255)),// Magenta - stricter
                LightColor.White => (new Scalar(0, 0, 230), new Scalar(180, 30, 255)),      // White (low saturation, high value) - stricter
                _ => (new Scalar(0, 0, 230), new Scalar(180, 255, 255))                     // Any bright (default) - stricter
            };
        }

        static Scalar GetColorAtPoint(Mat frame, OpenCvSharp.Point point)
        {
            // Ensure the point is within the frame bounds
            if (point.X >= 0 && point.X < frame.Width && point.Y >= 0 && point.Y < frame.Height)
            {
                // Get the color value at the specified point (BGR format in OpenCV)
                // Note: OpenCV uses row-major indexing (row, column) so Y comes before X
                Vec3b color = frame.At<Vec3b>(point.Y, point.X);
                return new Scalar(color.Item0, color.Item1, color.Item2);
            }
            return new Scalar(0, 0, 0);
        }

        static double CalculateColorDifference(Scalar color1, Scalar color2)
        {
            // Calculate Euclidean distance in BGR color space
            // Note: BGR color space is not perceptually uniform. For better results matching
            // human perception, consider using CIE Lab or CIE Delta E color spaces.
            double bDiff = color1.Val0 - color2.Val0;
            double gDiff = color1.Val1 - color2.Val1;
            double rDiff = color1.Val2 - color2.Val2;
            return Math.Sqrt(bDiff * bDiff + gDiff * gDiff + rDiff * rDiff);
        }

        static double CalculateDistance(OpenCvSharp.Point p1, OpenCvSharp.Point p2)
        {
            // Calculate Euclidean distance between two points
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        static void ExportDrawingToPng(List<OpenCvSharp.Point> points, int width, int height, string filename)
        {
            // Create a black canvas with the same dimensions as the camera frame
            using (var image = new Image<Rgba32>(width, height))
            {
                // Fill with black background
                image.Mutate(ctx => ctx.BackgroundColor(new Rgba32(0, 0, 0)));

                if (points.Count > 0)
                {
                    // Draw lines connecting consecutive points (yellow lines)
                    image.Mutate(ctx =>
                    {
                        for (int i = 1; i < points.Count; i++)
                        {
                            var p1 = new PointF(points[i - 1].X, points[i - 1].Y);
                            var p2 = new PointF(points[i].X, points[i].Y);
                            ctx.DrawLine(new Rgba32(255, 255, 0), 2, p1, p2);
                        }
                    });

                    // Draw all points (cyan circles)
                    image.Mutate(ctx =>
                    {
                        foreach (var point in points)
                        {
                            var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(point.X, point.Y), 3);
                            ctx.Fill(new Rgba32(0, 255, 255), circle);
                        }
                    });
                }

                // Save the image
                image.Save(filename);
            }
        }

        /// <summary>
        /// Creates a Mat image from tracked points for real-time shape detection.
        /// </summary>
        static Mat CreateMatFromPoints(List<OpenCvSharp.Point> points, int width, int height)
        {
            // Create a black canvas
            var mat = new Mat(height, width, MatType.CV_8UC1, new Scalar(0));

            if (points.Count > 1)
            {
                // Draw lines connecting consecutive points (white lines on black background)
                for (int i = 1; i < points.Count; i++)
                {
                    Cv2.Line(mat, points[i - 1], points[i], new Scalar(255), 3);
                }

                // Draw all points as small circles
                foreach (var point in points)
                {
                    Cv2.Circle(mat, point, 2, new Scalar(255), -1);
                }
            }

            return mat;
        }

        /// <summary>
        /// Applies outlier detection to a list of points if enabled.
        /// Returns the filtered list of points and logs statistics if outliers were removed.
        /// </summary>
        static List<OpenCvSharp.Point> ApplyOutlierDetectionIfEnabled(List<OpenCvSharp.Point> points, bool outlierDetectionEnabled)
        {
            if (outlierDetectionEnabled && points.Count >= 4)
            {
                var originalCount = points.Count;
                var filteredPoints = OutlierDetection.RemoveOutliersHybrid(points);
                if (filteredPoints.Count < originalCount)
                {
                    Console.WriteLine($"Outlier detection: Removed {originalCount - filteredPoints.Count} outlier(s) from {originalCount} points");
                }
                return filteredPoints;
            }
            return points;
        }

        static void ExportPointsToCsv(List<OpenCvSharp.Point> points, string filename)
        {
            // Create CSV file with point data
            using (var writer = new StreamWriter(filename))
            {
                // Write header
                writer.WriteLine("Index,X,Y");
                
                // Write each point
                for (int i = 0; i < points.Count; i++)
                {
                    writer.WriteLine($"{i},{points[i].X},{points[i].Y}");
                }
            }
        }

        static void CaptureCircularScreenshot(Mat frame, OpenCvSharp.Point center, int radius, string filename)
        {
            // Calculate the bounding box for the circular region
            int x = Math.Max(0, center.X - radius);
            int y = Math.Max(0, center.Y - radius);
            int width = Math.Min(frame.Width - x, radius * 2);
            int height = Math.Min(frame.Height - y, radius * 2);
            
            // Extract the region of interest
            var roi = new Rect(x, y, width, height);
            using (var croppedFrame = new Mat(frame, roi))
            {
                // Convert OpenCV Mat to ImageSharp Image
                var bytes = croppedFrame.ToBytes(".png");
                using (var image = Image.Load<Rgba32>(bytes))
                {
                    // Create a mask for the circular region
                    // Calculate the center of the cropped image relative to the original circle center
                    int circleCenterX = center.X - x;
                    int circleCenterY = center.Y - y;
                    
                    // Apply circular mask - make pixels outside the circle transparent
                    image.ProcessPixelRows(accessor =>
                    {
                        for (int py = 0; py < accessor.Height; py++)
                        {
                            var pixelRow = accessor.GetRowSpan(py);
                            for (int px = 0; px < pixelRow.Length; px++)
                            {
                                // Calculate distance from the circle center
                                int dx = px - circleCenterX;
                                int dy = py - circleCenterY;
                                double distance = Math.Sqrt(dx * dx + dy * dy);
                                
                                // If outside the circle, make pixel transparent
                                if (distance > radius)
                                {
                                    pixelRow[px] = new Rgba32(0, 0, 0, 0);
                                }
                            }
                        }
                    });
                    
                    // Save the circular screenshot
                    image.Save(filename);
                }
            }
            
            // After saving, perform symbol detection using enhanced detector
            DetectAndRecordSymbolsEnhanced(filename);
        }

        static void UpdateSymbolsCsv(string imageFilename, List<(string symbolName, double confidence)> detectedSymbols)
        {
            string csvFilename = "detected_symbols.csv";
            bool fileExists = File.Exists(csvFilename);
            
            try
            {
                using (var writer = new StreamWriter(csvFilename, append: true))
                {
                    // Write header if file doesn't exist
                    if (!fileExists)
                    {
                        writer.WriteLine("Timestamp,ImageFilename,DetectedSymbols,Confidence,Count");
                    }
                    
                    // Prepare the CSV row
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string symbolsList = detectedSymbols.Count > 0 
                        ? string.Join(";", detectedSymbols.Select(s => s.symbolName))
                        : "None";
                    string confidenceList = detectedSymbols.Count > 0
                        ? string.Join(";", detectedSymbols.Select(s => s.confidence.ToString("F3")))
                        : "N/A";
                    int count = detectedSymbols.Count;
                    
                    // Write the data
                    writer.WriteLine($"{timestamp},{imageFilename},{symbolsList},{confidenceList},{count}");
                }
                
                if (detectedSymbols.Count > 0)
                {
                    Console.WriteLine($"Detected {detectedSymbols.Count} symbol(s): {string.Join(", ", detectedSymbols.Select(s => $"{s.symbolName} ({s.confidence:F2})"))}");
                }
                else
                {
                    Console.WriteLine("No symbols detected in the image.");
                }
                Console.WriteLine($"Symbol detection results saved to: {csvFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating symbols CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced symbol detection using geometric shape analysis.
        /// </summary>
        public static void DetectAndRecordSymbolsEnhanced(string imageFilename)
        {
            try
            {
                Console.WriteLine($"\n=== Enhanced Shape Detection ===");
                
                var (shape, confidence) = EnhancedShapeDetector.DetectShape(imageFilename);
                
                Console.WriteLine($"Enhanced detection: {shape} ({confidence:F3})");
                
                if (confidence > MIN_SAVED_CONFIDENCE)
                {
                    var detectedSymbols = new List<(string symbolName, double confidence)>
                    {
                        (shape, confidence)
                    };
                    UpdateSymbolsCsv(imageFilename, detectedSymbols);
                }
                else
                {
                    Console.WriteLine("Confidence too low; skipping save.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in enhanced detection: {ex.Message}");
            }
        }

        /// <summary>
        /// Test the enhanced shape detector with generated symbols.
        /// </summary>
        static void TestEnhancedShapeDetector()
        {
            Console.WriteLine("Enhanced Shape Detector Test");
            Console.WriteLine("============================\n");

            // Check if symbols exist
            if (!Directory.Exists("symbols"))
            {
                Console.WriteLine("Generating test symbols...");
                SymbolTemplateGenerator.GenerateExampleTemplates();
            }

            // Test each shape type
            var testFiles = new Dictionary<string, string>
            {
                { "Circle", "symbols/circle/circle_1.png" },
                { "Square", "symbols/square/square_1.png" },
                { "Triangle", "symbols/triangle/triangle_1.png" },
                { "Star", "symbols/star/star_1.png" }
            };

            Console.WriteLine("Testing shape detection accuracy:\n");

            foreach (var (expectedShape, filePath) in testFiles)
            {
                if (File.Exists(filePath))
                {
                    var (detectedShape, confidence) = EnhancedShapeDetector.DetectShape(filePath);
                    var match = detectedShape.ToLower().Contains(expectedShape.ToLower()) ? "✓" : "✗";
                    
                    Console.WriteLine($"{match} {expectedShape,-10} -> Detected: {detectedShape,-12} (confidence: {confidence:F3})");
                    
                    // Show detailed analysis for first shape
                    if (expectedShape == "Circle")
                    {
                        Console.WriteLine("\nDetailed analysis for circle:");
                        Console.WriteLine(EnhancedShapeDetector.GetShapeAnalysisDetails(filePath));
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine($"✗ {expectedShape,-10} -> File not found: {filePath}");
                }
            }

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("Comparison: Enhanced vs Template Matching");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("\nEnhanced Detector Advantages:");
            Console.WriteLine("✓ Rotation invariant - works with tilted shapes");
            Console.WriteLine("✓ Scale invariant - works with different sizes");
            Console.WriteLine("✓ Better for hand-drawn shapes");
            Console.WriteLine("✓ Works without training images");
            Console.WriteLine("✓ Analyzes geometric properties");
            Console.WriteLine("\nTemplate Matching Advantages:");
            Console.WriteLine("✓ Can learn custom symbols from examples");
            Console.WriteLine("✓ Good for very specific symbol patterns");
            Console.WriteLine("✓ Can match exact templates");
            
            Console.WriteLine("\nRecommendation: Use enhanced detector for hand-drawn shapes!");
        }

        static void GenerateSampleImage(string outputPath)
        {
            // Create a sample image with bright spots
            int width = 640;
            int height = 480;
            
            using (var image = new Image<Rgba32>(width, height))
            {
                // Fill with dark background
                image.Mutate(ctx => ctx.BackgroundColor(new Rgba32(30, 30, 30)));

                // Add some bright light spots (white/yellow)
                image.Mutate(ctx =>
                {
                    ctx.Fill(new Rgba32(255, 255, 0), new RectangleF(135, 135, 30, 30)); // Yellow circle approx
                    ctx.Fill(new Rgba32(255, 255, 255), new RectangleF(387, 187, 26, 26)); // White
                    ctx.Fill(new Rgba32(255, 255, 200), new RectangleF(290, 340, 20, 20)); // Light yellow
                    ctx.Fill(new Rgba32(200, 255, 255), new RectangleF(482, 82, 36, 36)); // Light cyan
                    ctx.Fill(new Rgba32(255, 255, 255), new RectangleF(85, 385, 30, 30)); // White
                });

                image.Save(outputPath);
                Console.WriteLine($"Sample image generated: {outputPath}\n");
            }
        }

        static void ProcessImage(string imagePath)
        {
            Console.WriteLine($"Processing image: {imagePath}");

            using (var image = Image.Load<Rgba32>(imagePath))
            {
                Console.WriteLine($"Image size: {image.Width}x{image.Height}");

                // Detect bright regions
                var brightRegions = DetectBrightRegions(image);

                Console.WriteLine($"Found {brightRegions.Count} bright light region(s)");

                // Create output images
                var outputMap = image.Clone();
                var pointMap = new Image<Rgba32>(image.Width, image.Height);
                pointMap.Mutate(ctx => ctx.BackgroundColor(new Rgba32(0, 0, 0)));

                int detectionCount = 0;
                var centerPoints = new List<(int x, int y)>();
                
                foreach (var region in brightRegions)
                {
                    detectionCount++;
                    centerPoints.Add((region.CenterX, region.CenterY));

                    Console.WriteLine($"  Light #{detectionCount}:");
                    Console.WriteLine($"    Position: ({region.CenterX}, {region.CenterY})");
                    Console.WriteLine($"    Area: {region.PixelCount} pixels");
                    Console.WriteLine($"    Bounding Box: ({region.MinX}, {region.MinY}) - {region.MaxX - region.MinX}x{region.MaxY - region.MinY}");

                    // Draw on output map
                    outputMap.Mutate(ctx =>
                    {
                        // Draw bounding box
                        var rect = new RectangleF(region.MinX, region.MinY, region.MaxX - region.MinX, region.MaxY - region.MinY);
                        ctx.Draw(new Rgba32(255, 0, 0), 2, rect);

                        // Draw center point with a small circle
                        var centerCircle = new RectangleF(region.CenterX - 5, region.CenterY - 5, 10, 10);
                        ctx.Fill(new Rgba32(0, 0, 255), centerCircle);
                    });

                    // Draw on point map
                    pointMap.Mutate(ctx =>
                    {
                        var pointCircle = new RectangleF(region.CenterX - 10, region.CenterY - 10, 20, 20);
                        ctx.Fill(new Rgba32(0, 255, 255), pointCircle);
                    });
                }
                
                // Draw lines connecting consecutive points
                if (centerPoints.Count > 1)
                {
                    outputMap.Mutate(ctx =>
                    {
                        for (int i = 1; i < centerPoints.Count; i++)
                        {
                            var p1 = new PointF(centerPoints[i - 1].x, centerPoints[i - 1].y);
                            var p2 = new PointF(centerPoints[i].x, centerPoints[i].y);
                            ctx.DrawLine(new Rgba32(0, 255, 0), 2, p1, p2);
                        }
                    });
                    
                    pointMap.Mutate(ctx =>
                    {
                        for (int i = 1; i < centerPoints.Count; i++)
                        {
                            var p1 = new PointF(centerPoints[i - 1].x, centerPoints[i - 1].y);
                            var p2 = new PointF(centerPoints[i].x, centerPoints[i].y);
                            ctx.DrawLine(new Rgba32(255, 255, 0), 2, p1, p2);
                        }
                    });
                }

                // Save outputs
                string baseName = Path.GetFileNameWithoutExtension(imagePath);
                string? outputDir = Path.GetDirectoryName(imagePath);
                if (string.IsNullOrEmpty(outputDir))
                    outputDir = ".";

                string outputPath = Path.Combine(outputDir, $"{baseName}_detected.png");
                string pointMapPath = Path.Combine(outputDir, $"{baseName}_point_map.png");

                outputMap.Save(outputPath);
                pointMap.Save(pointMapPath);

                Console.WriteLine($"\nOutput files created:");
                Console.WriteLine($"  Detection overlay: {outputPath}");
                Console.WriteLine($"  Point map: {pointMapPath}");

                outputMap.Dispose();
                pointMap.Dispose();
            }

            Console.WriteLine("\nProcessing complete!");
        }

        static List<BrightRegion> DetectBrightRegions(Image<Rgba32> image)
        {
            var regions = new List<BrightRegion>();
            var visited = new bool[image.Width, image.Height];
            int brightnessThreshold = 230; // Threshold for considering a pixel as "bright"
            int maxSaturationForWhite = 80; // Maximum saturation for bright white lights (reduced from 100 for stricter detection)

            // Scan the image for bright pixels
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (visited[x, y])
                        continue;

                    var pixel = image[x, y];
                    
                    // Check if pixel is bright (high RGB values, low saturation difference)
                    int maxChannel = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B);
                    int minChannel = Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
                    int saturation = maxChannel - minChannel;

                    if (maxChannel >= brightnessThreshold && saturation < maxSaturationForWhite)
                    {
                        // Found a bright pixel, flood fill to find the entire region
                        var region = FloodFill(image, visited, x, y, brightnessThreshold, maxSaturationForWhite);
                        
                        // Filter out small regions (noise)
                        if (region.PixelCount >= 50)
                        {
                            regions.Add(region);
                        }
                    }
                }
            }

            return regions;
        }

        static BrightRegion FloodFill(Image<Rgba32> image, bool[,] visited, int startX, int startY, int brightnessThreshold, int maxSaturationForWhite)
        {
            var region = new BrightRegion();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                region.AddPixel(x, y);

                // Check 4-connected neighbors
                foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < image.Width && ny >= 0 && ny < image.Height && !visited[nx, ny])
                    {
                        var pixel = image[nx, ny];
                        int maxChannel = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B);
                        int minChannel = Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
                        int saturation = maxChannel - minChannel;

                        if (maxChannel >= brightnessThreshold && saturation < maxSaturationForWhite)
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            region.CalculateCenter();
            return region;
        }
    }

    class BrightRegion
    {
        public int MinX { get; private set; } = int.MaxValue;
        public int MaxX { get; private set; } = int.MinValue;
        public int MinY { get; private set; } = int.MaxValue;
        public int MaxY { get; private set; } = int.MinValue;
        public int PixelCount { get; private set; }
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }

        private int sumX = 0;
        private int sumY = 0;

        public void AddPixel(int x, int y)
        {
            MinX = Math.Min(MinX, x);
            MaxX = Math.Max(MaxX, x);
            MinY = Math.Min(MinY, y);
            MaxY = Math.Max(MaxY, y);
            sumX += x;
            sumY += y;
            PixelCount++;
        }

        public void CalculateCenter()
        {
            if (PixelCount > 0)
            {
                CenterX = sumX / PixelCount;
                CenterY = sumY / PixelCount;
            }
        }
    }

    public static class NetworkManager
    {
        private static readonly UdpClient udpClient = new UdpClient();
        private const string DefaultTailScaleHost = "100.66.230.53";
        private const int DefaultTailScalePort = 5555;
        private static readonly IPEndPoint endPoint = CreateTailScaleEndpoint();

       
    
        public static short CurrentSpellShapeID { get; private set; } = 0;
        public static int PacketOffset = 0;
    
        public static void GenerateNewSpellShapeID()
        {
            long timestamp = DateTime.UtcNow.Ticks;
            byte[] timeBytes = BitConverter.GetBytes(timestamp);
    
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(timeBytes);
                // Truncate SHA256 to 2 bytes (Short) as per spec
                CurrentSpellShapeID = BitConverter.ToInt16(hash, 0);
            }
            PacketOffset = 0;
            Console.WriteLine($">>> NEW SPELL SHAPE ID GENERATED: {CurrentSpellShapeID}");
        }

        private static IPEndPoint CreateTailScaleEndpoint()
        {
            string? host = Environment.GetEnvironmentVariable("TAILSCALE_HOST");
            string? portText = Environment.GetEnvironmentVariable("TAILSCALE_PORT");

            if (string.IsNullOrWhiteSpace(host))
            {
                host = DefaultTailScaleHost;
            }

            int port = DefaultTailScalePort;
            if (!string.IsNullOrWhiteSpace(portText) && int.TryParse(portText, out int parsedPort))
            {
                port = parsedPort;
            }

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            Console.WriteLine($"[NET] Tailscale UDP target: {endpoint.Address}:{endpoint.Port}");
            return endpoint;
        }
    
        public static void SendStreamPacket(float x, float y, bool isDrawing)
        {
            List<byte> packet = new List<byte>();
            packet.Add(0); // Type 0: Stream
            packet.AddRange(BitConverter.GetBytes(CurrentSpellShapeID));
            packet.AddRange(BitConverter.GetBytes(PacketOffset));
            packet.AddRange(BitConverter.GetBytes(x));
            packet.AddRange(BitConverter.GetBytes(y));
            packet.Add(isDrawing ? (byte)1 : (byte)0);
    
            Send(packet.ToArray());
            PacketOffset++;
        }
    
        // UPDATED: Added frameWidth so we can do math inside here
        public static void SendActionPacket(string shapeName, List<OpenCvSharp.Point> points, double frameWidth, TrackingMode CurrentMode)
        {
            byte classId = 0;
            byte laneId = 3; // Default to No Lane
            string cleanName = shapeName.ToLower().Trim();
            Console.WriteLine($"Raw Shape Name: {shapeName}");
    
            if (cleanName == "line") 
            {
                classId = 5; // "Cast" Motion
                
                // 2. Override laneId based on FLICK direction
                var start = points.First();
                var end = points.Last();
                double angle = Math.Atan2(end.Y - start.Y, end.X - start.X) * 180 / Math.PI;
                Console.WriteLine($"the angle is {angle}");
                laneId = angle switch {
                    < -115 and > -165 => (byte)1, // Flick Left-Up
                    <= -65 and >= -115 => (byte)2, // Flick Straight-Up
                    < -15 and >= -65   => (byte)3, // Flick Right-Up
                    _ => (byte)0                  // Default / No Lane (The catch-all)
                };
            } 
            else 
            {
                // 3. Ingredient Logic
                classId = cleanName switch {
                    "circle"    => 1,
                    "triangle"  => 2,
                    "rectangle" => 3,
                    "square"    => 3,
                    "star"      => 4,
                    _           => 0
                };
            }
    
            // if (classId == 0) return;
    
            List<byte> packet = new List<byte>();
            packet.Add(1); // Type 1: Action
            packet.AddRange(BitConverter.GetBytes(CurrentSpellShapeID));
            packet.Add(classId);
            packet.Add(laneId);
    
            byte[] data = packet.ToArray();
            // Redundancy spam for UDP
            for (int i = 0; i < 20; i++) { Send(data); }
        
        }
    
        private static void Send(byte[] data)
        {
            try
            {
                udpClient.Send(data, data.Length, endPoint);
            }
            catch {  }
        }
    }
}
