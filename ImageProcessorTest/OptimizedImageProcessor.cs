using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

public static class OptimizedImageProcessor
{
    private static List<(Mat mask, int area)> FindComponentAreas(Mat image, byte targetValue)
    {
        var mask = new Mat();
        Cv2.InRange(image, new Scalar(targetValue), new Scalar(targetValue), mask);
    
        var labels = new Mat();
        var stats = new Mat();
        var centroids = new Mat();
    
        int numComponents = Cv2.ConnectedComponentsWithStats(mask, labels, stats, centroids, PixelConnectivity.Connectivity4);
    
        var components = new List<(Mat mask, int area)>();
    
        for (int i = 1; i < numComponents; i++)
        {
            int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
        
            var componentMask = new Mat();
            Cv2.InRange(labels, new Scalar(i), new Scalar(i), componentMask);
        
            components.Add((componentMask, area));
        }
    
        mask.Dispose();
        labels.Dispose();
        stats.Dispose();
        centroids.Dispose();
    
        return components;
    }

    // Original implementation
    public static ITransformableObject ForceTwoComponentsBruteForce(Mat binaryImage, bool showImages = false)
    {
        if (binaryImage.Type() != MatType.CV_8UC1)
            throw new ArgumentException("Input image must be binary CV_8UC1 with values 0 or 255.");

        var result = binaryImage.Clone();
        var blackComponents = FindComponentAreas(result, 0);
        var whiteComponents = FindComponentAreas(result, 255);

        Mat largestBlackMask = null;
        Mat largestWhiteMask = null;

        if (blackComponents.Count > 0)
        {
            var largestBlack = blackComponents.OrderByDescending(x => x.area).First();
            largestBlackMask = largestBlack.mask.Clone();
        }

        if (whiteComponents.Count > 0)
        {
            var largestWhite = whiteComponents.OrderByDescending(x => x.area).First();
            largestWhiteMask = largestWhite.mask.Clone();
        }

        var newResult = new Mat(result.Size(), MatType.CV_8UC1);

        for (int y = 0; y < result.Rows; y++)
        {
            for (int x = 0; x < result.Cols; x++)
            {
                bool belongsToLargestBlack = largestBlackMask != null && largestBlackMask.At<byte>(y, x) > 0;
                bool belongsToLargestWhite = largestWhiteMask != null && largestWhiteMask.At<byte>(y, x) > 0;

                if (belongsToLargestBlack)
                {
                    newResult.Set<byte>(y, x, 0);
                }
                else if (belongsToLargestWhite)
                {
                    newResult.Set<byte>(y, x, 255);
                }
                else
                {
                    byte nearestColor = FindNearestLargeComponentColor(x, y, largestBlackMask, largestWhiteMask);
                    newResult.Set<byte>(y, x, nearestColor);
                }
            }
        }

        var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        var cleaned = new Mat();
        Cv2.MorphologyEx(newResult, cleaned, MorphTypes.Close, kernel);

        foreach (var component in blackComponents)
            component.mask.Dispose();
        foreach (var component in whiteComponents)
            component.mask.Dispose();
        largestBlackMask?.Dispose();
        largestWhiteMask?.Dispose();
        newResult.Dispose();
        kernel.Dispose();

        return new TransformableObject(cleaned, MatFormat.TwoDimensional8BitUnsignedIntOneChannel);
    }

