using System;
using System.Collections.Generic;
using System.Linq;
using GoRogue;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace FingerprintGenerator
{
    /// <summary>
    /// A fingerprint
    /// </summary>
    /// <remarks>
    /// The pattern is represented in a 64x64 ArrayView of enum for easy memory/rending.
    /// </remarks>
    public class Fingerprint
    {
        private int _seed;
        private readonly int _width = 64;
        private readonly int _height = 64;
        
        public ArrayView<RidgeType> Pattern { get; }

        public Fingerprint(int seed)
        {
            _seed = seed;
            Pattern = Generate();
        }

        public ArrayView<RidgeType> Generate()
        {
            var random = new Random(_seed);
            var pattern = SeedStartingPattern(random);
            
            while (pattern.Positions().Any(p => pattern[p] == RidgeType.Undefined))
                pattern = NextFingerPrintCycle(pattern);
            
            pattern = FinalizeRidges(pattern);

            return pattern;
        }

        private ArrayView<RidgeType> FinalizeRidges(ArrayView<RidgeType> pattern)
        {
            for (int i = 0; i < pattern.Width; i++)
            {
                for (int j = 0; j < pattern.Height; j++)
                {
                    if (DistanceBetween((i, j), (_width / 2, _height / 2)) > _width/2)
                        pattern[i, j] = RidgeType.Groove;
                    
                    else if (pattern[i, j] == RidgeType.RidgeUndefined)
                        pattern[i,j] = DistinguishRidgeFromNeighbors(pattern, (i, j));
                    
                    //otherwise, do nothing
                }
            }
        
            return pattern;
        }

        private ArrayView<RidgeType> SeedStartingPattern(Random random)
        {
            var pattern = new ArrayView<RidgeType>(_width, _height);

            //pick 8+4d8 starting points
            int startingPointCount = 8;
            startingPointCount += random.Next(0, 8) + 1;
            startingPointCount += random.Next(0, 8) + 1;
            startingPointCount += random.Next(0, 8) + 1;
            startingPointCount += random.Next(0, 8) + 1;

            for(int i = 0; i <  startingPointCount; i++)
            {
                var x = random.Next(1, 62);
                var y = random.Next(1, 62);
                
                int x1 = random.Next(1, 63);
                int y1 = random.Next(1, 63);
                int x2 = random.Next(1, 63);
                int y2 = random.Next(1, 63);
                
                foreach (var point in Lines.Get(x1, y1, x2, y2))
                {
                    var chance = random.Next(1, 101);
                    pattern[point] = chance % 100 <= 20 ? RidgeType.RidgeUndefined : RidgeType.Groove;
                }
            }
            return pattern;
        }

        private double DistanceBetween(Point p1, Point p2)
        {
            var xPrime = (p2.X - p1.X) * (p2.X - p1.X);
            var yPrime = (p2.Y - p1.Y) * (p2.Y - p1.Y);
            return Math.Sqrt(xPrime + yPrime);
        }

        private ArrayView<RidgeType> NextFingerPrintCycle(ArrayView<RidgeType> pattern)
        {
            var changes = new Dictionary<Point, RidgeType>();
            for (int i = 0; i < pattern.Width; i++)
            {
                for (int j = 0; j < pattern.Height; j++)
                {
                    if (pattern[i, j] == RidgeType.Undefined)
                    {
                        var change = SetStateFromUndefined(pattern, (i, j));
                        if (change != RidgeType.Undefined)
                            changes.Add((i, j), change);
                    }
                }
            }

            int k = 0;
            foreach (var change in changes)
            {
                k++;
                if (k % 13 == 0)
                {
                    if (Ridge(pattern, change.Key))
                        pattern[change.Key] = RidgeType.Groove;
                    else
                        pattern[change.Key] = RidgeType.RidgeUndefined;
                }
                pattern[change.Key] = change.Value;
            }

            return pattern;
        }

        private bool Ridge(ArrayView<RidgeType> pattern, Point pos) 
            => pattern[WrapPoint(pos)] >= RidgeType.RidgeUndefined;
        
        private RidgeType DistinguishRidgeFromNeighbors(ArrayView<RidgeType> pattern, Point pos)
        {
            var upRight = WrapPoint(pos + (1, -1));
            var downRight = WrapPoint(pos + (1, 1));
            var upLeft = WrapPoint(pos + (-1, -1));
            var downLeft = WrapPoint(pos + (-1, 1));

            var left = WrapPoint(pos + (-1, 0));
            var right = WrapPoint(pos + (1, 0));
            var down = WrapPoint(pos + (0, 1));
            var up = WrapPoint(pos + (0, -1));
            
            //Major lines
            if (Ridge(pattern, left) && Ridge(pattern, right))
                return RidgeType.RidgeLeftRightThick;
            
            if (Ridge(pattern, down) && Ridge(pattern, up))
                return RidgeType.RidgeUpDownThick;
            
            //top-left and bottom-right quadrants
            if ((pos.X < _width / 2 && pos.Y < _height/2) || (pos.X >= _width/2 && pos.Y >= _height/2))
            {
                if (Ridge(pattern, upRight) && Ridge(pattern, downLeft))
                    return RidgeType.RidgeSlash;
                if (Ridge(pattern, upRight) && Ridge(pattern, left))
                    return RidgeType.RidgeSlash;
                if (Ridge(pattern, downLeft) && Ridge(pattern, right))
                    return RidgeType.RidgeSlash;
                
                if (Ridge(pattern, upLeft) && Ridge(pattern, downRight))
                    return RidgeType.RidgeBackSlash;
                if (Ridge(pattern, upLeft) && Ridge(pattern, right))
                    return RidgeType.RidgeBackSlash;
                if (Ridge(pattern, left) && Ridge(pattern, downRight))
                    return RidgeType.RidgeBackSlash;
            }
            else
            {
                if (Ridge(pattern, upLeft) && Ridge(pattern, downRight))
                    return RidgeType.RidgeBackSlash;
                if (Ridge(pattern, upLeft) && Ridge(pattern, right))
                    return RidgeType.RidgeBackSlash;
                if (Ridge(pattern, left) && Ridge(pattern, downRight))
                    return RidgeType.RidgeBackSlash;
                
                if (Ridge(pattern, upRight) && Ridge(pattern, downLeft))
                    return RidgeType.RidgeSlash;
                if (Ridge(pattern, upRight) && Ridge(pattern, left))
                    return RidgeType.RidgeSlash;
                if (Ridge(pattern, downLeft) && Ridge(pattern, right))
                    return RidgeType.RidgeSlash;
            }
            
            //connecting long lines
            if (Ridge(pattern, upRight) && Ridge(pattern, down))
                return RidgeType.RidgeSlash;
            if (Ridge(pattern, up) && Ridge(pattern, downLeft))
                return RidgeType.RidgeSlash;
            if (Ridge(pattern, upLeft) && Ridge(pattern, down))
                return RidgeType.RidgeBackSlash;
            if (Ridge(pattern, up) && Ridge(pattern, downRight))
                return RidgeType.RidgeBackSlash;
            
            //multiple connections on bottom
            if (Ridge(pattern, downLeft) && Ridge(pattern, downRight))
                return RidgeType.RidgeA;
            if (Ridge(pattern, downLeft) && Ridge(pattern, down))
                return RidgeType.RidgeA;
            if (Ridge(pattern, downRight) && Ridge(pattern, down))
                return RidgeType.RidgeA;
            
            //Multiple connections from above
            if (Ridge(pattern, upLeft) && Ridge(pattern, upRight))
                return RidgeType.RidgeV;
            if (Ridge(pattern, upLeft) && Ridge(pattern, up))
                return RidgeType.RidgeV;
            if (Ridge(pattern, upRight) && Ridge(pattern, up))
                return RidgeType.RidgeV;
            
            //multiple connections from the right
            if (Ridge(pattern, downRight) && Ridge(pattern, upRight))
                return RidgeType.RidgeLessThan;
            if (Ridge(pattern, downRight) && Ridge(pattern, right))
                return RidgeType.RidgeLessThan;
            if (Ridge(pattern, upRight) && Ridge(pattern, right))
                return RidgeType.RidgeLessThan;
            
            //multiple connections from the left
            if (Ridge(pattern, downLeft) && Ridge(pattern, upLeft))
                return RidgeType.RidgeGreaterThan;
            if (Ridge(pattern, downLeft) && Ridge(pattern, left))
                return RidgeType.RidgeGreaterThan;
            if (Ridge(pattern, upLeft) && Ridge(pattern, left))
                return RidgeType.RidgeGreaterThan;
            
            //minor lines
            if (Ridge(pattern, left) || Ridge(pattern, right))
                return RidgeType.RidgeLeftRightThin;
            if (Ridge(pattern, down) || Ridge(pattern, up))
                return RidgeType.RidgeUpDownThin;
            
            return RidgeType.RidgeDefined;
        }
        

        private RidgeType SetStateFromUndefined(ArrayView<RidgeType> pattern, Point pos)
        {
            var neighbors = GetCardinalNeighboringStates(pattern, pos);
            if(neighbors.Contains(RidgeType.Groove))
                return RidgeType.RidgeUndefined;
            
            else if(neighbors.Any(s => IsRidge(s)))
            {
                if (Ridge(pattern, pos + (1, 0)))
                    return RidgeType.Groove;
                if (Ridge(pattern, pos + (-1, 0)))
                    return RidgeType.Groove;
                if (Ridge(pattern, pos + (0, -1)))
                    return RidgeType.Groove;
                if (Ridge(pattern, pos + (0, 1)))
                    return RidgeType.Groove;
            }

            return RidgeType.Undefined;
        }

        private bool IsRidge(RidgeType pattern) => pattern >= RidgeType.RidgeUndefined;

        public IEnumerable<RidgeType> GetCardinalNeighboringStates(ArrayView<RidgeType> pattern, Point point)
        {
            //cardinals
            yield return pattern[WrapPoint(point + (0, 1))];
            yield return pattern[WrapPoint(point + (0, -1))];
            yield return pattern[WrapPoint(point + (-1, 0))];
            yield return pattern[WrapPoint(point + (1, 0))];
        }
        public IEnumerable<RidgeType> GetDiagonalNeighboringStates(ArrayView<RidgeType> pattern, Point point)
        {
            //diagonals
            yield return pattern[WrapPoint(point + (1, 1))];
            yield return pattern[WrapPoint(point + (1, -1))];
            yield return pattern[WrapPoint(point + (-1, 1))];
            yield return pattern[WrapPoint(point + (-1, -1))];
        }
        
        private Point WrapPoint(Point point)
        {
            if (point.X >= _width)
                point = (_width - 1, point.Y);
            if (point.X < 0)
                point = (0, point.Y);

            if (point.Y >= _height)
                point = (point.X, _height - 1);
            if (point.Y < 0)
                point = (point.X, 0);

            return point;
        }
        public IEnumerable<string> PatternMap()
        {
            for (int i = 0; i < Pattern.Height; i++)
            {
                var line = "";

                for (int j = 0; j < Pattern.Width; j++)
                {
                    line += GetSymbol(Pattern[j, i]);
                }

                yield return line;
            }
        }

        private char GetSymbol(RidgeType fingerPrintCaState)
        {
            switch (fingerPrintCaState)
            {
                case RidgeType.Groove: return ' ';
                case RidgeType.RidgeA: return '^';
                case RidgeType.RidgeDefined: return '#';
                case RidgeType.RidgeV: return 'V';
                case RidgeType.RidgeSlash: return '/';
                case RidgeType.RidgeBackSlash: return '\\';
                case RidgeType.RidgeGreaterThan: return ')';
                case RidgeType.RidgeLessThan: return '(';
                case RidgeType.RidgeLeftRightThick: return '=';
                case RidgeType.RidgeLeftRightThin: return '-';
                case RidgeType.RidgeUpDownThick: return '|';
                case RidgeType.RidgeUpDownThin: return ':';
                default: return '#';
            }
        }
    }
}
