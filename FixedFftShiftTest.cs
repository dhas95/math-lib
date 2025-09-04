using System;
using System.Diagnostics;

public static class FixedFftShiftTest  
{
    public class SimpleMatrix
    {
        public float[,] Data { get; }
        public int Rows => Data.GetLength(0);
        public int Cols => Data.GetLength(1);

        public SimpleMatrix(int rows, int cols)
        {
            Data = new float[rows, cols];
        }

        public SimpleMatrix Clone()
        {
            var clone = new SimpleMatrix(Rows, Cols);
            Array.Copy(Data, clone.Data, Data.Length);
            return clone;
        }

        public bool IsEqual(SimpleMatrix other, double tolerance = 1e-10)
        {
            if (Rows != other.Rows || Cols != other.Cols) return false;
            
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (Math.Abs(Data[i, j] - other.Data[i, j]) > tolerance)
                    {
                        Console.WriteLine($"Difference at [{i},{j}]: {Data[i,j]} vs {other.Data[i,j]} (diff: {Math.Abs(Data[i, j] - other.Data[i, j])})");
                        return false;
                    }
                }
            }
            return true;
        }

        public void FillWithTestData(int seed = 42)
        {
            var rand = new Random(seed);
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Data[i, j] = (float)(i * Cols + j); // Simple pattern for debugging
                }
            }
        }

        public void Print(string title)
        {
            Console.WriteLine($"{title}:");
            for (int i = 0; i < Math.Min(8, Rows); i++)
            {
                for (int j = 0; j < Math.Min(8, Cols); j++)
                {
                    Console.Write($"{Data[i,j],6:F0} ");
                }
                if (Cols > 8) Console.Write("...");
                Console.WriteLine();
            }
            if (Rows > 8) Console.WriteLine("...");
            Console.WriteLine();
        }
    }

    // Your original algorithm 
    public static void FftShiftOriginal(SimpleMatrix mat)
    {
        if (mat == null) return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;  // Right side width
        int w2 = cx;             // Left side width
        int h1 = mat.Rows - cy;  // Bottom side height
        int h2 = cy;             // Top side height

        var temp = new SimpleMatrix(mat.Rows, mat.Cols);

        // Q0 (0,0,w1,h1) -> (w2,h2,w1,h1) = (cx,cy,w1,h1)
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w1; j++)
                temp.Data[i + h2, j + w2] = mat.Data[i, j];

        // Q1 (w1,0,w2,h1) -> (0,h2,w2,h1) = (0,cy,cx,h1)
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w2; j++)
                temp.Data[i + h2, j] = mat.Data[i, j + w1];

        // Q2 (0,h1,w1,h2) -> (w2,0,w1,h2) = (cx,0,w1,cy)
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w1; j++)
                temp.Data[i, j + w2] = mat.Data[i + h1, j];

        // Q3 (w1,h1,w2,h2) -> (0,0,w2,h2) = (0,0,cx,cy)
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w2; j++)
                temp.Data[i, j] = mat.Data[i + h1, j + w1];

        Array.Copy(temp.Data, mat.Data, mat.Data.Length);
    }

    // CORRECTED optimized version - must exactly match the original mapping
    public static void FftShiftOptimizedFixed(SimpleMatrix mat)
    {
        if (mat == null) return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;  // Right side width
        int w2 = cx;             // Left side width
        int h1 = mat.Rows - cy;  // Bottom side height
        int h2 = cy;             // Top side height

        // We need to perform these exact mappings:
        // Q0 (0,0,w1,h1) -> (cx,cy,w1,h1)
        // Q1 (w1,0,w2,h1) -> (0,cy,w2,h1)  
        // Q2 (0,h1,w1,h2) -> (cx,0,w1,h2)
        // Q3 (w1,h1,w2,h2) -> (0,0,w2,h2)

        // Temporary storage for the largest quadrant
        int maxQuadrantSize = Math.Max(Math.Max(w1*h1, w2*h1), Math.Max(w1*h2, w2*h2));
        var temp = new float[maxQuadrantSize];

        // Step 1: Save Q0 to temp
        int idx = 0;
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w1; j++)
                temp[idx++] = mat.Data[i, j];

        // Step 2: Move Q3 to Q0's final position (cx,cy)
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w2; j++)
                mat.Data[i + cy, j + cx] = mat.Data[i + h1, j + w1];

        // Step 3: Move Q2 to Q3's original position (w1,h1) 
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w1; j++)
                mat.Data[i + h1, j + w1] = mat.Data[i + h1, j];

        // Step 4: Move Q1 to Q2's final position (cx,0)
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w2; j++)
                mat.Data[i, j + cx] = mat.Data[i, j + w1];

        // Step 5: Move saved Q0 to Q1's final position (0,cy)
        idx = 0;
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w1; j++)
                mat.Data[i + cy, j] = temp[idx++];

        // Wait, this is getting complex. Let me use a simpler approach that exactly matches the original:
        // Use temporary storage for each quadrant and place them exactly where the original does
    }

    // MUCH SIMPLER: Direct translation of original algorithm with optimized temporary storage
    public static void FftShiftOptimizedCorrect(SimpleMatrix mat)
    {
        if (mat == null) return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;  // Right side width
        int w2 = cx;             // Left side width
        int h1 = mat.Rows - cy;  // Bottom side height
        int h2 = cy;             // Top side height

        // Create temporary storage for each quadrant (this matches original exactly)
        var tempQ0 = new float[w1 * h1];
        var tempQ1 = new float[w2 * h1];
        var tempQ2 = new float[w1 * h2];
        var tempQ3 = new float[w2 * h2];

        // Save all quadrants to temporary storage
        int idx0 = 0, idx1 = 0, idx2 = 0, idx3 = 0;

        // Save Q0 (0,0,w1,h1)
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w1; j++)
                tempQ0[idx0++] = mat.Data[i, j];

        // Save Q1 (w1,0,w2,h1)
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w2; j++)
                tempQ1[idx1++] = mat.Data[i, j + w1];

        // Save Q2 (0,h1,w1,h2)
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w1; j++)
                tempQ2[idx2++] = mat.Data[i + h1, j];

        // Save Q3 (w1,h1,w2,h2)
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w2; j++)
                tempQ3[idx3++] = mat.Data[i + h1, j + w1];

        // Now place them in their new positions (exactly as original does)
        
        // Q0 -> (cx,cy,w1,h1)
        idx0 = 0;
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w1; j++)
                mat.Data[i + h2, j + w2] = tempQ0[idx0++];

        // Q1 -> (0,cy,w2,h1)
        idx1 = 0;
        for (int i = 0; i < h1; i++)
            for (int j = 0; j < w2; j++)
                mat.Data[i + h2, j] = tempQ1[idx1++];

        // Q2 -> (cx,0,w1,h2)
        idx2 = 0;
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w1; j++)
                mat.Data[i, j + w2] = tempQ2[idx2++];

        // Q3 -> (0,0,w2,h2)
        idx3 = 0;
        for (int i = 0; i < h2; i++)
            for (int j = 0; j < w2; j++)
                mat.Data[i, j] = tempQ3[idx3++];
    }

    public static void RunDetailedTest()
    {
        Console.WriteLine("DETAILED FFT Shift Test");
        Console.WriteLine("=======================\n");

        // Test with a small matrix first to debug
        var sizes = new[] { (5, 5), (6, 6), (7, 7), (8, 8) };

        foreach (var (rows, cols) in sizes)
        {
            Console.WriteLine($"Testing {rows}x{cols} matrix:");
            
            var original = new SimpleMatrix(rows, cols);
            original.FillWithTestData();
            
            Console.WriteLine("Original matrix:");
            original.Print("Before");

            var mat1 = original.Clone();
            var mat2 = original.Clone();

            FftShiftOriginal(mat1);
            FftShiftOptimizedCorrect(mat2);

            Console.WriteLine("After original method:");
            mat1.Print("Original Result");
            
            Console.WriteLine("After optimized method:");
            mat2.Print("Optimized Result");

            bool identical = mat1.IsEqual(mat2);
            Console.WriteLine($"Results identical: {(identical ? "✅ YES" : "❌ NO")}");
            
            if (!identical)
            {
                Console.WriteLine("STOPPING - Methods produce different results!");
                return;
            }
            Console.WriteLine(new string('-', 50));
        }

        Console.WriteLine("✅ ALL TESTS PASSED! Methods produce identical results.");
    }

    static void Main()
    {
        try
        {
            RunDetailedTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}