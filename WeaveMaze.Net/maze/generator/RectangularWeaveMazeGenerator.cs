using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 矩形编织式迷宫生成器。
    /// 算法流程：添加环和交叉 → 区域标记 → 生成生成树
    /// </summary>
    public class RectangularWeaveMazeGenerator
    {
        /// <summary>
        /// 0-3 的所有 24 种排列，用于随机选择方向
        /// </summary>
        private static readonly int[][] Permutations = GeneratePermutations();

        private readonly Random random;

        public RectangularWeaveMazeGenerator()
            : this(Random.Shared)
        {
        }

        public RectangularWeaveMazeGenerator(Random random)
        {
            this.random = random;
        }

        /// <summary>
        /// 同步生成迷宫
        /// </summary>
        /// <param name="width">迷宫宽度（列数）</param>
        /// <param name="height">迷宫高度（行数）</param>
        /// <param name="loopFrac">环比例（0~1）</param>
        /// <param name="crossFrac">交叉比例（0~1）</param>
        /// <param name="longPassages">是否启用长通道模式</param>
        /// <param name="mask">可选遮罩，null 表示标准矩形</param>
        /// <returns>包含生成结果的字段（Cells 已填充）</returns>
        public RectangularWeaveMazeField Generate(int width,
                                                  int height,
                                                  double loopFrac,
                                                  double crossFrac,
                                                  bool longPassages,
                                                  bool[][]? mask)
        {
            var field = new RectangularWeaveMazeField(width, height, loopFrac, crossFrac, longPassages, mask);

            var cells = CreateCells(field);
            AddLoopsAndCrosses(cells, field.Height, field.Width, field.LoopFrac, field.CrossFrac);
            var regions = AssignRegions(cells, field.Height, field.Width);
            CreateSpanningTree(cells, field.Height, field.Width, regions, field.LongPassages);

            field.Cells = cells;
            return field;
        }

        /// <summary>
        /// 异步生成迷宫
        /// </summary>
        public async Task<RectangularWeaveMazeField> GenerateAsync(int width,
                                                                   int height,
                                                                   double loopFrac,
                                                                   double crossFrac,
                                                                   bool longPassages,
                                                                   bool[][]? mask)
        {
            return await Task.Run(() => Generate(width, height, loopFrac, crossFrac, longPassages, mask));
        }

        #region 单元格创建

        /// <summary>
        /// 根据字段参数创建单元格数组
        /// </summary>
        private static SquareCell[][] CreateCells(RectangularWeaveMazeField field)
        {
            var cells = new SquareCell[field.Height][];
            for (int i = field.Height - 1; i >= 0; --i)
            {
                cells[i] = new SquareCell[field.Width];
                for (int j = field.Width - 1; j >= 0; --j)
                {
                    bool white = field.Mask != null ? field.Mask[i][j] : true;
                    cells[i][j] = new SquareCell(j, i, white);
                }
            }
            return cells;
        }

        #endregion

        #region 环和交叉添加

        /// <summary>
        /// 在迷宫中添加环（Loop）和交叉（Cross）结构，形成编织式迷宫的跨越特征。
        /// 先添加环，再添加交叉。每次添加后验证不产生图论环路，否则回滚。
        /// </summary>
        private void AddLoopsAndCrosses(SquareCell[][] cells, int height, int width, double loopFraction, double crossFraction)
        {
            // 收集内部可用单元格（排除边界，因为环和交叉需要四方向邻居）
            var cellList = new List<SquareCell>();
            for (int i = height - 2; i >= 1; --i)
            {
                for (int j = width - 2; j >= 1; --j)
                {
                    if (cells[i][j].White)
                    {
                        cellList.Add(cells[i][j]);
                    }
                }
            }

            // 阶段1：添加环
            int loops = 0;
            int maxLoops = (int)Math.Round(cellList.Count * loopFraction);
            while (loops < maxLoops && cellList.Count > 0)
            {
                int index = (int)(cellList.Count * random.NextDouble());
                var cell = cellList[index];
                cellList.RemoveAt(index);

                var permutation = Permutations[random.Next(Permutations.Length)];
                for (int i = permutation.Length - 1; i >= 0; --i)
                {
                    bool northSouthHopsEastWest = random.NextDouble() < 0.5;
                    if (TryAddLoop(cells, height, width, cell, permutation[i], northSouthHopsEastWest))
                    {
                        ++loops;
                        break;
                    }
                    if (TryAddLoop(cells, height, width, cell, permutation[i], !northSouthHopsEastWest))
                    {
                        ++loops;
                        break;
                    }
                }
            }

            // 收集剩余平坦的内部单元格用于交叉
            cellList.Clear();
            for (int i = height - 2; i >= 1; --i)
            {
                for (int j = width - 2; j >= 1; --j)
                {
                    if (cells[i][j].White && cells[i][j].IsFlat())
                    {
                        cellList.Add(cells[i][j]);
                    }
                }
            }

            // 阶段2：添加交叉
            int crosses = 0;
            int maxCrosses = (int)Math.Round(cellList.Count * crossFraction);
            while (crosses < maxCrosses && cellList.Count > 0)
            {
                int index = (int)(cellList.Count * random.NextDouble());
                var cell = cellList[index];
                cellList.RemoveAt(index);

                bool northSouthHopsEastWest = random.NextDouble() < 0.5;
                if (AddCross(cells, cell, northSouthHopsEastWest))
                {
                    ++crosses;
                }
                else if (AddCross(cells, cell, !northSouthHopsEastWest))
                {
                    ++crosses;
                }
            }
        }

        /// <summary>
        /// 尝试添加指定方向的环
        /// </summary>
        private bool TryAddLoop(SquareCell[][] cells, int height, int width, SquareCell cell, int direction, bool northSouthHopsEastWest)
        {
            return direction switch
            {
                0 => AddNorthEastLoop(cells, cell, northSouthHopsEastWest),
                1 => AddSouthEastLoop(cells, cell, northSouthHopsEastWest),
                2 => AddSouthWestLoop(cells, cell, northSouthHopsEastWest),
                _ => AddNorthWestLoop(cells, cell, northSouthHopsEastWest),
            };
        }

        /// <summary>
        /// 添加东北方向环
        /// </summary>
        private bool AddNorthEastLoop(SquareCell[][] cells, SquareCell cell, bool northSouthHopsEastWest)
        {
            var northCell = cells[cell.Y - 1][cell.X];
            if (!northCell.White || northCell.IsNotFlat()) return false;

            var northEastCell = cells[cell.Y - 1][cell.X + 1];
            if (!northEastCell.White || northEastCell.IsNotFlat()) return false;

            var eastCell = cells[cell.Y][cell.X + 1];
            if (!eastCell.White || eastCell.IsNotFlat()) return false;

            var southCell = cells[cell.Y + 1][cell.X];
            if (!southCell.White) return false;

            var westCell = cells[cell.Y][cell.X - 1];
            if (!westCell.White) return false;

            cell.Backup();
            northCell.Backup();
            northEastCell.Backup();
            eastCell.Backup();
            southCell.Backup();
            westCell.Backup();

            WireCross(cell, northCell, eastCell, southCell, westCell, northSouthHopsEastWest);

            if (northCell.Lower.East == null)
            {
                northCell.Lower.East = northEastCell.Lower;
                northEastCell.Lower.West = northCell.Lower;
            }
            if (eastCell.Lower.North == null)
            {
                eastCell.Lower.North = northEastCell.Lower;
                northEastCell.Lower.South = eastCell.Lower;
            }

            if (FindLoop(cells, cell.Lower) || FindLoop(cells, cell.Upper))
            {
                cell.Restore();
                northCell.Restore();
                northEastCell.Restore();
                eastCell.Restore();
                southCell.Restore();
                westCell.Restore();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 添加东南方向环
        /// </summary>
        private bool AddSouthEastLoop(SquareCell[][] cells, SquareCell cell, bool northSouthHopsEastWest)
        {
            var southCell = cells[cell.Y + 1][cell.X];
            if (!southCell.White || southCell.IsNotFlat()) return false;

            var southEastCell = cells[cell.Y + 1][cell.X + 1];
            if (!southEastCell.White || southEastCell.IsNotFlat()) return false;

            var eastCell = cells[cell.Y][cell.X + 1];
            if (!eastCell.White || eastCell.IsNotFlat()) return false;

            var northCell = cells[cell.Y - 1][cell.X];
            if (!northCell.White) return false;

            var westCell = cells[cell.Y][cell.X - 1];
            if (!westCell.White) return false;

            cell.Backup();
            northCell.Backup();
            southEastCell.Backup();
            eastCell.Backup();
            southCell.Backup();
            westCell.Backup();

            WireCross(cell, northCell, eastCell, southCell, westCell, northSouthHopsEastWest);

            if (southCell.Lower.East == null)
            {
                southCell.Lower.East = southEastCell.Lower;
                southEastCell.Lower.West = southCell.Lower;
            }
            if (eastCell.Lower.South == null)
            {
                eastCell.Lower.South = southEastCell.Lower;
                southEastCell.Lower.North = eastCell.Lower;
            }

            if (FindLoop(cells, cell.Lower) || FindLoop(cells, cell.Upper))
            {
                cell.Restore();
                northCell.Restore();
                southEastCell.Restore();
                eastCell.Restore();
                southCell.Restore();
                westCell.Restore();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 添加西南方向环
        /// </summary>
        private bool AddSouthWestLoop(SquareCell[][] cells, SquareCell cell, bool northSouthHopsEastWest)
        {
            var southCell = cells[cell.Y + 1][cell.X];
            if (!southCell.White || southCell.IsNotFlat()) return false;

            var southWestCell = cells[cell.Y + 1][cell.X - 1];
            if (!southWestCell.White || southWestCell.IsNotFlat()) return false;

            var westCell = cells[cell.Y][cell.X - 1];
            if (!westCell.White || westCell.IsNotFlat()) return false;

            var northCell = cells[cell.Y - 1][cell.X];
            if (!northCell.White) return false;

            var eastCell = cells[cell.Y][cell.X + 1];
            if (!eastCell.White) return false;

            cell.Backup();
            northCell.Backup();
            southWestCell.Backup();
            eastCell.Backup();
            southCell.Backup();
            westCell.Backup();

            WireCross(cell, northCell, eastCell, southCell, westCell, northSouthHopsEastWest);

            if (southCell.Lower.West == null)
            {
                southCell.Lower.West = southWestCell.Lower;
                southWestCell.Lower.East = southCell.Lower;
            }
            if (westCell.Lower.South == null)
            {
                westCell.Lower.South = southWestCell.Lower;
                southWestCell.Lower.North = westCell.Lower;
            }

            if (FindLoop(cells, cell.Lower) || FindLoop(cells, cell.Upper))
            {
                cell.Restore();
                northCell.Restore();
                southWestCell.Restore();
                eastCell.Restore();
                southCell.Restore();
                westCell.Restore();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 添加西北方向环
        /// </summary>
        private bool AddNorthWestLoop(SquareCell[][] cells, SquareCell cell, bool northSouthHopsEastWest)
        {
            var northCell = cells[cell.Y - 1][cell.X];
            if (!northCell.White || northCell.IsNotFlat()) return false;

            var northWestCell = cells[cell.Y - 1][cell.X - 1];
            if (!northWestCell.White || northWestCell.IsNotFlat()) return false;

            var westCell = cells[cell.Y][cell.X - 1];
            if (!westCell.White || westCell.IsNotFlat()) return false;

            var southCell = cells[cell.Y + 1][cell.X];
            if (!southCell.White) return false;

            var eastCell = cells[cell.Y][cell.X + 1];
            if (!eastCell.White) return false;

            cell.Backup();
            northCell.Backup();
            northWestCell.Backup();
            eastCell.Backup();
            southCell.Backup();
            westCell.Backup();

            WireCross(cell, northCell, eastCell, southCell, westCell, northSouthHopsEastWest);

            if (northCell.Lower.West == null)
            {
                northCell.Lower.West = northWestCell.Lower;
                northWestCell.Lower.East = northCell.Lower;
            }
            if (westCell.Lower.North == null)
            {
                westCell.Lower.North = northWestCell.Lower;
                northWestCell.Lower.South = westCell.Lower;
            }

            if (FindLoop(cells, cell.Lower) || FindLoop(cells, cell.Upper))
            {
                cell.Restore();
                northCell.Restore();
                northWestCell.Restore();
                eastCell.Restore();
                southCell.Restore();
                westCell.Restore();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 添加十字交叉
        /// </summary>
        private bool AddCross(SquareCell[][] cells, SquareCell cell, bool northSouthHopsEastWest)
        {
            var northCell = cells[cell.Y - 1][cell.X];
            if (!northCell.White) return false;

            var eastCell = cells[cell.Y][cell.X + 1];
            if (!eastCell.White) return false;

            var southCell = cells[cell.Y + 1][cell.X];
            if (!southCell.White) return false;

            var westCell = cells[cell.Y][cell.X - 1];
            if (!westCell.White) return false;

            cell.Backup();
            northCell.Backup();
            eastCell.Backup();
            southCell.Backup();
            westCell.Backup();

            WireCross(cell, northCell, eastCell, southCell, westCell, northSouthHopsEastWest);

            if (FindLoop(cells, cell.Lower) || FindLoop(cells, cell.Upper))
            {
                cell.Restore();
                northCell.Restore();
                eastCell.Restore();
                southCell.Restore();
                westCell.Restore();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在指定单元格处接线跨越结构。
        /// 当 northSouthHopsEastWest 为 true 时，南北通道走上层（跨越东西通道）；
        /// 为 false 时，东西通道走上层（跨越南北通道）。
        /// </summary>
        private static void WireCross(SquareCell cell, SquareCell northCell, SquareCell eastCell, SquareCell southCell, SquareCell westCell,
            bool northSouthHopsEastWest)
        {
            if (northSouthHopsEastWest)
            {
                // 南北通道走上层，跨越东西通道

                // 北向连接：转移到 upper
                if (cell.Lower.North != null)
                {
                    cell.Lower.North.South = cell.Upper;
                    cell.Upper.North = cell.Lower.North;
                    cell.Lower.North = null;
                }
                else
                {
                    northCell.Lower.South = cell.Upper;
                    cell.Upper.North = northCell.Lower;
                }

                // 南向连接：转移到 upper
                if (cell.Lower.South != null)
                {
                    cell.Lower.South.North = cell.Upper;
                    cell.Upper.South = cell.Lower.South;
                    cell.Lower.South = null;
                }
                else
                {
                    southCell.Lower.North = cell.Upper;
                    cell.Upper.South = southCell.Lower;
                }

                // 东向连接：保持在 lower
                if (cell.Lower.East == null)
                {
                    cell.Lower.East = eastCell.Lower;
                    eastCell.Lower.West = cell.Lower;
                }

                // 西向连接：保持在 lower
                if (cell.Lower.West == null)
                {
                    cell.Lower.West = westCell.Lower;
                    westCell.Lower.East = cell.Lower;
                }
            }
            else
            {
                // 东西通道走上层，跨越南北通道

                // 东向连接：转移到 upper
                if (cell.Lower.East != null)
                {
                    cell.Lower.East.West = cell.Upper;
                    cell.Upper.East = cell.Lower.East;
                    cell.Lower.East = null;
                }
                else
                {
                    eastCell.Lower.West = cell.Upper;
                    cell.Upper.East = eastCell.Lower;
                }

                // 西向连接：转移到 upper
                if (cell.Lower.West != null)
                {
                    cell.Lower.West.East = cell.Upper;
                    cell.Upper.West = cell.Lower.West;
                    cell.Lower.West = null;
                }
                else
                {
                    westCell.Lower.East = cell.Upper;
                    cell.Upper.West = westCell.Lower;
                }

                // 北向连接：保持在 lower
                if (cell.Lower.North == null)
                {
                    cell.Lower.North = northCell.Lower;
                    northCell.Lower.South = cell.Lower;
                }

                // 南向连接：保持在 lower
                if (cell.Lower.South == null)
                {
                    cell.Lower.South = southCell.Lower;
                    southCell.Lower.North = cell.Lower;
                }
            }
        }

        /// <summary>
        /// 从种子节点出发，检测图中是否存在图论环路（非树边）。
        /// 使用 DFS，visitedBy 记录父节点。若遇到已访问且非父节点的邻居，则存在环路。
        /// </summary>
        private static bool FindLoop(SquareCell[][] cells, SquareNode seed)
        {
            int height = cells.Length;
            int width = cells[0].Length;

            // 重置所有节点的 visitedBy
            for (int i = height - 1; i >= 0; --i)
            {
                for (int j = width - 1; j >= 0; --j)
                {
                    cells[i][j].Lower.VisitedBy = null;
                    cells[i][j].Upper.VisitedBy = null;
                }
            }

            seed.VisitedBy = seed;
            var stack = new List<SquareNode> { seed };
            int stackIndex = stack.Count - 1;

            while (stackIndex >= 0)
            {
                var node = stack[stackIndex];
                stack.RemoveAt(stackIndex);
                stackIndex--;

                if (CheckNeighbor(node.North, node) ||
                    CheckNeighbor(node.East, node) ||
                    CheckNeighbor(node.South, node) ||
                    CheckNeighbor(node.West, node))
                {
                    return true;
                }
            }

            return false;

            bool CheckNeighbor(SquareNode? neighbor, SquareNode parent)
            {
                if (neighbor == null) return false;
                if (neighbor.VisitedBy != null)
                {
                    // 邻居已被访问，如果邻居不是当前节点的父节点，则存在环路
                    return neighbor != parent.VisitedBy;
                }
                neighbor.VisitedBy = parent;
                stack.Add(neighbor);
                stackIndex++;
                return false;
            }
        }

        #endregion

        #region 区域标记

        /// <summary>
        /// 为所有节点分配区域标识。通过 DFS 洪水填充，沿节点的四方向连接遍历，
        /// 连通的节点分配相同的区域 ID。返回按区域 ID 索引的节点列表。
        /// </summary>
        private static List<SquareNode[]> AssignRegions(SquareCell[][] cells, int height, int width)
        {
            var regionNodes = new List<SquareNode[]>();
            int regionId = 0;

            for (int i = height - 1; i >= 0; --i)
            {
                for (int j = width - 1; j >= 0; --j)
                {
                    var cell = cells[i][j];
                    if (cell.Lower.Region < 0)
                    {
                        if (regionId < regionNodes.Count)
                            regionNodes[regionId] = AssignRegion(regionId, cell.Lower);
                        else
                            regionNodes.Add(AssignRegion(regionId, cell.Lower));
                        regionId++;
                    }
                    if (cell.Upper.Region < 0)
                    {
                        if (regionId < regionNodes.Count)
                            regionNodes[regionId] = AssignRegion(regionId, cell.Upper);
                        else
                            regionNodes.Add(AssignRegion(regionId, cell.Upper));
                        regionId++;
                    }
                }
            }

            return regionNodes;
        }

        /// <summary>
        /// 从种子节点出发，DFS 填充同一区域的所有节点
        /// </summary>
        private static SquareNode[] AssignRegion(int region, SquareNode seed)
        {
            var nodes = new List<SquareNode>();
            seed.Region = region;
            nodes.Add(seed);

            var stack = new List<SquareNode> { seed };
            while (stack.Count > 0)
            {
                int last = stack.Count - 1;
                var node = stack[last];
                stack.RemoveAt(last);

                TryEnqueue(node.North);
                TryEnqueue(node.East);
                TryEnqueue(node.South);
                TryEnqueue(node.West);
            }

            return nodes.ToArray();

            void TryEnqueue(SquareNode? neighbor)
            {
                if (neighbor != null && neighbor.Region < 0)
                {
                    neighbor.Region = region;
                    stack.Add(neighbor);
                    nodes.Add(neighbor);
                }
            }
        }

        #endregion

        #region 生成树

        /// <summary>
        /// 使用随机化 Kruskal 算法变体生成生成树，将所有区域连通。
        /// longCorridors 为 true 时使用 Hunt-and-Kill 变体，生成更长的通道。
        /// </summary>
        private void CreateSpanningTree(SquareCell[][] cells, int height, int width, List<SquareNode[]> regions, bool longCorridors)
        {
            int maxX = width - 1;
            int maxY = height - 1;

            // 收集可扩展节点：upper 节点没有 north 和 east 连接的 lower 节点
            var nodes = new List<SquareNode>();
            for (int i = maxY; i >= 0; --i)
            {
                for (int j = maxX; j >= 0; --j)
                {
                    var cell = cells[i][j];
                    if (cell.White && cell.Upper.North == null && cell.Upper.East == null)
                    {
                        nodes.Add(cell.Lower);
                    }
                }
            }

            if (longCorridors)
            {
                ShuffleList(nodes);
            }

            while (nodes.Count > 0)
            {
                int index = longCorridors ? nodes.Count - 1 : (int)(nodes.Count * random.NextDouble());
                var node = nodes[index];

                if (longCorridors)
                {
                    MoveToEnd(nodes, node);
                }

                var cell = node.Cell;
                var permutation = Permutations[random.Next(Permutations.Length)];
                bool connected = false;

                for (int i = permutation.Length - 1; i >= 0; --i)
                {
                    switch (permutation[i])
                    {
                        case 0: // 北
                            if (cell.Y > 0 && node.North == null)
                            {
                                var northCell = cells[cell.Y - 1][cell.X];
                                if (northCell.White && northCell.Lower.Region != node.Region)
                                {
                                    northCell.Lower.South = node;
                                    node.North = northCell.Lower;
                                    if (longCorridors) MoveToEnd(nodes, node.North);
                                    MergeRegions(northCell.Lower.Region, node.Region, regions);
                                    connected = true;
                                }
                            }
                            break;
                        case 1: // 东
                            if (cell.X < maxX && node.East == null)
                            {
                                var eastCell = cells[cell.Y][cell.X + 1];
                                if (eastCell.White && eastCell.Lower.Region != node.Region)
                                {
                                    eastCell.Lower.West = node;
                                    node.East = eastCell.Lower;
                                    if (longCorridors) MoveToEnd(nodes, node.East);
                                    MergeRegions(eastCell.Lower.Region, node.Region, regions);
                                    connected = true;
                                }
                            }
                            break;
                        case 2: // 南
                            if (cell.Y < maxY && node.South == null)
                            {
                                var southCell = cells[cell.Y + 1][cell.X];
                                if (southCell.White && southCell.Lower.Region != node.Region)
                                {
                                    southCell.Lower.North = node;
                                    node.South = southCell.Lower;
                                    if (longCorridors) MoveToEnd(nodes, node.South);
                                    MergeRegions(southCell.Lower.Region, node.Region, regions);
                                    connected = true;
                                }
                            }
                            break;
                        default: // 西
                            if (cell.X > 0 && node.West == null)
                            {
                                var westCell = cells[cell.Y][cell.X - 1];
                                if (westCell.White && westCell.Lower.Region != node.Region)
                                {
                                    westCell.Lower.East = node;
                                    node.West = westCell.Lower;
                                    if (longCorridors) MoveToEnd(nodes, node.West);
                                    MergeRegions(westCell.Lower.Region, node.Region, regions);
                                    connected = true;
                                }
                            }
                            break;
                    }

                    if (connected) break;
                }

                if (longCorridors)
                {
                    if (!connected)
                    {
                        nodes.RemoveAt(nodes.Count - 1);
                    }
                }
                else
                {
                    if (!connected)
                    {
                        nodes.RemoveAt(index);
                    }
                }
            }
        }

        /// <summary>
        /// 合并两个区域：将 region1 的所有节点归入 region2
        /// </summary>
        private static void MergeRegions(int region1, int region2, List<SquareNode[]> regions)
        {
            var region1Nodes = regions[region1];
            var region2Nodes = regions[region2];
            for (int i = region1Nodes.Length - 1; i >= 0; --i)
            {
                region1Nodes[i].Region = region2;
            }
            // 将 region1 的节点追加到 region2，并清空 region1
            var merged = new SquareNode[region2Nodes.Length + region1Nodes.Length];
            region2Nodes.CopyTo(merged, 0);
            region1Nodes.CopyTo(merged, region2Nodes.Length);
            regions[region2] = merged;
            regions[region1] = Array.Empty<SquareNode>();
        }

        /// <summary>将节点移到列表末尾（用于长通道模式）</summary>
        private static void MoveToEnd(List<SquareNode> nodes, SquareNode node)
        {
            int index = nodes.IndexOf(node);
            if (index < 0 || index == nodes.Count - 1) return;
            nodes.RemoveAt(index);
            nodes.Add(node);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 生成 0-3 的所有 24 种排列
        /// </summary>
        private static int[][] GeneratePermutations()
        {
            var result = new List<int[]>();
            var arr = new[] { 0, 1, 2, 3 };
            Permute(arr, 0, result);
            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="start"></param>
        /// <param name="result"></param>
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

        /// <summary>
        /// Fisher-Yates 洗牌
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion
    }
}
