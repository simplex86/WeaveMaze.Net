using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 二维点，用于图形绘制接口的坐标表示
    /// </summary>
    public struct MazePoint
    {
        public float X;
        public float Y;

        public MazePoint(float x, float y) { X = x; Y = y; }
    }

    /// <summary>
    /// 尺寸，用于图形绘制接口的尺寸表示
    /// </summary>
    public struct MazeSize
    {
        public float Width;
        public float Height;

        public MazeSize(float width, float height) { Width = width; Height = height; }
    }

    /// <summary>
    /// ARGB 颜色，用于图形绘制接口的颜色表示
    /// </summary>
    public struct MazeColor
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public MazeColor(byte r, byte g, byte b) : this(255, r, g, b) { }

        public MazeColor(byte a, byte r, byte g, byte b) { A = a; R = r; G = g; B = b; }

        public static MazeColor Black => new MazeColor(0, 0, 0);
        public static MazeColor White => new MazeColor(255, 255, 255);
        public static MazeColor Red => new MazeColor(255, 0, 0);
    }

    /// <summary>
    /// 图形绘制上下文抽象接口，屏蔽不同 UI 框架的绘图 API 差异。
    /// 各前端项目（WinForms、Avalonia 等）需实现此接口，将方法映射到平台原生 API。
    /// </summary>
    public interface IGraphicsContext : IDisposable
    {
        /// <summary>开始构建新路径</summary>
        void BeginPath();

        /// <summary>移动画笔到指定位置（开始新子路径）</summary>
        void MoveTo(float x, float y);

        /// <summary>从当前位置画直线到指定位置</summary>
        void LineTo(float x, float y);

        /// <summary>
        /// 向当前路径添加一段弧线。
        /// 使用圆心、半径和角度指定弧线，角度单位为度（Degree）。
        /// </summary>
        /// <param name="cx">弧线圆心 X</param>
        /// <param name="cy">弧线圆心 Y</param>
        /// <param name="radius">弧线半径</param>
        /// <param name="startAngleDeg">起始角度（度）</param>
        /// <param name="sweepAngleDeg">扫过角度（度），正值顺时针，负值逆时针</param>
        void PathArc(float cx, float cy, float radius, float startAngleDeg, float sweepAngleDeg);

        /// <summary>结束路径构建</summary>
        void EndPath();

        /// <summary>
        /// 用指定颜色和线宽描边当前路径
        /// </summary>
        /// <param name="color">描边颜色</param>
        /// <param name="width">线宽</param>
        /// <param name="roundedCaps">是否使用圆角线帽</param>
        void StrokePath(MazeColor color, double width, bool roundedCaps);

        /// <summary>
        /// 填充矩形
        /// </summary>
        void FillRectangle(float x, float y, float width, float height, MazeColor color);

        /// <summary>
        /// 推入平移变换
        /// </summary>
        void PushTranslate(float dx, float dy);

        /// <summary>
        /// 弹出变换（恢复到 PushTranslate 之前的状态）
        /// </summary>
        void PopTransform();
    }
}
