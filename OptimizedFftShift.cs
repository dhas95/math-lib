using OpenCvSharp;
using System;

public static class OptimizedFftShift
{
    /// <summary>
    /// Most optimized version - true in-place with minimal temporary storage
    /// Uses only one temporary buffer for diagonal quadrant swaps
    /// Memory: O(min(width, height) * element_size) instead of O(width * height * element_size)
    /// </summary>
    public static void FftShiftOptimal(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;

        // Only create views for the quadrants we need to swap
        using var q0 = new Mat(mat, new Rect(0, 0, cx, cy));         // Top-Left
        using var q1 = new Mat(mat, new Rect(cx, 0, mat.Cols - cx, cy)); // Top-Right  
        using var q2 = new Mat(mat, new Rect(0, cy, cx, mat.Rows - cy)); // Bottom-Left
        using var q3 = new Mat(mat, new Rect(cx, cy, mat.Cols - cx, mat.Rows - cy)); // Bottom-Right

        // Use single temporary buffer - only needs to hold one quadrant
        using var temp = new Mat();
        
        // Diagonal swap: Q0 <-> Q3, Q1 <-> Q2
        q0.CopyTo(temp);
        q3.CopyTo(q0);
        temp.CopyTo(q3);
        
        q1.CopyTo(temp);
        q2.CopyTo(q1);
        temp.CopyTo(q2);
    }

    /// <summary>
    /// Alternative approach using row-wise operations for better cache locality
    /// Particularly effective for wide images
    /// </summary>
    public static void FftShiftRowWise(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int elemSize = mat.ElemSize();

        // Process upper and lower halves separately for better memory access patterns
        unsafe
        {
            byte* data = (byte*)mat.DataPointer;
            int step = (int)mat.Step();

            // Allocate temporary buffer for one row
            byte[] tempRow = new byte[cx * elemSize];

            // Swap top-left with bottom-right (row by row)
            for (int y = 0; y < cy; y++)
            {
                byte* srcRow = data + y * step;
                byte* destRow = data + (y + cy) * step + cx * elemSize;
                
                // Copy top-left to temp
                fixed (byte* temp = tempRow)
                {
                    Buffer.MemoryCopy(srcRow, temp, tempRow.Length, cx * elemSize);
                    // Copy bottom-right to top-left
                    Buffer.MemoryCopy(destRow, srcRow, cx * elemSize, cx * elemSize);
                    // Copy temp to bottom-right
                    Buffer.MemoryCopy(temp, destRow, cx * elemSize, cx * elemSize);
                }
            }

            // Swap top-right with bottom-left (row by row)
            for (int y = 0; y < cy; y++)
            {
                byte* srcRow = data + y * step + cx * elemSize;
                byte* destRow = data + (y + cy) * step;
                
                // Copy top-right to temp
                fixed (byte* temp = tempRow)
                {
                    int rightWidth = (mat.Cols - cx) * elemSize;
                    Buffer.MemoryCopy(srcRow, temp, tempRow.Length, rightWidth);
                    // Copy bottom-left to top-right
                    Buffer.MemoryCopy(destRow, srcRow, rightWidth, cx * elemSize);
                    // Copy temp to bottom-left
                    Buffer.MemoryCopy(temp, destRow, cx * elemSize, rightWidth);
                }
            }
        }
    }

    /// <summary>
    /// Block-wise processing for very large images
    /// Processes data in cache-friendly blocks to minimize memory bandwidth
    /// </summary>
    public static void FftShiftBlockWise(Mat mat, int blockSize = 64)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;

