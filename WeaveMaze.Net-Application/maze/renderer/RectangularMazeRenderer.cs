using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    internal class RectangularMazeRenderer
    {
        private int width;
        private int height;
        private RectangularWeaveMazeField field;

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.15f;
        private const bool DefaultRoundedCorners = true;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private bool roundedCorners = DefaultRoundedCorners;
        private Color wallColor = Color.Black;
        private Color solutionColor = Color.Red;
        private Color backgroundColor = Color.White;
        private bool showSolution = true;

        private float cursorX, cursorY;

        public RectangularMazeRenderer SetSize(int width, int height)
        {
            this.width = width;
            this.height = height;
            return this;
        }

        public RectangularMazeRenderer SetField(RectangularWeaveMazeField field)
        {
            this.field = field;
            return this;
        }

        public RectangularMazeRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public RectangularMazeRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public RectangularMazeRenderer SetRoundedCorners(bool value) { roundedCorners = value; return this; }
        public RectangularMazeRenderer SetWallColor(Color color) { wallColor = color; return this; }
        public RectangularMazeRenderer SetSolutionColor(Color color) { solutionColor = color; return this; }
        public RectangularMazeRenderer SetBackgroundColor(Color color) { backgroundColor = color; return this; }
        public RectangularMazeRenderer SetShowSolution(bool value) { showSolution = value; return this; }

        public void Draw(Graphics grap)
        {
            var cells = field.Cells;
            if (cells == null) return;

            int mazeHeight = field.Height;
            int mazeWidth = field.Width;

            float cellSize = Math.Min((float)width / mazeWidth, (float)height / mazeHeight);
            float offsetX = ((float)width - cellSize * mazeWidth) / 2;
            float offsetY = ((float)height - cellSize * mazeHeight) / 2;

            float cellMarginFrac = (1 - passageWidthFrac) / 2;
            float d0 = cellMarginFrac * cellSize;
            float d1 = (1 - cellMarginFrac) * cellSize;
            float dm = cellSize / 2;
            float r0 = (d1 - d0) / 2;

            grap.SmoothingMode = SmoothingMode.AntiAlias;
            grap.PixelOffsetMode = PixelOffsetMode.Half;

            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                grap.FillRectangle(bgBrush, 0, 0, width, height);
            }

            grap.TranslateTransform(offsetX, offsetY);

            float lineW = lineWidthFrac * cellSize;

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

            float dx1 = x1 - cursorX;
            float dy1 = y1 - cursorY;
            float len1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (len1 < 0.001f) { MoveTo(path, x2, y2); return; }
            dx1 /= len1; dy1 /= len1;

            float dx2 = x2 - x1;
            float dy2 = y2 - y1;
            float len2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (len2 < 0.001f) { LineTo(path, x1, y1); return; }
            dx2 /= len2; dy2 /= len2;

            if (radius > len1) radius = len1;
            if (radius > len2) radius = len2;

            float t1x = x1 - dx1 * radius;
            float t1y = y1 - dy1 * radius;
            float t2x = x1 + dx2 * radius;
            float t2y = y1 + dy2 * radius;

            float cx = x1 + radius * (dx2 - dx1);
            float cy = y1 + radius * (dy2 - dy1);

            path.AddLine(cursorX, cursorY, t1x, t1y);

            float startAngle = (float)Math.Atan2(t1y - cy, t1x - cx) * 180f / (float)Math.PI;
            float endAngle = (float)Math.Atan2(t2y - cy, t2x - cx) * 180f / (float)Math.PI;
            float sweep = endAngle - startAngle;
            if (sweep > 180) sweep -= 360;
            if (sweep < -180) sweep += 360;

            path.AddArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweep);

            cursorX = t2x;
            cursorY = t2y;
        }

        #endregion

        #region 墙壁绘制

        private void DrawWallPaths(Graphics grap, Pen pen, Cell[][] cells, int mazeHeight, int mazeWidth,
            float cellSize, float d0, float d1, float dm, float r0)
        {
            var path = new GraphicsPath();

            for (int i = 0; i < mazeHeight; i++)
            {
                float oy = i * cellSize;
                for (int j = 0; j < mazeWidth; j++)
                {
                    float ox = j * cellSize;
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
                                    (lower.East != null ? 0b0100 : 0) |
                                    (lower.South != null ? 0b0010 : 0) |
                                    (lower.West != null ? 0b0001 : 0);

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

        private void DrawWallFlat(GraphicsPath path, float ox, float oy, float cellSize,
            float d0, float d1, float dm, float r0, int value)
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

        private void DrawSolutionPaths(Graphics grap, Pen pen, Cell[][] cells, int mazeHeight, int mazeWidth,
            float cellSize, float d0, float d1, float dm, float r0)
        {
            var path = new GraphicsPath();

            for (int i = 0; i < mazeHeight; i++)
            {
                float oy = i * cellSize;
                for (int j = 0; j < mazeWidth; j++)
                {
                    float ox = j * cellSize;
                    var cell = cells[i][j];

                    if (!cell.White) continue;

                    if (cell.Upper.North2 != null)
                    {
                        MoveTo(path, ox + dm, oy);
                        LineTo(path, ox + dm, oy + cellSize);
                    }
                    else if (cell.Upper.East2 != null)
                    {
                        MoveTo(path, ox, oy + dm);
                        LineTo(path, ox + cellSize, oy + dm);
                    }

                    if (cell.Upper.North != null && cell.Lower.East2 != null)
                    {
                        MoveTo(path, ox, oy + dm);
                        LineTo(path, ox + d0, oy + dm);
                        MoveTo(path, ox + d1, oy + dm);
                        LineTo(path, ox + cellSize, oy + dm);
                    }
                    else if (cell.Upper.East != null && cell.Lower.North2 != null)
                    {
                        MoveTo(path, ox + dm, oy);
                        LineTo(path, ox + dm, oy + d0);
                        MoveTo(path, ox + dm, oy + d1);
                        LineTo(path, ox + dm, oy + cellSize);
                    }
                    else if (cell.Upper.North == null && cell.Upper.East == null)
                    {
                        var lower = cell.Lower;
                        int value = (lower.North2 != null ? 0b1000 : 0) |
                                    (lower.East2 != null ? 0b0100 : 0) |
                                    (lower.South2 != null ? 0b0010 : 0) |
                                    (lower.West2 != null ? 0b0001 : 0);

                        DrawSolutionFlat(path, ox, oy, cellSize, dm, value);
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
