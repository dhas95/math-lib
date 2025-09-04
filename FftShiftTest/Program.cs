using System;
using System.Diagnostics;

// Mock implementation for testing the algorithm logic without OpenCV dependencies
public class MockMat : IDisposable
{
    private double[,] data;
    public int Rows { get; private set; }
    public int Cols { get; private set; }
    
    public MockMat(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        data = new double[rows, cols];
    }
    
    public MockMat(MockMat source, int x, int y, int width, int height)
    {
        // Create a view/sub-matrix
        Rows = height;
        Cols = width;
        data = new double[height, width];
        
        // Copy the region
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                data[i, j] = source.data[y + i, x + j];
            }
        }
    }
    
    public double this[int row, int col]
    {
        get => data[row, col];
        set => data[row, col] = value;
    }
    
    public void CopyTo(MockMat dest)
    {
        if (dest.Rows != Rows || dest.Cols != Cols)
        {
            dest.data = new double[Rows, Cols];
            dest.Rows = Rows;
            dest.Cols = Cols;
        }
        
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                dest.data[i, j] = data[i, j];
            }
        }
    }
    
    public void CopyToRegion(MockMat dest, int destX, int destY)
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                dest.data[destY + i, destX + j] = data[i, j];
            }
        }
    }
    
    public MockMat Clone()
    {
        var clone = new MockMat(Rows, Cols);
        CopyTo(clone);
        return clone;
    }
    
    public bool Equals(MockMat other)
    {
        if (other.Rows != Rows || other.Cols != Cols) return false;
        
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Math.Abs(data[i, j] - other.data[i, j]) > 1e-10)
                    return false;
            }
        }
        return true;
    }
    
    public void Dispose() { }
    
    public bool Empty() => data == null || Rows == 0 || Cols == 0;
    
    public void PrintMatrix(string name = "")
    {
        Console.WriteLine($"Matrix {name} ({Rows}x{Cols}):");
        for (int i = 0; i < Math.Min(8, Rows); i++)
        {
            for (int j = 0; j < Math.Min(8, Cols); j++)
            {
                Console.Write($"{data[i, j]:F2} ");
            }
            if (Cols > 8) Console.Write("...");
            Console.WriteLine();
        }
        if (Rows > 8) Console.WriteLine("...");
        Console.WriteLine();
    }
}

public static class FftShiftMethods
{
    // Original method adapted for MockMat
    public static void FftShiftOriginal(MockMat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;
        int w2 = cx;
        int h1 = mat.Rows - cy;
        int h2 = cy;

        // Single allocation - temporary storage for the entire result
        using var temp = new MockMat(mat.Rows, mat.Cols);

        // Create views (simulated)
        var srcQ0 = new MockMat(mat, 0, 0, w1, h1);
        var srcQ1 = new MockMat(mat, w1, 0, w2, h1);
        var srcQ2 = new MockMat(mat, 0, h1, w1, h2);
        var srcQ3 = new MockMat(mat, w1, h1, w2, h2);

        // Copy quadrants to their new positions
        srcQ0.CopyToRegion(temp, w2, h2); // q0 destination
        srcQ1.CopyToRegion(temp, 0, h2);  // q1 destination  
        srcQ2.CopyToRegion(temp, w2, 0);  // q2 destination
        srcQ3.CopyToRegion(temp, 0, 0);   // q3 destination

        // Copy result back to original
        temp.CopyTo(mat);

        // Cleanup
        srcQ0.Dispose();
        srcQ1.Dispose();
        srcQ2.Dispose();
        srcQ3.Dispose();
    }
    
    // Test different optimization attempts
    public static void FftShiftOptimized1(MockMat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;
        int w2 = cx;
        int h1 = mat.Rows - cy;
        int h2 = cy;

        using var temp = new MockMat(1, 1); // Will be resized as needed

        // Create quadrant views
        using var q0 = new MockMat(mat, 0, 0, w1, h1);
        using var q1 = new MockMat(mat, w1, 0, w2, h1);
        using var q2 = new MockMat(mat, 0, h1, w1, h2);
        using var q3 = new MockMat(mat, w1, h1, w2, h2);

        // Save q0, then rotate: q0←q3←q2←q1←q0
        q0.CopyTo(temp);
        q3.CopyToRegion(mat, 0, 0);      // q3 → q0 position
        q2.CopyToRegion(mat, w1, h1);    // q2 → q3 position
        q1.CopyToRegion(mat, 0, h1);     // q1 → q2 position
        temp.CopyToRegion(mat, w1, 0);   // q0 → q1 position
    }
    
