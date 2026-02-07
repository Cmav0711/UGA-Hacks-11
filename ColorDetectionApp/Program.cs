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

namespace ColorDetectionApp
{
    // Enum for predefined color options
    enum LightColor
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

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bright Light Color Detection and Mapping");
            Console.WriteLine("========================================\n");

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
            
            // Track the last time light was detected
            DateTime? lastLightDetectedTime = null;
            bool imageExported = false;
            
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

                using (var frame = new Mat())
                using (var window = new Window($"Brightest Point Tracker ({targetColor}) - Press 'q' to quit, 'c' to clear, 'b' to calibrate, '+/-' for radius, '[/]' for timeout"))
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

                        // Find the brightest point in this frame
                        // If we have previous points, only search within the tracking circle
                        var searchCenter = brightestPoints.Count > 0 
                            ? brightestPoints[brightestPoints.Count - 1] 
                            : (OpenCvSharp.Point?)null;
                        var brightestPoint = FindBrightestPointWithColor(frame, targetColor, searchCenter, trackingRadius);
                        
                        if (brightestPoint.HasValue)
                        {
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
                        }
                        
                        // Check if light has not been detected for the configured timeout
                        if (lastLightDetectedTime.HasValue && 
                            brightestPoints.Count > 0 && 
                            !imageExported &&
                            (DateTime.Now - lastLightDetectedTime.Value).TotalSeconds >= noLightTimeout)
                        {
                            // Export the drawing to PNG and CSV
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string pngFilename = $"light_drawing_{timestamp}.png";
                            string csvFilename = $"light_drawing_{timestamp}.csv";
                            ExportDrawingToPng(brightestPoints, (int)capture.FrameWidth, (int)capture.FrameHeight, pngFilename);
                            ExportPointsToCsv(brightestPoints, csvFilename);
                            Console.WriteLine($"\nNo light detected for {noLightTimeout}s - Drawing exported to: {pngFilename}");
                            Console.WriteLine($"Points data exported to: {csvFilename}");
                            imageExported = true;
                            
                            // Clear all points after export
                            brightestPoints.Clear();
                            Console.WriteLine("All points cleared after export");
                        }

                        // Draw lines connecting consecutive points
                        for (int i = 1; i < brightestPoints.Count; i++)
                        {
                            Cv2.Line(frame, brightestPoints[i - 1], brightestPoints[i], 
                                    new Scalar(255, 128, 0), 2);
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

                        // Display frame count and point count
                        string info = $"Points tracked: {brightestPoints.Count} | Tracking radius: {trackingRadius}px | Timeout: {noLightTimeout:F1}s";
                        if (calibratedColor.HasValue)
                        {
                            info += " [CALIBRATED]";
                        }
                        Cv2.PutText(frame, info, new OpenCvSharp.Point(10, 30), 
                                   HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 255, 255), 2);

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
                            Console.WriteLine($"No-light timeout decreased to {noLightTimeout:F1}s");
                        }
                        else if (key == ']' || key == '}')
                        {
                            noLightTimeout += 0.5;
                            if (noLightTimeout > 30.0) noLightTimeout = 30.0;
                            Console.WriteLine($"No-light timeout increased to {noLightTimeout:F1}s");
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
                    if (maxVal >= 200)
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
                        Cv2.InRange(hsvFrame, new Scalar(0, 70, 100), new Scalar(10, 255, 255), lowerRedMask);
                        Cv2.InRange(hsvFrame, new Scalar(170, 70, 100), new Scalar(180, 255, 255), upperRedMask);
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
                            // Create mask for bright pixels (>= 200)
                            Cv2.Threshold(gray, brightMask, 200, 255, ThresholdTypes.Binary);
                            
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
            // Note: Lower saturation thresholds allow detection of dimmer/desaturated lights
            return color switch
            {
                // Red is handled separately in FindBrightestPointWithColor due to hue wraparound
                LightColor.Green => (new Scalar(40, 30, 50), new Scalar(80, 255, 255)),    // Green
                LightColor.Blue => (new Scalar(100, 30, 50), new Scalar(130, 255, 255)),   // Blue
                LightColor.Yellow => (new Scalar(20, 70, 100), new Scalar(40, 255, 255)),  // Yellow
                LightColor.Cyan => (new Scalar(80, 30, 50), new Scalar(100, 255, 255)),    // Cyan
                LightColor.Magenta => (new Scalar(140, 30, 50), new Scalar(170, 255, 255)),// Magenta
                LightColor.White => (new Scalar(0, 0, 200), new Scalar(180, 30, 255)),     // White (low saturation, high value)
                _ => (new Scalar(0, 0, 200), new Scalar(180, 255, 255))                    // Any bright (default)
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
            int brightnessThreshold = 200; // Threshold for considering a pixel as "bright"

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

                    if (maxChannel >= brightnessThreshold && saturation < 100)
                    {
                        // Found a bright pixel, flood fill to find the entire region
                        var region = FloodFill(image, visited, x, y, brightnessThreshold);
                        
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

        static BrightRegion FloodFill(Image<Rgba32> image, bool[,] visited, int startX, int startY, int brightnessThreshold)
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

                        if (maxChannel >= brightnessThreshold && saturation < 100)
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
}
