using System;
using Avalonia.Media;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// 迷宫构建器，提供 MoveTo/LineTo/ArcTo 等绘制原语。
    /// 供迷宫墙壁渲染和解法路径渲染共用。Avalonia 版本。
    /// </summary>
    internal class WeaveMazeBuilder
    {
        private float cursorX, cursorY;
        private StreamGeometryContext? context;
        private bool hasActiveFigure;

        /// <summary>是否启用圆角</summary>
        public bool RoundedCorners { get; set; } = true;

        /// <summary>开始新的几何体绘制</summary>
        public StreamGeometry BeginGeometry()
        {
            var geometry = new StreamGeometry();
            context = geometry.Open();
            hasActiveFigure = false;
            return geometry;
        }

        /// <summary>结束几何体绘制</summary>
        public void EndGeometry()
        {
            if (hasActiveFigure)
            {
                context!.EndFigure(false);
            }
            context?.Dispose();
            context = null;
            hasActiveFigure = false;
        }

        /// <summary>移动画笔到指定位置（开始新子路径）</summary>
        public void MoveTo(StreamGeometry geometry, float x, float y)
        {
            if (hasActiveFigure)
            {
                context!.EndFigure(false);
            }
            context!.BeginFigure(new Avalonia.Point(x, y), false);
            hasActiveFigure = true;
            cursorX = x;
            cursorY = y;
        }

        /// <summary>从当前位置画直线到指定位置</summary>
        public void LineTo(StreamGeometry geometry, float x, float y)
        {
            context!.LineTo(new Avalonia.Point(x, y));
            cursorX = x;
            cursorY = y;
        }

        /// <summary>
        /// 从当前位置经中间点画圆角弧线到终点。
        /// (x1,y1) 为拐角顶点，(x2,y2) 为弧线结束后的方向点。
        /// 当 RoundedCorners 为 false 时退化为折线。
        /// </summary>
        public void ArcTo(StreamGeometry geometry, float x1, float y1, float x2, float y2, float radius)
        {
            if (!RoundedCorners)
            {
                LineTo(geometry, x1, y1);
                LineTo(geometry, x2, y2);
                return;
            }

            var dx1 = x1 - cursorX;
            var dy1 = y1 - cursorY;
            var len1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (len1 < 0.001f) { MoveTo(geometry, x2, y2); return; }
            dx1 /= len1; dy1 /= len1;

            var dx2 = x2 - x1;
            var dy2 = y2 - y1;
            var len2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (len2 < 0.001f) { LineTo(geometry, x1, y1); return; }
            dx2 /= len2; dy2 /= len2;

            if (radius > len1) radius = len1;
            if (radius > len2) radius = len2;

            var t1x = x1 - dx1 * radius;
            var t1y = y1 - dy1 * radius;
            var t2x = x1 + dx2 * radius;
            var t2y = y1 + dy2 * radius;

            var cx = x1 + radius * (dx2 - dx1);
            var cy = y1 + radius * (dy2 - dy1);

            // 直线到弧线起点
            context!.LineTo(new Avalonia.Point(t1x, t1y));

            // 计算弧线参数
            var startAngle = Math.Atan2(t1y - cy, t1x - cx);
            var endAngle = Math.Atan2(t2y - cy, t2x - cx);
            var sweep = endAngle - startAngle;
            if (sweep > Math.PI) sweep -= 2 * Math.PI;
            if (sweep < -Math.PI) sweep += 2 * Math.PI;

            // 用 ArcTo 绘制弧线
            var size = new Avalonia.Size(radius, radius);
            var rotationAngle = 0.0;
            var isLargeArc = Math.Abs(sweep) > Math.PI;
            var sweepDirection = sweep > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
            context!.ArcTo(new Avalonia.Point(t2x, t2y), size, rotationAngle, isLargeArc, sweepDirection);

            cursorX = t2x;
            cursorY = t2y;
        }
    }
}
