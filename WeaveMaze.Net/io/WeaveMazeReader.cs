using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫数据读取器，从文件中重建 WeaveMazeField、WeaveMazeGate 和 WeaveMazeSolution 数据。
    /// </summary>
    public static class WeaveMazeReader
    {
        /// <summary>
        /// 从文件中读取迷宫字段数据
        /// </summary>
        public static WeaveMazeField ReadField(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // 读取并验证魔数
            if (reader.ReadByte() != (byte)'W')
                throw new InvalidDataException("无效的文件格式：魔数不匹配");

            // 读取形状（忽略，始终创建 RectangularWeaveMazeField）
            reader.ReadByte();

            // 读取参数
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            if (width <= 0 || height <= 0)
                throw new InvalidDataException("无效的迷宫尺寸");

            double loopFrac = reader.ReadByte() / 100.0;
            double crossFrac = reader.ReadByte() / 100.0;
            bool longPassages = reader.ReadByte() != 0;

            // 创建字段实例
            var field = new RectangularWeaveMazeField(width, height, loopFrac, crossFrac, longPassages);

            // 读取单元格数据
            int cellCount = width * height;
            var cellWhite = new bool[cellCount];
            var cellOverNS = new bool[cellCount];
            var cellOverEW = new bool[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                byte bits = reader.ReadByte();
                cellWhite[i] = (bits & 0x01) != 0;
                cellOverNS[i] = (bits & 0x02) != 0;
                cellOverEW[i] = (bits & 0x04) != 0;
            }

            field.CellWhite = cellWhite;
            field.CellOverNS = cellOverNS;
            field.CellOverEW = cellOverEW;

            // 读取图数据
            int vertexCount = reader.ReadInt32();
            var graph = new List<List<WeaveAdjacency>>(vertexCount);
            var vertexCellX = new int[vertexCount];
            var vertexCellY = new int[vertexCount];
            var vertexIsUpper = new bool[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                // 计算顶点所属单元格坐标和层级
                int cellIndex = v / 2;
                vertexCellX[v] = cellIndex % width;
                vertexCellY[v] = cellIndex / width;
                vertexIsUpper[v] = (v % 2) == 1;

                int edgeCount = reader.ReadInt32();
                var edges = new List<WeaveAdjacency>(edgeCount);
                for (int e = 0; e < edgeCount; e++)
                {
                    int neighbor = reader.ReadInt32();
                    int direction = reader.ReadByte();
                    edges.Add(new WeaveAdjacency(neighbor, direction));
                }
                graph.Add(edges);
            }

            field.Graph = graph;
            field.VertexCount = vertexCount;
            field.VertexCellX = vertexCellX;
            field.VertexCellY = vertexCellY;
            field.VertexIsUpper = vertexIsUpper;

            return field;
        }

        /// <summary>
        /// 从文件中读取出入口数据
        /// </summary>
        public static WeaveMazeGate[] ReadGates(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // 跳过头部（2 字节）
            reader.ReadByte();
            reader.ReadByte();

            // 读取参数（需要宽高来跳过单元格数据）
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();

            // 跳过单元格数据
            int cellCount = width * height;
            stream.Position += cellCount;

            // 跳过图数据
            int vertexCount = reader.ReadInt32();
            for (int v = 0; v < vertexCount; v++)
            {
                int edgeCount = reader.ReadInt32();
                for (int e = 0; e < edgeCount; e++)
                {
                    reader.ReadInt32();
                    reader.ReadByte();
                }
            }

            // 读取出入口数据
            int gateCount = reader.ReadByte();
            var gates = new WeaveMazeGate[gateCount];
            for (int i = 0; i < gateCount; i++)
            {
                int cellX = reader.ReadUInt16();
                int cellY = reader.ReadUInt16();
                int direction = reader.ReadByte();
                gates[i] = new WeaveMazeGate(cellX, cellY, direction);
            }

            return gates;
        }

        /// <summary>
        /// 从文件中读取解法数据
        /// </summary>
        public static WeaveMazeSolution ReadSolution(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // 跳过头部（2 字节）
            reader.ReadByte();
            reader.ReadByte();

            // 读取宽高以跳过参数
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();

            // 跳过单元格数据
            int cellCount = width * height;
            stream.Position += cellCount;

            // 跳过图数据
            int vertexCount = reader.ReadInt32();
            for (int v = 0; v < vertexCount; v++)
            {
                int edgeCount = reader.ReadInt32();
                for (int e = 0; e < edgeCount; e++)
                {
                    reader.ReadInt32();
                    reader.ReadByte();
                }
            }

            // 跳过出入口数据
            int gateCount = reader.ReadByte();
            for (int i = 0; i < gateCount; i++)
            {
                reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadByte();
            }

            // 读取解法数据
            int pathCount = reader.ReadInt32();
            var solutionPath = new List<int>(pathCount);
            for (int i = 0; i < pathCount; i++)
            {
                solutionPath.Add(reader.ReadInt32());
            }

            return new WeaveMazeSolution { Path = solutionPath };
        }

        /// <summary>
        /// 从文件中读取迷宫字段数据（异步）
        /// </summary>
        public static async Task<WeaveMazeField> ReadFieldAsync(string path)
        {
            return await Task.Run(() => ReadField(path));
        }

        /// <summary>
        /// 从文件中读取出入口数据（异步）
        /// </summary>
        public static async Task<WeaveMazeGate[]> ReadGatesAsync(string path)
        {
            return await Task.Run(() => ReadGates(path));
        }

        /// <summary>
        /// 从文件中读取解法数据（异步）
        /// </summary>
        public static async Task<WeaveMazeSolution> ReadSolutionAsync(string path)
        {
            return await Task.Run(() => ReadSolution(path));
        }
    }
}
