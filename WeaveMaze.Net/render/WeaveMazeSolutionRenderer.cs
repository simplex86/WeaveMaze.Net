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

            BuildSolutionDirs(graph, field.Height, field.Width);

            if (field is CircularWeaveMazeField)
            {
                DrawCircularSolution(context, graph);
            }
            else
            {
                DrawRectangularSolution(context, graph);
            }
        }

        #region 矩形解路径渲染

        private void DrawRectangularSolution(IGraphicsContext context, List<List<WeaveAdjacency>> graph)
        {
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

            context.PushTranslate(offsetX, offsetY);

            context.BeginPath();
            DrawRectSolutionPaths(context, graph, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            context.EndPath();
            context.StrokePath(solutionColor, lineW, true);

            context.PopTransform();
        }

        private void DrawRectSolutionPaths(IGraphicsContext context,
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
                        DrawRectSolutionFlat(context, ox, oy, cellSize, dm, lowerDir);
                    }
                }
            }
        }

        private void DrawRectSolutionFlat(IGraphicsContext context, float ox, float oy, float cellSize, float dm, int value)
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

        #region 圆形解路径渲染

        /// <summary>
        /// 圆形迷宫解路径渲染。
        /// 几何映射：矩形水平线→弧线，矩形垂直线→径向线。
        /// 方向：0=In, 1=CW, 2=Out, 3=CCW
        /// </summary>
        private void DrawCircularSolution(IGraphicsContext context, List<List<WeaveAdjacency>> graph)
        {
            int rings = field.Height;
            int sectors = field.Width;

            float cx = width / 2f;
            float cy = height / 2f;
            float maxRadius = Math.Min(width, height) / 2f * 0.95f;

            float sectorAngle = 360f / sectors;
            float ringWidth = maxRadius / rings;

            float cellMarginFrac = (1 - passageWidthFrac) / 2;
            float d0 = cellMarginFrac * ringWidth;
            float d1 = (1 - cellMarginFrac) * ringWidth;
            float angD0 = cellMarginFrac * sectorAngle;
            float angD1 = (1 - cellMarginFrac) * sectorAngle;

            var lineW = lineWidthFrac * ringWidth;

            var cellWhite = field.CellWhite!;
            var cellOverNS = field.CellOverNS!;
            var cellOverEW = field.CellOverEW!;

            context.BeginPath();

            for (int ring = 0; ring < rings; ring++)
            {
                for (int sector = 0; sector < sectors; sector++)
                {
                    int ci = field.CellIndex(sector, ring);
                    if (!cellWhite[ci]) continue;

                    float innerR = ring * ringWidth;
                    float outerR = (ring + 1) * ringWidth;
                    float startAngle = sector * sectorAngle;
                    float endAngle = (sector + 1) * sectorAngle;

                    float passInnerR = innerR + d0;
                    float passOuterR = innerR + d1;
                    float passStartAngle = startAngle + angD0;
                    float passEndAngle = startAngle + angD1;
                    float centerR = innerR + ringWidth / 2;
                    float centerAngle = startAngle + sectorAngle / 2;

                    var lowerDir = lowerSolDirs.TryGetValue(ci, out var ld) ? ld : 0;
                    var upperDir = upperSolDirs.TryGetValue(ci, out var ud) ? ud : 0;

                    if (cellOverNS[ci])
                    {
                        // In/Out 跨越：upper 走 In/Out（径向），lower 走 CW/CCW（弧向）
                        if ((upperDir & 0b1010) != 0)
                            DrawSolRadial(context, cx, cy, centerAngle, innerR, outerR);
                        if ((lowerDir & 0b0101) != 0)
                        {
                            DrawSolArc(context, cx, cy, centerR, startAngle, passStartAngle);
                            DrawSolArc(context, cx, cy, centerR, passEndAngle, endAngle);
                        }
                    }
                    else if (cellOverEW[ci])
                    {
                        // CW/CCW 跨越：upper 走 CW/CCW（弧向），lower 走 In/Out（径向）
                        if ((upperDir & 0b0101) != 0)
                            DrawSolArc(context, cx, cy, centerR, startAngle, endAngle);
                        if ((lowerDir & 0b1010) != 0)
                        {
                            DrawSolRadial(context, cx, cy, centerAngle, innerR, passInnerR);
                            DrawSolRadial(context, cx, cy, centerAngle, passOuterR, outerR);
                        }
                    }
                    else if (lowerDir != 0)
                    {
                        DrawCircularSolutionFlat(context, cx, cy,
                            innerR, outerR, startAngle, endAngle,
                            centerR, centerAngle, lowerDir);
                    }
                }
            }

            context.EndPath();
            context.StrokePath(solutionColor, lineW, true);
        }

        /// <summary>
        /// 平坦单元格解路径绘制。
        /// 路径延伸到单元格边界（innerR/outerR/startAngle/endAngle），确保相邻单元格的路径连接。
        /// In/Out 方向画径向线，CW/CCW 方向画弧线，都在单元格中心交汇。
        /// 转弯（两个相邻方向）画为连续路径。
        /// </summary>
        private void DrawCircularSolutionFlat(IGraphicsContext context, float cx, float cy,
            float innerR, float outerR, float startAngle, float endAngle,
            float centerR, float centerAngle, int value)
        {
            bool hasIn  = (value & 0b1000) != 0;
            bool hasCW  = (value & 0b0100) != 0;
            bool hasOut = (value & 0b0010) != 0;
            bool hasCCW = (value & 0b0001) != 0;

            // 计算单元格边界点和中心点的笛卡尔坐标
            float ToX(float angle, float r) => cx + r * (float)Math.Cos(angle * Math.PI / 180);
            float ToY(float angle, float r) => cy + r * (float)Math.Sin(angle * Math.PI / 180);

            float inX = ToX(centerAngle, innerR), inY = ToY(centerAngle, innerR);
            float outX = ToX(centerAngle, outerR), outY = ToY(centerAngle, outerR);
            float cwX = ToX(endAngle, centerR), cwY = ToY(endAngle, centerR);
            float ccwX = ToX(startAngle, centerR), ccwY = ToY(startAngle, centerR);
            float midX = ToX(centerAngle, centerR), midY = ToY(centerAngle, centerR);

            int dirCount = (hasIn ? 1 : 0) + (hasCW ? 1 : 0) + (hasOut ? 1 : 0) + (hasCCW ? 1 : 0);

            switch (dirCount)
            {
                case 1:
                    // 死胡同：从边界到中心
                    if (hasIn) { context.MoveTo(inX, inY); context.LineTo(midX, midY); }
                    else if (hasCW) { context.MoveTo(cwX, cwY); context.LineTo(midX, midY); }
                    else if (hasOut) { context.MoveTo(outX, outY); context.LineTo(midX, midY); }
                    else { context.MoveTo(ccwX, ccwY); context.LineTo(midX, midY); }
                    break;

                case 2:
                    // 直通或转弯
                    if (hasIn && hasOut)
                    {
                        // In-Out 直通：径向线
                        context.MoveTo(inX, inY); context.LineTo(outX, outY);
                    }
                    else if (hasCW && hasCCW)
                    {
                        // CW-CCW 直通：弧线
                        DrawSolArc(context, cx, cy, centerR, startAngle, endAngle);
                    }
                    else
                    {
                        // 转弯：连续路径，经过中心点
                        // 先确定两个方向的边界点
                        (float x, float y) p1, p2;
                        if (hasIn) { p1 = (inX, inY); }
                        else if (hasCW) { p1 = (cwX, cwY); }
                        else if (hasOut) { p1 = (outX, outY); }
                        else { p1 = (ccwX, ccwY); }

                        if (hasCCW) { p2 = (ccwX, ccwY); }
                        else if (hasOut) { p2 = (outX, outY); }
                        else if (hasCW) { p2 = (cwX, cwY); }
                        else { p2 = (inX, inY); }

                        context.MoveTo(p1.x, p1.y);
                        context.LineTo(midX, midY);
                        context.LineTo(p2.x, p2.y);
                    }
                    break;

                default:
                    // 3+ 方向：每个方向画一条从边界到中心的线
                    if (hasIn) { context.MoveTo(inX, inY); context.LineTo(midX, midY); }
                    if (hasCW) { context.MoveTo(cwX, cwY); context.LineTo(midX, midY); }
                    if (hasOut) { context.MoveTo(outX, outY); context.LineTo(midX, midY); }
                    if (hasCCW) { context.MoveTo(ccwX, ccwY); context.LineTo(midX, midY); }
                    break;
            }
        }

        /// <summary>绘制解路径弧线段</summary>
        private static void DrawSolArc(IGraphicsContext context, float cx, float cy,
            float radius, float startAngleDeg, float endAngleDeg)
        {
            context.MoveTo(cx + radius * (float)Math.Cos(startAngleDeg * Math.PI / 180),
                           cy + radius * (float)Math.Sin(startAngleDeg * Math.PI / 180));
            context.PathArc(cx, cy, radius, startAngleDeg, endAngleDeg - startAngleDeg);
        }

        /// <summary>绘制解路径径向线段</summary>
        private static void DrawSolRadial(IGraphicsContext context, float cx, float cy,
            float angleDeg, float innerR, float outerR)
        {
            float rad = angleDeg * (float)Math.PI / 180;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);
            context.MoveTo(cx + innerR * cos, cy + innerR * sin);
            context.LineTo(cx + outerR * cos, cy + outerR * sin);
        }

        #endregion

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