    private static byte FindNearestLargeComponentColor(int x, int y, Mat largestBlackMask, Mat largestWhiteMask)
    {
        int searchRadius = 20;
        double nearestBlackDistance = double.MaxValue;
        double nearestWhiteDistance = double.MaxValue;

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    if (checkX >= 0 && checkX < (largestBlackMask?.Cols ?? largestWhiteMask?.Cols ?? 0) &&
                        checkY >= 0 && checkY < (largestBlackMask?.Rows ?? largestWhiteMask?.Rows ?? 0))
                    {
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        if (largestBlackMask != null && largestBlackMask.At<byte>(checkY, checkX) > 0)
                        {
                            nearestBlackDistance = Math.Min(nearestBlackDistance, distance);
                        }

                        if (largestWhiteMask != null && largestWhiteMask.At<byte>(checkY, checkX) > 0)
                        {
                            nearestWhiteDistance = Math.Min(nearestWhiteDistance, distance);
                        }
                    }
                }
            }

            if (nearestBlackDistance < double.MaxValue && nearestWhiteDistance < double.MaxValue)
                break;
        }

        if (nearestBlackDistance < nearestWhiteDistance)
            return 0;
        else if (nearestWhiteDistance < nearestBlackDistance)
            return 255;
        else
            return 128;
    }

    // OPTIMIZED VERSION: Using distance transforms
    public static ITransformableObject ForceTwoComponentsOptimized(Mat binaryImage, bool showImages = false)
    {
        if (binaryImage.Type() != MatType.CV_8UC1)
            throw new ArgumentException("Input image must be binary CV_8UC1 with values 0 or 255.");

        var result = binaryImage.Clone();
        var blackComponents = FindComponentAreas(result, 0);
        var whiteComponents = FindComponentAreas(result, 255);

        Mat largestBlackMask = new Mat(result.Size(), MatType.CV_8UC1, Scalar.All(0));
        Mat largestWhiteMask = new Mat(result.Size(), MatType.CV_8UC1, Scalar.All(0));

        if (blackComponents.Count > 0)
        {
            var largestBlack = blackComponents.OrderByDescending(x => x.area).First();
            largestBlack.mask.CopyTo(largestBlackMask);
        }

        if (whiteComponents.Count > 0)
        {
            var largestWhite = whiteComponents.OrderByDescending(x => x.area).First();
            largestWhite.mask.CopyTo(largestWhiteMask);
        }

        var newResult = new Mat(result.Size(), MatType.CV_8UC1);
        var combinedLargestMask = new Mat();
        Cv2.BitwiseOr(largestBlackMask, largestWhiteMask, combinedLargestMask);

        var unassignedMask = new Mat();
        Cv2.BitwiseNot(combinedLargestMask, unassignedMask);

        largestBlackMask.CopyTo(newResult, largestBlackMask);
        newResult.SetTo(Scalar.All(255), largestWhiteMask);

        if (Cv2.CountNonZero(unassignedMask) > 0)
        {
            var distFromBlack = new Mat();
            var invertedBlackMask = new Mat();
            Cv2.BitwiseNot(largestBlackMask, invertedBlackMask);
            Cv2.DistanceTransform(invertedBlackMask, distFromBlack, DistanceTypes.L2, DistanceTransformMasks.Mask3);

            var distFromWhite = new Mat();
            var invertedWhiteMask = new Mat();
            Cv2.BitwiseNot(largestWhiteMask, invertedWhiteMask);
            Cv2.DistanceTransform(invertedWhiteMask, distFromWhite, DistanceTypes.L2, DistanceTransformMasks.Mask3);

            var assignToBlack = new Mat();
            Cv2.Compare(distFromBlack, distFromWhite, assignToBlack, CmpType.LT);

            var assignToBlackUnassigned = new Mat();
            var assignToWhiteUnassigned = new Mat();
            Cv2.BitwiseAnd(assignToBlack, unassignedMask, assignToBlackUnassigned);
            Cv2.BitwiseNot(assignToBlackUnassigned, assignToWhiteUnassigned);
            Cv2.BitwiseAnd(assignToWhiteUnassigned, unassignedMask, assignToWhiteUnassigned);

            newResult.SetTo(Scalar.All(0), assignToBlackUnassigned);
            newResult.SetTo(Scalar.All(255), assignToWhiteUnassigned);

            distFromBlack.Dispose();
            distFromWhite.Dispose();
            invertedBlackMask.Dispose();
            invertedWhiteMask.Dispose();
            assignToBlack.Dispose();
            assignToBlackUnassigned.Dispose();
            assignToWhiteUnassigned.Dispose();
        }

        var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        var cleaned = new Mat();
        Cv2.MorphologyEx(newResult, cleaned, MorphTypes.Close, kernel);

        foreach (var component in blackComponents)
            component.mask.Dispose();
        foreach (var component in whiteComponents)
            component.mask.Dispose();
        largestBlackMask.Dispose();
        largestWhiteMask.Dispose();
        combinedLargestMask.Dispose();
        unassignedMask.Dispose();
        newResult.Dispose();
        kernel.Dispose();

        return new TransformableObject(cleaned, MatFormat.TwoDimensional8BitUnsignedIntOneChannel);
    }
}