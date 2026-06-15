using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫解法生成器。从已生成的迷宫中寻找边界间最长路径，
    /// 返回解路径的顶点索引列表。
    /// </summary>
    public class WeaveMazeSolutionGenerator
    {
        /// <summary>
        /// 为已生成的迷宫求解，返回解法数据
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public WeaveMazeSolution Generate(WeaveMazeField field)
        {
            var graph = field.Graph;
            if (graph == null)
            {
                return new WeaveMazeSolution { Path = new List<int>() };
            }

            int height = field.Height;
            int width = field.Width;

            var borderVertices = FindBorderVertices(field, height, width);
            var bestPath = new List<int>();

            foreach (var vertex in borderVertices)
            {
                Flood(vertex, field, borderVertices, bestPath);
            }

            return new WeaveMazeSolution { Path = bestPath };
        }

        /// <summary>
        /// 为已生成的迷宫求解，返回解法数据
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public async Task<WeaveMazeSolution> GenerateAsync(WeaveMazeField field)
        {
            return await Task.Run(() => Generate(field));
        }

        #region 边界顶点查找

        /// <summary>
        /// 找到迷宫边界上的所有 Lower 顶点索引
        /// </summary>
        private static HashSet<int> FindBorderVertices(WeaveMazeField field, int height, int width)
        {
            var borderCells = new HashSet<int>();

            // 每列的首尾白色单元格
            for (int x = width - 1; x >= 0; --x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (field.CellWhite![field.CellIndex(x, y)])
                    {
                        borderCells.Add(field.CellIndex(x, y));
                        break;
                    }
                }
                for (int y = height - 1; y >= 0; --y)
                {
                    if (field.CellWhite![field.CellIndex(x, y)])
                    {
                        borderCells.Add(field.CellIndex(x, y));
                        break;
                    }
                }
            }

            // 每行的首尾白色单元格
            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (field.CellWhite![field.CellIndex(x, y)])
                    {
                        borderCells.Add(field.CellIndex(x, y));
                        break;
                    }
                }
                for (int x = width - 1; x >= 0; --x)
                {
                    if (field.CellWhite![field.CellIndex(x, y)])
                    {
                        borderCells.Add(field.CellIndex(x, y));
                        break;
                    }
                }
            }

            // 转换为 Lower 顶点索引
            var borderVertices = new HashSet<int>();
            foreach (var cellIdx in borderCells)
            {
                int x = cellIdx % width;
                int y = cellIdx / width;
                if (field.IsValidSolutionEndpoint(x, y))
                    borderVertices.Add(cellIdx * 2);
            }
            return borderVertices;
        }

        #endregion

        #region BFS 洪水填充

        /// <summary>
        /// 从种子顶点进行 BFS，记录距离。当找到比当前最远边界顶点更远的边界顶点时，更新最优解。
        /// </summary>
        private static void Flood(int seed, WeaveMazeField field,
            HashSet<int> borderVertices, List<int> bestPath)
        {
            var graph = field.Graph!;
            int vertexCount = field.VertexCount;

            var distance = new int[vertexCount];
            var parent = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                distance[i] = -1;
                parent[i] = -1;
            }

            distance[seed] = 0;
            parent[seed] = seed;

            var queue = new Queue<int>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                int dist = distance[v];

                // 如果当前顶点是边界顶点且距离大于已知最优，更新解
                if (borderVertices.Contains(v) && dist > bestPath.Count)
                {
                    bestPath.Clear();
                    int n = v;
                    while (true)
                    {
                        bestPath.Add(n);
                        if (parent[n] == n) break;
                        n = parent[n];
                    }
                }

                int nextDist = dist + 1;

                foreach (var adj in graph[v])
                {
                    int neighbor = adj.Neighbor;
                    if (distance[neighbor] < 0)
                    {
                        distance[neighbor] = nextDist;
                        parent[neighbor] = v;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        #endregion
    }
}
