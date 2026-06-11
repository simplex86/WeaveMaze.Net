using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// WinForms 平台的 IGraphicsContext 实现，将抽象绘图操作映射到 System.Drawing API。
    /// </summary>
    internal class GraphicsContext : IGraphicsContext
    {
        private readonly Graphics _graphics;
        private GraphicsState? _savedState;
        private readonly Stack<GraphicsState> _transformStack = new();
        private GraphicsPath? _currentPath;
        private float _cursorX, _cursorY;
        private bool _disposed;

        public GraphicsContext(Graphics graphics)
        {
            _graphics = graphics;
            _savedState = _graphics.Save();
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }

        public void BeginPath()
        {
            _currentPath = new GraphicsPath();
        }

        public void MoveTo(float x, float y)
        {
            _currentPath!.StartFigure();
            _cursorX = x;
            _cursorY = y;
        }

        public void LineTo(float x, float y)
        {
            _currentPath!.AddLine(_cursorX, _cursorY, x, y);
            _cursorX = x;
            _cursorY = y;
        }

        public void PathArc(float cx, float cy, float radius, float startAngleDeg, float sweepAngleDeg)
        {
            _currentPath!.AddArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngleDeg, sweepAngleDeg);
            var endAngleRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180.0;
            _cursorX = (float)(cx + radius * Math.Cos(endAngleRad));
            _cursorY = (float)(cy + radius * Math.Sin(endAngleRad));
        }

        public void EndPath()
        {
            // GraphicsPath 不需要显式结束
        }

        public void StrokePath(MazeColor color, double width, bool roundedCaps)
        {
            if (_currentPath == null) return;

            using var pen = new Pen(ToColor(color), (float)width);
            pen.StartCap = roundedCaps ? LineCap.Round : LineCap.Square;
            pen.EndCap = roundedCaps ? LineCap.Round : LineCap.Square;
            pen.LineJoin = LineJoin.Round;
            _graphics.DrawPath(pen, _currentPath);

            _currentPath.Dispose();
            _currentPath = null;
        }

        public void FillRectangle(float x, float y, float width, float height, MazeColor color)
        {
            using var brush = new SolidBrush(ToColor(color));
            _graphics.FillRectangle(brush, x, y, width, height);
        }

        public void PushTranslate(float dx, float dy)
        {
            _transformStack.Push(_graphics.Save());
            _graphics.TranslateTransform(dx, dy);
        }

        public void PopTransform()
        {
            _graphics.Restore(_transformStack.Pop());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _currentPath?.Dispose();
                if (_savedState != null)
                {
                    _graphics.Restore(_savedState);
                    _savedState = null;
                }
                _disposed = true;
            }
        }

        private static Color ToColor(MazeColor color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
