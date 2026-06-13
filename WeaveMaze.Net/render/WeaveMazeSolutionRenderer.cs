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
        // 键为单元格一维索引 (y * Width + x)
        private Dictionary<int, int> lowerSolDirs = new();
        private Dictionary<int, int> upperSolDirs = new();

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
            var graph = field.Graph;
            if (graph == null) return;

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

            BuildSolutionDirs(graph, mazeHeight, mazeWidth);

            // 构建并描边解路径
            context.BeginPath();
            DrawSolutionPaths(context, graph, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            context.EndPath();
            context.StrokePath(solutionColor, lineW, true);

            context.PopTransform();
        }

        #region 解路径方向构建

        /// <summary>
        /// 从 WeaveMazeSolution.Path 构建每个单元格的解路径方向位掩码。
        /// lowerSolDirs[cellIndex] = lower 层的解路径方向（N=0b1000, E=0b0100, S=0b0010, W=0b0001）
        /// upperSolDirs[cellIndex] = upper 层的解路径方向
        /// </summary>
        private void BuildSolutionDirs(List<List<WeaveAdjacency>> graph, int height, int width)
        {
            lowerSolDirs.Clear();
            upperSolDirs.Clear();

            var solPath = solution.Path;
            if (solPath == null || solPath.Count == 0) return;

            // 遍历路径中每对相邻顶点，记录方向
            for (int i = 0; i < solPath.Count - 1; i++)
            {
                var v0 = solPath[i];
                var v1 = solPath[i + 1];

                // 在 v0 的邻接表中查找 v1 的方向
                int dir0 = FindDirection(graph, v0, v1);
                // 反方向
                int dir1 = OppositeDir(dir0);

                AddDir(v0, dir0);
                AddDir(v1, dir1);
            }

            // 为路径端点添加终端方向（从出入口数据获取）
            AddGateTerminalDirs();
        }

        /// <summary>
        /// 在顶点 v0 的邻接表中查找指向 v1 的边方向。
        /// </summary>
        private static int FindDirection(List<List<WeaveAdjacency>> graph, int v0, int v1)
        {
            foreach (var adj in graph[v0])
            {
                if (adj.Neighbor == v1) return adj.Direction;
            }
            return -1;
        }

        /// <summary>方向取反：0↔2, 1↔3</summary>
        private static int OppositeDir(int dir) => dir switch
        {
            0 => 2,
            1 => 3,
            2 => 0,
            3 => 1,
            _ => -1
        };

        /// <summary>方向常量转位掩码</summary>
        private static int DirToBit(int dir) => dir switch
        {
            0 => 0b1000,
            1 => 0b0100,
            2 => 0b0010,
            3 => 0b0001,
            _ => 0
        };

        private void AddDir(int vertex, int dir)
        {
            var cellX = field.VertexCellX![vertex];
            var cellY = field.VertexCellY![vertex];
            int cellIndex = field.CellIndex(cellX, cellY);
            var dict = field.VertexIsUpper![vertex] ? upperSolDirs : lowerSolDirs;
            dict.TryGetValue(cellIndex, out int existing);
            dict[cellIndex] = existing | DirToBit(dir);
        }

        private void AddGateTerminalDirs()
        {
            if (gates == null) return;
            foreach (var gate in gates)
            {
                AddDir(field.LowerIndex(gate.CellX, gate.CellY), gate.Direction);
            }
        }

        #endregion

        #region 解路径绘制

        private void DrawSolutionPaths(IGraphicsContext context,
                                       List<List<WeaveAdjacency>> graph,
                                       int mazeHeight,
                                       int mazeWidth,
                                       float cellSize,
                                       float d0,
                                       float d1,
                                       float dm,
                                       float r0)
        {
            var cellWhite = field.CellWhite!;
            var cellOverNS = field.CellOverNS!;
            var cellOverEW = field.CellOverEW!;

            for (int i = 0; i < mazeHeight; i++)
            {
                var oy = i * cellSize;
                for (int j = 0; j < mazeWidth; j++)
                {
                    var ox = j * cellSize;
                    int cellIndex = field.CellIndex(j, i);

                    if (!cellWhite[cellIndex]) continue;

                    var lowerDir = lowerSolDirs.TryGetValue(cellIndex, out var ld) ? ld : 0;
                    var upperDir = upperSolDirs.TryGetValue(cellIndex, out var ud) ? ud : 0;

                    if (cellOverNS[cellIndex])
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
                    else if (cellOverEW[cellIndex])
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
