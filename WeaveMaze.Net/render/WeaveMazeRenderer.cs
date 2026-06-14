using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫墙壁渲染器。平台无关实现，通过 IGraphicsContext 抽象绘图操作。
    /// 支持矩形和圆形两种拓扑结构的渲染。
    /// </summary>
    public class WeaveMazeRenderer
    {
        private int width;
        private int height;
        private WeaveMazeField field;
        private WeaveMazeGate[]? gates;
        private readonly Dictionary<(int x, int y), int> gateDirs = new();

        private const float DefaultPassageWidthFrac = 0.7f;
        private const float DefaultLineWidthFrac = 0.05f;
        private const bool DefaultRoundedCorners = true;

        private float passageWidthFrac = DefaultPassageWidthFrac;
        private float lineWidthFrac = DefaultLineWidthFrac;
        private bool roundedCorners = DefaultRoundedCorners;
        private MazeColor wallColor = MazeColor.Black;
        private MazeColor backgroundColor = MazeColor.White;

        private readonly WeaveMazeBuilder pathBuilder = new();

        public WeaveMazeRenderer SetSize(int width, int height) { this.width = width; this.height = height; return this; }
        public WeaveMazeRenderer SetField(WeaveMazeField field) { this.field = field; return this; }
        public WeaveMazeRenderer SetGates(WeaveMazeGate[]? gates)
        {
            this.gates = gates;
            gateDirs.Clear();
            if (gates != null)
            {
                foreach (var gate in gates)
                {
                    var key = (gate.CellX, gate.CellY);
                    gateDirs.TryGetValue(key, out int existing);
                    gateDirs[key] = existing | gate.DirectionBit;
                }
            }
            return this;
        }
        public WeaveMazeRenderer SetPassageWidthFrac(float frac) { passageWidthFrac = frac; return this; }
        public WeaveMazeRenderer SetLineWidthFrac(float frac) { lineWidthFrac = frac; return this; }
        public WeaveMazeRenderer SetRoundedCorners(bool value) { roundedCorners = value; pathBuilder.RoundedCorners = value; return this; }
        public WeaveMazeRenderer SetWallColor(MazeColor color) { wallColor = color; return this; }
        public WeaveMazeRenderer SetBackgroundColor(MazeColor color) { backgroundColor = color; return this; }

        public void Draw(IGraphicsContext context)
        {
            var graph = field.Graph;
            if (graph == null) return;

            var cellWhite = field.CellWhite;
            var cellOverNS = field.CellOverNS;
            var cellOverEW = field.CellOverEW;
            if (cellWhite == null || cellOverNS == null || cellOverEW == null) return;

            // 绘制背景
            context.FillRectangle(0, 0, width, height, backgroundColor);

            if (field is CircularWeaveMazeField)
            {
                DrawCircular(context, graph, cellWhite, cellOverNS, cellOverEW);
            }
            else
            {
                DrawRectangular(context, graph, cellWhite, cellOverNS, cellOverEW);
            }
        }

        #region 矩形迷宫渲染

        private void DrawRectangular(IGraphicsContext context,
            List<List<WeaveAdjacency>> graph,
            bool[] cellWhite, bool[] cellOverNS, bool[] cellOverEW)
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

            context.PushTranslate(offsetX, offsetY);

            var lineW = lineWidthFrac * cellSize;

            context.BeginPath();
            DrawRectWallPaths(context, graph, cellWhite, cellOverNS, cellOverEW, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            context.EndPath();
            context.StrokePath(wallColor, lineW, roundedCorners);

            context.PopTransform();
        }

        private void DrawRectWallPaths(IGraphicsContext context,
                                   List<List<WeaveAdjacency>> graph,
                                   bool[] cellWhite,
                                   bool[] cellOverNS,
                                   bool[] cellOverEW,
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
                    var ci = field.CellIndex(j, i);

                    if (!cellWhite[ci]) continue;

                    if (cellOverNS[ci])
                    {
                        DrawWallNorthSouthOver(context, ox, oy, cellSize, d0, d1);
                    }
                    else if (cellOverEW[ci])
                    {
                        DrawWallEastWestOver(context, ox, oy, cellSize, d0, d1);
                    }
                    else
                    {
                        var lower = field.LowerIndex(j, i);
                        int value = (HasDir(graph, lower, 0) ? 0b1000 : 0) |
                                    (HasDir(graph, lower, 1) ? 0b0100 : 0) |
                                    (HasDir(graph, lower, 2) ? 0b0010 : 0) |
                                    (HasDir(graph, lower, 3) ? 0b0001 : 0);

                        if (gateDirs.TryGetValue((j, i), out int gateBits))
                        {
                            value |= gateBits;
                        }

                        DrawWallFlat(context, ox, oy, cellSize, d0, d1, dm, r0, value);
                    }
                }
            }
        }

        private void DrawWallNorthSouthOver(IGraphicsContext context, float ox, float oy, float cellSize, float d0, float d1)
        {
            pathBuilder.MoveTo(context, ox + d0, oy);
            pathBuilder.LineTo(context, ox + d0, oy + cellSize);
            pathBuilder.MoveTo(context, ox + d1, oy);
            pathBuilder.LineTo(context, ox + d1, oy + cellSize);
            pathBuilder.MoveTo(context, ox, oy + d0);
            pathBuilder.LineTo(context, ox + d0, oy + d0);
            pathBuilder.MoveTo(context, ox, oy + d1);
            pathBuilder.LineTo(context, ox + d0, oy + d1);
            pathBuilder.MoveTo(context, ox + d1, oy + d0);
            pathBuilder.LineTo(context, ox + cellSize, oy + d0);
            pathBuilder.MoveTo(context, ox + d1, oy + d1);
            pathBuilder.LineTo(context, ox + cellSize, oy + d1);
        }

        private void DrawWallEastWestOver(IGraphicsContext context, float ox, float oy, float cellSize, float d0, float d1)
        {
            pathBuilder.MoveTo(context, ox, oy + d0);
            pathBuilder.LineTo(context, ox + cellSize, oy + d0);
            pathBuilder.MoveTo(context, ox, oy + d1);
            pathBuilder.LineTo(context, ox + cellSize, oy + d1);
            pathBuilder.MoveTo(context, ox + d0, oy);
            pathBuilder.LineTo(context, ox + d0, oy + d0);
            pathBuilder.MoveTo(context, ox + d1, oy);
            pathBuilder.LineTo(context, ox + d1, oy + d0);
            pathBuilder.MoveTo(context, ox + d0, oy + d1);
            pathBuilder.LineTo(context, ox + d0, oy + cellSize);
            pathBuilder.MoveTo(context, ox + d1, oy + d1);
            pathBuilder.LineTo(context, ox + d1, oy + cellSize);
        }

        private void DrawWallFlat(IGraphicsContext context,
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
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.LineTo(context, ox + d0, oy + dm);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + d1, oy + dm, r0);
                    pathBuilder.LineTo(context, ox + d1, oy);
                    break;
                case 0b0100:
                    pathBuilder.MoveTo(context, ox + cellSize, oy + d0);
                    pathBuilder.LineTo(context, ox + dm, oy + d0);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox + d0, oy + dm, r0);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.LineTo(context, ox + cellSize, oy + d1);
                    break;
                case 0b0010:
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.LineTo(context, ox + d0, oy + dm);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox + dm, oy + d0, r0);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    pathBuilder.LineTo(context, ox + d1, oy + cellSize);
                    break;
                case 0b0001:
                    pathBuilder.MoveTo(context, ox, oy + d0);
                    pathBuilder.LineTo(context, ox + dm, oy + d0);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + d1, oy + dm, r0);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + dm, oy + d1, r0);
                    pathBuilder.LineTo(context, ox, oy + d1);
                    break;

                case 0b1100:
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox + cellSize, oy + d1, d1);
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    break;
                case 0b0110:
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox + cellSize, oy + d0, d1);
                    pathBuilder.MoveTo(context, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0011:
                    pathBuilder.MoveTo(context, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox, oy + d0, d1);
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1001:
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox, oy + d1, d1);
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1010:
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.LineTo(context, ox + d0, oy + cellSize);
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.LineTo(context, ox + d1, oy + cellSize);
                    break;
                case 0b0101:
                    pathBuilder.MoveTo(context, ox, oy + d0);
                    pathBuilder.LineTo(context, ox + cellSize, oy + d0);
                    pathBuilder.MoveTo(context, ox, oy + d1);
                    pathBuilder.LineTo(context, ox + cellSize, oy + d1);
                    break;

                case 0b1101:
                    pathBuilder.MoveTo(context, ox, oy + d1);
                    pathBuilder.LineTo(context, ox + cellSize, oy + d1);
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
                case 0b1110:
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.LineTo(context, ox + d0, oy + cellSize);
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(context, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    break;
                case 0b0111:
                    pathBuilder.MoveTo(context, ox, oy + d0);
                    pathBuilder.LineTo(context, ox + cellSize, oy + d0);
                    pathBuilder.MoveTo(context, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox, oy + d1, d0);
                    break;
                case 0b1011:
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.LineTo(context, ox + d1, oy + cellSize);
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox, oy + d1, d0);
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;

                case 0b1111:
                    pathBuilder.MoveTo(context, ox + d1, oy);
                    pathBuilder.ArcTo(context, ox + d1, oy + d0, ox + cellSize, oy + d0, d0);
                    pathBuilder.MoveTo(context, ox + d1, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d1, oy + d1, ox + cellSize, oy + d1, d0);
                    pathBuilder.MoveTo(context, ox + d0, oy + cellSize);
                    pathBuilder.ArcTo(context, ox + d0, oy + d1, ox, oy + d1, d0);
                    pathBuilder.MoveTo(context, ox + d0, oy);
                    pathBuilder.ArcTo(context, ox + d0, oy + d0, ox, oy + d0, d0);
                    break;
            }
        }

        #endregion

        #region 圆形迷宫渲染

        /// <summary>
        /// 圆形迷宫渲染。
        ///
        /// 几何映射：矩形水平线 → 弧线（半径=常数），矩形垂直线 → 径向线（角度=常数）
        ///
        /// 每个单元格的通道区域定义为环形扇区：
        ///   passInnerR = innerR + d0       （通道内径）
        ///   passOuterR = innerR + d1       （通道外径）
        ///   passStartAngle = startAngle + angD0  （通道起始角，CCW侧）
        ///   passEndAngle = startAngle + angD1    （通道结束角，CW侧）
        ///
        /// 墙壁绘制规则（与矩形完全对应）：
        ///   方向0(In)被阻断 → 弧线在 passInnerR，从 passStartAngle 到 passEndAngle
        ///   方向2(Out)被阻断 → 弧线在 passOuterR，从 passStartAngle 到 passEndAngle
        ///   方向3(CCW)被阻断 → 径向线在 passStartAngle，从 passInnerR 到 passOuterR
        ///   方向1(CW)被阻断 → 径向线在 passEndAngle，从 passInnerR 到 passOuterR
        ///
        /// 延伸墙（通道开口到单元格边缘的连接墙）：
        ///   In开放  → 径向线在 passStartAngle/passEndAngle，从 innerR 到 passInnerR
        ///   Out开放 → 径向线在 passStartAngle/passEndAngle，从 passOuterR 到 outerR
        ///   CCW开放 → 弧线在 passInnerR/passOuterR，从 startAngle 到 passStartAngle
        ///   CW开放  → 弧线在 passInnerR/passOuterR，从 passEndAngle 到 endAngle
        /// </summary>
        private void DrawCircular(IGraphicsContext context,
            List<List<WeaveAdjacency>> graph,
            bool[] cellWhite, bool[] cellOverNS, bool[] cellOverEW)
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

                    if (cellOverNS[ci])
                    {
                        DrawCircularOverNS(context, cx, cy,
                            innerR, outerR, startAngle, endAngle,
                            passInnerR, passOuterR, passStartAngle, passEndAngle);
                    }
                    else if (cellOverEW[ci])
                    {
                        DrawCircularOverEW(context, cx, cy,
                            innerR, outerR, startAngle, endAngle,
                            passInnerR, passOuterR, passStartAngle, passEndAngle);
                    }
                    else
                    {
                        var lower = field.LowerIndex(sector, ring);
                        int value = (HasDir(graph, lower, 0) ? 0b1000 : 0) |
                                    (HasDir(graph, lower, 1) ? 0b0100 : 0) |
                                    (HasDir(graph, lower, 2) ? 0b0010 : 0) |
                                    (HasDir(graph, lower, 3) ? 0b0001 : 0);

                        if (gateDirs.TryGetValue((sector, ring), out int gateBits))
                            value |= gateBits;

                        DrawCircularFlat(context, cx, cy,
                            innerR, outerR, startAngle, endAngle,
                            passInnerR, passOuterR, passStartAngle, passEndAngle, value);
                    }
                }
            }

            context.EndPath();
            context.StrokePath(wallColor, lineW, roundedCorners);
        }

        /// <summary>
        /// InOut 跨越单元格。
        /// Upper 层走 In/Out（径向通道），Lower 层走 CW/CCW（弧向通道）。
        /// 对应矩形的 DrawWallNorthSouthOver。
        /// </summary>
        private void DrawCircularOverNS(IGraphicsContext context, float cx, float cy,
            float innerR, float outerR, float startAngle, float endAngle,
            float passInnerR, float passOuterR, float passStartAngle, float passEndAngle)
        {
            // In/Out 通道侧壁（径向全幅，对应矩形两条垂直线）
            DrawRadial(context, cx, cy, passStartAngle, innerR, outerR);
            DrawRadial(context, cx, cy, passEndAngle, innerR, outerR);
            DrawArc(context, cx, cy, passInnerR, startAngle, passStartAngle);
            DrawArc(context, cx, cy, passInnerR, passEndAngle, endAngle);
            DrawArc(context, cx, cy, passOuterR, startAngle, passStartAngle);
            DrawArc(context, cx, cy, passOuterR, passEndAngle, endAngle);
        }

        /// <summary>
        /// CWCCW 跨越单元格。
        /// Upper 层走 CW/CCW（弧向通道），Lower 层走 In/Out（径向通道）。
        /// 对应矩形的 DrawWallEastWestOver。
        /// </summary>
        private void DrawCircularOverEW(IGraphicsContext context, float cx, float cy,
            float innerR, float outerR, float startAngle, float endAngle,
            float passInnerR, float passOuterR, float passStartAngle, float passEndAngle)
        {
            // CW/CCW 通道侧壁（弧向全幅，对应矩形两条水平线）
            DrawArc(context, cx, cy, passInnerR, startAngle, endAngle);
            DrawArc(context, cx, cy, passOuterR, startAngle, endAngle);
            DrawRadial(context, cx, cy, passStartAngle, innerR, passInnerR);
            DrawRadial(context, cx, cy, passStartAngle, passOuterR, outerR);
            DrawRadial(context, cx, cy, passEndAngle, innerR, passInnerR);
            DrawRadial(context, cx, cy, passEndAngle, passOuterR, outerR);
        }

        private void DrawCircularFlat(IGraphicsContext context, float cx, float cy,
            float innerR, float outerR, float startAngle, float endAngle,
            float passInnerR, float passOuterR, float passStartAngle, float passEndAngle,
            int value)
        {
            bool hasIn  = (value & 0b1000) != 0;
            bool hasCW  = (value & 0b0100) != 0;
            bool hasOut = (value & 0b0010) != 0;
            bool hasCCW = (value & 0b0001) != 0;

            // 圆角模式下，墙壁在角落处缩短 r0，留出空间给圆角弧线
            float r0 = roundedCorners ? (passOuterR - passInnerR) / 2 : 0;
            // 弧墙的角度缩短量（弧长 = r0 对应的角度）
            float angR0Inner = r0 > 0 ? r0 / passInnerR * (float)(180.0 / Math.PI) : 0;
            float angR0Outer = r0 > 0 ? r0 / passOuterR * (float)(180.0 / Math.PI) : 0;

            // In 墙（弧线 passInnerR）：角落 A(CCW侧) 和 B(CW侧)
            if (!hasIn)
            {
                float sa = passStartAngle + (!hasCCW ? angR0Inner : 0);
                float ea = passEndAngle - (!hasCW ? angR0Inner : 0);
                if (ea > sa) DrawArc(context, cx, cy, passInnerR, sa, ea);
            }
            // Out 墙（弧线 passOuterR）：角落 D(CCW侧) 和 C(CW侧)
            if (!hasOut)
            {
                float sa = passStartAngle + (!hasCCW ? angR0Outer : 0);
                float ea = passEndAngle - (!hasCW ? angR0Outer : 0);
                if (ea > sa) DrawArc(context, cx, cy, passOuterR, sa, ea);
            }
            // CCW 墙（径向 passStartAngle）：角落 A(In侧) 和 D(Out侧)
            if (!hasCCW)
            {
                float sr = passInnerR + (!hasIn ? r0 : 0);
                float er = passOuterR - (!hasOut ? r0 : 0);
                if (er > sr) DrawRadial(context, cx, cy, passStartAngle, sr, er);
            }
            // CW 墙（径向 passEndAngle）：角落 B(In侧) 和 C(Out侧)
            if (!hasCW)
            {
                float sr = passInnerR + (!hasIn ? r0 : 0);
                float er = passOuterR - (!hasOut ? r0 : 0);
                if (er > sr) DrawRadial(context, cx, cy, passEndAngle, sr, er);
            }

            if (hasIn)
            {
                DrawRadial(context, cx, cy, passStartAngle, innerR, passInnerR);
                DrawRadial(context, cx, cy, passEndAngle, innerR, passInnerR);
            }
            if (hasOut)
            {
                DrawRadial(context, cx, cy, passStartAngle, passOuterR, outerR);
                DrawRadial(context, cx, cy, passEndAngle, passOuterR, outerR);
            }
            if (hasCCW)
            {
                DrawArc(context, cx, cy, passInnerR, startAngle, passStartAngle);
                DrawArc(context, cx, cy, passOuterR, startAngle, passStartAngle);
            }
            if (hasCW)
            {
                DrawArc(context, cx, cy, passInnerR, passEndAngle, endAngle);
                DrawArc(context, cx, cy, passOuterR, passEndAngle, endAngle);
            }

            if (roundedCorners)
                DrawCircularCorners(context, cx, cy,
                    passInnerR, passOuterR, passStartAngle, passEndAngle,
                    hasIn, hasCW, hasOut, hasCCW);
        }

        private void DrawCircularCorners(IGraphicsContext context, float cx, float cy,
            float passInnerR, float passOuterR, float passStartAngle, float passEndAngle,
            bool hasIn, bool hasCW, bool hasOut, bool hasCCW)
        {
            float r0 = (passOuterR - passInnerR) / 2;
            if (!hasIn && !hasCCW)  DrawCornerArc(context, cx, cy, passStartAngle, passInnerR, r0, true, true);
            if (!hasIn && !hasCW)   DrawCornerArc(context, cx, cy, passEndAngle, passInnerR, r0, true, false);
            if (!hasOut && !hasCW)  DrawCornerArc(context, cx, cy, passEndAngle, passOuterR, r0, false, false);
            if (!hasOut && !hasCCW) DrawCornerArc(context, cx, cy, passStartAngle, passOuterR, r0, false, true);
        }

        private void DrawCornerArc(IGraphicsContext context, float cx, float cy,
            float angle, float radius, float r0, bool isInner, bool isCCW)
        {
            float rad = angle * (float)Math.PI / 180;
            float cosA = (float)Math.Cos(rad), sinA = (float)Math.Sin(rad);
            float cornerX = cx + radius * cosA, cornerY = cy + radius * sinA;
            float rSign = isInner ? 1 : -1, aSign = isCCW ? 1 : -1;
            float arcCX = cornerX + rSign * r0 * cosA - aSign * r0 * sinA;
            float arcCY = cornerY + rSign * r0 * sinA + aSign * r0 * cosA;
            float startX = cornerX - aSign * r0 * sinA, startY = cornerY + aSign * r0 * cosA;
            float endX = cornerX + rSign * r0 * cosA, endY = cornerY + rSign * r0 * sinA;
            float sa = (float)(Math.Atan2(startY - arcCY, startX - arcCX) * 180.0 / Math.PI);
            float ea = (float)(Math.Atan2(endY - arcCY, endX - arcCX) * 180.0 / Math.PI);
            float sweep = ea - sa;
            if (sweep > 180) sweep -= 360;
            if (sweep < -180) sweep += 360;
            context.MoveTo(startX, startY);
            context.PathArc(arcCX, arcCY, r0, sa, sweep);
        }

        /// <summary>绘制弧线段</summary>
        private void DrawArc(IGraphicsContext context, float cx, float cy, float radius, float startAngleDeg, float endAngleDeg)
        {
            context.MoveTo(cx + radius * (float)Math.Cos(startAngleDeg * Math.PI / 180),
                           cy + radius * (float)Math.Sin(startAngleDeg * Math.PI / 180));
            context.PathArc(cx, cy, radius, startAngleDeg, endAngleDeg - startAngleDeg);
        }

        /// <summary>绘制径向线段（从内半径到外半径，指定角度）</summary>
        private void DrawRadial(IGraphicsContext context, float cx, float cy, float angleDeg, float innerR, float outerR)
        {
            float rad = angleDeg * (float)Math.PI / 180;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);
            context.MoveTo(cx + innerR * cos, cy + innerR * sin);
            context.LineTo(cx + outerR * cos, cy + outerR * sin);
        }

        #endregion

        #region 辅助方法

        private static bool HasDir(List<List<WeaveAdjacency>> graph, int vertex, int direction)
        {
            foreach (var adj in graph[vertex])
                if (adj.Direction == direction) return true;
            return false;
        }

        #endregion
    }
}
