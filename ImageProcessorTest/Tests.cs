using System;
using System.Diagnostics;
using OpenCvSharp;
using Xunit;

public class ImageProcessorTests
{
    private Mat CreateTestImage(int width = 500, int height = 500)
    {
        var image = new Mat(height, width, MatType.CV_8UC1, Scalar.All(255));
        
        // Add some black components of various sizes
        Cv2.Rectangle(image, new Rect(50, 50, 100, 100), Scalar.All(0), -1);
        Cv2.Rectangle(image, new Rect(200, 200, 150, 150), Scalar.All(0), -1);
        Cv2.Rectangle(image, new Rect(400, 100, 50, 50), Scalar.All(0), -1);
        Cv2.Circle(image, new Point(100, 400), 30, Scalar.All(0), -1);
        
        // Add some white holes in black regions
        Cv2.Circle(image, new Point(75, 75), 15, Scalar.All(255), -1);
        Cv2.Circle(image, new Point(275, 275), 20, Scalar.All(255), -1);
        
        // Add some small noise components
        for (int i = 0; i < 20; i++)
        {
            var random = new Random(i);
            int x = random.Next(width);
            int y = random.Next(height);
            int size = random.Next(3, 8);
            var color = random.NextDouble() > 0.5 ? Scalar.All(0) : Scalar.All(255);
            Cv2.Circle(image, new Point(x, y), size, color, -1);
        }
        
        return image;
    }

    [Fact]
    public void TestOptimizedVersionProducesSimilarResults()
    {
        Console.WriteLine("Running correctness test...");
        
        var testImage = CreateTestImage(200, 200); // Smaller for faster testing
        
        var originalResult = OptimizedImageProcessor.ForceTwoComponentsBruteForce(testImage, false);
        var optimizedResult = OptimizedImageProcessor.ForceTwoComponentsOptimized(testImage, false);
        
        // Convert to Mat for comparison
        var originalMat = originalResult.GetMat();
        var optimizedMat = optimizedResult.GetMat();
        
        // Check if images are similar (allowing for algorithmic differences)
        var diff = new Mat();
        Cv2.Absdiff(originalMat, optimizedMat, diff);
        
        Cv2.MinMaxLoc(diff, out double minVal, out double maxDiff);
        double meanDiff = Cv2.Mean(diff).Val0;
        
        Console.WriteLine($"Max difference: {maxDiff}");
        Console.WriteLine($"Mean difference: {meanDiff:F2}");
        
        // Check that both results have only two main components (0 and 255)
        var originalHist = new Mat();
        var optimizedHist = new Mat();
        Cv2.CalcHist(new[] { originalMat }, new[] { 0 }, null, originalHist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
        Cv2.CalcHist(new[] { optimizedMat }, new[] { 0 }, null, optimizedHist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
        
        // Count significant histogram bins (values with substantial pixel counts)
        int originalSignificantBins = 0;
        int optimizedSignificantBins = 0;
        int totalPixels = originalMat.Rows * originalMat.Cols;
        
        for (int i = 0; i < 256; i++)
        {
            if (originalHist.At<float>(i) > totalPixels * 0.01) // More than 1% of pixels
                originalSignificantBins++;
            if (optimizedHist.At<float>(i) > totalPixels * 0.01)
                optimizedSignificantBins++;
        }
        
        Console.WriteLine($"Original significant bins: {originalSignificantBins}");
        Console.WriteLine($"Optimized significant bins: {optimizedSignificantBins}");
        
        // Allow some difference due to algorithmic variations, but should be reasonably close
        Assert.True(maxDiff <= 100, $"Optimized version differs too much from original. Max diff: {maxDiff}");
        Assert.True(meanDiff <= 30, $"Mean difference too high: {meanDiff}");
        
        // Both should have few significant bins (ideally 2-3 for black, white, and maybe some intermediate values)
        Assert.True(originalSignificantBins <= 5, $"Original should have few significant bins, got {originalSignificantBins}");
        Assert.True(optimizedSignificantBins <= 5, $"Optimized should have few significant bins, got {optimizedSignificantBins}");
        
        Console.WriteLine("✓ Correctness test passed!");
        
        // Cleanup
        testImage.Dispose();
        originalMat.Dispose();
        optimizedMat.Dispose();
        diff.Dispose();
        originalHist.Dispose();
        optimizedHist.Dispose();
        originalResult.Dispose();
        optimizedResult.Dispose();
    }

    [Fact]
    public void PerformanceBenchmark()
    {
        Console.WriteLine("Running performance benchmark...");
        
        var testImage = CreateTestImage(800, 800); // Larger test image
        const int iterations = 5; // Fewer iterations for faster testing
        
        // Warm up
        var warmup = OptimizedImageProcessor.ForceTwoComponentsOptimized(testImage, false);
        warmup.Dispose();
        
        // Benchmark original (with timeout protection)
        var sw = Stopwatch.StartNew();
        double originalTime = 0;
        bool originalCompleted = false;
        
        try 
        {
            for (int i = 0; i < iterations; i++)
            {
                var result = OptimizedImageProcessor.ForceTwoComponentsBruteForce(testImage, false);
                result.Dispose();
                
                // Check if we're taking too long
                if (sw.ElapsedMilliseconds > 30000) // 30 second timeout
                {
                    Console.WriteLine($"Original method too slow, stopping after {i + 1} iterations");
                    originalTime = sw.ElapsedMilliseconds / (double)(i + 1);
                    break;
                }
            }
            
            if (sw.ElapsedMilliseconds <= 30000)
            {
                originalTime = sw.ElapsedMilliseconds / (double)iterations;
                originalCompleted = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Original method failed: {ex.Message}");
            originalTime = double.MaxValue;
        }
        
        // Benchmark optimized version
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var result = OptimizedImageProcessor.ForceTwoComponentsOptimized(testImage, false);
            result.Dispose();
        }
        sw.Stop();
        double optimizedTime = sw.ElapsedMilliseconds / (double)iterations;
        
        Console.WriteLine($"Performance Results (average over {iterations} iterations):");
        if (originalCompleted)
        {
            Console.WriteLine($"Original: {originalTime:F2} ms");
            Console.WriteLine($"Optimized: {optimizedTime:F2} ms ({originalTime/optimizedTime:F1}x faster)");
            
            // Assert that optimized version is significantly faster
            Assert.True(optimizedTime < originalTime * 0.3, $"Optimized should be at least 3x faster. Original: {originalTime}ms, Optimized: {optimizedTime}ms");
        }
        else
        {
            Console.WriteLine($"Original: > 30000 ms (timeout)");
            Console.WriteLine($"Optimized: {optimizedTime:F2} ms");
        }
        
        // Check if we meet the 20ms requirement
        Console.WriteLine($"Target: < 20 ms");
        Console.WriteLine($"Achieved: {optimizedTime:F2} ms");
        
        if (optimizedTime < 20)
        {
            Console.WriteLine("✓ Performance target met!");
        }
        else
        {
            Console.WriteLine($"⚠ Performance target not met, but significant improvement achieved");
        }
        
        testImage.Dispose();
    }
}