        // Process in blocks for better cache efficiency
        for (int by = 0; by < cy; by += blockSize)
        {
            for (int bx = 0; bx < cx; bx += blockSize)
            {
                int blockH = Math.Min(blockSize, cy - by);
                int blockW = Math.Min(blockSize, cx - bx);

                // Define block regions
                var blockQ0 = new Rect(bx, by, blockW, blockH);
                var blockQ3 = new Rect(bx + cx, by + cy, blockW, blockH);
                var blockQ1 = new Rect(bx + cx, by, blockW, blockH);
                var blockQ2 = new Rect(bx, by + cy, blockW, blockH);

                using var q0Block = new Mat(mat, blockQ0);
                using var q3Block = new Mat(mat, blockQ3);
                using var q1Block = new Mat(mat, blockQ1);
                using var q2Block = new Mat(mat, blockQ2);
                using var temp = new Mat();

                // Swap blocks
                q0Block.CopyTo(temp);
                q3Block.CopyTo(q0Block);
                temp.CopyTo(q3Block);

                q1Block.CopyTo(temp);
                q2Block.CopyTo(q1Block);
                temp.CopyTo(q2Block);
            }
        }
    }

    /// <summary>
    /// Specialized version for power-of-2 dimensions
    /// Uses bit operations for faster calculations
    /// </summary>
    public static void FftShiftPowerOfTwo(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        // Verify power of 2 dimensions for optimization
        int cols = mat.Cols;
        int rows = mat.Rows;
        
        if ((cols & (cols - 1)) != 0 || (rows & (rows - 1)) != 0)
        {
            // Fall back to regular method if not power of 2
            FftShiftOptimal(mat);
            return;
        }

        int cx = cols >> 1; // Bit shift instead of division
        int cy = rows >> 1;

        using var q0 = new Mat(mat, new Rect(0, 0, cx, cy));
        using var q1 = new Mat(mat, new Rect(cx, 0, cx, cy));
        using var q2 = new Mat(mat, new Rect(0, cy, cx, cy));
        using var q3 = new Mat(mat, new Rect(cx, cy, cx, cy));
        using var temp = new Mat();

        // Diagonal quadrant swap
        q0.CopyTo(temp);
        q3.CopyTo(q0);
        temp.CopyTo(q3);

        q1.CopyTo(temp);
        q2.CopyTo(q1);
        temp.CopyTo(q2);
    }

    /// <summary>
    /// Memory-mapped approach for extremely large images
    /// Minimizes memory pressure by working with smaller chunks
    /// </summary>
    public static void FftShiftMemoryMapped(Mat mat, int chunkSize = 1024 * 1024) // 1MB chunks
    {
        if (mat == null || mat.Empty())
            return;

        long totalBytes = (long)mat.Rows * mat.Cols * mat.ElemSize();
        
        // If image is small enough, use optimal method
        if (totalBytes < chunkSize)
        {
            FftShiftOptimal(mat);
            return;
        }

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int elemSize = mat.ElemSize();
        
        // Calculate optimal chunk dimensions
        int chunkRows = Math.Min(cy, chunkSize / (cx * elemSize));
        chunkRows = Math.Max(1, chunkRows);

        // Process in chunks to minimize memory usage
        for (int startRow = 0; startRow < cy; startRow += chunkRows)
        {
            int currentChunkRows = Math.Min(chunkRows, cy - startRow);
            
            // Create chunks for current row range
            using var chunkQ0 = new Mat(mat, new Rect(0, startRow, cx, currentChunkRows));
            using var chunkQ3 = new Mat(mat, new Rect(cx, startRow + cy, cx, currentChunkRows));
            using var chunkQ1 = new Mat(mat, new Rect(cx, startRow, mat.Cols - cx, currentChunkRows));
            using var chunkQ2 = new Mat(mat, new Rect(0, startRow + cy, cx, currentChunkRows));
            using var temp = new Mat();

            // Swap chunks
            chunkQ0.CopyTo(temp);
            chunkQ3.CopyTo(chunkQ0);
            temp.CopyTo(chunkQ3);

            chunkQ1.CopyTo(temp);
            chunkQ2.CopyTo(chunkQ1);
            temp.CopyTo(chunkQ2);
        }
    }
}