using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫解法路径渲染器。平台无关实现，通过 IGraphicsContext 抽象绘图操作。
    /// </summary>
    public class WeaveMazeSolutionRenderer
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
        private MazeColor solutionColor = MazeColor.Red;
        private bool roundedCorners = true;

        private readonly WeaveMazeBuilder pathBuilder = new();

        // 解路径方向位掩码：N=0b1000, E=0b0100, S=0b0010, W=0b0001
        private Dictionary<SquareCell, int> lowerSolDirs = new();
        private Dictionary<SquareCell, int> upperSolDirs = new();

        public WeaveMazeSolutionRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public WeaveMazeSolutionRenderer SetField(WeaveMazeField field) { this.field = field; return this; }
        public WeaveMazeSolutionRenderer SetSolution(WeaveMazeSolution solution) { this.solution = solution; return this; }
        public WeaveMazeSolutionRenderer SetGates(WeaveMazeGate[]? gates) { this.gates = gates; return this; }
        public WeaveMazeSolutionRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public WeaveMazeSolutionRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public WeaveMazeSolutionRenderer SetSolutionColor(MazeColor color) { solutionColor = color; return this; }
        public WeaveMazeSolutionRenderer SetRoundedCorners(bool value) { roundedCorners = value; pathBuilder.RoundedCorners = value; return this; }

        public void Draw(IGraphicsContext context)
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

            // 平移变换
            context.PushTranslate(offsetX, offsetY);

            BuildSolutionDirs(cells, mazeHeight, mazeWidth);

            // 构建并描边解路径
            context.BeginPath();
            DrawSolutionPaths(context, cells, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            context.EndPath();
            context.StrokePath(solutionColor, lineW, true);

            context.PopTransform();
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

            // 为路径端点添加终端方向（从出入口数据获取）
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

        private void DrawSolutionPaths(IGraphicsContext context,
                                       SquareCell[][] cells,
                                       int mazeHeight,
                                       int mazeWidth,
                                       float cellSize,
                                       float d0,
                                       float d1,
                                       float dm,
                                       float r0)
        {
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
                            pathBuilder.MoveTo(context, ox + dm, oy);
                            pathBuilder.LineTo(context, ox + dm, oy + cellSize);
                        }
                        if ((lowerDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(context, ox, oy + dm);
                            pathBuilder.LineTo(context, ox + d0, oy + dm);
                            pathBuilder.MoveTo(context, ox + d1, oy + dm);
                            pathBuilder.LineTo(context, ox + cellSize, oy + dm);
                        }
                    }
                    else if (cell.Upper.East != null)
                    {
                        // 东西跨越：upper 层走东西，lower 层走南北
                        if ((upperDir & 0b0101) != 0)
                        {
                            pathBuilder.MoveTo(context, ox, oy + dm);
                            pathBuilder.LineTo(context, ox + cellSize, oy + dm);
                        }
                        if ((lowerDir & 0b1010) != 0)
                        {
                            pathBuilder.MoveTo(context, ox + dm, oy);
                            pathBuilder.LineTo(context, ox + dm, oy + d0);
                            pathBuilder.MoveTo(context, ox + dm, oy + d1);
                            pathBuilder.LineTo(context, ox + dm, oy + cellSize);
                        }
                    }
                    else if (lowerDir != 0)
                    {
                        DrawSolutionFlat(context, ox, oy, cellSize, dm, lowerDir);
                    }
                }
            }
        }

        private void DrawSolutionFlat(IGraphicsContext context, float ox, float oy, float cellSize, float dm, int value)
        {
            switch (value)
            {
                case 0b1000:
                    pathBuilder.MoveTo(context, ox + dm, oy);
                    pathBuilder.LineTo(context, ox + dm, oy + dm);
                    break;
                case 0b0100:
                    pathBuilder.MoveTo(context, ox + cellSize, oy + dm);
                    pathBuilder.LineTo(context, ox + dm, oy + dm);
                    break;
                case 0b0010:
                    pathBuilder.MoveTo(context, ox + dm, oy + cellSize);
                    pathBuilder.LineTo(context, ox + dm, oy + dm);
                    break;
                case 0b0001:
                    pathBuilder.MoveTo(context, ox, oy + dm);
                    pathBuilder.LineTo(context, ox + dm, oy + dm);
                    break;

                case 0b1100:
                    pathBuilder.MoveTo(context, ox + dm, oy);
                    pathBuilder.ArcTo(context, ox + dm, oy + dm, ox + cellSize, oy + dm, dm);
                    break;
                case 0b0110:
                    pathBuilder.MoveTo(context, ox + cellSize, oy + dm);
                    pathBuilder.ArcTo(context, ox + dm, oy + dm, ox + dm, oy + cellSize, dm);
                    break;
                case 0b0011:
                    pathBuilder.MoveTo(context, ox + dm, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + dm, oy + dm, ox, oy + dm, dm);
                    break;
                case 0b1001:
                    pathBuilder.MoveTo(context, ox, oy + dm);
                    pathBuilder.ArcTo(context, ox + dm, oy + dm, ox + dm, oy, dm);
                    break;

                case 0b1010:
                    pathBuilder.MoveTo(context, ox + dm, oy);
                    pathBuilder.LineTo(context, ox + dm, oy + cellSize);
                    break;
                case 0b0101:
                    pathBuilder.MoveTo(context, ox, oy + dm);
                    pathBuilder.LineTo(context, ox + cellSize, oy + dm);
                    break;
            }
        }

        #endregion
    }
}
