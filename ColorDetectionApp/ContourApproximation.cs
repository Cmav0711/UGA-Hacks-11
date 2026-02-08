using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorDetectionApp
{
    /// <summary>
    /// Provides configurable contour approximation functionality with visualization support.
    /// Contour approximation reduces the number of points in a contour by approximating
    /// it with a polygon that has fewer vertices while maintaining shape characteristics.
    /// </summary>
    public class ContourApproximation
    {
        /// <summary>
        /// Approximates a contour with configurable epsilon parameter.
        /// </summary>
        /// <param name="contour">Input contour to approximate</param>
        /// <param name="epsilonFactor">Factor to multiply with perimeter (e.g., 0.01 = 1%, 0.05 = 5%)</param>
        /// <returns>Approximated contour points</returns>
        public static Point[] ApproximateContour(Point[] contour, double epsilonFactor = 0.04)
        {
            var perimeter = Cv2.ArcLength(contour, true);
            var epsilon = epsilonFactor * perimeter;
            var approx = Cv2.ApproxPolyDP(contour, epsilon, true);
            return approx;
        }

        /// <summary>
        /// Analyzes contour approximation at multiple epsilon levels and returns results.
        /// </summary>
        public static List<ContourApproximationResult> AnalyzeAtMultipleLevels(
            Point[] contour, 
            double[]? epsilonFactors = null)
        {
            if (epsilonFactors == null)
            {
                // Default epsilon factors: from very detailed (0.5%) to very simplified (10%)
                epsilonFactors = new[] { 0.005, 0.01, 0.02, 0.04, 0.06, 0.08, 0.10 };
            }

            var results = new List<ContourApproximationResult>();
            var perimeter = Cv2.ArcLength(contour, true);

            foreach (var factor in epsilonFactors)
            {
                var epsilon = factor * perimeter;
                var approx = Cv2.ApproxPolyDP(contour, epsilon, true);
                
                results.Add(new ContourApproximationResult
                {
                    EpsilonFactor = factor,
                    Epsilon = epsilon,
                    OriginalPoints = contour.Length,
                    ApproximatedPoints = approx.Length,
                    Contour = approx,
                    CompressionRatio = (double)approx.Length / contour.Length
                });
            }

            return results;
        }

        /// <summary>
        /// Visualizes contour approximation results by saving images at different epsilon levels.
        /// </summary>
        public static void VisualizeApproximationLevels(
            string imagePath, 
            string outputPrefix = "contour_approx",
            double[]? epsilonFactors = null)
        {
            try
            {
                using var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    Console.WriteLine("Error: Could not load image");
                    return;
                }

                // Preprocess
                using var blurred = new Mat();
                Cv2.GaussianBlur(image, blurred, new Size(5, 5), 0);

                using var thresh = new Mat();
                Cv2.AdaptiveThreshold(blurred, thresh, 255, 
                    AdaptiveThresholdTypes.GaussianC, 
                    ThresholdTypes.BinaryInv, 11, 2);

                // Find contours
                Cv2.FindContours(thresh, out var contours, out _, 
                    RetrievalModes.External, 
                    ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    Console.WriteLine("No contours found in image");
                    return;
                }

                // Get the largest contour
                var largestContour = contours
                    .OrderByDescending(c => Cv2.ContourArea(c))
                    .First();

                // Analyze at multiple levels
                var results = AnalyzeAtMultipleLevels(largestContour, epsilonFactors);

                Console.WriteLine("\nContour Approximation Analysis:");
                Console.WriteLine("================================");
                Console.WriteLine($"Original contour has {largestContour.Length} points");
                Console.WriteLine();

                // Create visualizations for each level
                foreach (var result in results)
                {
                    // Create colored output image
                    using var colorImage = new Mat();
                    Cv2.CvtColor(image, colorImage, ColorConversionCodes.GRAY2BGR);

                    // Draw original contour in blue (thin)
                    Cv2.DrawContours(colorImage, new[] { largestContour }, 0, 
                        new Scalar(255, 0, 0), 1);

                    // Draw approximated contour in green (thick)
                    Cv2.DrawContours(colorImage, new[] { result.Contour }, 0, 
                        new Scalar(0, 255, 0), 2);

                    // Draw vertices of approximated contour as red circles
                    foreach (var point in result.Contour)
                    {
                        Cv2.Circle(colorImage, point, 4, new Scalar(0, 0, 255), -1);
                    }

                    // Add text information
                    var infoText = $"Epsilon: {result.EpsilonFactor:F3} ({result.EpsilonFactor * 100:F1}%)";
                    var pointsText = $"Points: {result.OriginalPoints} -> {result.ApproximatedPoints}";
                    var ratioText = $"Ratio: {result.CompressionRatio:F3}";

                    Cv2.PutText(colorImage, infoText, new OpenCvSharp.Point(10, 30), 
                        HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 255, 255), 2);
                    Cv2.PutText(colorImage, pointsText, new OpenCvSharp.Point(10, 60), 
                        HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 255, 255), 2);
                    Cv2.PutText(colorImage, ratioText, new OpenCvSharp.Point(10, 90), 
                        HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 255, 255), 2);

                    // Save the visualization
                    var outputPath = $"{outputPrefix}_eps{result.EpsilonFactor:F3}.png";
                    Cv2.ImWrite(outputPath, colorImage);

                    Console.WriteLine($"Epsilon Factor: {result.EpsilonFactor:F3} ({result.EpsilonFactor * 100:F1}%)");
                    Console.WriteLine($"  Epsilon Value: {result.Epsilon:F2}");
                    Console.WriteLine($"  Original Points: {result.OriginalPoints}");
                    Console.WriteLine($"  Approximated Points: {result.ApproximatedPoints}");
                    Console.WriteLine($"  Compression Ratio: {result.CompressionRatio:F3} ({result.CompressionRatio * 100:F1}%)");
                    Console.WriteLine($"  Saved: {outputPath}");
                    Console.WriteLine();
                }

                Console.WriteLine($"Generated {results.Count} visualization images");
                Console.WriteLine("Blue = original contour, Green = approximated, Red = vertices");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error visualizing contour approximation: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides interactive contour approximation testing with real-time parameter adjustment.
        /// </summary>
        public static void InteractiveApproximation(string imagePath)
        {
            try
            {
                using var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    Console.WriteLine("Error: Could not load image");
                    return;
                }

                // Preprocess
                using var blurred = new Mat();
                Cv2.GaussianBlur(image, blurred, new Size(5, 5), 0);

                using var thresh = new Mat();
                Cv2.AdaptiveThreshold(blurred, thresh, 255, 
                    AdaptiveThresholdTypes.GaussianC, 
                    ThresholdTypes.BinaryInv, 11, 2);

                // Find contours
                Cv2.FindContours(thresh, out var contours, out _, 
                    RetrievalModes.External, 
                    ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    Console.WriteLine("No contours found in image");
                    return;
                }

                var largestContour = contours
                    .OrderByDescending(c => Cv2.ContourArea(c))
                    .First();

                double epsilonFactor = 0.04; // Default value
                const string windowName = "Contour Approximation - Use +/- to adjust epsilon, Q to quit";

                Console.WriteLine("\nInteractive Contour Approximation Mode");
                Console.WriteLine("======================================");
                Console.WriteLine("Controls:");
                Console.WriteLine("  '+' or '=' : Increase epsilon (less detail)");
                Console.WriteLine("  '-' : Decrease epsilon (more detail)");
                Console.WriteLine("  'r' : Reset to default (0.04)");
                Console.WriteLine("  's' : Save current visualization");
                Console.WriteLine("  'q' or ESC : Quit");
                Console.WriteLine();

                Cv2.NamedWindow(windowName, WindowFlags.Normal);

                while (true)
                {
                    // Create visualization
                    using var colorImage = new Mat();
                    Cv2.CvtColor(image, colorImage, ColorConversionCodes.GRAY2BGR);

                    var approx = ApproximateContour(largestContour, epsilonFactor);

                    // Draw original contour in blue
                    Cv2.DrawContours(colorImage, new[] { largestContour }, 0, 
                        new Scalar(255, 0, 0), 1);

                    // Draw approximated contour in green
                    Cv2.DrawContours(colorImage, new[] { approx }, 0, 
                        new Scalar(0, 255, 0), 2);

                    // Draw vertices
                    foreach (var point in approx)
                    {
                        Cv2.Circle(colorImage, point, 4, new Scalar(0, 0, 255), -1);
                    }

                    // Add info
                    var perimeter = Cv2.ArcLength(largestContour, true);
                    var epsilon = epsilonFactor * perimeter;
                    
                    Cv2.PutText(colorImage, $"Epsilon Factor: {epsilonFactor:F3} ({epsilonFactor * 100:F1}%)", 
                        new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 0.7, 
                        new Scalar(255, 255, 255), 2);
                    Cv2.PutText(colorImage, $"Epsilon Value: {epsilon:F2}", 
                        new OpenCvSharp.Point(10, 60), HersheyFonts.HersheySimplex, 0.7, 
                        new Scalar(255, 255, 255), 2);
                    Cv2.PutText(colorImage, $"Points: {largestContour.Length} -> {approx.Length}", 
                        new OpenCvSharp.Point(10, 90), HersheyFonts.HersheySimplex, 0.7, 
                        new Scalar(255, 255, 255), 2);
                    Cv2.PutText(colorImage, $"Compression: {(double)approx.Length / largestContour.Length:F3}", 
                        new OpenCvSharp.Point(10, 120), HersheyFonts.HersheySimplex, 0.7, 
                        new Scalar(255, 255, 255), 2);

                    Cv2.ImShow(windowName, colorImage);

                    var key = Cv2.WaitKey(50);
                    if (key == 'q' || key == 27) // 'q' or ESC
                    {
                        break;
                    }
                    else if (key == '+' || key == '=')
                    {
                        epsilonFactor = Math.Min(epsilonFactor + 0.005, 0.20);
                        Console.WriteLine($"Epsilon factor: {epsilonFactor:F3} -> {approx.Length} points");
                    }
                    else if (key == '-')
                    {
                        epsilonFactor = Math.Max(epsilonFactor - 0.005, 0.001);
                        Console.WriteLine($"Epsilon factor: {epsilonFactor:F3} -> {approx.Length} points");
                    }
                    else if (key == 'r')
                    {
                        epsilonFactor = 0.04;
                        Console.WriteLine($"Reset to default epsilon factor: {epsilonFactor:F3}");
                    }
                    else if (key == 's')
                    {
                        var outputPath = $"contour_approx_interactive_eps{epsilonFactor:F3}.png";
                        Cv2.ImWrite(outputPath, colorImage);
                        Console.WriteLine($"Saved current visualization to: {outputPath}");
                    }
                }

                Cv2.DestroyAllWindows();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in interactive approximation: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets detailed information about contour approximation for a given image.
        /// </summary>
        public static string GetApproximationInfo(string imagePath, double epsilonFactor = 0.04)
        {
            try
            {
                using var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    return "Error: Could not load image";
                }

                using var blurred = new Mat();
                Cv2.GaussianBlur(image, blurred, new Size(5, 5), 0);

                using var thresh = new Mat();
                Cv2.AdaptiveThreshold(blurred, thresh, 255, 
                    AdaptiveThresholdTypes.GaussianC, 
                    ThresholdTypes.BinaryInv, 11, 2);

                Cv2.FindContours(thresh, out var contours, out _, 
                    RetrievalModes.External, 
                    ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    return "No contours found in image";
                }

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                var perimeter = Cv2.ArcLength(largestContour, true);
                var epsilon = epsilonFactor * perimeter;
                var approx = Cv2.ApproxPolyDP(largestContour, epsilon, true);
                
                var area = Cv2.ContourArea(largestContour);
                var approxArea = Cv2.ContourArea(approx);
                var areaPreservation = (approxArea / area) * 100;

                return $@"Contour Approximation Analysis:
- Original Points: {largestContour.Length}
- Approximated Points: {approx.Length}
- Compression Ratio: {(double)approx.Length / largestContour.Length:F3} ({(double)approx.Length / largestContour.Length * 100:F1}%)
- Epsilon Factor: {epsilonFactor:F3} ({epsilonFactor * 100:F1}%)
- Epsilon Value: {epsilon:F2} pixels
- Perimeter: {perimeter:F1} pixels
- Original Area: {area:F1} pixels²
- Approximated Area: {approxArea:F1} pixels²
- Area Preservation: {areaPreservation:F1}%";
            }
            catch (Exception ex)
            {
                return $"Error analyzing contour approximation: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Represents the result of a contour approximation operation.
    /// </summary>
    public class ContourApproximationResult
    {
        public double EpsilonFactor { get; set; }
        public double Epsilon { get; set; }
        public int OriginalPoints { get; set; }
        public int ApproximatedPoints { get; set; }
        public Point[] Contour { get; set; } = Array.Empty<Point>();
        public double CompressionRatio { get; set; }
    }
}
