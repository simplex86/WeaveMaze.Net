using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// 迷宫解法路径渲染器。独立于迷宫墙壁渲染器，
    /// 自行计算布局参数并管理 Graphics 状态。
    /// </summary>
    internal class RectangularWeaveMazeSolutionRenderer
    {
        private int width;
        private int height;
        private WeaveMazeField field;
        private WeaveMazeSolution solution;

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.05f;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private Color solutionColor = Color.Red;
        private bool roundedCorners = true;

        private readonly RectangularWeaveMazeBuilder pathBuilder = new();

        // 解路径方向位掩码：N=0b1000, E=0b0100, S=0b0010, W=0b0001
        private Dictionary<SquareCell, int> lowerSolDirs = new();
        private Dictionary<SquareCell, int> upperSolDirs = new();

        public RectangularWeaveMazeSolutionRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public RectangularWeaveMazeSolutionRenderer SetField(WeaveMazeField field) { this.field = field; return this; }
        public RectangularWeaveMazeSolutionRenderer SetSolution(WeaveMazeSolution solution) { this.solution = solution; return this; }
        public RectangularWeaveMazeSolutionRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public RectangularWeaveMazeSolutionRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public RectangularWeaveMazeSolutionRenderer SetSolutionColor(Color color) { solutionColor = color; return this; }
        public RectangularWeaveMazeSolutionRenderer SetRoundedCorners(bool value) { roundedCorners = value; pathBuilder.RoundedCorners = value; return this; }

        public void Draw(Graphics grap)
        {
            var cells = field.Cells;
            if (cells == null) return;

            var mazeHeight = field.Height;
            var mazeWidth = field.Width;

            var cellSize = Math.Min((float)width / mazeWidth, (float)height / mazeHeight);
            var offsetX = ((float)width - cellSize * mazeWidth) / 2;
            var offsetY = ((float)height - cellSize * mazeHeight) / 2;

            var cellMarginFrac = (1 - passageWidthFrac) / 2;
            var d0 = cellMarginFrac * cellSize;
            var d1 = (1 - cellMarginFrac) * cellSize;
            var dm = cellSize / 2;
            var r0 = (d1 - d0) / 2;
            var lineW = lineWidthFrac * cellSize;

            var state = grap.Save();
            grap.SmoothingMode = SmoothingMode.AntiAlias;
            grap.PixelOffsetMode = PixelOffsetMode.Half;
            grap.TranslateTransform(offsetX, offsetY);

            BuildSolutionDirs(cells, mazeHeight, mazeWidth);

            using var solPen = new Pen(solutionColor, lineW);
            solPen.StartCap = LineCap.Round;
            solPen.EndCap = LineCap.Round;
            solPen.LineJoin = LineJoin.Round;
            DrawSolutionPaths(grap, solPen, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);

            grap.Restore(state);
        }

        #region 解路径方向构建

        /// <summary>
        /// 从 WeaveMazeSolution.Path 构建每个单元格的解路径方向位掩码。
        /// lowerSolDirs[cell] = lower 层的解路径方向（N=0b1000, E=0b0100, S=0b0010, W=0b0001）
        /// upperSolDirs[cell] = upper 层的解路径方向
        /// </summary>
        private void BuildSolutionDirs(SquareCell[][] cells, int height, int width)
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
            AddTerminalDir(solPath[0]);
            AddTerminalDir(solPath[solPath.Count - 1]);
        }

        private void AddDir(SquareNode node, int dir)
        {
            var cell = node.Cell;
            var dict = (node == cell.Upper) ? upperSolDirs : lowerSolDirs;
            dict.TryGetValue(cell, out int existing);
            dict[cell] = existing | dir;
        }

        private void AddTerminalDir(SquareNode node)
        {
            // WireTerminal 将端点的出口方向设为自引用，据此确定终端方向
            if (node.North == node) AddDir(node, 0b1000);
            else if (node.East == node) AddDir(node, 0b0100);
            else if (node.South == node) AddDir(node, 0b0010);
            else if (node.West == node) AddDir(node, 0b0001);
        }

        #endregion

        #region 解路径绘制

        private void DrawSolutionPaths(Graphics grap,
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

                    var lowerDir = lowerSolDirs.TryGetValue(cell, out var ld) ? ld : 0;
                    var upperDir = upperSolDirs.TryGetValue(cell, out var ud) ? ud : 0;

                    if (cell.Upper.North != null)
                    {
                        // 南北跨越：upper 层走南北，lower 层走东西
                        if ((upperDir & 0b1010) != 0)
                        {
                            pathBuilder.MoveTo(path, ox + dm, oy);
                            pathBuilder.LineTo(path, ox + dm, oy + cellSize);
                        }
                        if ((lowerDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(path, ox, oy + dm);
                            pathBuilder.LineTo(path, ox + d0, oy + dm);
                            pathBuilder.MoveTo(path, ox + d1, oy + dm);
                            pathBuilder.LineTo(path, ox + cellSize, oy + dm);
                        }
                    }
                    else if (cell.Upper.East != null)
                    {
                        // 东西跨越：upper 层走东西，lower 层走南北
                        if ((upperDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(path, ox, oy + dm);
                            pathBuilder.LineTo(path, ox + cellSize, oy + dm);
                        }
                        if ((lowerDir & 0b1010) != 0)
                        {
                            pathBuilder.MoveTo(path, ox + dm, oy);
                            pathBuilder.LineTo(path, ox + dm, oy + d0);
                            pathBuilder.MoveTo(path, ox + dm, oy + d1);
                            pathBuilder.LineTo(path, ox + dm, oy + cellSize);
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
                case 0b1000:
                    pathBuilder.MoveTo(path, ox + dm, oy);
                    pathBuilder.LineTo(path, ox + dm, oy + dm);
                    break;
                case 0b0100:
                    pathBuilder.MoveTo(path, ox + cellSize, oy + dm);
                    pathBuilder.LineTo(path, ox + dm, oy + dm);
                    break;
                case 0b0010:
                    pathBuilder.MoveTo(path, ox + dm, oy + cellSize);
                    pathBuilder.LineTo(path, ox + dm, oy + dm);
                    break;
                case 0b0001:
                    pathBuilder.MoveTo(path, ox, oy + dm);
                    pathBuilder.LineTo(path, ox + dm, oy + dm);
                    break;

                case 0b1100:
                    pathBuilder.MoveTo(path, ox + dm, oy);
                    pathBuilder.ArcTo(path, ox + dm, oy + dm, ox + cellSize, oy + dm, dm);
                    break;
                case 0b0110:
                    pathBuilder.MoveTo(path, ox + cellSize, oy + dm);
                    pathBuilder.ArcTo(path, ox + dm, oy + dm, ox + dm, oy + cellSize, dm);
                    break;
                case 0b0011:
                    pathBuilder.MoveTo(path, ox + dm, oy + cellSize);
                    pathBuilder.ArcTo(path, ox + dm, oy + dm, ox, oy + dm, dm);
                    break;
                case 0b1001:
                    pathBuilder.MoveTo(path, ox, oy + dm);
                    pathBuilder.ArcTo(path, ox + dm, oy + dm, ox + dm, oy, dm);
                    break;

                case 0b1010:
                    pathBuilder.MoveTo(path, ox + dm, oy);
                    pathBuilder.LineTo(path, ox + dm, oy + cellSize);
                    break;
                case 0b0101:
                    pathBuilder.MoveTo(path, ox, oy + dm);
                    pathBuilder.LineTo(path, ox + cellSize, oy + dm);
                    break;
            }
        }

        #endregion
    }
}
