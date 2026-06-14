using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫生成器（拓扑无关）。
    /// 算法流程：添加环和交叉 → 区域标记 → 生成生成树
    ///
    /// 通过 WeaveMazeField.GetNeighbor 和 IsInteriorCell 抽象拓扑差异，
    /// 统一支持矩形、圆形等不同拓扑结构。
    ///
    /// 方向约定（由子类赋予语义）：
    ///   0 和 2 为对向（如北/南、内/外）
    ///   1 和 3 为对向（如东/西、顺时针/逆时针）
    /// </summary>
    public class WeaveMazeGenerator
    {
        private static readonly int[][] Permutations = GeneratePermutations();

        /// <summary>
        /// 环路方向对：每个环路由两个垂直方向组合形成对角线。
        /// LoopDirectionPairs[i] = (dirA, dirB)，对角邻居 = dirA邻居的dirB邻居。
        /// 索引 0: 方向0+1（如北东、内顺时针）
        /// 索引 1: 方向2+1（如南东、外顺时针）
        /// 索引 2: 方向2+3（如南西、外逆时针）
        /// 索引 3: 方向0+3（如北西、内逆时针）
        /// </summary>
        private static readonly (int dirA, int dirB)[] LoopDirectionPairs =
        {
            (0, 1), (2, 1), (2, 3), (0, 3)
        };

        private readonly Random random;

        public WeaveMazeGenerator() : this(Random.Shared) { }
        public WeaveMazeGenerator(Random random) { this.random = random; }

        public WeaveMazeField Generate(WeaveMazeField field)
        {
            InitializeGraph(field);
            AddLoopsAndCrosses(field);
            var (region, regionNodes) = AssignRegions(field);
            CreateSpanningTree(field, region, regionNodes);
            FinalizeField(field);
            return field;
        }

        public async Task<WeaveMazeField> GenerateAsync(WeaveMazeField field)
        {
            return await Task.Run(() => Generate(field));
        }

        #region 图初始化

        private void InitializeGraph(WeaveMazeField field)
        {
            int width = field.Width;
            int height = field.Height;
            int cellCount = width * height;
            int vertexCount = cellCount * 2;

            var graph = new List<List<WeaveAdjacency>>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
                graph.Add(new List<WeaveAdjacency>());

            var cellWhite = new bool[cellCount];
            var vertexCellX = new int[vertexCount];
            var vertexCellY = new int[vertexCount];
            var vertexIsUpper = new bool[vertexCount];

            var whiteMask = field.CreateCellWhiteMask();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ci = field.CellIndex(x, y);
                    cellWhite[ci] = whiteMask[y][x];

                    int lower = field.LowerIndex(x, y);
                    int upper = field.UpperIndex(x, y);

                    vertexCellX[lower] = x; vertexCellY[lower] = y; vertexIsUpper[lower] = false;
                    vertexCellX[upper] = x; vertexCellY[upper] = y; vertexIsUpper[upper] = true;
                }
            }

            field.Graph = graph;
            field.VertexCount = vertexCount;
            field.CellWhite = cellWhite;
            field.VertexCellX = vertexCellX;
            field.VertexCellY = vertexCellY;
            field.VertexIsUpper = vertexIsUpper;
            field.CellOverNS = new bool[cellCount];
            field.CellOverEW = new bool[cellCount];
        }

        #endregion

        #region 环和交叉添加

        private void AddLoopsAndCrosses(WeaveMazeField field)
        {
            var graph = field.Graph!;
            int width = field.Width;
            int height = field.Height;

            // 收集内部单元格（由子类 IsInteriorCell 判定）
            var cellList = new List<int>();
            for (int y = height - 1; y >= 0; --y)
                for (int x = width - 1; x >= 0; --x)
                {
                    if (!field.IsInteriorCell(x, y)) continue;
                    int ci = field.CellIndex(x, y);
                    if (field.CellWhite![ci])
                        cellList.Add(ci);
                }

            // 阶段1：添加环
            int loops = 0;
            int maxLoops = (int)Math.Round(cellList.Count * field.LoopFrac);
            while (loops < maxLoops && cellList.Count > 0)
            {
                int index = (int)(cellList.Count * random.NextDouble());
                int ci = cellList[index];
                cellList.RemoveAt(index);
                int x = ci % width, y = ci / width;

                var permutation = Permutations[random.Next(Permutations.Length)];
                for (int i = permutation.Length - 1; i >= 0; --i)
                {
                    bool dir0HopsDir1 = random.NextDouble() < 0.5;
                    if (AddLoop(field, x, y, permutation[i], dir0HopsDir1)) { ++loops; break; }
                    if (AddLoop(field, x, y, permutation[i], !dir0HopsDir1)) { ++loops; break; }
                }
            }

            // 收集剩余平坦的内部单元格
            cellList.Clear();
            for (int y = height - 1; y >= 0; --y)
                for (int x = width - 1; x >= 0; --x)
                {
                    if (!field.IsInteriorCell(x, y)) continue;
                    int ci = field.CellIndex(x, y);
                    if (field.CellWhite![ci] && !IsCellNotFlat(graph, field, x, y))
                        cellList.Add(ci);
                }

            // 阶段2：添加交叉
            int crosses = 0;
            int maxCrosses = (int)Math.Round(cellList.Count * field.CrossFrac);
            while (crosses < maxCrosses && cellList.Count > 0)
            {
                int index = (int)(cellList.Count * random.NextDouble());
                int ci = cellList[index];
                cellList.RemoveAt(index);
                int x = ci % width, y = ci / width;

                bool dir0HopsDir1 = random.NextDouble() < 0.5;
                if (AddCross(field, x, y, dir0HopsDir1)) { ++crosses; }
                else if (AddCross(field, x, y, !dir0HopsDir1)) { ++crosses; }
            }
        }

        /// <summary>
        /// 添加环路。loopDir 对应 LoopDirectionPairs 的索引，
        /// 决定对角线方向（由 dirA 和 dirB 两个垂直方向组合）。
        /// </summary>
        private bool AddLoop(WeaveMazeField field, int cx, int cy, int loopDir, bool dir0HopsDir1)
        {
            var graph = field.Graph!;
            var (dirA, dirB) = LoopDirectionPairs[loopDir];
            int oppA = OppositeDir(dirA);
            int oppB = OppositeDir(dirB);

            // 获取四方向邻居坐标
            var nA = field.GetNeighbor(cx, cy, dirA);
            var nB = field.GetNeighbor(cx, cy, dirB);
            var nOppA = field.GetNeighbor(cx, cy, oppA);
            var nOppB = field.GetNeighbor(cx, cy, oppB);

            // 对角邻居 = dirA邻居的dirB邻居
            var diagonal = nA.HasValue ? field.GetNeighbor(nA.Value.x, nA.Value.y, dirB) : null;

            // 验证：dirA邻居、对角邻居、dirB邻居必须白色且平坦；对向邻居只需白色
            if (!nA.HasValue || !IsCellWhite(field, nA.Value.x, nA.Value.y) || IsCellNotFlat(graph, field, nA.Value.x, nA.Value.y)) return false;
            if (!diagonal.HasValue || !IsCellWhite(field, diagonal.Value.x, diagonal.Value.y) || IsCellNotFlat(graph, field, diagonal.Value.x, diagonal.Value.y)) return false;
            if (!nB.HasValue || !IsCellWhite(field, nB.Value.x, nB.Value.y) || IsCellNotFlat(graph, field, nB.Value.x, nB.Value.y)) return false;
            if (!nOppA.HasValue || !IsCellWhite(field, nOppA.Value.x, nOppA.Value.y)) return false;
            if (!nOppB.HasValue || !IsCellWhite(field, nOppB.Value.x, nOppB.Value.y)) return false;

            var backup = BackupAffectedVertices(graph, field, cx, cy, new[] { diagonal.Value });
            WireCross(graph, field, cx, cy, dir0HopsDir1);

            // 添加到对角邻居的额外边
            if (!HasDir(graph, field.LowerIndex(nA.Value.x, nA.Value.y), dirB))
                AddEdge(graph, field.LowerIndex(nA.Value.x, nA.Value.y), dirB, field.LowerIndex(diagonal.Value.x, diagonal.Value.y), oppB);
            if (!HasDir(graph, field.LowerIndex(nB.Value.x, nB.Value.y), dirA))
                AddEdge(graph, field.LowerIndex(nB.Value.x, nB.Value.y), dirA, field.LowerIndex(diagonal.Value.x, diagonal.Value.y), oppA);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(cx, cy)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(cx, cy)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        private bool AddCross(WeaveMazeField field, int cx, int cy, bool dir0HopsDir1)
        {
            var graph = field.Graph!;

            // 四方向邻居必须均为白色
            for (int d = 0; d < 4; d++)
            {
                var n = field.GetNeighbor(cx, cy, d);
                if (!n.HasValue || !IsCellWhite(field, n.Value.x, n.Value.y)) return false;
            }

            var backup = BackupAffectedVertices(graph, field, cx, cy, null);
            WireCross(graph, field, cx, cy, dir0HopsDir1);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(cx, cy)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(cx, cy)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        /// <summary>
        /// 接线跨越结构。dir0HopsDir1=true 时方向对0(0,2)走上层跨越方向对1(1,3)；
        /// false 时方向对1走上层跨越方向对0。
        /// </summary>
        private static void WireCross(List<List<WeaveAdjacency>> graph, WeaveMazeField field,
            int cx, int cy, bool dir0HopsDir1)
        {
            int lower = field.LowerIndex(cx, cy);
            int upper = field.UpperIndex(cx, cy);

            // 方向对：hopping 走上层，staying 留下层
            int[] hoppingDirs = dir0HopsDir1 ? new[] { 0, 2 } : new[] { 1, 3 };
            int[] stayingDirs = dir0HopsDir1 ? new[] { 1, 3 } : new[] { 0, 2 };

            // 走上层的方向：已有边则迁移到上层，否则新建上层边
            foreach (var dir in hoppingDirs)
            {
                int oppDir = OppositeDir(dir);
                var neighborCoord = field.GetNeighbor(cx, cy, dir);
                if (!neighborCoord.HasValue) continue;
                int neighborLower = field.LowerIndex(neighborCoord.Value.x, neighborCoord.Value.y);

                if (HasDir(graph, lower, dir))
                {
                    int nb = GetGraphNeighbor(graph, lower, dir);
                    RemoveEdge(graph, lower, dir, nb, oppDir);
                    AddEdge(graph, upper, dir, nb, oppDir);
                }
                else
                {
                    AddEdge(graph, upper, dir, neighborLower, oppDir);
                }
            }

            // 留下层的方向：若无边则新建下层边
            foreach (var dir in stayingDirs)
            {
                int oppDir = OppositeDir(dir);
                var neighborCoord = field.GetNeighbor(cx, cy, dir);
                if (!neighborCoord.HasValue) continue;
                int neighborLower = field.LowerIndex(neighborCoord.Value.x, neighborCoord.Value.y);

                if (!HasDir(graph, lower, dir))
                {
                    AddEdge(graph, lower, dir, neighborLower, oppDir);
                }
            }
        }

        #endregion

        #region 环路检测

        private static bool FindLoop(List<List<WeaveAdjacency>> graph, int vertexCount, int seed)
        {
            var visitedBy = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++) visitedBy[i] = -1;

            visitedBy[seed] = seed;
            var stack = new List<int> { seed };
            int stackIndex = stack.Count - 1;

            while (stackIndex >= 0)
            {
                int v = stack[stackIndex];
                stack.RemoveAt(stackIndex);
                stackIndex--;

                foreach (var adj in graph[v])
                {
                    int neighbor = adj.Neighbor;
                    if (visitedBy[neighbor] >= 0)
                    {
                        if (neighbor != visitedBy[v]) return true;
                    }
                    else
                    {
                        visitedBy[neighbor] = v;
                        stack.Add(neighbor);
                        stackIndex++;
                    }
                }
            }

            return false;
        }

        #endregion

        #region 区域标记

        private static (int[] region, List<int[]> regionNodes) AssignRegions(WeaveMazeField field)
        {
            var graph = field.Graph!;
            int vertexCount = field.VertexCount;

            var region = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++) region[i] = -1;

            var regionNodes = new List<int[]>();
            int regionId = 0;

            for (int v = vertexCount - 1; v >= 0; --v)
            {
                if (region[v] >= 0) continue;

                var nodes = new List<int>();
                region[v] = regionId;
                nodes.Add(v);

                var stack = new List<int> { v };
                while (stack.Count > 0)
                {
                    int last = stack.Count - 1;
                    int current = stack[last];
                    stack.RemoveAt(last);

                    foreach (var adj in graph[current])
                    {
                        int neighbor = adj.Neighbor;
                        if (region[neighbor] < 0)
                        {
                            region[neighbor] = regionId;
                            stack.Add(neighbor);
                            nodes.Add(neighbor);
                        }
                    }
                }

                regionNodes.Add(nodes.ToArray());
                regionId++;
            }

            return (region, regionNodes);
        }

        #endregion

        #region 生成树

        private void CreateSpanningTree(WeaveMazeField field, int[] region, List<int[]> regionNodes)
        {
            var graph = field.Graph!;
            int width = field.Width;
            int height = field.Height;
            bool longCorridors = field.LongPassages;

            // 收集候选顶点：白色单元格中 Upper 无方向0和方向1连接的 Lower 顶点
            var nodes = new List<int>();
            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = width - 1; x >= 0; --x)
                {
                    if (!IsCellWhite(field, x, y)) continue;
                    int upper = field.UpperIndex(x, y);
                    if (!HasDir(graph, upper, 0) && !HasDir(graph, upper, 1))
                        nodes.Add(field.LowerIndex(x, y));
                }
            }

            // 随机打乱
            Shuffle(nodes);

            int nodeIndex = 0;
            while (nodeIndex < nodes.Count)
            {
                int node = nodes[nodeIndex];
                int cellX = field.VertexCellX![node];
                int cellY = field.VertexCellY![node];
                int nodeRegion = region[node];

                var permutation = Permutations[random.Next(Permutations.Length)];
                bool connected = false;

                for (int i = permutation.Length - 1; i >= 0; --i)
                {
                    int dir = permutation[i];
                    var neighborCoord = field.GetNeighbor(cellX, cellY, dir);

                    if (neighborCoord.HasValue && !HasDir(graph, node, dir))
                    {
                        int neighborLower = field.LowerIndex(neighborCoord.Value.x, neighborCoord.Value.y);
                        if (IsCellWhite(field, neighborCoord.Value.x, neighborCoord.Value.y) && region[neighborLower] != nodeRegion)
                        {
                            AddEdge(graph, node, dir, neighborLower, OppositeDir(dir));
                            MergeRegions(region[neighborLower], nodeRegion, region, regionNodes);
                            nodeRegion = region[node];
                            if (longCorridors) MoveToEnd(nodes, neighborLower);
                            connected = true;
                        }
                    }

                    if (connected) break;
                }

                nodeIndex++;
            }

            // 二次连通扫描：主循环中某些节点可能因为所有邻居都在同一区域而未能跨区域连接，
            // 导致存在多个连通分量。反复扫描直到所有区域合并为一个。
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    int node = nodes[i];
                    int cellX = field.VertexCellX![node];
                    int cellY = field.VertexCellY![node];
                    int nodeRegion = region[node];

                    for (int dir = 0; dir < 4; dir++)
                    {
                        var neighborCoord = field.GetNeighbor(cellX, cellY, dir);
                        if (!neighborCoord.HasValue || HasDir(graph, node, dir)) continue;

                        int neighborLower = field.LowerIndex(neighborCoord.Value.x, neighborCoord.Value.y);
                        if (IsCellWhite(field, neighborCoord.Value.x, neighborCoord.Value.y) && region[neighborLower] != nodeRegion)
                        {
                            AddEdge(graph, node, dir, neighborLower, OppositeDir(dir));
                            MergeRegions(region[neighborLower], nodeRegion, region, regionNodes);
                            nodeRegion = region[node];
                            changed = true;
                            break;
                        }
                    }
                }
            }
        }

        private static void MergeRegions(int region1, int region2, int[] region, List<int[]> regionNodes)
        {
            if (region1 == region2) return;

            var r1Nodes = regionNodes[region1];
            var r2Nodes = regionNodes[region2];

            for (int i = r1Nodes.Length - 1; i >= 0; --i)
                region[r1Nodes[i]] = region2;

            var merged = new int[r2Nodes.Length + r1Nodes.Length];
            Array.Copy(r2Nodes, merged, r2Nodes.Length);
            Array.Copy(r1Nodes, 0, merged, r2Nodes.Length, r1Nodes.Length);
            regionNodes[region2] = merged;
            regionNodes[region1] = Array.Empty<int>();
        }

        private static void MoveToEnd(List<int> nodes, int vertex)
        {
            int idx = nodes.IndexOf(vertex);
            if (idx >= 0)
            {
                nodes.RemoveAt(idx);
                nodes.Add(vertex);
            }
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int j = (int)((i + 1) * random.NextDouble());
                if (j > i) j = i;
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion

        #region 收尾

        private static void FinalizeField(WeaveMazeField field)
        {
            var graph = field.Graph!;
            int cellCount = field.Width * field.Height;

            for (int i = 0; i < cellCount; i++)
            {
                int upper = i * 2 + 1;
                // 方向0在Upper层 → CellOverNS（对向跨越）；方向1在Upper层 → CellOverEW（侧向跨越）
                field.CellOverNS![i] = HasDir(graph, upper, 0);
                field.CellOverEW![i] = HasDir(graph, upper, 1);
            }
        }

        #endregion

        #region 图操作辅助方法

        private static int OppositeDir(int dir) => (dir + 2) % 4;

        private static bool HasDir(List<List<WeaveAdjacency>> graph, int vertex, int direction)
        {
            foreach (var adj in graph[vertex])
                if (adj.Direction == direction) return true;
            return false;
        }

        private static int GetGraphNeighbor(List<List<WeaveAdjacency>> graph, int vertex, int direction)
        {
            foreach (var adj in graph[vertex])
                if (adj.Direction == direction) return adj.Neighbor;
            return -1;
        }

        private static void AddEdge(List<List<WeaveAdjacency>> graph, int v1, int dir1, int v2, int dir2)
        {
            foreach (var adj in graph[v1])
                if (adj.Direction == dir1 && adj.Neighbor == v2) return;
            graph[v1].Add(new WeaveAdjacency(v2, dir1));
            graph[v2].Add(new WeaveAdjacency(v1, dir2));
        }

        private static void RemoveEdge(List<List<WeaveAdjacency>> graph, int v1, int dir1, int v2, int dir2)
        {
            graph[v1].RemoveAll(adj => adj.Direction == dir1 && adj.Neighbor == v2);
            graph[v2].RemoveAll(adj => adj.Direction == dir2 && adj.Neighbor == v1);
        }

        private static bool IsCellNotFlat(List<List<WeaveAdjacency>> graph, WeaveMazeField field, int x, int y)
        {
            int upper = field.UpperIndex(x, y);
            return graph[upper].Count > 0;
        }

        private bool IsCellWhite(WeaveMazeField field, int x, int y)
        {
            if (x < 0 || x >= field.Width || y < 0 || y >= field.Height) return false;
            return field.CellWhite![field.CellIndex(x, y)];
        }

        #endregion

        #region 备份/恢复

        private static Dictionary<int, List<WeaveAdjacency>> BackupAffectedVertices(
            List<List<WeaveAdjacency>> graph, WeaveMazeField field, int cx, int cy,
            (int x, int y)[]? extraCells)
        {
            var affected = new HashSet<int>();

            // 中心单元格
            affected.Add(field.LowerIndex(cx, cy));
            affected.Add(field.UpperIndex(cx, cy));

            // 四方向相邻单元格（通过拓扑抽象获取）
            for (int d = 0; d < 4; d++)
            {
                var n = field.GetNeighbor(cx, cy, d);
                if (n.HasValue)
                {
                    affected.Add(field.LowerIndex(n.Value.x, n.Value.y));
                    affected.Add(field.UpperIndex(n.Value.x, n.Value.y));
                }
            }

            // 额外单元格（对角线）
            if (extraCells != null)
            {
                foreach (var (ex, ey) in extraCells)
                {
                    if (ex >= 0 && ex < field.Width && ey >= 0 && ey < field.Height)
                    {
                        affected.Add(field.LowerIndex(ex, ey));
                        affected.Add(field.UpperIndex(ex, ey));
                    }
                }
            }

            // 中心单元格当前邻居（它们的边可能被重定向）
            int lower = field.LowerIndex(cx, cy);
            int upper = field.UpperIndex(cx, cy);
            foreach (var adj in graph[lower]) affected.Add(adj.Neighbor);
            foreach (var adj in graph[upper]) affected.Add(adj.Neighbor);

            var backup = new Dictionary<int, List<WeaveAdjacency>>();
            foreach (var v in affected)
                backup[v] = new List<WeaveAdjacency>(graph[v]);

            return backup;
        }

        private static void RestoreAffectedVertices(
            List<List<WeaveAdjacency>> graph,
            Dictionary<int, List<WeaveAdjacency>> backup)
        {
            foreach (var kvp in backup)
                graph[kvp.Key] = kvp.Value;
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
