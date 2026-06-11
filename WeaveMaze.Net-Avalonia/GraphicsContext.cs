using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace SimplexLab.WeaveMaze.TApplication
{
    /// <summary>
    /// Avalonia 平台的 IGraphicsContext 实现，将抽象绘图操作映射到 Avalonia.Media API。
    /// </summary>
    internal class GraphicsContext : IGraphicsContext
    {
        private readonly DrawingContext _context;
        private readonly Stack<IDisposable> _transformStack = new();
        private StreamGeometry? _currentGeometry;
        private StreamGeometryContext? _geometryContext;
        private bool _hasActiveFigure;
        private bool _disposed;

        public GraphicsContext(DrawingContext context)
        {
            _context = context;
        }

        public void BeginPath()
        {
            _currentGeometry = new StreamGeometry();
            _geometryContext = _currentGeometry.Open();
            _hasActiveFigure = false;
        }

        public void MoveTo(float x, float y)
        {
            if (_hasActiveFigure)
            {
                _geometryContext!.EndFigure(false);
            }
            _geometryContext!.BeginFigure(new Avalonia.Point(x, y), false);
            _hasActiveFigure = true;
        }

        public void LineTo(float x, float y)
        {
            _geometryContext!.LineTo(new Avalonia.Point(x, y));
        }

        public void PathArc(float cx, float cy, float radius, float startAngleDeg, float sweepAngleDeg)
        {
            var startRad = startAngleDeg * Math.PI / 180.0;
            var endRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180.0;

            var endPoint = new Avalonia.Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad));
            var size = new Avalonia.Size(radius, radius);
            var isLargeArc = Math.Abs(sweepAngleDeg) > 180;
            var sweepDirection = sweepAngleDeg > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

            _geometryContext!.ArcTo(endPoint, size, 0, isLargeArc, sweepDirection);
        }

        public void EndPath()
        {
            if (_hasActiveFigure)
            {
                _geometryContext!.EndFigure(false);
            }
            _geometryContext?.Dispose();
            _geometryContext = null;
            _hasActiveFigure = false;
        }

        public void StrokePath(MazeColor color, double width, bool roundedCaps)
        {
            if (_currentGeometry == null) return;

            var pen = new Pen(ToBrush(color), width)
            {
                LineCap = roundedCaps ? PenLineCap.Round : PenLineCap.Square,
                LineJoin = PenLineJoin.Round
            };
            _context.DrawGeometry(null, pen, _currentGeometry);

            _currentGeometry = null;
        }

        public void FillRectangle(float x, float y, float width, float height, MazeColor color)
        {
            _context.DrawRectangle(ToBrush(color), null,
                new Avalonia.Rect(x, y, width, height));
        }

        public void PushTranslate(float dx, float dy)
        {
            _transformStack.Push(_context.PushTransform(Matrix.CreateTranslation(dx, dy)));
        }

        public void PopTransform()
        {
            _transformStack.Pop().Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _geometryContext?.Dispose();
                _currentGeometry = null;
                while (_transformStack.Count > 0)
                {
                    _transformStack.Pop().Dispose();
                }
                _disposed = true;
            }
        }

        private static IBrush ToBrush(MazeColor color)
        {
            return new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
