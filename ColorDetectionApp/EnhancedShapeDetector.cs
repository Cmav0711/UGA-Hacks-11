using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorDetectionApp
{
    /// <summary>
    /// Enhanced shape detector using contour analysis and geometric features.
    /// Much more robust than template matching for hand-drawn shapes.
    /// </summary>
    public class EnhancedShapeDetector
    {
        /// <summary>
        /// Detects shapes in an image using contour analysis and geometric properties.
        /// This method is rotation-invariant, scale-invariant, and works well with
        /// hand-drawn or messy shapes.
        /// Note: This method discards the contour for backward compatibility with existing code.
        /// Use DetectShapeFromMat() directly if you need contour information.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="epsilonFactor">Optional contour approximation epsilon factor (default: 0.04)</param>
        public static (string shape, double confidence) DetectShape(string imagePath, double epsilonFactor = 0.04)
        {
            try
            {
                using var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    return ("unknown", 0.0);
                }

                var (shape, confidence, _) = DetectShapeFromMat(image, epsilonFactor);
                return (shape, confidence);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in enhanced shape detection: {ex.Message}");
                return ("error", 0.0);
            }
        }

        /// <summary>
        /// Detects shapes from a Mat object for real-time detection.
        /// This method is rotation-invariant, scale-invariant, and works well with
        /// hand-drawn or messy shapes.
        /// </summary>
        /// <param name="image">Input image (can be color or grayscale)</param>
        /// <param name="epsilonFactor">Optional contour approximation epsilon factor (default: 0.04)</param>
        public static (string shape, double confidence, Point[] contour) DetectShapeFromMat(Mat image, double epsilonFactor = 0.04)
        {
            try
            {
                if (image.Empty())
                {
                    return ("unknown", 0.0, Array.Empty<Point>());
                }

                // Convert to grayscale if needed
                Mat gray;
                if (image.Channels() > 1)
                {
                    gray = new Mat();
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    gray = image;
                }

                // Preprocess: Apply Gaussian blur to reduce noise
                using var blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

                // Apply adaptive thresholding for better handling of uneven lighting
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
                    return ("empty", 0.0, Array.Empty<Point>());
                }

                // Get the largest contour (assuming it's the main shape)
                var largestContour = contours
                    .OrderByDescending(c => Cv2.ContourArea(c))
                    .First();

                // Analyze the shape with custom epsilon factor
                var (shape, confidence) = AnalyzeContour(largestContour, epsilonFactor);

                // Clean up if we created a new grayscale image
                if (image.Channels() > 1)
                {
                    gray.Dispose();
                }

                return (shape, confidence, largestContour);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in enhanced shape detection: {ex.Message}");
                return ("error", 0.0, Array.Empty<Point>());
            }
        }

        private static (string shape, double confidence) AnalyzeContour(Point[] contour, double epsilonFactor = 0.04)
        {
            // Calculate basic properties
            var area = Cv2.ContourArea(contour);
            var perimeter = Cv2.ArcLength(contour, true);

            // Too small to analyze reliably
            if (area < 100)
            {
                return ("too_small", 0.3);
            }

            // Approximate the contour to a polygon with configurable epsilon
            var epsilon = epsilonFactor * perimeter;
            var approx = Cv2.ApproxPolyDP(contour, epsilon, true);
            int vertices = approx.Length;

            // Calculate circularity (4π × area / perimeter²)
            // Perfect circle = 1.0, square ≈ 0.785
            var circularity = (4 * Math.PI * area) / (perimeter * perimeter);

            // Get bounding rectangle for aspect ratio
            var rect = Cv2.BoundingRect(approx);
            var aspectRatio = (double)rect.Width / rect.Height;

            // Calculate convexity
            var convexHull = Cv2.ConvexHull(contour);
            var convexArea = Cv2.ContourArea(convexHull);
            var convexity = area / convexArea;

            // Classify based on multiple features
            return ClassifyShape(vertices, circularity, aspectRatio, convexity, area);
        }

        private static (string shape, double confidence) ClassifyShape(
            int vertices, double circularity, double aspectRatio, double convexity, double area)
        {
            // Circle detection
            if (circularity > 0.75)
            {
                // High circularity indicates a circle
                var confidence = Math.Min(circularity, 1.0) * 0.95;
                return ("circle", confidence);
            }


            // Treat oval-like shapes as circles per simplified rules
            if (circularity > 0.60 && (aspectRatio < 0.75 || aspectRatio > 1.33))
            {
                return ("circle", circularity * 0.85);
            }

            // Star detection (concave shape with multiple vertices)
            if (vertices >= 8 && convexity < 0.85)
            {
                var confidence = (1.0 - convexity) * 1.2; // Lower convexity = more star-like
                confidence = Math.Min(confidence, 0.95);
                return ("star", confidence);
            }

            // Treat cross/plus-like shapes as circles per simplified rules
            if (vertices >= 12 && convexity < 0.90)
            {
                return ("star", 0.80);
            }

            // Polygon classification based on vertices
            switch (vertices)
            {
                case 3:
                    return ("triangle", 0.90);

                case 4:
                    return ("rectangle", 0.88);

                case 5:
                case 6:
                case 7:
                case 8:
                    // Any polygon above a square (and not a star/cross) is treated as a circle
                    return ("circle", 0.80);

                default:
                    // Many vertices could be a circle approximation or complex polygon
                    if (vertices > 4)
                    {
                        // Any polygon above a square (and not a star/cross) is treated as a circle
                        return ("circle", 0.80);
                    }
                    if (vertices <= 2)
                    {
                        return ("line", 0.65);
                    }
                    return ($"polygon_{vertices}", 0.65);
            }
        }

        /// <summary>
        /// Detects multiple shapes in an image, useful when the image contains more than one shape.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="epsilonFactor">Optional contour approximation epsilon factor (default: 0.04)</param>
        public static List<(string shape, double confidence, Rect boundingBox)> DetectMultipleShapes(
            string imagePath, 
            double epsilonFactor = 0.04)
        {
            var results = new List<(string shape, double confidence, Rect boundingBox)>();

            try
            {
                using var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                if (image.Empty())
                {
                    return results;
                }

                // Preprocess
                using var blurred = new Mat();
                Cv2.GaussianBlur(image, blurred, new Size(5, 5), 0);

                using var thresh = new Mat();
                Cv2.AdaptiveThreshold(blurred, thresh, 255, 
                    AdaptiveThresholdTypes.GaussianC, 
                    ThresholdTypes.BinaryInv, 11, 2);

                // Find all contours
                Cv2.FindContours(thresh, out var contours, out _, 
                    RetrievalModes.External, 
                    ContourApproximationModes.ApproxSimple);

                // Analyze each significant contour
                foreach (var contour in contours)
                {
                    var area = Cv2.ContourArea(contour);
                    if (area < 100) continue; // Skip tiny contours

                    var (shape, confidence) = AnalyzeContour(contour, epsilonFactor);
                    var boundingBox = Cv2.BoundingRect(contour);

                    results.Add((shape, confidence, boundingBox));
                }

                // Sort by confidence, descending
                results = results.OrderByDescending(r => r.confidence).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in multiple shape detection: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Provides detailed shape analysis information for debugging and validation.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="epsilonFactor">Optional contour approximation epsilon factor (default: 0.04)</param>
        public static string GetShapeAnalysisDetails(string imagePath, double epsilonFactor = 0.04)
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
                var area = Cv2.ContourArea(largestContour);
                var perimeter = Cv2.ArcLength(largestContour, true);
                var circularity = (4 * Math.PI * area) / (perimeter * perimeter);
                
                var epsilon = epsilonFactor * perimeter;
                var approx = Cv2.ApproxPolyDP(largestContour, epsilon, true);
                var vertices = approx.Length;
                
                var rect = Cv2.BoundingRect(approx);
                var aspectRatio = (double)rect.Width / rect.Height;
                
                var convexHull = Cv2.ConvexHull(largestContour);
                var convexArea = Cv2.ContourArea(convexHull);
                var convexity = area / convexArea;

                var (shape, confidence) = AnalyzeContour(largestContour, epsilonFactor);

                return $@"Shape Analysis Details:
- Detected Shape: {shape}
- Confidence: {confidence:F3}
- Vertices: {vertices}
- Epsilon Factor: {epsilonFactor:F3} ({epsilonFactor * 100:F1}%)
- Epsilon Value: {epsilon:F2}
- Area: {area:F1} pixels
- Perimeter: {perimeter:F1} pixels
- Circularity: {circularity:F3} (1.0 = perfect circle)
- Aspect Ratio: {aspectRatio:F3} (W/H)
- Convexity: {convexity:F3} (1.0 = fully convex)
- Bounding Box: {rect.Width}x{rect.Height}";
            }
            catch (Exception ex)
            {
                return $"Error analyzing shape: {ex.Message}";
            }
        }
    }
}
