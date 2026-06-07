using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫解法生成器。从已生成的迷宫中寻找边界间最长路径，
    /// 并将解路径写入节点的 xxx2 字段供渲染器使用。
    /// </summary>
    public class WeaveMazeSolutionGenerator
    {
        private static readonly int[][] Permutations = GeneratePermutations();

        /// <summary>
        /// 为已生成的迷宫求解，返回解法数据
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public WeaveMazeSolution Generate(RectangularWeaveMazeField field)
        {
            var cells = field.Cells;
            if (cells == null)
            {
                return new WeaveMazeSolution { Path = new List<SquareNode>() };
            }

            int height = field.Height;
            int width = field.Width;

            var borderNodes = FindBorderNodes(cells, height, width);
            var bestSolution = new List<SquareNode>();

            foreach (var node in borderNodes)
            {
                Flood(node, cells, height, width, borderNodes, bestSolution);
            }

            WireSolution(bestSolution, cells, height, width);

            return new WeaveMazeSolution { Path = bestSolution };
        }

        /// <summary>
        /// 为已生成的迷宫求解，返回解法数据
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public async Task<WeaveMazeSolution> GenerateAsync(RectangularWeaveMazeField field)
        {
            return await Task.Run(() => Generate(field));
        }

        #region 边界节点查找

        /// <summary>
        /// 找到迷宫边界上的所有节点
        /// </summary>
        private static HashSet<SquareNode> FindBorderNodes(SquareCell[][] cells, int height, int width)
        {
            var borderCells = new HashSet<SquareCell>();

            // 每列的首尾白色单元格
            for (int x = width - 1; x >= 0; --x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (cells[y][x].White)
                    {
                        borderCells.Add(cells[y][x]);
                        break;
                    }
                }
                for (int y = height - 1; y >= 0; --y)
                {
                    if (cells[y][x].White)
                    {
                        borderCells.Add(cells[y][x]);
                        break;
                    }
                }
            }

            // 每行的首尾白色单元格
            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (cells[y][x].White)
                    {
                        borderCells.Add(cells[y][x]);
                        break;
                    }
                }
                for (int x = width - 1; x >= 0; --x)
                {
                    if (cells[y][x].White)
                    {
                        borderCells.Add(cells[y][x]);
                        break;
                    }
                }
            }

            var borderNodes = new HashSet<SquareNode>();
            foreach (var cell in borderCells)
            {
                borderNodes.Add(cell.Lower);
            }
            return borderNodes;
        }

        #endregion

        #region BFS 洪水填充

        /// <summary>
        /// 从种子节点进行 BFS，记录距离。当找到比当前最远边界节点更远的边界节点时，更新最优解。
        /// </summary>
        private static void Flood(SquareNode seed, SquareCell[][] cells, int height, int width,
            HashSet<SquareNode> borderNodes, List<SquareNode> bestSolution)
        {
            // 重置 visitedBy
            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = width - 1; x >= 0; --x)
                {
                    cells[y][x].Lower.VisitedBy = null;
                    cells[y][x].Upper.VisitedBy = null;
                }
            }

            seed.VisitedBy = seed;
            seed.Region = 0;

            var queue = new Queue<SquareNode>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                // 如果当前节点是边界节点且距离大于已知最优，更新解
                if (borderNodes.Contains(node) && node.Region > bestSolution.Count)
                {
                    bestSolution.Clear();
                    var n = node;
                    while (true)
                    {
                        bestSolution.Add(n);
                        if (n.VisitedBy == null || n.VisitedBy == n) break;
                        n = n.VisitedBy;
                    }
                }

                int nextLength = node.Region + 1;

                TryVisit(node.North);
                TryVisit(node.East);
                TryVisit(node.South);
                TryVisit(node.West);

                void TryVisit(SquareNode? neighbor)
                {
                    if (neighbor != null && neighbor.VisitedBy == null)
                    {
                        neighbor.VisitedBy = node;
                        neighbor.Region = nextLength;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        #endregion

        #region 解路径写入

        /// <summary>
        /// 将解路径写入节点的 xxx2 字段
        /// </summary>
        private static void WireSolution(List<SquareNode> solution, SquareCell[][] cells, int height, int width)
        {
            // 清除所有 xxx2 字段
            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = width - 1; x >= 0; --x)
                {
                    cells[y][x].Lower.North2 = null;
                    cells[y][x].Lower.East2 = null;
                    cells[y][x].Lower.South2 = null;
                    cells[y][x].Lower.West2 = null;
                    cells[y][x].Upper.North2 = null;
                    cells[y][x].Upper.East2 = null;
                    cells[y][x].Upper.South2 = null;
                    cells[y][x].Upper.West2 = null;
                }
            }

            if (solution.Count == 0) return;

            // 标记路径端点
            WireTerminal(cells, height, width, solution[0]);
            WireTerminal(cells, height, width, solution[solution.Count - 1]);

            // 沿路径设置 xxx2 连接
            for (int i = solution.Count - 2; i >= 0; --i)
            {
                var n0 = solution[i];
                var n1 = solution[i + 1];

                if (n0.North == n1)
                {
                    n0.North2 = n1;
                    n1.South2 = n0;
                }
                else if (n0.East == n1)
                {
                    n0.East2 = n1;
                    n1.West2 = n0;
                }
                else if (n0.South == n1)
                {
                    n0.South2 = n1;
                    n1.North2 = n0;
                }
                else if (n0.West == n1)
                {
                    n0.West2 = n1;
                    n1.East2 = n0;
                }
            }
        }

        /// <summary>
        /// 为解路径的端点标记终端方向（同时设置 xxx 和 xxx2 为自引用）
        /// </summary>
        private static void WireTerminal(SquareCell[][] cells, int height, int width, SquareNode node)
        {
            var cell = node.Cell;
            var permutation = Permutations[Random.Shared.Next(Permutations.Length)];

            for (int i = permutation.Length - 1; i >= 0; --i)
            {
                switch (permutation[i])
                {
                    case 0: // 北
                        if (cell.Y == 0 || !cells[cell.Y - 1][cell.X].White)
                        {
                            node.North = node.North2 = node;
                            return;
                        }
                        break;
                    case 1: // 东
                        if (cell.X == width - 1 || !cells[cell.Y][cell.X + 1].White)
                        {
                            node.East = node.East2 = node;
                            return;
                        }
                        break;
                    case 2: // 南
                        if (cell.Y == height - 1 || !cells[cell.Y + 1][cell.X].White)
                        {
                            node.South = node.South2 = node;
                            return;
                        }
                        break;
                    default: // 西
                        if (cell.X == 0 || !cells[cell.Y][cell.X - 1].White)
                        {
                            node.West = node.West2 = node;
                            return;
                        }
                        break;
                }
            }
        }

        #endregion

        #region 工具方法

        private static int[][] GeneratePermutations()
        {
            var result = new List<int[]>();
            var arr = new[] { 0, 1, 2, 3 };
            Permute(arr, 0, result);
            return result.ToArray();
        }

        private static void Permute(int[] arr, int start, List<int[]> result)
        {
            if (start == arr.Length)
            {
                result.Add((int[])arr.Clone());
                return;
            }
            for (int i = start; i < arr.Length; i++)
            {
                (arr[start], arr[i]) = (arr[i], arr[start]);
                Permute(arr, start + 1, result);
                (arr[start], arr[i]) = (arr[i], arr[start]);
            }
        }

        #endregion
    }
}
