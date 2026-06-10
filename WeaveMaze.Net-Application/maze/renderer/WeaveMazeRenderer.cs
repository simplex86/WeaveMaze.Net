using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    internal class WeaveMazeRenderer
    {
        private int width;
        private int height;
        private WeaveMazeField field;

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.05f;
        private const bool DefaultRoundedCorners = true;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private bool roundedCorners = DefaultRoundedCorners;
        private Color wallColor = Color.Black;
        private Color backgroundColor = Color.White;

        private readonly RectangularWeaveMazeBuilder pathBuilder = new();

        public WeaveMazeRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public WeaveMazeRenderer SetField(WeaveMazeField field) { this.field = field; return this; }
        public WeaveMazeRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public WeaveMazeRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public WeaveMazeRenderer SetRoundedCorners(bool value) { roundedCorners = value; pathBuilder.RoundedCorners = value; return this; }
        public WeaveMazeRenderer SetWallColor(Color color) { wallColor = color; return this; }
        public WeaveMazeRenderer SetBackgroundColor(Color color) { backgroundColor = color; return this; }

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

            var state = grap.Save();
            grap.SmoothingMode = SmoothingMode.AntiAlias;
            grap.PixelOffsetMode = PixelOffsetMode.Half;

            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                grap.FillRectangle(bgBrush, 0, 0, width, height);
            }

            grap.TranslateTransform(offsetX, offsetY);

            var lineW = lineWidthFrac * cellSize;

            using (var wallPen = new Pen(wallColor, lineW))
            {
                wallPen.StartCap = roundedCorners ? LineCap.Round : LineCap.Square;
                wallPen.EndCap = roundedCorners ? LineCap.Round : LineCap.Square;
                wallPen.LineJoin = LineJoin.Round;
                DrawWallPaths(grap, wallPen, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            }

            grap.Restore(state);
        }

        #region 墙壁绘制

        private void DrawWallPaths(Graphics grap,
                                   Pen pen,
                                   SquareCell[][] cells,
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
            pathBuilder.MoveTo(path, ox + d0, oy);
            pathBuilder.LineTo(path, ox + d0, oy + cellSize);
            pathBuilder.MoveTo(path, ox + d1, oy);
            pathBuilder.LineTo(path, ox + d1, oy + cellSize);
            pathBuilder.MoveTo(path, ox, oy + d0);
            pathBuilder.LineTo(path, ox + d0, oy + d0);
            pathBuilder.MoveTo(path, ox, oy + d1);
            pathBuilder.LineTo(path, ox + d0, oy + d1);
            pathBuilder.MoveTo(path, ox + d1, oy + d0);
            pathBuilder.LineTo(path, ox + cellSize, oy + d0);
            pathBuilder.MoveTo(path, ox + d1, oy + d1);
            pathBuilder.LineTo(path, ox + cellSize, oy + d1);
        }

        private void DrawWallEastWestOver(GraphicsPath path, float ox, float oy, float cellSize, float d0, float d1)
        {
            pathBuilder.MoveTo(path, ox, oy + d0);
            pathBuilder.LineTo(path, ox + cellSize, oy + d0);
            pathBuilder.MoveTo(path, ox, oy + d1);
            pathBuilder.LineTo(path, ox + cellSize, oy + d1);
            pathBuilder.MoveTo(path, ox + d0, oy);
            pathBuilder.LineTo(path, ox + d0, oy + d0);
            pathBuilder.MoveTo(path, ox + d1, oy);
            pathBuilder.LineTo(path, ox + d1, oy + d0);
            pathBuilder.MoveTo(path, ox + d0, oy + d1);
            pathBuilder.LineTo(path, ox + d0, oy + cellSize);
            pathBuilder.MoveTo(path, ox + d1, oy + d1);
            pathBuilder.LineTo(path, ox + d1, oy + cellSize);
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
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.LineTo(path, ox + d0, oy + dm);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + d1, oy + dm, r0);
                    pathBuilder.LineTo(path, ox + d1, oy);
                    break;
                case 0b0100:
                    pathBuilder.MoveTo(path, ox + cellSize, oy + d0);
                    pathBuilder.LineTo(path, ox + dm, oy + d0);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox + d0, oy + dm, r0);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.LineTo(path, ox + cellSize, oy + d1);
                    break;
                case 0b0010:
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.LineTo(path, ox + d0, oy + dm);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox + dm, oy + d0, r0);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    pathBuilder.LineTo(path, ox + d1, oy + cellSize);
                    break;
                case 0b0001:
                    pathBuilder.MoveTo(path, ox, oy + d0);
                    pathBuilder.LineTo(path, ox + dm, oy + d0);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.LineTo(path, ox, oy + d1);
                    break;

                case 0b1100:
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox + cellSize, oy + d1, d1);
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    break;
                case 0b0110:
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox + cellSize, oy + d0, d1);
                    pathBuilder.MoveTo(path, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0011:
                    pathBuilder.MoveTo(path, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox, oy + d0, d1);
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1001:
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox, oy + d1, d1);
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1010:
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.LineTo(path, ox + d0, oy + cellSize);
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.LineTo(path, ox + d1, oy + cellSize);
                    break;
                case 0b0101:
                    pathBuilder.MoveTo(path, ox, oy + d0);
                    pathBuilder.LineTo(path, ox + cellSize, oy + d0);
                    pathBuilder.MoveTo(path, ox, oy + d1);
                    pathBuilder.LineTo(path, ox + cellSize, oy + d1);
                    break;

                case 0b1101:
                    pathBuilder.MoveTo(path, ox, oy + d1);
                    pathBuilder.LineTo(path, ox + cellSize, oy + d1);
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
                case 0b1110:
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.LineTo(path, ox + d0, oy + cellSize);
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(path, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0111:
                    pathBuilder.MoveTo(path, ox, oy + d0);
                    pathBuilder.LineTo(path, ox + cellSize, oy + d0);
                    pathBuilder.MoveTo(path, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1011:
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.LineTo(path, ox + d1, oy + cellSize);
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1111:
                    pathBuilder.MoveTo(path, ox + d1, oy);
                    pathBuilder.ArcTo(path, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(path, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    pathBuilder.MoveTo(path, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + d0, oy + d1, ox, oy + d1, d0);
                    pathBuilder.MoveTo(path, ox + d0, oy);
                    pathBuilder.ArcTo(path, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
            }
        }

        #endregion
    }
}
