using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace ColorDetectionApp
{
    /// <summary>
    /// Test class for validating outlier detection functionality
    /// </summary>
    public class TestOutlierDetection
    {
        public static void RunTests()
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("Outlier Detection Test Suite");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            int passed = 0;
            int failed = 0;

            // Test 1: IQR method with obvious outliers
            Console.WriteLine("Test 1: IQR Method with Obvious Outliers");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                // Create points with regular spacing and one huge jump (outlier)
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 10),    // distance ~14
                    new Point(20, 20),    // distance ~14
                    new Point(30, 30),    // distance ~14
                    new Point(500, 500),  // distance ~664 (OUTLIER!)
                    new Point(40, 40),    // distance ~651 (jump back)
                    new Point(50, 50),    // distance ~14
                    new Point(60, 60)     // distance ~14
                };

                var filtered = OutlierDetection.RemoveOutliersIQR(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                // The outlier point (500, 500) should be removed
                bool hasOutlier = filtered.Any(p => p.X == 500 && p.Y == 500);
                
                if (!hasOutlier && filtered.Count < points.Count)
                {
                    Console.WriteLine("✓ PASSED: Outlier successfully removed");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Outlier not removed properly");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 2: Z-Score method with outliers
            Console.WriteLine("Test 2: Z-Score Method with Outliers");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(5, 5),      // distance ~7
                    new Point(10, 10),    // distance ~7
                    new Point(15, 15),    // distance ~7
                    new Point(300, 15),   // distance ~285 (OUTLIER!)
                    new Point(20, 20),    // distance ~396 (jump back)
                    new Point(25, 25),    // distance ~7
                    new Point(30, 30)     // distance ~7
                };

                var filtered = OutlierDetection.RemoveOutliersZScore(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                bool hasOutlier = filtered.Any(p => p.X == 300 && p.Y == 15);
                
                if (!hasOutlier && filtered.Count < points.Count)
                {
                    Console.WriteLine("✓ PASSED: Outlier successfully removed");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Outlier not removed properly");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 3: Hybrid method
            Console.WriteLine("Test 3: Hybrid Method (IQR + Z-Score)");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 0),
                    new Point(20, 0),
                    new Point(30, 0),
                    new Point(1000, 1000), // Major outlier
                    new Point(40, 0),
                    new Point(50, 0)
                };

                var filtered = OutlierDetection.RemoveOutliersHybrid(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                bool hasOutlier = filtered.Any(p => p.X == 1000 && p.Y == 1000);
                
                if (!hasOutlier && filtered.Count < points.Count)
                {
                    Console.WriteLine("✓ PASSED: Outlier successfully removed");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Outlier not removed properly");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 4: No outliers (should keep all points)
            Console.WriteLine("Test 4: No Outliers Present");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 10),
                    new Point(20, 20),
                    new Point(30, 30),
                    new Point(40, 40)
                };

                var filtered = OutlierDetection.RemoveOutliersIQR(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                if (filtered.Count == points.Count)
                {
                    Console.WriteLine("✓ PASSED: All points kept (no outliers)");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Valid points incorrectly removed");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 5: Statistics calculation
            Console.WriteLine("Test 5: Point Statistics Calculation");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 0),   // distance 10
                    new Point(20, 0),   // distance 10
                    new Point(30, 0)    // distance 10
                };

                var stats = OutlierDetection.GetStatistics(points);
                
                Console.WriteLine(stats);
                
                // Mean distance should be 10 for this uniform spacing
                if (Math.Abs(stats.MeanDistance - 10) < 0.1 && 
                    Math.Abs(stats.MedianDistance - 10) < 0.1)
                {
                    Console.WriteLine("✓ PASSED: Statistics calculated correctly");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Statistics calculation incorrect");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 6: Edge case - too few points
            Console.WriteLine("Test 6: Edge Case - Too Few Points");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 10)
                };

                var filtered = OutlierDetection.RemoveOutliersIQR(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                // Should return original points since there aren't enough for IQR
                if (filtered.Count == points.Count)
                {
                    Console.WriteLine("✓ PASSED: Handles too few points correctly");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Incorrect handling of too few points");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 7: Multiple outliers
            Console.WriteLine("Test 7: Multiple Outliers");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 10),
                    new Point(500, 500),   // outlier 1
                    new Point(20, 20),
                    new Point(30, 30),
                    new Point(600, 100),   // outlier 2
                    new Point(40, 40),
                    new Point(50, 50)
                };

                var filtered = OutlierDetection.RemoveOutliersHybrid(points);
                
                Console.WriteLine($"Original points: {points.Count}");
                Console.WriteLine($"Filtered points: {filtered.Count}");
                
                bool hasOutlier1 = filtered.Any(p => p.X == 500 && p.Y == 500);
                bool hasOutlier2 = filtered.Any(p => p.X == 600 && p.Y == 100);
                
                if (!hasOutlier1 && !hasOutlier2 && filtered.Count < points.Count)
                {
                    Console.WriteLine("✓ PASSED: Multiple outliers successfully removed");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Not all outliers removed");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Summary
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"Test Results: {passed} passed, {failed} failed");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            if (failed == 0)
            {
                Console.WriteLine("✓ All tests passed!");
            }
            else
            {
                Console.WriteLine($"✗ {failed} test(s) failed");
            }
        }
    }
}
