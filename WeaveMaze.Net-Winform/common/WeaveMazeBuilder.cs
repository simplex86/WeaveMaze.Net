using System;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// 迷宫构建器，提供 MoveTo/LineTo/ArcTo 等绘制原语。
    /// 供迷宫墙壁渲染和解法路径渲染共用。
    /// </summary>
    internal class WeaveMazeBuilder
    {
        private float cursorX, cursorY;

        /// <summary>是否启用圆角</summary>
        public bool RoundedCorners { get; set; } = true;

        /// <summary>移动画笔到指定位置（开始新子路径）</summary>
        public void MoveTo(GraphicsPath path, float x, float y)
        {
            path.StartFigure();
            cursorX = x;
            cursorY = y;
        }

        /// <summary>从当前位置画直线到指定位置</summary>
        public void LineTo(GraphicsPath path, float x, float y)
        {
            path.AddLine(cursorX, cursorY, x, y);
            cursorX = x;
            cursorY = y;
        }

        /// <summary>
        /// 从当前位置经中间点画圆角弧线到终点。
        /// (x1,y1) 为拐角顶点，(x2,y2) 为弧线结束后的方向点。
        /// 当 RoundedCorners 为 false 时退化为折线。
        /// </summary>
        public void ArcTo(GraphicsPath path, float x1, float y1, float x2, float y2, float radius)
        {
            if (!RoundedCorners)
            {
                LineTo(path, x1, y1);
                LineTo(path, x2, y2);
                return;
            }

            var dx1 = x1 - cursorX;
            var dy1 = y1 - cursorY;
            var len1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (len1 < 0.001f) { MoveTo(path, x2, y2); return; }
            dx1 /= len1; dy1 /= len1;

            var dx2 = x2 - x1;
            var dy2 = y2 - y1;
            var len2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (len2 < 0.001f) { LineTo(path, x1, y1); return; }
            dx2 /= len2; dy2 /= len2;

            if (radius > len1) radius = len1;
            if (radius > len2) radius = len2;

            var t1x = x1 - dx1 * radius;
            var t1y = y1 - dy1 * radius;
            var t2x = x1 + dx2 * radius;
            var t2y = y1 + dy2 * radius;

            var cx = x1 + radius * (dx2 - dx1);
            var cy = y1 + radius * (dy2 - dy1);

            path.AddLine(cursorX, cursorY, t1x, t1y);

            var startAngle = (float)Math.Atan2(t1y - cy, t1x - cx) * 180f / (float)Math.PI;
            var endAngle   = (float)Math.Atan2(t2y - cy, t2x - cx) * 180f / (float)Math.PI;
            var sweep = endAngle - startAngle;
            if (sweep > 180) sweep -= 360;
            if (sweep < -180) sweep += 360;

            path.AddArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweep);

            cursorX = t2x;
            cursorY = t2y;
        }
    }
}
