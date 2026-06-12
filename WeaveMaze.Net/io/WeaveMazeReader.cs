using System;
using System.IO;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫数据读取器，从内存流中重建 WeaveMazeField 和 WeaveMazeGate 数据。
    /// </summary>
    public static class WeaveMazeReader
    {
        // 方向常量：0=北, 1=东, 2=南, 3=西
        private static readonly int[] Dy = { -1, 0, 1, 0 };
        private static readonly int[] Dx = { 0, 1, 0, -1 };
        private static readonly int[] Opposite = { 2, 3, 0, 1 };

        /// <summary>
        /// 从内存流中重建迷宫数据
        /// </summary>
        public static (WeaveMazeField? field, WeaveMazeGate[]? gates) Read(MemoryStream stream)
        {
            WeaveMazeField? field = null;
            WeaveMazeGate[]? gates = null;

            try
            {
                // 读取并验证魔数
                if (stream.ReadByte() != (byte)'W') return (null, null);

                // 读取迷宫形状
                int shapeByte = stream.ReadByte();
                if (shapeByte < 0 || shapeByte > (int)EWeaveMazeShape.Customized) return (null, null);
                var shape = (EWeaveMazeShape)shapeByte;

                var size = 2u;

                // 读取参数
                int width = ReadInt32LE(stream);
                int height = ReadInt32LE(stream);
                if (width <= 0 || height <= 0) return (null, null);

                int loopFracByte = stream.ReadByte();
                int crossFracByte = stream.ReadByte();
                int longPassagesByte = stream.ReadByte();
                if (loopFracByte < 0 || crossFracByte < 0 || longPassagesByte < 0) return (null, null);

                double loopFrac = loopFracByte / 100.0;
                double crossFrac = crossFracByte / 100.0;
                bool longPassages = longPassagesByte != 0;
                size += 11;

                // 读取遮罩（仅 Customized 类型）
                CustomizedWeaveMazeMask? mask = null;
                if (shape == EWeaveMazeShape.Customized)
                {
                    int maskSize = ReadInt32LE(stream);
                    if (maskSize <= 0) return (null, null);

                    var maskData = new byte[maskSize];
                    if (stream.Read(maskData, 0, maskSize) < maskSize) return (null, null);

                    var data = new bool[height][];
                    for (int y = 0; y < height; y++)
                    {
                        data[y] = new bool[width];
                        for (int x = 0; x < width; x++)
                        {
                            int bitIndex = y * width + x;
                            data[y][x] = (maskData[bitIndex / 8] & (1 << (bitIndex % 8))) != 0;
                        }
                    }
                    mask = new CustomizedWeaveMazeMask(data);
                    size += 4 + (uint)maskSize;
                }

                // 根据形状创建空白字段实例
                field = shape switch
                {
                    EWeaveMazeShape.Rectangular => new RectangularWeaveMazeField(width, height, loopFrac, crossFrac, longPassages),
                    EWeaveMazeShape.Customized => new CustomizedWeaveMazeField(mask!, loopFrac, crossFrac, longPassages),
                    _ => null!
                };
                if (field == null) return (null, null);

                // 创建单元格（设置 White 标志）
                var cells = field.CreateCells();

                // 读取单元格数据
                int cellSize = ReadInt32LE(stream);
                if (cellSize != width * height) return (null, null);

                var cellBytes = new byte[cellSize];
                if (stream.Read(cellBytes, 0, cellSize) < cellSize) return (null, null);
                size += 4 + (uint)cellSize;

                // 还原连接关系
                ApplyConnections(cells, cellBytes, height, width);
                field.Cells = cells;

                // 读取出入口数据
                int gateCount = stream.ReadByte();
                if (gateCount < 0) return (null, null);
                size += 1;

                gates = new WeaveMazeGate[gateCount];
                for (int i = 0; i < gateCount; i++)
                {
                    int cellY = ReadUInt16LE(stream);
                    int cellX = ReadUInt16LE(stream);
                    int direction = stream.ReadByte();
                    if (direction < 0 || direction > 3) return (null, null);
                    if (cellY < 0 || cellY >= height || cellX < 0 || cellX >= width) return (null, null);
                    gates[i] = new WeaveMazeGate(cells[cellY][cellX], direction);
                    size += 5;
                }

                // 读取并验证总字节数
                if (!VerifySize(stream, size)) return (null, null);
            }
            catch
            {
                field = null;
                gates = null;
            }

            return (field, gates);
        }

        /// <summary>
        /// 从内存流中重建迷宫数据（异步）
        /// </summary>
        public static async Task<(WeaveMazeField? field, WeaveMazeGate[]? gates)> ReadAsync(MemoryStream stream)
        {
            return await Task.Run(() => Read(stream));
        }

        /// <summary>
        /// 根据编码的连接位图还原所有单元格的节点连接关系。
        /// 利用连接的双向性：通过检查相邻单元格对应方向的 bit 来确定目标层级（Lower/Upper）。
        /// </summary>
        private static void ApplyConnections(SquareCell[][] cells, byte[] cellBytes, int height, int width)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = cells[y][x];
                    if (!cell.White) continue;

                    byte cellByte = cellBytes[y * width + x];

                    for (int d = 0; d < 4; d++)
                    {
                        int ny = y + Dy[d];
                        int nx = x + Dx[d];
                        if (ny < 0 || ny >= height || nx < 0 || nx >= width) continue;

                        var adjCell = cells[ny][nx];
                        if (!adjCell.White) continue;

                        byte adjByte = cellBytes[ny * width + nx];
                        int oppD = Opposite[d];

                        // Lower 节点连接
                        if ((cellByte & (1 << d)) != 0)
                        {
                            if ((adjByte & (1 << oppD)) != 0)
                                SetDirection(cell.Lower, d, adjCell.Lower);
                            else if ((adjByte & (1 << (oppD + 4))) != 0)
                                SetDirection(cell.Lower, d, adjCell.Upper);
                        }

                        // Upper 节点连接
                        if ((cellByte & (1 << (d + 4))) != 0)
                        {
                            if ((adjByte & (1 << oppD)) != 0)
                                SetDirection(cell.Upper, d, adjCell.Lower);
                            else if ((adjByte & (1 << (oppD + 4))) != 0)
                                SetDirection(cell.Upper, d, adjCell.Upper);
                        }
                    }
                }
            }
        }

        private static void SetDirection(SquareNode node, int direction, SquareNode target)
        {
            switch (direction)
            {
                case 0: node.North = target; break;
                case 1: node.East = target; break;
                case 2: node.South = target; break;
                case 3: node.West = target; break;
            }
        }

        private static bool VerifySize(MemoryStream stream, uint expectedSize)
        {
            var sizeBytes = new byte[4];
            if (stream.Read(sizeBytes, 0, 4) < 4) return false;
            uint storedSize = (uint)(sizeBytes[0] | (sizeBytes[1] << 8) | (sizeBytes[2] << 16) | (sizeBytes[3] << 24));
            return storedSize == expectedSize;
        }

        #region 小端序读取工具

        private static int ReadInt32LE(MemoryStream stream)
        {
            var bytes = new byte[4];
            if (stream.Read(bytes, 0, 4) < 4) throw new IOException("意外的流结尾");
            return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
        }

        private static int ReadUInt16LE(MemoryStream stream)
        {
            var bytes = new byte[2];
            if (stream.Read(bytes, 0, 2) < 2) throw new IOException("意外的流结尾");
            return bytes[0] | (bytes[1] << 8);
        }

        #endregion
    }
}
