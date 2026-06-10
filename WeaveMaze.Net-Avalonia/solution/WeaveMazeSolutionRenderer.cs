using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// 迷宫解法路径渲染器。独立于迷宫墙壁渲染器，
    /// 自行计算布局参数并管理 DrawingContext 状态。Avalonia 版本。
    /// </summary>
    internal class WeaveMazeSolutionRenderer
    {
        private int width;
        private int height;
        private WeaveMazeField field;
        private WeaveMazeSolution solution;
        private WeaveMazeGate[]? gates;

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.05f;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private Color solutionColor = Colors.Red;
        private bool roundedCorners = true;

        private readonly WeaveMazeBuilder pathBuilder = new();

        private Dictionary<SquareCell, int> lowerSolDirs = new();
        private Dictionary<SquareCell, int> upperSolDirs = new();

        public WeaveMazeSolutionRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public WeaveMazeSolutionRenderer SetField(WeaveMazeField field) { this.field = field; return this; }
        public WeaveMazeSolutionRenderer SetSolution(WeaveMazeSolution solution) { this.solution = solution; return this; }
        public WeaveMazeSolutionRenderer SetGates(WeaveMazeGate[]? gates) { this.gates = gates; return this; }
        public WeaveMazeSolutionRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public WeaveMazeSolutionRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public WeaveMazeSolutionRenderer SetSolutionColor(Color color) { solutionColor = color; return this; }
        public WeaveMazeSolutionRenderer SetRoundedCorners(bool value) { roundedCorners = value; pathBuilder.RoundedCorners = value; return this; }

        public void Draw(DrawingContext context)
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

            using var translate = context.PushTransform(
                Matrix.CreateTranslation(offsetX, offsetY));

            BuildSolutionDirs(cells, mazeHeight, mazeWidth);

            var solPen = new Pen(new SolidColorBrush(solutionColor), lineW)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            DrawSolutionPaths(context, solPen, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
        }

        #region 解路径方向构建

        private void BuildSolutionDirs(SquareCell[][] cells, int height, int width)
        {
            lowerSolDirs.Clear();
            upperSolDirs.Clear();

            var solPath = solution.Path;
            if (solPath == null || solPath.Count == 0) return;

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

            AddGateTerminalDirs();
        }

        private void AddDir(SquareNode node, int dir)
        {
            var cell = node.Cell;
            var dict = (node == cell.Upper) ? upperSolDirs : lowerSolDirs;
            dict.TryGetValue(cell, out int existing);
            dict[cell] = existing | dir;
        }

        private void AddGateTerminalDirs()
        {
            if (gates == null) return;
            foreach (var gate in gates)
            {
                AddDir(gate.Cell.Lower, gate.DirectionBit);
            }
        }

        #endregion

        #region 解路径绘制

        private void DrawSolutionPaths(DrawingContext context,
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
            var geometry = pathBuilder.BeginGeometry();

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
                        if ((upperDir & 0b1010) != 0)
                        {
                            pathBuilder.MoveTo(geometry, ox + dm, oy);
                            pathBuilder.LineTo(geometry, ox + dm, oy + cellSize);
                        }
                        if ((lowerDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(geometry, ox, oy + dm);
                            pathBuilder.LineTo(geometry, ox + d0, oy + dm);
                            pathBuilder.MoveTo(geometry, ox + d1, oy + dm);
                            pathBuilder.LineTo(geometry, ox + cellSize, oy + dm);
                        }
                    }
                    else if (cell.Upper.East != null)
                    {
                        if ((upperDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(geometry, ox, oy + dm);
                            pathBuilder.LineTo(geometry, ox + cellSize, oy + dm);
                        }
                        if ((lowerDir & 0b1010) != 0)
                        {
                            pathBuilder.MoveTo(geometry, ox + dm, oy);
                            pathBuilder.LineTo(geometry, ox + dm, oy + d0);
                            pathBuilder.MoveTo(geometry, ox + dm, oy + d1);
                            pathBuilder.LineTo(geometry, ox + dm, oy + cellSize);
                        }
                    }
                    else if (lowerDir != 0)
                    {
                        DrawSolutionFlat(geometry, ox, oy, cellSize, dm, lowerDir);
                    }
                }
            }

            pathBuilder.EndGeometry();
            context.DrawGeometry(null, pen, geometry);
        }

        private void DrawSolutionFlat(StreamGeometry path, float ox, float oy, float cellSize, float dm, int value)
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