    // Another attempt with different swap order
    public static void FftShiftOptimized2(MockMat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;
        int w2 = cx;
        int h1 = mat.Rows - cy;
        int h2 = cy;

        using var temp = new MockMat(1, 1);

        // Extract quadrants to temporary storage
        var tempQ0 = new MockMat(mat, 0, 0, w1, h1);
        var tempQ1 = new MockMat(mat, w1, 0, w2, h1);
        var tempQ2 = new MockMat(mat, 0, h1, w1, h2);
        var tempQ3 = new MockMat(mat, w1, h1, w2, h2);

        // Apply the exact same mapping as original
        tempQ0.CopyToRegion(mat, w2, h2); // q0 → bottom-right
        tempQ1.CopyToRegion(mat, 0, h2);  // q1 → bottom-left
        tempQ2.CopyToRegion(mat, w2, 0);  // q2 → top-right
        tempQ3.CopyToRegion(mat, 0, 0);   // q3 → top-left

        tempQ0.Dispose();
        tempQ1.Dispose();
        tempQ2.Dispose();
        tempQ3.Dispose();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("FFT Shift Algorithm Testing");
        Console.WriteLine("==========================");
        
        // Test with different matrix sizes
        int[] sizes = { 4, 8, 16, 64 };
        
        foreach (int size in sizes)
        {
            Console.WriteLine($"\nTesting {size}x{size} matrix:");
            TestEquality(size);
            TestPerformance(size);
        }
    }
    
    static void TestEquality(int size)
    {
        // Create test matrix with known pattern
        var original = new MockMat(size, size);
        
        // Fill with a pattern that makes it easy to verify correctness
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                original[i, j] = i * size + j; // Sequential numbers
            }
        }
        
        Console.WriteLine("Original matrix:");
        original.PrintMatrix();
        
        // Test each method
        var mat1 = original.Clone();
        var mat2 = original.Clone();
        var mat3 = original.Clone();
        
        FftShiftMethods.FftShiftOriginal(mat1);
        FftShiftMethods.FftShiftOptimized1(mat2);
        FftShiftMethods.FftShiftOptimized2(mat3);
        
        Console.WriteLine("After FftShiftOriginal:");
        mat1.PrintMatrix();
        
        Console.WriteLine("After FftShiftOptimized1:");
        mat2.PrintMatrix();
        
        Console.WriteLine("After FftShiftOptimized2:");
        mat3.PrintMatrix();
        
        // Check equality
        bool opt1Equal = mat1.Equals(mat2);
        bool opt2Equal = mat1.Equals(mat3);
        
        Console.WriteLine($"Optimized1 equals Original: {opt1Equal}");
        Console.WriteLine($"Optimized2 equals Original: {opt2Equal}");
        
        mat1.Dispose();
        mat2.Dispose();
        mat3.Dispose();
        original.Dispose();
    }
    
    static void TestPerformance(int size)
    {
        const int iterations = 1000;
        
        var testMat = new MockMat(size, size);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                testMat[i, j] = i * size + j;
            }
        }
        
        // Warm up
        var warmup = testMat.Clone();
        FftShiftMethods.FftShiftOriginal(warmup);
        warmup.Dispose();
        
        // Test original method
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var mat = testMat.Clone();
            FftShiftMethods.FftShiftOriginal(mat);
            mat.Dispose();
        }
        sw.Stop();
        long originalTime = sw.ElapsedMilliseconds;
        
        // Test optimized method 1
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var mat = testMat.Clone();
            FftShiftMethods.FftShiftOptimized1(mat);
            mat.Dispose();
        }
        sw.Stop();
        long opt1Time = sw.ElapsedMilliseconds;
        
        // Test optimized method 2
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var mat = testMat.Clone();
            FftShiftMethods.FftShiftOptimized2(mat);
            mat.Dispose();
        }
        sw.Stop();
        long opt2Time = sw.ElapsedMilliseconds;
        
        Console.WriteLine($"Performance ({iterations} iterations):");
        Console.WriteLine($"  Original:    {originalTime}ms");
        Console.WriteLine($"  Optimized1:  {opt1Time}ms ({(double)originalTime/opt1Time:F2}x)");
        Console.WriteLine($"  Optimized2:  {opt2Time}ms ({(double)originalTime/opt2Time:F2}x)");
        
        testMat.Dispose();
    }
}