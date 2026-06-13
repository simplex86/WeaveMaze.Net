using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫出入口生成器。根据迷宫字段和解法数据独立生成出入口信息。
    /// 出入口生成与迷宫生成、解法求解完全解耦，支持生成无出入口的迷宫。
    /// </summary>
    public class WeaveMazeGateGenerator
    {
        private static readonly int[][] Permutations = GeneratePermutations();

        private readonly Random random;

        public WeaveMazeGateGenerator()
            : this(Random.Shared)
        {
        }

        public WeaveMazeGateGenerator(Random random)
        {
            this.random = random;
        }

        /// <summary>
        /// 根据解法路径的端点生成出入口。
        /// 为解法路径的起点和终点各创建一个出入口，方向指向迷宫边界外侧。
        /// </summary>
        /// <param name="field">已生成的迷宫字段</param>
        /// <param name="solution">已求解的解法数据</param>
        /// <returns>出入口数组</returns>
        public WeaveMazeGate[] Generate(WeaveMazeField field, WeaveMazeSolution solution)
        {
            if (field.CellWhite == null || !solution.IsValid) return Array.Empty<WeaveMazeGate>();

            var gates = new List<WeaveMazeGate>();
            var path = solution.Path;

            var entryGate = CreateGate(field, path[0]);
            if (entryGate != null) gates.Add(entryGate);

            var exitGate = CreateGate(field, path[path.Count - 1]);
            if (exitGate != null) gates.Add(exitGate);

            return gates.ToArray();
        }

        /// <summary>
        /// 根据解法路径的端点生成出入口（异步）
        /// </summary>
        public async Task<WeaveMazeGate[]> GenerateAsync(WeaveMazeField field, WeaveMazeSolution solution)
        {
            return await Task.Run(() => Generate(field, solution));
        }

        /// <summary>
        /// 为指定顶点创建出入口。在顶点所属单元格的边界方向中随机选择一个方向作为出入口方向。
        /// </summary>
        private WeaveMazeGate? CreateGate(WeaveMazeField field, int vertex)
        {
            int cellIndex = vertex / 2;
            int x = cellIndex % field.Width;
            int y = cellIndex / field.Width;
            var permutation = Permutations[random.Next(Permutations.Length)];

            for (int i = permutation.Length - 1; i >= 0; --i)
            {
                switch (permutation[i])
                {
                    case 0: // 北
                        if (y == 0 || !field.CellWhite[field.CellIndex(x, y - 1)])
                            return new WeaveMazeGate(x, y, 0);
                        break;
                    case 1: // 东
                        if (x == field.Width - 1 || !field.CellWhite[field.CellIndex(x + 1, y)])
                            return new WeaveMazeGate(x, y, 1);
                        break;
                    case 2: // 南
                        if (y == field.Height - 1 || !field.CellWhite[field.CellIndex(x, y + 1)])
                            return new WeaveMazeGate(x, y, 2);
                        break;
                    default: // 西
                        if (x == 0 || !field.CellWhite[field.CellIndex(x - 1, y)])
                            return new WeaveMazeGate(x, y, 3);
                        break;
                }
            }

            return null;
        }

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
