using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorDetectionApp
{
    /// <summary>
    /// Provides statistical outlier detection methods for filtering extreme outliers 
    /// from tracked image points based on distance analysis.
    /// </summary>
    public class OutlierDetection
    {
        /// <summary>
        /// Removes outliers from a list of points using the IQR (Interquartile Range) method
        /// based on the distances between consecutive points.
        /// Uses a robust approach that handles cases where outliers affect quartile calculations.
        /// </summary>
        /// <param name="points">List of points to filter</param>
        /// <param name="multiplier">IQR multiplier for outlier threshold (default: 1.5, higher = more permissive)</param>
        /// <returns>Filtered list of points with outliers removed</returns>
        public static List<Point> RemoveOutliersIQR(List<Point> points, double multiplier = 1.5)
        {
            if (points == null || points.Count < 4)
            {
                // Need at least 4 points for meaningful IQR calculation
                return points ?? new List<Point>();
            }

            // Calculate distances between consecutive points
            var distances = new List<double>();
            for (int i = 1; i < points.Count; i++)
            {
                double distance = CalculateDistance(points[i - 1], points[i]);
                distances.Add(distance);
            }

            if (distances.Count == 0)
            {
                return points;
            }

            // Calculate IQR (Interquartile Range) using more robust percentile calculation
            var sortedDistances = distances.OrderBy(d => d).ToList();
            
            // Use exact percentile calculation for better robustness
            double q1 = CalculatePercentile(sortedDistances, 25);
            double q3 = CalculatePercentile(sortedDistances, 75);
            double iqr = q3 - q1;
            
            // Calculate median for fallback
            double median = CalculateMedian(sortedDistances);
            
            // Determine upper bound
            double upperBound;
            if (iqr < 0.01) // Essentially all distances are the same
            {
                // Use median-based approach with a small tolerance
                upperBound = median + Math.Max(median * 2.0, 50.0);
            }
            else
            {
                // Standard IQR method
                upperBound = q3 + (multiplier * iqr);
                
                // Additional safeguard: if upperBound is too large compared to median,
                // cap it to prevent outliers from inflating the bound
                // Use a more aggressive cap: max of 4x median or 150 pixels
                double safeUpperBound = median + Math.Max(median * 4.0, 150.0);
                upperBound = Math.Min(upperBound, safeUpperBound);
            }
            
            double lowerBound = Math.Max(0, q1 - (multiplier * iqr)); // Distance can't be negative
            
            // Filter points based on distance thresholds
            var filteredPoints = new List<Point> { points[0] }; // Always keep the first point
            
            for (int i = 1; i < points.Count; i++)
            {
                double distance = distances[i - 1];
                
                // Keep point if its distance is within bounds
                if (distance >= lowerBound && distance <= upperBound)
                {
                    filteredPoints.Add(points[i]);
                }
            }
            
            return filteredPoints;
        }
        
        /// <summary>
        /// Calculates a specific percentile value from a sorted list.
        /// </summary>
        private static double CalculatePercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0)
                return 0;
            if (sortedValues.Count == 1)
                return sortedValues[0];
                
            double n = sortedValues.Count;
            double position = (percentile / 100.0) * (n - 1);
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            
            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];
                
            double fraction = position - lowerIndex;
            return sortedValues[lowerIndex] + fraction * (sortedValues[upperIndex] - sortedValues[lowerIndex]);
        }

        /// <summary>
        /// Removes outliers from a list of points using the Modified Z-score method
        /// based on the distances between consecutive points.
        /// Uses Median Absolute Deviation (MAD) for robustness against outliers.
        /// </summary>
        /// <param name="points">List of points to filter</param>
        /// <param name="threshold">Modified Z-score threshold for outlier detection (default: 3.5, higher = more permissive)</param>
        /// <returns>Filtered list of points with outliers removed</returns>
        public static List<Point> RemoveOutliersZScore(List<Point> points, double threshold = 3.5)
        {
            if (points == null || points.Count < 3)
            {
                // Need at least 3 points for meaningful statistical analysis
                return points ?? new List<Point>();
            }

            // Calculate distances between consecutive points
            var distances = new List<double>();
            for (int i = 1; i < points.Count; i++)
            {
                double distance = CalculateDistance(points[i - 1], points[i]);
                distances.Add(distance);
            }

            if (distances.Count == 0)
            {
                return points;
            }

            // Calculate median
            double median = CalculateMedian(distances);
            
            // Calculate MAD (Median Absolute Deviation)
            var absoluteDeviations = distances.Select(d => Math.Abs(d - median)).ToList();
            double mad = CalculateMedian(absoluteDeviations);
            
            // If MAD is too small, use a fallback based on median
            if (mad < 0.01)
            {
                mad = median * 0.5; // Use 50% of median as MAD if it's essentially zero
            }
            
            // Filter points based on modified z-score
            var filteredPoints = new List<Point> { points[0] }; // Always keep the first point
            
            for (int i = 1; i < points.Count; i++)
            {
                double distance = distances[i - 1];
                
                // Calculate modified z-score
                // Modified z-score = 0.6745 * (x - median) / MAD
                double modifiedZScore = 0.6745 * (distance - median) / mad;
                
                // Keep point if its modified z-score is within threshold
                if (Math.Abs(modifiedZScore) <= threshold)
                {
                    filteredPoints.Add(points[i]);
                }
            }
            
            return filteredPoints;
        }

        /// <summary>
        /// Removes outliers using a hybrid approach combining IQR and Z-score methods
        /// for more robust outlier detection. Applies iteratively until no more outliers are found.
        /// </summary>
        /// <param name="points">List of points to filter</param>
        /// <param name="iqrMultiplier">IQR multiplier (default: 1.5)</param>
        /// <param name="zScoreThreshold">Z-score threshold (default: 3.5)</param>
        /// <param name="maxIterations">Maximum number of iterations (default: 3)</param>
        /// <returns>Filtered list of points with outliers removed</returns>
        public static List<Point> RemoveOutliersHybrid(List<Point> points, 
            double iqrMultiplier = 1.5, double zScoreThreshold = 3.5, int maxIterations = 3)
        {
            if (points == null || points.Count < 4)
            {
                return points ?? new List<Point>();
            }

            var currentPoints = new List<Point>(points);
            int iteration = 0;
            
            while (iteration < maxIterations)
            {
                int countBefore = currentPoints.Count;
                
                // Apply IQR method first
                currentPoints = RemoveOutliersIQR(currentPoints, iqrMultiplier);
                
                // Then apply Z-score method
                currentPoints = RemoveOutliersZScore(currentPoints, zScoreThreshold);
                
                int countAfter = currentPoints.Count;
                
                // If no points were removed, we're done
                if (countBefore == countAfter)
                {
                    break;
                }
                
                iteration++;
            }
            
            return currentPoints;
        }

        /// <summary>
        /// Provides statistics about the points collection for analysis.
        /// </summary>
        public static PointStatistics GetStatistics(List<Point> points)
        {
            if (points == null || points.Count < 2)
            {
                return new PointStatistics
                {
                    PointCount = points?.Count ?? 0,
                    MeanDistance = 0,
                    MedianDistance = 0,
                    MaxDistance = 0,
                    MinDistance = 0,
                    StdDevDistance = 0
                };
            }

            var distances = new List<double>();
            for (int i = 1; i < points.Count; i++)
            {
                double distance = CalculateDistance(points[i - 1], points[i]);
                distances.Add(distance);
            }

            if (distances.Count == 0)
            {
                return new PointStatistics { PointCount = points.Count };
            }

            double mean = distances.Average();
            double median = CalculateMedian(distances);
            double max = distances.Max();
            double min = distances.Min();
            double variance = distances.Sum(d => Math.Pow(d - mean, 2)) / distances.Count;
            double stdDev = Math.Sqrt(variance);

            return new PointStatistics
            {
                PointCount = points.Count,
                MeanDistance = mean,
                MedianDistance = median,
                MaxDistance = max,
                MinDistance = min,
                StdDevDistance = stdDev
            };
        }

        private static double CalculateDistance(Point p1, Point p2)
        {
            int dx = p2.X - p1.X;
            int dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double CalculateMedian(List<double> values)
        {
            if (values.Count == 0)
                return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int middle = sorted.Count / 2;

            if (sorted.Count % 2 == 0)
            {
                // Even number of elements - return average of two middle values
                return (sorted[middle - 1] + sorted[middle]) / 2.0;
            }
            else
            {
                // Odd number of elements - return middle value
                return sorted[middle];
            }
        }
    }

    /// <summary>
    /// Statistics about a collection of points for analysis.
    /// </summary>
    public class PointStatistics
    {
        public int PointCount { get; set; }
        public double MeanDistance { get; set; }
        public double MedianDistance { get; set; }
        public double MaxDistance { get; set; }
        public double MinDistance { get; set; }
        public double StdDevDistance { get; set; }

        public override string ToString()
        {
            return $@"Point Statistics:
  Point Count: {PointCount}
  Mean Distance: {MeanDistance:F2} pixels
  Median Distance: {MedianDistance:F2} pixels
  Min Distance: {MinDistance:F2} pixels
  Max Distance: {MaxDistance:F2} pixels
  Std Dev Distance: {StdDevDistance:F2} pixels";
        }
    }
}
