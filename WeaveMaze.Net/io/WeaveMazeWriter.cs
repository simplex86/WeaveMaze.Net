using System;
using System.IO;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫数据写入器，将 WeaveMazeField、WeaveMazeGate 和 WeaveMazeSolution 数据写入文件。
    /// </summary>
    public static class WeaveMazeWriter
    {
        /// <summary>
        /// 将迷宫数据写入文件（不含出入口和解法）
        /// </summary>
        public static bool Write(WeaveMazeField field, string path)
        {
            return Write(field, null, default, path);
        }

        /// <summary>
        /// 将迷宫数据、出入口和解法写入文件
        /// </summary>
        public static bool Write(WeaveMazeField field, WeaveMazeGate[]? gates, WeaveMazeSolution solution, string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(stream);

                WriteHead(writer);
                WriteParams(field, writer);
                WriteCells(field, writer);
                WriteGraph(field, writer);
                WriteGates(gates, writer);
                WriteSolution(solution, writer);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将迷宫数据、出入口和解法写入文件（异步）
        /// </summary>
        public static async Task<bool> WriteAsync(WeaveMazeField field, WeaveMazeGate[]? gates, WeaveMazeSolution solution, string path)
        {
            return await Task.Run(() => Write(field, gates, solution, path));
        }

        private static void WriteHead(BinaryWriter writer)
        {
            writer.Write((byte)'W');
            writer.Write((byte)EWeaveMazeShape.Rectangular);
        }

        private static void WriteParams(WeaveMazeField field, BinaryWriter writer)
        {
            writer.Write(field.Width);
            writer.Write(field.Height);
            writer.Write((byte)Math.Round(field.LoopFrac * 100));
            writer.Write((byte)Math.Round(field.CrossFrac * 100));
            writer.Write(field.LongPassages ? (byte)1 : (byte)0);
        }

        private static void WriteCells(WeaveMazeField field, BinaryWriter writer)
        {
            int cellCount = field.Width * field.Height;
            var cellWhite = field.CellWhite ?? throw new InvalidOperationException("迷宫尚未生成，无法写入");
            var cellOverNS = field.CellOverNS ?? throw new InvalidOperationException("迷宫尚未生成，无法写入");
            var cellOverEW = field.CellOverEW ?? throw new InvalidOperationException("迷宫尚未生成，无法写入");

            for (int i = 0; i < cellCount; i++)
            {
                byte bits = 0;
                if (cellWhite[i]) bits |= 0x01;
                if (cellOverNS[i]) bits |= 0x02;
                if (cellOverEW[i]) bits |= 0x04;
                writer.Write(bits);
            }
        }

        private static void WriteGraph(WeaveMazeField field, BinaryWriter writer)
        {
            var graph = field.Graph ?? throw new InvalidOperationException("迷宫尚未生成，无法写入");
            int vertexCount = field.VertexCount;

            writer.Write(vertexCount);

            for (int v = 0; v < vertexCount; v++)
            {
                var edges = graph[v];
                writer.Write(edges.Count);
                foreach (var adj in edges)
                {
                    writer.Write(adj.Neighbor);
                    writer.Write((byte)adj.Direction);
                }
            }
        }

        private static void WriteGates(WeaveMazeGate[]? gates, BinaryWriter writer)
        {
            int count = gates?.Length ?? 0;
            writer.Write((byte)count);

            if (gates != null)
            {
                foreach (var gate in gates)
                {
                    writer.Write((ushort)gate.CellX);
                    writer.Write((ushort)gate.CellY);
                    writer.Write((byte)gate.Direction);
                }
            }
        }

        private static void WriteSolution(WeaveMazeSolution solution, BinaryWriter writer)
        {
            var path = solution.Path;
            int count = path?.Count ?? 0;
            writer.Write(count);

            if (path != null)
            {
                foreach (int vertex in path)
                {
                    writer.Write(vertex);
                }
            }
        }
    }
}
