using System;
using System.IO;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫数据写入器，将 WeaveMazeField 和 WeaveMazeGate 数据写入内存流。
    /// </summary>
    public static class WeaveMazeWriter
    {
        /// <summary>
        /// 将迷宫数据写入内存流（不含出入口）
        /// </summary>
        public static bool Write(WeaveMazeField field, MemoryStream stream)
        {
            return Write(field, Array.Empty<WeaveMazeGate>(), stream);
        }

        /// <summary>
        /// 将迷宫数据和出入口写入内存流
        /// </summary>
        public static bool Write(WeaveMazeField field, WeaveMazeGate[] gates, MemoryStream stream)
        {
            var size = 0u;
            try
            {
                size += WriteHead(field, stream);
                size += WriteParams(field, stream);
                size += WriteMask(field, stream);
                size += WriteCells(field, stream);
                size += WriteGates(gates, stream);
                WriteSize(stream, size);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 将迷宫数据和出入口写入内存流（异步）
        /// </summary>
        public static async Task<bool> WriteAsync(WeaveMazeField field, WeaveMazeGate[] gates, MemoryStream stream)
        {
            return await Task.Run(() => Write(field, gates, stream));
        }

        private static uint WriteHead(WeaveMazeField field, MemoryStream stream)
        {
            stream.WriteByte((byte)'W');
            stream.WriteByte((byte)GetShape(field));
            return 2;
        }

        private static EWeaveMazeShape GetShape(WeaveMazeField field)
        {
            return field is CustomizedWeaveMazeField ? EWeaveMazeShape.Customized : EWeaveMazeShape.Rectangular;
        }

        private static uint WriteParams(WeaveMazeField field, MemoryStream stream)
        {
            WriteInt32LE(stream, field.Width);
            WriteInt32LE(stream, field.Height);
            stream.WriteByte((byte)Math.Round(field.LoopFrac * 100));
            stream.WriteByte((byte)Math.Round(field.CrossFrac * 100));
            stream.WriteByte(field.LongPassages ? (byte)1 : (byte)0);
            return 11;
        }

        private static uint WriteMask(WeaveMazeField field, MemoryStream stream)
        {
            if (field is not CustomizedWeaveMazeField customized)
                return 0;

            int totalCells = field.Width * field.Height;
            int byteCount = (totalCells + 7) / 8;

            WriteInt32LE(stream, byteCount);

            var maskData = new byte[byteCount];
            for (int y = 0; y < field.Height; y++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    if (customized.Mask[y, x])
                    {
                        int bitIndex = y * field.Width + x;
                        maskData[bitIndex / 8] |= (byte)(1 << (bitIndex % 8));
                    }
                }
            }
            stream.Write(maskData, 0, maskData.Length);

            return 4 + (uint)byteCount;
        }

        private static uint WriteCells(WeaveMazeField field, MemoryStream stream)
        {
            var cells = field.Cells ?? throw new InvalidOperationException("迷宫尚未生成，无法写入");

            int cellCount = field.Width * field.Height;
            WriteInt32LE(stream, cellCount);

            for (int y = 0; y < field.Height; y++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    stream.WriteByte(EncodeCell(cells[y][x]));
                }
            }

            return 4 + (uint)cellCount;
        }

        /// <summary>
        /// 将单元格的连接状态编码为 1 字节。
        /// 低 4 位：Lower 节点的北东南西连接（bit0=北, bit1=东, bit2=南, bit3=西）
        /// 高 4 位：Upper 节点的北东南西连接（bit4=北, bit5=东, bit6=南, bit7=西）
        /// </summary>
        private static byte EncodeCell(SquareCell cell)
        {
            byte bits = 0;
            if (cell.Lower.North != null) bits |= 0x01;
            if (cell.Lower.East != null)  bits |= 0x02;
            if (cell.Lower.South != null) bits |= 0x04;
            if (cell.Lower.West != null)  bits |= 0x08;
            if (cell.Upper.North != null) bits |= 0x10;
            if (cell.Upper.East != null)  bits |= 0x20;
            if (cell.Upper.South != null) bits |= 0x40;
            if (cell.Upper.West != null)  bits |= 0x80;
            return bits;
        }

        private static uint WriteGates(WeaveMazeGate[] gates, MemoryStream stream)
        {
            int count = gates?.Length ?? 0;
            stream.WriteByte((byte)count);

            if (gates != null)
            {
                foreach (var gate in gates)
                {
                    WriteUInt16LE(stream, (ushort)gate.Cell.Y);
                    WriteUInt16LE(stream, (ushort)gate.Cell.X);
                    stream.WriteByte((byte)gate.Direction);
                }
            }

            return (uint)(1 + count * 5);
        }

        private static void WriteSize(MemoryStream stream, uint size)
        {
            WriteUInt32LE(stream, size);
        }

        #region 小端序写入工具

        private static void WriteInt32LE(MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        private static void WriteUInt16LE(MemoryStream stream, ushort value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
        }

        private static void WriteUInt32LE(MemoryStream stream, uint value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        #endregion
    }
}
