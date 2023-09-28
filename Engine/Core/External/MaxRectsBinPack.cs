/*
 	Based on the Public Domain MaxRectsBinPack.cpp source by Jukka Jylänki
 	https://github.com/juj/RectangleBinPack/
 
 	Ported to C# by Sven Magnus
 	This version is also public domain - do whatever you want with it.
*/
using System.Collections.Generic;

namespace Staple.Internal
{
    public class MaxRectsBinPack
    {
        public int binWidth = 0;
        public int binHeight = 0;
        public bool allowRotations;

        public List<Rect> usedRectangles = new();
        public List<Rect> freeRectangles = new();

        public enum FreeRectChoiceHeuristic
        {
            RectBestShortSideFit, //< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
            RectBestLongSideFit, //< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
            RectBestAreaFit, //< -BAF: Positions the rectangle into the smallest free rect into which it fits.
            RectBottomLeftRule, //< -BL: Does the Tetris placement.
            RectContactPointRule //< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
        };

        public MaxRectsBinPack(int width, int height, bool rotations = true)
        {
            Init(width, height, rotations);
        }

        public void Init(int width, int height, bool rotations = true)
        {
            binWidth = width;
            binHeight = height;
            allowRotations = rotations;

            Rect n = new(Vector2Int.Zero, new Vector2Int(width, height));

            usedRectangles.Clear();

            freeRectangles.Clear();
            freeRectangles.Add(n);
        }

        public Rect Insert(int width, int height, FreeRectChoiceHeuristic method)
        {
            Rect newNode = new();
            int score1 = 0; // Unused in this function. We don't need to know the score after finding the position.
            int score2 = 0;
            switch (method)
            {
                case FreeRectChoiceHeuristic.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2); break;
                case FreeRectChoiceHeuristic.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2); break;
                case FreeRectChoiceHeuristic.RectContactPointRule: newNode = FindPositionForNewNodeContactPoint(width, height, ref score1); break;
                case FreeRectChoiceHeuristic.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;
                case FreeRectChoiceHeuristic.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
            }

            if (newNode.Height == 0)
                return newNode;

