using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 编织式迷宫字段基类，用于存储迷宫的生成参数和结果数据。
    /// 渲染时可完全基于此结构中的参数和数据完成。
    ///
    /// 顶点编号规则：每个单元格 (x, y) 拥有两个顶点：
    ///   Lower 顶点索引 = (y * Width + x) * 2
    ///   Upper 顶点索引 = (y * Width + x) * 2 + 1
    /// 总顶点数 = Width * Height * 2
    /// </summary>
    public abstract class WeaveMazeField
    {
        #region 常量

        /// <summary>迷宫最小尺寸</summary>
        public const int MinSize = 1;

        /// <summary>迷宫最大尺寸</summary>
        public const int MaxSize = 200;

        /// <summary>迷宫默认尺寸</summary>
        public const int DefaultSize = 30;

        /// <summary>环比例最小值</summary>
        public const double MinLoopFrac = 0;

        /// <summary>环比例最大值</summary>
        public const double MaxLoopFrac = 1;

        /// <summary>环比例默认值（5%）</summary>
        public const double DefaultLoopFrac = 0.05;

        /// <summary>交叉比例最小值</summary>
        public const double MinCrossFrac = 0;

        /// <summary>交叉比例最大值</summary>
        public const double MaxCrossFrac = 1;

        /// <summary>交叉比例默认值（25%）</summary>
        public const double DefaultCrossFrac = 0.25;

        /// <summary>长通道模式默认值</summary>
        public const bool DefaultLongPassages = false;

        #endregion

        #region 生成参数

        /// <summary>迷宫宽度（列数）</summary>
        public int Width { get; set; }

        /// <summary>迷宫高度（行数）</summary>
        public int Height { get; set; }

        /// <summary>环比例：单元格中形成回环的比例（0~1）</summary>
        public double LoopFrac { get; set; }

        /// <summary>交叉比例：单元格中形成十字交叉的比例（0~1）</summary>
        public double CrossFrac { get; set; }

        /// <summary>是否启用长通道模式（Hunt-and-Kill 变体，生成更长的蜿蜒通道）</summary>
        public bool LongPassages { get; set; }

        #endregion

        #region 生成结果

        /// <summary>
        /// 邻接表（图）。每个顶点的邻接表存储该顶点的所有出边。
        /// 在调用 Generate 之前为 null。
        /// </summary>
        internal List<List<WeaveAdjacency>>? Graph { get; set; }

        /// <summary>顶点总数 = Width * Height * 2</summary>
        internal int VertexCount { get; set; }

        /// <summary>每个顶点所属单元格的列坐标（x）</summary>
        internal int[]? VertexCellX { get; set; }

        /// <summary>每个顶点所属单元格的行坐标（y）</summary>
        internal int[]? VertexCellY { get; set; }

        /// <summary>每个顶点是否为 Upper 层</summary>
        internal bool[]? VertexIsUpper { get; set; }

        /// <summary>每个单元格是否可用（白色区域）</summary>
        internal bool[]? CellWhite { get; set; }

        /// <summary>每个单元格是否有南北向跨越（Upper 层走南北）</summary>
        internal bool[]? CellOverNS { get; set; }

        /// <summary>每个单元格是否有东西向跨越（Upper 层走东西）</summary>
        internal bool[]? CellOverEW { get; set; }

        #endregion

        #region 索引计算

        /// <summary>获取单元格 (x, y) 的 Lower 顶点索引</summary>
        internal int LowerIndex(int x, int y) => (y * Width + x) * 2;

        /// <summary>获取单元格 (x, y) 的 Upper 顶点索引</summary>
        internal int UpperIndex(int x, int y) => (y * Width + x) * 2 + 1;

        /// <summary>获取单元格 (x, y) 的一维索引</summary>
        internal int CellIndex(int x, int y) => y * Width + x;

        #endregion

        /// <summary>
        /// 创建编织式迷宫字段
        /// </summary>
        protected WeaveMazeField(int width, int height, double loopFrac, double crossFrac, bool longPassages)
        {
            Width = width;
            Height = height;
            LoopFrac = loopFrac;
            CrossFrac = crossFrac;
            LongPassages = longPassages;
        }

        /// <summary>
        /// 创建单元格白色遮罩。由子类实现，根据是否有遮罩决定每个单元格是否可用。
        /// 返回 bool[][]，索引为 [y][x]，true 表示可用。
        /// </summary>
        internal abstract bool[][] CreateCellWhiteMask();

        #region 拓扑抽象

        /// <summary>
        /// 获取指定单元格在给定方向上的邻居坐标。
        /// 方向约定：0 和 2 为对向（如北/南、内/外），1 和 3 为对向（如东/西、顺时针/逆时针）。
        /// 返回 null 表示该方向无有效邻居（超出边界）。
        /// </summary>
        /// <param name="x">单元格列坐标</param>
        /// <param name="y">单元格行坐标</param>
        /// <param name="direction">方向索引（0~3）</param>
        /// <returns>邻居坐标，或 null 表示无邻居</returns>
        internal abstract (int x, int y)? GetNeighbor(int x, int y, int direction);

        /// <summary>
        /// 判断指定单元格是否为内部单元格（可作为环/交叉的中心）。
        /// 内部单元格在所有四个方向上都有有效邻居。
        /// </summary>
        internal abstract bool IsInteriorCell(int x, int y);

        /// <summary>
        /// 判断指定单元格是否可作为解法路径的端点（边界单元格）。
        /// 默认所有白色边界单元格均可。子类可重写以限制端点位置（如圆形迷宫仅外环）。
        /// </summary>
        internal virtual bool IsValidSolutionEndpoint(int x, int y) => true;

        /// <summary>
        /// 判断指定单元格的指定方向是否可以放置出入口。
        /// 默认所有边界方向均可。子类可重写以限制出入口位置（如圆形迷宫仅外环向外）。
        /// </summary>
        internal virtual bool CanPlaceGate(int x, int y, int direction) => true;

        #endregion
    }
}
