using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫质量评估器，基于图论度量计算8项指标得分。
    ///
    /// 与 Maze.Net 的关键区别：
    /// - WeaveAdjacency 无 IsOpen 字段，邻接表中所有边均为开放通道
    /// - 需要构建"无墙图"来计算图论最短距离
    /// - 顶点总数只计入活跃顶点（有至少一条边的顶点）
    /// - 入口/出口从解路径的首尾顶点推导
    /// </summary>
    public class WeaveMazeScoreEvaluator
    {
        /// <summary>
        /// 评估迷宫质量
        /// </summary>
        /// <param name="field">迷宫字段（必须已完成生成）</param>
        /// <param name="gates">出入口数组（至少需要入口和出口）</param>
        /// <param name="solution">迷宫解法</param>
        public static WeaveMazeScore Evaluate(WeaveMazeField field, WeaveMazeGate[] gates, WeaveMazeSolution solution)
        {
            var score = new WeaveMazeScore();

            // 步骤1: 可解性检查
            if (gates == null || gates.Length < 2 || !solution.IsValid)
            {
                score.IsSolvable = false;
                score.TotalScore = 0;
                score.Difficulty = MazeDifficulty.NeedsOptimization;
                return score;
            }

            var graph = field.Graph!;
            int totalVertexCount = field.VertexCount;
            var pathList = solution.Path!;
            int shortestPathLength = pathList.Count - 1;

            // 起点与终点相同，退化为0分
            if (shortestPathLength <= 0)
            {
                score.IsSolvable = true;
                score.TotalScore = 0;
                score.Difficulty = MazeDifficulty.NeedsOptimization;
                return score;
            }

            score.IsSolvable = true;

            // 入口/出口顶点索引
            int entrance = pathList[0];
            int exit = pathList[pathList.Count - 1];

            // 将解路径缓存为集合，避免多次枚举
            var pathSet = new HashSet<int>(pathList);

            // 计算活跃顶点数（有至少一条边的顶点）
            int activeVertexCount = 0;
            for (int v = 0; v < totalVertexCount; v++)
            {
                if (graph[v].Count > 0)
                    activeVertexCount++;
            }

            // 步骤2: 计算基础图论量
            int graphTheoreticDistance = ComputeGraphTheoreticDistance(field, entrance, exit);
            int graphDiameter = ComputeGraphDiameter(graph, totalVertexCount);
            var (branchPoints, deadEnds) = ClassifyVertices(graph, entrance, exit, totalVertexCount);
            var deadEndLengths = ComputeDeadEndLengths(graph, deadEnds, branchPoints, entrance, exit);
            double avgDeadEndLength = deadEndLengths.Count > 0 ? deadEndLengths.Average() : 0;
            double avgBFSDepth = ComputeAverageBFSDepth(graph, entrance, totalVertexCount);
            int branchPointsOnPath = CountBranchPointsOnPath(pathList, branchPoints);
            double detourLength = ComputeDetourLength(graph, pathList, pathSet, branchPoints);

            // 死路长度变异系数
            double deadEndCV = 0;
            if (deadEndLengths.Count > 1 && avgDeadEndLength > 0)
            {
                double sumSqDiff = deadEndLengths.Sum(l => (l - avgDeadEndLength) * (l - avgDeadEndLength));
                double variance = sumSqDiff / deadEndLengths.Count;
                deadEndCV = Math.Sqrt(variance) / avgDeadEndLength;
            }

            // 步骤3: 计算各指标原始比值
            score.PathEfficiencyRaw = graphTheoreticDistance > 0
                ? (double)shortestPathLength / graphTheoreticDistance : 1.0;
            score.StructuralComplexityRaw = activeVertexCount > 0
                ? (double)(branchPoints.Count + deadEnds.Count) / activeVertexCount : 0;
            score.ExplorationDepthRaw = graphDiameter > 0
                ? avgBFSDepth / graphDiameter : 0;
            score.DecisionDensityRaw = branchPointsOnPath > 0
                ? (double)shortestPathLength / branchPointsOnPath : double.MaxValue;
            score.DeadEndReasonabilityRaw = graphDiameter > 0
                ? avgDeadEndLength / graphDiameter : 0;
            score.SolutionConcealmentRaw = shortestPathLength > 0
                ? detourLength / shortestPathLength : 0;
            int branchPlusDead = branchPoints.Count + deadEnds.Count;
            score.BranchBalanceRaw = branchPlusDead > 0
                ? (double)branchPoints.Count / branchPlusDead : 0;
            score.DeadEndDiversityRaw = deadEndCV;

            // 步骤4: 代入评分函数
            score.PathEfficiencyScore = ScorePathEfficiency(score.PathEfficiencyRaw);
            score.StructuralComplexityScore = ScoreStructuralComplexity(score.StructuralComplexityRaw);
            score.ExplorationDepthScore = ScoreExplorationDepth(score.ExplorationDepthRaw);
            score.DecisionDensityScore = ScoreDecisionDensity(score.DecisionDensityRaw);
            score.DeadEndReasonabilityScore = ScoreDeadEndReasonability(score.DeadEndReasonabilityRaw);
            score.SolutionConcealmentScore = ScoreSolutionConcealment(score.SolutionConcealmentRaw);
            score.BranchBalanceScore = ScoreBranchBalance(score.BranchBalanceRaw);
            score.DeadEndDiversityScore = ScoreDeadEndDiversity(score.DeadEndDiversityRaw);

            // 步骤5: 按权重计算总分
            score.TotalScore =
                  score.PathEfficiencyScore * 2.0
                + score.StructuralComplexityScore * 1.5
                + score.ExplorationDepthScore * 1.5
                + score.DecisionDensityScore * 1.5
                + score.DeadEndReasonabilityScore * 1.0
                + score.SolutionConcealmentScore * 1.0
                + score.BranchBalanceScore * 1.0
                + score.DeadEndDiversityScore * 0.5;

            // 步骤6: 确定难度等级
            score.Difficulty = ClassifyDifficulty(score.TotalScore);

            return score;
        }

        /// <summary>
        /// 异步评估迷宫质量
        /// </summary>
        public static async Task<WeaveMazeScore> EvaluateAsync(WeaveMazeField field, WeaveMazeGate[] gates, WeaveMazeSolution solution)
        {
            return await Task.Run(() => Evaluate(field, gates, solution));
        }

        #region 图论基础量计算

        /// <summary>
        /// 计算图论最短距离（移除所有墙壁后的BFS距离）。
        ///
        /// 编织迷宫的邻接表只包含开放通道，没有"关闭的边"。
        /// 因此需要构建"无墙图"：每个白色单元格的 Lower 顶点连接到
        /// 上下左右相邻白色单元格的 Lower 顶点，然后做 BFS。
        /// 入口/出口如果是 Upper 顶点，映射到同单元格的 Lower 顶点。
        /// </summary>
        private static int ComputeGraphTheoreticDistance(WeaveMazeField field, int entrance, int exit)
        {
            int width = field.Width;
            int height = field.Height;
            var cellWhite = field.CellWhite!;

            // 将 Upper 顶点映射到同单元格的 Lower 顶点
            int entranceCell = entrance / 2;
            int exitCell = exit / 2;

            // 无墙图：每个单元格一个顶点，索引为 cellIndex = y * width + x
            int cellCount = width * height;
            var distance = new int[cellCount];
            for (int i = 0; i < cellCount; i++)
                distance[i] = -1;

            distance[entranceCell] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(entranceCell);

            // 四方向偏移：北、东、南、西
            int[] dy = { -1, 0, 1, 0 };
            int[] dx = { 0, 1, 0, -1 };

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                if (cell == exitCell)
                    break;

                int cx = cell % width;
                int cy = cell / width;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    int nCell = ny * width + nx;
                    if (!cellWhite[nCell] || distance[nCell] >= 0)
                        continue;

                    distance[nCell] = distance[cell] + 1;
                    queue.Enqueue(nCell);
                }
            }

            return distance[exitCell] >= 0 ? distance[exitCell] : 0;
        }

        /// <summary>
        /// 计算图直径（仅考虑开放边，使用2次BFS法）
        /// </summary>
        private static int ComputeGraphDiameter(List<List<WeaveAdjacency>> graph, int vertexCount)
        {
            if (vertexCount <= 1)
                return 0;

            // 找第一个有边的顶点作为起点
            int start = 0;
            for (int v = 0; v < vertexCount; v++)
            {
                if (graph[v].Count > 0)
                {
                    start = v;
                    break;
                }
            }

            // 第一次BFS：从起点出发找最远顶点A
            int farthest = BFSToFarthest(graph, start, vertexCount, out _);
            // 第二次BFS：从A出发找最远顶点B，距离即为直径
            BFSToFarthest(graph, farthest, vertexCount, out int diameter);

            return diameter;
        }

        /// <summary>
        /// 从指定顶点做BFS，返回最远顶点及其距离（仅沿开放边）
        /// </summary>
        private static int BFSToFarthest(List<List<WeaveAdjacency>> graph, int start, int vertexCount, out int maxDistance)
        {
            var distance = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                distance[i] = -1;

            distance[start] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(start);

            int farthest = start;
            maxDistance = 0;

            while (queue.Count > 0)
            {
                int v = queue.Dequeue();

                foreach (var edge in graph[v])
                {
                    if (distance[edge.Neighbor] >= 0)
                        continue;

                    distance[edge.Neighbor] = distance[v] + 1;
                    queue.Enqueue(edge.Neighbor);

                    if (distance[edge.Neighbor] > maxDistance)
                    {
                        maxDistance = distance[edge.Neighbor];
                        farthest = edge.Neighbor;
                    }
                }
            }

            return farthest;
        }

        /// <summary>
        /// 分类顶点：分支点（开放邻居≥3）和死路（开放邻居=1，排除入口/出口）
        /// </summary>
        private static (HashSet<int> branchPoints, HashSet<int> deadEnds) ClassifyVertices(
            List<List<WeaveAdjacency>> graph, int entrance, int exit, int vertexCount)
        {
            var branchPoints = new HashSet<int>();
            var deadEnds = new HashSet<int>();

            for (int v = 0; v < vertexCount; v++)
            {
                int openNeighbors = graph[v].Count;

                if (openNeighbors >= 3)
                    branchPoints.Add(v);
                else if (openNeighbors == 1 && v != entrance && v != exit)
                    deadEnds.Add(v);
            }

            return (branchPoints, deadEnds);
        }

        /// <summary>
        /// 计算每条死路的长度（从死路端点到最近分支点/入口/出口的边数）
        /// </summary>
        private static List<double> ComputeDeadEndLengths(
            List<List<WeaveAdjacency>> graph, HashSet<int> deadEnds, HashSet<int> branchPoints,
            int entrance, int exit)
        {
            var stopSet = new HashSet<int>(branchPoints);
            stopSet.Add(entrance);
            stopSet.Add(exit);

            var lengths = new List<double>();

            foreach (int deadEnd in deadEnds)
            {
                int length = 0;
                int current = deadEnd;
                int prev = -1;

                while (true)
                {
                    int next = -1;
                    foreach (var edge in graph[current])
                    {
                        if (edge.Neighbor != prev)
                        {
                            next = edge.Neighbor;
                            break;
                        }
                    }

                    if (next == -1)
                        break;

                    length++;

                    if (stopSet.Contains(next))
                        break;

                    prev = current;
                    current = next;
                }

                lengths.Add(length);
            }

            return lengths;
        }

        /// <summary>
        /// 计算从入口出发的平均BFS深度（仅沿开放边）
        /// </summary>
        private static double ComputeAverageBFSDepth(List<List<WeaveAdjacency>> graph, int entrance, int vertexCount)
        {
            var distance = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                distance[i] = -1;

            distance[entrance] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(entrance);

            long totalDepth = 0;
            int reachableCount = 0;

            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                totalDepth += distance[v];
                reachableCount++;

                foreach (var edge in graph[v])
                {
                    if (distance[edge.Neighbor] >= 0)
                        continue;

                    distance[edge.Neighbor] = distance[v] + 1;
                    queue.Enqueue(edge.Neighbor);
                }
            }

            return reachableCount > 0 ? (double)totalDepth / reachableCount : 0;
        }

        /// <summary>
        /// 统计最短路径上的分支点数
        /// </summary>
        private static int CountBranchPointsOnPath(List<int> pathList, HashSet<int> branchPoints)
        {
            int count = 0;
            foreach (int v in pathList)
            {
                if (branchPoints.Contains(v))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 计算最短路径岔路总长度：最短路径上每个分支点引出的非路径方向子树的边数总和
        /// </summary>
        private static double ComputeDetourLength(
            List<List<WeaveAdjacency>> graph, List<int> pathList, HashSet<int> pathSet, HashSet<int> branchPoints)
        {
            double totalDetour = 0;

            for (int i = 0; i < pathList.Count; i++)
            {
                int v = pathList[i];
                if (!branchPoints.Contains(v))
                    continue;

                // 确定路径上的前后邻居
                var pathNeighbors = new HashSet<int>();
                if (i > 0) pathNeighbors.Add(pathList[i - 1]);
                if (i < pathList.Count - 1) pathNeighbors.Add(pathList[i + 1]);

                // 对每个非路径方向的开放邻居，计算子树边数
                foreach (var edge in graph[v])
                {
                    if (pathNeighbors.Contains(edge.Neighbor))
                        continue;

                    totalDetour += CountSubtreeEdges(graph, edge.Neighbor, v);
                }
            }

            return totalDetour;
        }

        /// <summary>
        /// 计算从start出发（不经过parent）的子树边数
        /// </summary>
        private static int CountSubtreeEdges(List<List<WeaveAdjacency>> graph, int start, int parent)
        {
            int count = 0;
            var stack = new Stack<(int vertex, int par)>();
            stack.Push((start, parent));

            while (stack.Count > 0)
            {
                var (vertex, par) = stack.Pop();

                foreach (var edge in graph[vertex])
                {
                    if (edge.Neighbor == par)
                        continue;

                    count++;
                    stack.Push((edge.Neighbor, vertex));
                }
            }

            return count;
        }

        #endregion

        #region 评分函数

        /// <summary>
        /// 路径效率评分：ratio = 最短路径长度 / 图论最短距离
        /// score = clamp(10 / sqrt(ratio), 0, 10)
        /// </summary>
        private static double ScorePathEfficiency(double ratio)
        {
            if (ratio < 1.0) ratio = 1.0;
            return Clamp(10.0 / Math.Sqrt(ratio), 0, 10);
        }

        /// <summary>
        /// 结构复杂度评分：ratio = (分支点+死路)/活跃顶点数
        /// score = clamp(10 - 40 * (ratio - 0.5)², 0, 10)
        /// </summary>
        private static double ScoreStructuralComplexity(double ratio)
        {
            return Clamp(10 - 40 * (ratio - 0.5) * (ratio - 0.5), 0, 10);
        }

        /// <summary>
        /// 探索深度评分：ratio = 平均BFS深度/图直径
        /// ratio ≤ 0.4: score = 10 * ratio / 0.4
        /// ratio > 0.4: score = 10 - 10 * (ratio - 0.4)
        /// </summary>
        private static double ScoreExplorationDepth(double ratio)
        {
            if (ratio <= 0.4)
                return Clamp(10 * ratio / 0.4, 0, 10);
            else
                return Clamp(10 - 10 * (ratio - 0.4), 0, 10);
        }

        /// <summary>
        /// 决策密度评分：steps = 最短路径长度/路径上分支点数
        /// score = clamp(10 - 0.18 * (steps - 6.5)², 0, 10)
        /// </summary>
        private static double ScoreDecisionDensity(double steps)
        {
            if (double.IsInfinity(steps) || steps <= 0)
                return 0;
            return Clamp(10 - 0.18 * (steps - 6.5) * (steps - 6.5), 0, 10);
        }

        /// <summary>
        /// 死路合理性评分：ratio = 死路平均长度/图直径
        /// score = clamp(10 - 80 * (ratio - 0.3)², 0, 10)
        /// </summary>
        private static double ScoreDeadEndReasonability(double ratio)
        {
            return Clamp(10 - 80 * (ratio - 0.3) * (ratio - 0.3), 0, 10);
        }

        /// <summary>
        /// 解的隐蔽性评分：ratio = 最短路径岔路子树总边数 / 最短路径长度
        /// score = clamp(10 * log(1 + ratio) / log(4), 0, 10)
        /// </summary>
        private static double ScoreSolutionConcealment(double ratio)
        {
            if (ratio <= 0)
                return 0;
            return Clamp(10.0 * Math.Log(1 + ratio) / Math.Log(4), 0, 10);
        }

        /// <summary>
        /// 岔路均衡度评分：ratio = 分支点数/(分支点数+死路数)
        /// score = clamp(10 - 40 * (ratio - 0.35)², 0, 10)
        /// </summary>
        private static double ScoreBranchBalance(double ratio)
        {
            return Clamp(10 - 40 * (ratio - 0.35) * (ratio - 0.35), 0, 10);
        }

        /// <summary>
        /// 死路多样性评分：cv = 死路长度的变异系数
        /// score = clamp(10 - 25 * (cv - 0.5)², 0, 10)
        /// </summary>
        private static double ScoreDeadEndDiversity(double cv)
        {
            return Clamp(10 - 25 * (cv - 0.5) * (cv - 0.5), 0, 10);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion

        #region 难度分级

        private static MazeDifficulty ClassifyDifficulty(double totalScore)
        {
            if (totalScore >= 90) return MazeDifficulty.Expert;
            if (totalScore >= 80) return MazeDifficulty.Hard;
            if (totalScore >= 70) return MazeDifficulty.Medium;
            if (totalScore >= 60) return MazeDifficulty.Easy;
            return MazeDifficulty.NeedsOptimization;
        }

        #endregion
    }
}
