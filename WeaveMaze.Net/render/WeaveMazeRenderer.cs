using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫墙壁渲染器。平台无关实现，通过 IGraphicsContext 抽象绘图操作。
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

            // 绘制背景
            context.FillRectangle(0, 0, width, height, backgroundColor);

            // 平移变换
            context.PushTranslate(offsetX, offsetY);

            var lineW = lineWidthFrac * cellSize;

            // 构建并描边墙壁路径
            context.BeginPath();
            DrawWallPaths(context, graph, cellWhite, cellOverNS, cellOverEW, mazeHeight, mazeWidth, cellSize, d0, d1, dm, r0);
            context.EndPath();
            context.StrokePath(wallColor, lineW, roundedCorners);

            context.PopTransform();
        }

        #region 墙壁绘制

        private static bool HasDir(List<List<WeaveAdjacency>> graph, int vertex, int direction)
        {
            foreach (var adj in graph[vertex])
                if (adj.Direction == direction) return true;
            return false;
        }

        private void DrawWallPaths(IGraphicsContext context,
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
    }
}
