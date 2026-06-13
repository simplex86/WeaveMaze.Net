using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫生成器。
    /// 算法流程：添加环和交叉 → 区域标记 → 生成生成树
    /// </summary>
    public class WeaveMazeGenerator
    {
        private static readonly int[][] Permutations = GeneratePermutations();

        private const int N = 0, E = 1, S = 2, W = 3;
        private static readonly int[] Dy = { -1, 0, 1, 0 };
        private static readonly int[] Dx = { 0, 1, 0, -1 };

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
            int height = field.Height;
            int width = field.Width;

            var cellList = new List<int>();
            for (int i = height - 2; i >= 1; --i)
                for (int j = width - 2; j >= 1; --j)
                {
                    int ci = field.CellIndex(j, i);
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
                    bool nsHopsEw = random.NextDouble() < 0.5;
                    if (TryAddLoop(field, x, y, permutation[i], nsHopsEw)) { ++loops; break; }
                    if (TryAddLoop(field, x, y, permutation[i], !nsHopsEw)) { ++loops; break; }
                }
            }

            // 收集剩余平坦的内部单元格
            cellList.Clear();
            for (int i = height - 2; i >= 1; --i)
                for (int j = width - 2; j >= 1; --j)
                {
                    int ci = field.CellIndex(j, i);
                    if (field.CellWhite![ci] && !IsCellNotFlat(graph, field, j, i))
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

                bool nsHopsEw = random.NextDouble() < 0.5;
                if (AddCross(field, x, y, nsHopsEw)) { ++crosses; }
                else if (AddCross(field, x, y, !nsHopsEw)) { ++crosses; }
            }
        }

        private bool TryAddLoop(WeaveMazeField field, int x, int y, int direction, bool nsHopsEw)
        {
            return direction switch
            {
                0 => AddNorthEastLoop(field, x, y, nsHopsEw),
                1 => AddSouthEastLoop(field, x, y, nsHopsEw),
                2 => AddSouthWestLoop(field, x, y, nsHopsEw),
                _ => AddNorthWestLoop(field, x, y, nsHopsEw),
            };
        }

        private bool AddNorthEastLoop(WeaveMazeField field, int x, int y, bool nsHopsEw)
        {
            var graph = field.Graph!;
            int nx = x, ny = y - 1, nex = x + 1, ney = y - 1;
            int ex = x + 1, ey = y, sx = x, sy = y + 1, wx = x - 1, wy = y;

            if (!IsCellWhite(field, nx, ny) || IsCellNotFlat(graph, field, nx, ny)) return false;
            if (!IsCellWhite(field, nex, ney) || IsCellNotFlat(graph, field, nex, ney)) return false;
            if (!IsCellWhite(field, ex, ey) || IsCellNotFlat(graph, field, ex, ey)) return false;
            if (!IsCellWhite(field, sx, sy)) return false;
            if (!IsCellWhite(field, wx, wy)) return false;

            var backup = BackupAffectedVertices(graph, field, x, y, new[] { (nex, ney) });
            WireCross(graph, field, x, y, nx, ny, ex, ey, sx, sy, wx, wy, nsHopsEw);
            if (!HasDir(graph, field.LowerIndex(nx, ny), E))
                AddEdge(graph, field.LowerIndex(nx, ny), E, field.LowerIndex(nex, ney), W);
            if (!HasDir(graph, field.LowerIndex(ex, ey), N))
                AddEdge(graph, field.LowerIndex(ex, ey), N, field.LowerIndex(nex, ney), S);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(x, y)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(x, y)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        private bool AddSouthEastLoop(WeaveMazeField field, int x, int y, bool nsHopsEw)
        {
            var graph = field.Graph!;
            int sx = x, sy = y + 1, sex = x + 1, sey = y + 1;
            int ex = x + 1, ey = y, nx = x, ny = y - 1, wx = x - 1, wy = y;

            if (!IsCellWhite(field, sx, sy) || IsCellNotFlat(graph, field, sx, sy)) return false;
            if (!IsCellWhite(field, sex, sey) || IsCellNotFlat(graph, field, sex, sey)) return false;
            if (!IsCellWhite(field, ex, ey) || IsCellNotFlat(graph, field, ex, ey)) return false;
            if (!IsCellWhite(field, nx, ny)) return false;
            if (!IsCellWhite(field, wx, wy)) return false;

            var backup = BackupAffectedVertices(graph, field, x, y, new[] { (sex, sey) });
            WireCross(graph, field, x, y, nx, ny, ex, ey, sx, sy, wx, wy, nsHopsEw);
            if (!HasDir(graph, field.LowerIndex(sx, sy), E))
                AddEdge(graph, field.LowerIndex(sx, sy), E, field.LowerIndex(sex, sey), W);
            if (!HasDir(graph, field.LowerIndex(ex, ey), S))
                AddEdge(graph, field.LowerIndex(ex, ey), S, field.LowerIndex(sex, sey), N);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(x, y)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(x, y)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        private bool AddSouthWestLoop(WeaveMazeField field, int x, int y, bool nsHopsEw)
        {
            var graph = field.Graph!;
            int sx = x, sy = y + 1, swx = x - 1, swy = y + 1;
            int wx = x - 1, wy = y, nx = x, ny = y - 1, ex = x + 1, ey = y;

            if (!IsCellWhite(field, sx, sy) || IsCellNotFlat(graph, field, sx, sy)) return false;
            if (!IsCellWhite(field, swx, swy) || IsCellNotFlat(graph, field, swx, swy)) return false;
            if (!IsCellWhite(field, wx, wy) || IsCellNotFlat(graph, field, wx, wy)) return false;
            if (!IsCellWhite(field, nx, ny)) return false;
            if (!IsCellWhite(field, ex, ey)) return false;

            var backup = BackupAffectedVertices(graph, field, x, y, new[] { (swx, swy) });
            WireCross(graph, field, x, y, nx, ny, ex, ey, sx, sy, wx, wy, nsHopsEw);
            if (!HasDir(graph, field.LowerIndex(sx, sy), W))
                AddEdge(graph, field.LowerIndex(sx, sy), W, field.LowerIndex(swx, swy), E);
            if (!HasDir(graph, field.LowerIndex(wx, wy), S))
                AddEdge(graph, field.LowerIndex(wx, wy), S, field.LowerIndex(swx, swy), N);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(x, y)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(x, y)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        private bool AddNorthWestLoop(WeaveMazeField field, int x, int y, bool nsHopsEw)
        {
            var graph = field.Graph!;
            int nx = x, ny = y - 1, nwx = x - 1, nwy = y - 1;
            int wx = x - 1, wy = y, sx = x, sy = y + 1, ex = x + 1, ey = y;

            if (!IsCellWhite(field, nx, ny) || IsCellNotFlat(graph, field, nx, ny)) return false;
            if (!IsCellWhite(field, nwx, nwy) || IsCellNotFlat(graph, field, nwx, nwy)) return false;
            if (!IsCellWhite(field, wx, wy) || IsCellNotFlat(graph, field, wx, wy)) return false;
            if (!IsCellWhite(field, sx, sy)) return false;
            if (!IsCellWhite(field, ex, ey)) return false;

            var backup = BackupAffectedVertices(graph, field, x, y, new[] { (nwx, nwy) });
            WireCross(graph, field, x, y, nx, ny, ex, ey, sx, sy, wx, wy, nsHopsEw);
            if (!HasDir(graph, field.LowerIndex(nx, ny), W))
                AddEdge(graph, field.LowerIndex(nx, ny), W, field.LowerIndex(nwx, nwy), E);
            if (!HasDir(graph, field.LowerIndex(wx, wy), N))
                AddEdge(graph, field.LowerIndex(wx, wy), N, field.LowerIndex(nwx, nwy), S);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(x, y)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(x, y)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        private bool AddCross(WeaveMazeField field, int x, int y, bool nsHopsEw)
        {
            var graph = field.Graph!;
            int nx = x, ny = y - 1, ex = x + 1, ey = y;
            int sx = x, sy = y + 1, wx = x - 1, wy = y;

            if (!IsCellWhite(field, nx, ny)) return false;
            if (!IsCellWhite(field, ex, ey)) return false;
            if (!IsCellWhite(field, sx, sy)) return false;
            if (!IsCellWhite(field, wx, wy)) return false;

            var backup = BackupAffectedVertices(graph, field, x, y, null);
            WireCross(graph, field, x, y, nx, ny, ex, ey, sx, sy, wx, wy, nsHopsEw);

            if (FindLoop(graph, field.VertexCount, field.LowerIndex(x, y)) ||
                FindLoop(graph, field.VertexCount, field.UpperIndex(x, y)))
            { RestoreAffectedVertices(graph, backup); return false; }
            return true;
        }

        /// <summary>
        /// 接线跨越结构。nsHopsEw=true 时南北走上层跨越东西；false 时东西走上层跨越南北。
        /// </summary>
        private static void WireCross(List<List<WeaveAdjacency>> graph, WeaveMazeField field,
            int cx, int cy, int nx, int ny, int ex, int ey, int sx, int sy, int wx, int wy,
            bool nsHopsEw)
        {
            int lower = field.LowerIndex(cx, cy);
            int upper = field.UpperIndex(cx, cy);
            int northLower = field.LowerIndex(nx, ny);
            int eastLower = field.LowerIndex(ex, ey);
            int southLower = field.LowerIndex(sx, sy);
            int westLower = field.LowerIndex(wx, wy);

            if (nsHopsEw)
            {
                // 南北走上层
                if (HasDir(graph, lower, N))
                { int nb = GetNeighbor(graph, lower, N); RemoveEdge(graph, lower, N, nb, S); AddEdge(graph, upper, N, nb, S); }
                else
                { AddEdge(graph, upper, N, northLower, S); }

                if (HasDir(graph, lower, S))
                { int nb = GetNeighbor(graph, lower, S); RemoveEdge(graph, lower, S, nb, N); AddEdge(graph, upper, S, nb, N); }
                else
                { AddEdge(graph, upper, S, southLower, N); }

                if (!HasDir(graph, lower, E)) AddEdge(graph, lower, E, eastLower, W);
                if (!HasDir(graph, lower, W)) AddEdge(graph, lower, W, westLower, E);
            }
            else
            {
                // 东西走上层
                if (HasDir(graph, lower, E))
                { int nb = GetNeighbor(graph, lower, E); RemoveEdge(graph, lower, E, nb, W); AddEdge(graph, upper, E, nb, W); }
                else
                { AddEdge(graph, upper, E, eastLower, W); }

                if (HasDir(graph, lower, W))
                { int nb = GetNeighbor(graph, lower, W); RemoveEdge(graph, lower, W, nb, E); AddEdge(graph, upper, W, nb, E); }
                else
                { AddEdge(graph, upper, W, westLower, E); }

                if (!HasDir(graph, lower, N)) AddEdge(graph, lower, N, northLower, S);
                if (!HasDir(graph, lower, S)) AddEdge(graph, lower, S, southLower, N);
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
            int maxX = field.Width - 1;
            int maxY = field.Height - 1;
            bool longCorridors = field.LongPassages;

            // 收集候选顶点：白色单元格中 Upper 无北向和东向连接的 Lower 顶点
            var nodes = new List<int>();
            for (int y = maxY; y >= 0; --y)
            {
                for (int x = maxX; x >= 0; --x)
                {
                    if (!IsCellWhite(field, x, y)) continue;
                    int upper = field.UpperIndex(x, y);
                    if (!HasDir(graph, upper, N) && !HasDir(graph, upper, E))
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
                    switch (permutation[i])
                    {
                        case N: // 北
                            if (cellY > 0 && !HasDir(graph, node, N))
                            {
                                int northLower = field.LowerIndex(cellX, cellY - 1);
                                if (IsCellWhite(field, cellX, cellY - 1) && region[northLower] != nodeRegion)
                                {
                                    AddEdge(graph, node, N, northLower, S);
                                    MergeRegions(region[northLower], nodeRegion, region, regionNodes);
                                    nodeRegion = region[node];
                                    if (longCorridors) MoveToEnd(nodes, northLower);
                                    connected = true;
                                }
                            }
                            break;
                        case E: // 东
                            if (cellX < maxX && !HasDir(graph, node, E))
                            {
                                int eastLower = field.LowerIndex(cellX + 1, cellY);
                                if (IsCellWhite(field, cellX + 1, cellY) && region[eastLower] != nodeRegion)
                                {
                                    AddEdge(graph, node, E, eastLower, W);
                                    MergeRegions(region[eastLower], nodeRegion, region, regionNodes);
                                    nodeRegion = region[node];
                                    if (longCorridors) MoveToEnd(nodes, eastLower);
                                    connected = true;
                                }
                            }
                            break;
                        case S: // 南
                            if (cellY < maxY && !HasDir(graph, node, S))
                            {
                                int southLower = field.LowerIndex(cellX, cellY + 1);
                                if (IsCellWhite(field, cellX, cellY + 1) && region[southLower] != nodeRegion)
                                {
                                    AddEdge(graph, node, S, southLower, N);
                                    MergeRegions(region[southLower], nodeRegion, region, regionNodes);
                                    nodeRegion = region[node];
                                    if (longCorridors) MoveToEnd(nodes, southLower);
                                    connected = true;
                                }
                            }
                            break;
                        default: // 西
                            if (cellX > 0 && !HasDir(graph, node, W))
                            {
                                int westLower = field.LowerIndex(cellX - 1, cellY);
                                if (IsCellWhite(field, cellX - 1, cellY) && region[westLower] != nodeRegion)
                                {
                                    AddEdge(graph, node, W, westLower, E);
                                    MergeRegions(region[westLower], nodeRegion, region, regionNodes);
                                    nodeRegion = region[node];
                                    if (longCorridors) MoveToEnd(nodes, westLower);
                                    connected = true;
                                }
                            }
                            break;
                    }

                    if (connected) break;
                }

                nodeIndex++;
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
                field.CellOverNS![i] = HasDir(graph, upper, N);
                field.CellOverEW![i] = HasDir(graph, upper, E);
            }
        }

        #endregion

        #region 图操作辅助方法

        private static bool HasDir(List<List<WeaveAdjacency>> graph, int vertex, int direction)
        {
            foreach (var adj in graph[vertex])
                if (adj.Direction == direction) return true;
            return false;
        }

        private static int GetNeighbor(List<List<WeaveAdjacency>> graph, int vertex, int direction)
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

            // 四方向相邻单元格
            for (int d = 0; d < 4; d++)
            {
                int ax = cx + Dx[d], ay = cy + Dy[d];
                if (ax >= 0 && ax < field.Width && ay >= 0 && ay < field.Height)
                {
                    affected.Add(field.LowerIndex(ax, ay));
                    affected.Add(field.UpperIndex(ax, ay));
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