            int numRectanglesToProcess = freeRectangles.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(freeRectangles[i], ref newNode))
                {
                    freeRectangles.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();

            usedRectangles.Add(newNode);
            return newNode;
        }

        public void Insert(List<Rect> rects, List<Rect> dst, FreeRectChoiceHeuristic method)
        {
            dst.Clear();

            while (rects.Count > 0)
            {
                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestRectIndex = -1;
                Rect bestNode = new Rect();

                for (int i = 0; i < rects.Count; ++i)
                {
                    int score1 = 0;
                    int score2 = 0;
                    Rect newNode = ScoreRect((int)rects[i].Width, (int)rects[i].Height, method, ref score1, ref score2);

                    if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                    {
                        bestScore1 = score1;
                        bestScore2 = score2;
                        bestNode = newNode;
                        bestRectIndex = i;
                    }
                }

                if (bestRectIndex == -1)
                    return;

                PlaceRect(bestNode);
                rects.RemoveAt(bestRectIndex);
            }
        }

        public void Remove(Rect rect)
        {
            usedRectangles.Remove(rect);
            freeRectangles.Add(rect);
            PruneFreeList();
        }

        void PlaceRect(Rect node)
        {
            int numRectanglesToProcess = freeRectangles.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(freeRectangles[i], ref node))
                {
                    freeRectangles.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();

            usedRectangles.Add(node);
        }

        Rect ScoreRect(int width, int height, FreeRectChoiceHeuristic method, ref int score1, ref int score2)
        {
            Rect newNode = new Rect();
            score1 = int.MaxValue;
            score2 = int.MaxValue;
            switch (method)
            {
                case FreeRectChoiceHeuristic.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2); break;
                case FreeRectChoiceHeuristic.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2); break;
                case FreeRectChoiceHeuristic.RectContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                    score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
                    break;
                case FreeRectChoiceHeuristic.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;
                case FreeRectChoiceHeuristic.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
            }

            // Cannot fit the current rectangle.
            if (newNode.Height == 0)
            {
                score1 = int.MaxValue;
                score2 = int.MaxValue;
            }

            return newNode;
        }

        /// Computes the ratio of used surface area.
        public float Occupancy()
        {
            ulong usedSurfaceArea = 0;
            for (int i = 0; i < usedRectangles.Count; ++i)
                usedSurfaceArea += (uint)usedRectangles[i].Width * (uint)usedRectangles[i].Height;

            return (float)usedSurfaceArea / (binWidth * binHeight);
        }

        Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
        {
            Rect bestNode = new();
            //memset(bestNode, 0, sizeof(Rect));

            bestY = int.MaxValue;

            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int topSideY = (int)freeRectangles[i].top + height;
                    if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].left < bestX))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestY = topSideY;
                        bestX = (int)freeRectangles[i].left;
                    }
                }
                if (allowRotations && freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int topSideY = (int)freeRectangles[i].top + width;
                    if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].left < bestX))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestY = topSideY;
                        bestX = (int)freeRectangles[i].left;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            Rect bestNode = new Rect();
            //memset(&bestNode, 0, sizeof(Rect));

            bestShortSideFit = int.MaxValue;

            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int leftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - width);
                    int leftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - height);
                    int shortSideFit = (int)Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = (int)Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (allowRotations && freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int flippedLeftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - height);
                    int flippedLeftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - width);
                    int flippedShortSideFit = (int)Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                    int flippedLongSideFit = (int)Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                    if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            Rect bestNode = new();
            //memset(&bestNode, 0, sizeof(Rect));

            bestLongSideFit = int.MaxValue;

            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int leftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - width);
                    int leftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - height);
                    int shortSideFit = (int)Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = (int)Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (allowRotations && freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int leftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - height);
                    int leftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - width);
                    int shortSideFit = (int)Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = (int)Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
        {
            Rect bestNode = new Rect();
            //memset(&bestNode, 0, sizeof(Rect));

            bestAreaFit = int.MaxValue;

            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                int areaFit = (int)freeRectangles[i].Width * (int)freeRectangles[i].Height - width * height;

                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int leftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - width);
                    int leftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - height);
                    int shortSideFit = (int)Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (allowRotations && freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int leftoverHoriz = (int)Math.Abs((int)freeRectangles[i].Width - height);
                    int leftoverVert = (int)Math.Abs((int)freeRectangles[i].Height - width);
                    int shortSideFit = (int)Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.left = freeRectangles[i].left;
                        bestNode.top = freeRectangles[i].top;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }
            return bestNode;
        }

        /// Returns 0 if the two intervals i1 and i2 are disjoint, or the length of their overlap otherwise.
        int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
        {
            if (i1end < i2start || i2end < i1start)
                return 0;
            return (int)(Math.Min(i1end, i2end) - Math.Max(i1start, i2start));
        }

        int ContactPointScoreNode(int x, int y, int width, int height)
        {
            int score = 0;

            if (x == 0 || x + width == binWidth)
                score += height;
            if (y == 0 || y + height == binHeight)
                score += width;

            for (int i = 0; i < usedRectangles.Count; ++i)
            {
                if (usedRectangles[i].left == x + width || usedRectangles[i].left + usedRectangles[i].Width == x)
                    score += CommonIntervalLength((int)usedRectangles[i].top, (int)usedRectangles[i].top + (int)usedRectangles[i].Height, y, y + height);
                if (usedRectangles[i].top == y + height || usedRectangles[i].top + usedRectangles[i].Height == y)
                    score += CommonIntervalLength((int)usedRectangles[i].left, (int)usedRectangles[i].left + (int)usedRectangles[i].Width, x, x + width);
            }
            return score;
        }

        Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
        {
            Rect bestNode = new();

            bestContactScore = -1;

            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int score = ContactPointScoreNode((int)freeRectangles[i].left, (int)freeRectangles[i].top, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.left = (int)freeRectangles[i].left;
                        bestNode.top = (int)freeRectangles[i].top;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestContactScore = score;
                    }
                }
                if (allowRotations && freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int score = ContactPointScoreNode((int)freeRectangles[i].left, (int)freeRectangles[i].top, height, width);
                    if (score > bestContactScore)
                    {
                        bestNode.left = (int)freeRectangles[i].left;
                        bestNode.top = (int)freeRectangles[i].top;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestContactScore = score;
                    }
                }
            }
            return bestNode;
        }

        bool SplitFreeNode(Rect freeNode, ref Rect usedNode)
        {
            // Test with SAT if the rectangles even intersect.
            if (usedNode.left >= freeNode.left + freeNode.Width || usedNode.left + usedNode.Width <= freeNode.left ||
                usedNode.top >= freeNode.top + freeNode.Height || usedNode.top + usedNode.Height <= freeNode.top)
                return false;

            if (usedNode.left < freeNode.left + freeNode.Width && usedNode.left + usedNode.Width > freeNode.left)
            {
                // New node at the top side of the used node.
                if (usedNode.top > freeNode.top && usedNode.top < freeNode.top + freeNode.Height)
                {
                    var newNode = freeNode;
                    newNode.Height = usedNode.top - newNode.top;
                    freeRectangles.Add(newNode);
                }

                // New node at the bottom side of the used node.
                if (usedNode.top + usedNode.Height < freeNode.top + freeNode.Height)
                {
                    var newNode = freeNode;
                    newNode.top = usedNode.top + usedNode.Height;
                    newNode.Height = freeNode.top + freeNode.Height - (usedNode.top + usedNode.Height);
                    freeRectangles.Add(newNode);
                }
            }

            if (usedNode.top < freeNode.top + freeNode.Height && usedNode.top + usedNode.Height > freeNode.top)
            {
                // New node at the left side of the used node.
                if (usedNode.left > freeNode.left && usedNode.left < freeNode.left + freeNode.Width)
                {
                    var newNode = freeNode;
                    newNode.Width = usedNode.left - newNode.left;
                    freeRectangles.Add(newNode);
                }

                // New node at the right side of the used node.
                if (usedNode.left + usedNode.Width < freeNode.left + freeNode.Width)
                {
                    var newNode = freeNode;
                    newNode.left = usedNode.left + usedNode.Width;
                    newNode.Width = freeNode.left + freeNode.Width - (usedNode.left + usedNode.Width);
                    freeRectangles.Add(newNode);
                }
            }

            return true;
        }

        void PruneFreeList()
        {
            for (int i = 0; i < freeRectangles.Count; ++i)
                for (int j = i + 1; j < freeRectangles.Count; ++j)
                {
                    if (IsContainedIn(freeRectangles[i], freeRectangles[j]))
                    {
                        freeRectangles.RemoveAt(i);
                        --i;
                        break;
                    }
                    if (IsContainedIn(freeRectangles[j], freeRectangles[i]))
                    {
                        freeRectangles.RemoveAt(j);
                        --j;
                    }
                }
        }

        bool IsContainedIn(Rect a, Rect b)
        {
            return a.left >= b.left && a.top >= b.top
                && a.left + a.Width <= b.left + b.Width
                && a.top + a.Height <= b.top + b.Height;
        }
    }
}
