using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    internal class RectangularWeaveMazeRenderer
    {
        private int width;
        private int height;
        private RectangularWeaveMazeField field;
        private WeaveMazeSolution solution;

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.05f;
        private const bool DefaultRoundedCorners = true;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private bool roundedCorners = DefaultRoundedCorners;
        private Color wallColor = Color.Black;
        private Color solutionColor = Color.Red;
        private Color backgroundColor = Color.White;
        private bool showSolution = true;

        private float cursorX, cursorY;

        // 解路径方向位掩码：N=0b1000, E=0b0100, S=0b0010, W=0b0001
        private Dictionary<Cell, int> lowerSolDirs = new();
        private Dictionary<Cell, int> upperSolDirs = new();

        public RectangularWeaveMazeRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public RectangularWeaveMazeRenderer SetField(RectangularWeaveMazeField field) { this.field = field; return this; }
        public RectangularWeaveMazeRenderer SetSolution(WeaveMazeSolution solution) { this.solution = solution; return this; }
        public RectangularWeaveMazeRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public RectangularWeaveMazeRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public RectangularWeaveMazeRenderer SetRoundedCorners(bool value) { roundedCorners = value; return this; }
        public RectangularWeaveMazeRenderer SetWallColor(Color color) { wallColor = color; return this; }
        public RectangularWeaveMazeRenderer SetSolutionColor(Color color) { solutionColor = color; return this; }
        public RectangularWeaveMazeRenderer SetBackgroundColor(Color color) { backgroundColor = color; return this; }
        public RectangularWeaveMazeRenderer SetShowSolution(bool value) { showSolution = value; return this; }

        public void Draw(Graphics grap)
        {
            var cells = field.Cells;
            if (cells == null) return;

            var mazeHeight = field.Height;
            var mazeWidth  = field.Width;

            var cellSize = Math.Min((float)width / mazeWidth, (float)height / mazeHeight);
            var offsetX = ((float)width - cellSize * mazeWidth) / 2;
            var offsetY = ((float)height - cellSize * mazeHeight) / 2;

            var cellMarginFrac = (1 - passageWidthFrac) / 2;
            var d0 = cellMarginFrac * cellSize;
            var d1 = (1 - cellMarginFrac) * cellSize;
            var dm = cellSize / 2;
            var r0 = (d1 - d0) / 2;

            grap.SmoothingMode = SmoothingMode.AntiAlias;
            grap.PixelOffsetMode = PixelOffsetMode.Half;

            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                grap.FillRectangle(bgBrush, 0, 0, width, height);
            }

            grap.TranslateTransform(offsetX, offsetY);

            var lineW = lineWidthFrac * cellSize;

            // 从 WeaveMazeSolution 构建解路径方向数据
            BuildSolutionDirs(cells, mazeHeight, mazeWidth);

            if (showSolution)
            {
                using (var solPen = new Pen(solutionColor, lineW))
                {
                    solPen.StartCap = LineCap.Round;
                    solPen.EndCap = LineCap.Round;
                    solPen.LineJoin = LineJoin.Round;
                    DrawSolutionPaths(grap, solPen, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
                }
            }

            using (var wallPen = new Pen(wallColor, lineW))
            {
                wallPen.StartCap = roundedCorners ? LineCap.Round : LineCap.Square;
                wallPen.EndCap = roundedCorners ? LineCap.Round : LineCap.Square;
                wallPen.LineJoin = LineJoin.Round;
                DrawWallPaths(grap, wallPen, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            }
        }

        #region 解路径方向构建

        /// <summary>
        /// 从 WeaveMazeSolution.Path 构建每个单元格的解路径方向位掩码。
        /// lowerSolDirs[cell] = lower 层的解路径方向（N=0b1000, E=0b0100, S=0b0010, W=0b0001）
        /// upperSolDirs[cell] = upper 层的解路径方向
        /// </summary>
        private void BuildSolutionDirs(Cell[][] cells, int height, int width)
        {
            lowerSolDirs.Clear();
            upperSolDirs.Clear();

            var solPath = solution.Path;
            if (solPath == null || solPath.Count == 0) return;

            // 遍历路径中每对相邻节点，记录方向
            for (int i = 0; i < solPath.Count - 1; i++)
            {
                var n0 = solPath[i];
                var n1 = solPath[i + 1];

                if (n0.North == n1)
                {
                    AddDir(n0, 0b1000);
                    AddDir(n1, 0b0010);
                }
                else if (n0.East == n1)
                {
                    AddDir(n0, 0b0100);
                    AddDir(n1, 0b0001);
                }
                else if (n0.South == n1)
                {
                    AddDir(n0, 0b0010);
                    AddDir(n1, 0b1000);
                }
                else if (n0.West == n1)
                {
                    AddDir(n0, 0b0001);
                    AddDir(n1, 0b0100);
                }
            }

            // 为路径端点添加终端方向（指向迷宫边界外）
            AddTerminalDir(cells, height, width, solPath[0]);
            AddTerminalDir(cells, height, width, solPath[solPath.Count - 1]);
        }

        private void AddDir(Node node, int dir)
        {
            var cell = node.Cell;
            var dict = (node == cell.Upper) ? upperSolDirs : lowerSolDirs;
            dict.TryGetValue(cell, out int existing);
            dict[cell] = existing | dir;
        }

        private void AddTerminalDir(Cell[][] cells, int height, int width, Node node)
        {
            var cell = node.Cell;
            // 找到指向迷宫边界外的方向
            if (cell.Y == 0 || !cells[cell.Y - 1][cell.X].White)
                AddDir(node, 0b1000);
            if (cell.X == width - 1 || !cells[cell.Y][cell.X + 1].White)
                AddDir(node, 0b0100);
            if (cell.Y == height - 1 || !cells[cell.Y + 1][cell.X].White)
                AddDir(node, 0b0010);
            if (cell.X == 0 || !cells[cell.Y][cell.X - 1].White)
                AddDir(node, 0b0001);
        }

        #endregion

        #region 路径绘制原语

        private void MoveTo(GraphicsPath path, float x, float y)
        {
            path.StartFigure();
            cursorX = x;
            cursorY = y;
        }

        private void LineTo(GraphicsPath path, float x, float y)
        {
            path.AddLine(cursorX, cursorY, x, y);
            cursorX = x;
            cursorY = y;
        }

        private void ArcTo(GraphicsPath path, float x1, float y1, float x2, float y2, float radius)
        {
            if (!roundedCorners)
            {
                LineTo(path, x1, y1);
                LineTo(path, x2, y2);
                return;
            }

            var dx1 = x1 - cursorX;
            var dy1 = y1 - cursorY;
            var len1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (len1 < 0.001f) { MoveTo(path, x2, y2); return; }
            dx1 /= len1; dy1 /= len1;

            var dx2 = x2 - x1;
            var dy2 = y2 - y1;
            var len2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (len2 < 0.001f) { LineTo(path, x1, y1); return; }
            dx2 /= len2; dy2 /= len2;

            if (radius > len1) radius = len1;
            if (radius > len2) radius = len2;

            var t1x = x1 - dx1 * radius;
            var t1y = y1 - dy1 * radius;
            var t2x = x1 + dx2 * radius;
            var t2y = y1 + dy2 * radius;

            var cx = x1 + radius * (dx2 - dx1);
            var cy = y1 + radius * (dy2 - dy1);

            path.AddLine(cursorX, cursorY, t1x, t1y);

            var startAngle = (float)Math.Atan2(t1y - cy, t1x - cx) * 180f / (float)Math.PI;
            var endAngle   = (float)Math.Atan2(t2y - cy, t2x - cx) * 180f / (float)Math.PI;
            var sweep = endAngle - startAngle;
            if (sweep > 180) sweep -= 360;
            if (sweep < -180) sweep += 360;

            path.AddArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweep);

            cursorX = t2x;
            cursorY = t2y;
        }

        #endregion

        #region 墙壁绘制

        private void DrawWallPaths(Graphics grap, 
                                   Pen pen, 
                                   Cell[][] cells, 
                                   int mazeHeight, 
                                   int mazeWidth,
                                   float cellSize, 
                                   float d0, 
                                   float d1, 
                                   float dm, 
                                   float r0)
        {
            var path = new GraphicsPath();

            for (int i = 0; i < mazeHeight; i++)
            {
                var oy = i * cellSize;
                for (int j = 0; j < mazeWidth; j++)
                {
                    var ox = j * cellSize;
                    var cell = cells[i][j];

                    if (!cell.White) continue;

                    if (cell.Upper.North != null)
                    {
                        DrawWallNorthSouthOver(path, ox, oy, cellSize, d0, d1);
                    }
                    else if (cell.Upper.East != null)
                    {
                        DrawWallEastWestOver(path, ox, oy, cellSize, d0, d1);
                    }
                    else
                    {
                        var lower = cell.Lower;
                        int value = (lower.North != null ? 0b1000 : 0) |
                                    (lower.East  != null ? 0b0100 : 0) |
                                    (lower.South != null ? 0b0010 : 0) |
                                    (lower.West  != null ? 0b0001 : 0);

                        DrawWallFlat(path, ox, oy, cellSize, d0, d1, dm, r0, value);
                    }
                }
            }

            grap.DrawPath(pen, path);
        }

        private void DrawWallNorthSouthOver(GraphicsPath path, float ox, float oy, float cellSize, float d0, float d1)
        {
            MoveTo(path, ox + d0, oy);
            LineTo(path, ox + d0, oy + cellSize);
            MoveTo(path, ox + d1, oy);
            LineTo(path, ox + d1, oy + cellSize);
            MoveTo(path, ox, oy + d0);
            LineTo(path, ox + d0, oy + d0);
            MoveTo(path, ox, oy + d1);
            LineTo(path, ox + d0, oy + d1);
            MoveTo(path, ox + d1, oy + d0);
            LineTo(path, ox + cellSize, oy + d0);
            MoveTo(path, ox + d1, oy + d1);
            LineTo(path, ox + cellSize, oy + d1);
        }

        private void DrawWallEastWestOver(GraphicsPath path, float ox, float oy, float cellSize, float d0, float d1)
        {
            MoveTo(path, ox, oy + d0);
            LineTo(path, ox + cellSize, oy + d0);
            MoveTo(path, ox, oy + d1);
            LineTo(path, ox + cellSize, oy + d1);
            MoveTo(path, ox + d0, oy);
            LineTo(path, ox + d0, oy + d0);
            MoveTo(path, ox + d1, oy);
            LineTo(path, ox + d1, oy + d0);
            MoveTo(path, ox + d0, oy + d1);
            LineTo(path, ox + d0, oy + cellSize);
            MoveTo(path, ox + d1, oy + d1);
            LineTo(path, ox + d1, oy + cellSize);
        }

        private void DrawWallFlat(GraphicsPath path, 
                                  float ox, 
                                  float oy, 
                                  float cellSize,
                                  float d0, 
                                  float d1, 
                                  float dm, 
                                  float r0, 
                                  int value)
        {
            switch (value)
            {
                case 0b1000:
                    MoveTo(path, ox + d0, oy);
                    LineTo(path, ox + d0, oy + dm);
                    ArcTo(path, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    ArcTo(path, ox + d1, oy + d1, ox + d1, oy + dm, r0);
                    LineTo(path, ox + d1, oy);
                    break;
                case 0b0100:
                    MoveTo(path, ox + cellSize, oy + d0);
                    LineTo(path, ox + dm, oy + d0);
                    ArcTo(path, ox + d0, oy + d0, ox + d0, oy + dm, r0);
                    ArcTo(path, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    LineTo(path, ox + cellSize, oy + d1);
                    break;
                case 0b0010:
                    MoveTo(path, ox + d0, oy + cellSize);
                    LineTo(path, ox + d0, oy + dm);
                    ArcTo(path, ox + d0, oy + d0, ox + dm, oy + d0, r0);
                    ArcTo(path, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    LineTo(path, ox + d1, oy + cellSize);
                    break;
                case 0b0001:
                    MoveTo(path, ox, oy + d0);
                    LineTo(path, ox + dm, oy + d0);
                    ArcTo(path, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    ArcTo(path, ox + d1, oy + d1, ox + dm, oy + d1, r0);
                    LineTo(path, ox, oy + d1);
                    break;

                case 0b1100:
                    MoveTo(path, ox + d0, oy);
                    ArcTo(path, ox + d0, oy + d1, ox + cellSize, oy + d1, d1);
                    MoveTo(path, ox + d1, oy);
                    ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    break;
                case 0b0110:
                    MoveTo(path, ox + d0, oy + cellSize);
                    ArcTo(path, ox + d0, oy + d0, ox + cellSize, oy + d0, d1);
                    MoveTo(path, ox + d1, oy + cellSize);
                    ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0011:
                    MoveTo(path, ox + d1, oy + cellSize);
                    ArcTo(path, ox + d1, oy + d0, ox, oy + d0, d1);
                    MoveTo(path, ox + d0, oy + cellSize);
                    ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1001:
                    MoveTo(path, ox + d1, oy);
                    ArcTo(path, ox + d1, oy + d1, ox, oy + d1, d1);
                    MoveTo(path, ox + d0, oy);
                    ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1010:
                    MoveTo(path, ox + d0, oy);
                    LineTo(path, ox + d0, oy + cellSize);
                    MoveTo(path, ox + d1, oy);
                    LineTo(path, ox + d1, oy + cellSize);
                    break;
                case 0b0101:
                    MoveTo(path, ox, oy + d0);
                    LineTo(path, ox + cellSize, oy + d0);
                    MoveTo(path, ox, oy + d1);
                    LineTo(path, ox + cellSize, oy + d1);
                    break;

                case 0b1101:
                    MoveTo(path, ox, oy + d1);
                    LineTo(path, ox + cellSize, oy + d1);
                    MoveTo(path, ox + d1, oy);
                    ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    MoveTo(path, ox + d0, oy);
                    ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
                case 0b1110:
                    MoveTo(path, ox + d0, oy);
                    LineTo(path, ox + d0, oy + cellSize);
                    MoveTo(path, ox + d1, oy);
                    ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    MoveTo(path, ox + d1, oy + cellSize);
                    ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0111:
                    MoveTo(path, ox, oy + d0);
                    LineTo(path, ox + cellSize, oy + d0);
                    MoveTo(path, ox + d1, oy + cellSize);
                    ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    MoveTo(path, ox + d0, oy + cellSize);
                    ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1011:
                    MoveTo(path, ox + d1, oy);
                    LineTo(path, ox + d1, oy + cellSize);
                    MoveTo(path, ox + d0, oy + cellSize);
                    ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    MoveTo(path, ox + d0, oy);
                    ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1111:
                    MoveTo(path, ox + d1, oy);
                    ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    MoveTo(path, ox + d1, oy + cellSize);
                    ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    MoveTo(path, ox + d0, oy + cellSize);
                    ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    MoveTo(path, ox + d0, oy);
                    ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
            }
        }

        #endregion

        #region 解路径绘制

        private void DrawSolutionPaths(Graphics grap,
                                       Pen pen, 
                                       Cell[][] cells, 
                                       int mazeHeight, 
                                       int mazeWidth,
                                       float cellSize, 
                                       float d0, 
                                       float d1, 
                                       float dm, 
                                       float r0)
        {
            var path = new GraphicsPath();

            for (int i = 0; i < mazeHeight; i++)
            {
                var oy = i * cellSize;
                for (int j = 0; j < mazeWidth; j++)
                {
                    var ox = j * cellSize;
                    var cell = cells[i][j];

                    if (!cell.White) continue;

                    var lowerDir = lowerSolDirs.TryGetValue(cell, out var ld) ? ld : 0;
                    var upperDir = upperSolDirs.TryGetValue(cell, out var ud) ? ud : 0;

                    if (cell.Upper.North != null)
                    {
                        // 南北跨越：upper 层走南北，lower 层走东西
                        if ((upperDir & 0b1010) != 0)
                        {
                            MoveTo(path, ox + dm, oy);
                            LineTo(path, ox + dm, oy + cellSize);
                        }
                        if ((lowerDir & 0b0101) != 0)
                        {
                            MoveTo(path, ox, oy + dm);
                            LineTo(path, ox + d0, oy + dm);
                            MoveTo(path, ox + d1, oy + dm);
                            LineTo(path, ox + cellSize, oy + dm);
                        }
                    }
                    else if (cell.Upper.East != null)
                    {
                        // 东西跨越：upper 层走东西，lower 层走南北
                        if ((upperDir & 0b0101) != 0)
                        {
                            MoveTo(path, ox, oy + dm);
                            LineTo(path, ox + cellSize, oy + dm);
                        }
                        if ((lowerDir & 0b1010) != 0)
                        {
                            MoveTo(path, ox + dm, oy);
                            LineTo(path, ox + dm, oy + d0);
                            MoveTo(path, ox + dm, oy + d1);
                            LineTo(path, ox + dm, oy + cellSize);
                        }
                    }
                    else if (lowerDir != 0)
                    {
                        DrawSolutionFlat(path, ox, oy, cellSize, dm, lowerDir);
                    }
                }
            }

            grap.DrawPath(pen, path);
        }

        private void DrawSolutionFlat(GraphicsPath path, float ox, float oy, float cellSize, float dm, int value)
        {
            switch (value)
            {
                case 0b1100:
                    MoveTo(path, ox + dm, oy);
                    ArcTo(path, ox + dm, oy + dm, ox + cellSize, oy + dm, dm);
                    break;
                case 0b0110:
                    MoveTo(path, ox + cellSize, oy + dm);
                    ArcTo(path, ox + dm, oy + dm, ox + dm, oy + cellSize, dm);
                    break;
                case 0b0011:
                    MoveTo(path, ox + dm, oy + cellSize);
                    ArcTo(path, ox + dm, oy + dm, ox, oy + dm, dm);
                    break;
                case 0b1001:
                    MoveTo(path, ox, oy + dm);
                    ArcTo(path, ox + dm, oy + dm, ox + dm, oy, dm);
                    break;

                case 0b1010:
                    MoveTo(path, ox + dm, oy);
                    LineTo(path, ox + dm, oy + cellSize);
                    break;
                case 0b0101:
                    MoveTo(path, ox, oy + dm);
                    LineTo(path, ox + cellSize, oy + dm);
                    break;
            }
        }

        #endregion
    }
}
