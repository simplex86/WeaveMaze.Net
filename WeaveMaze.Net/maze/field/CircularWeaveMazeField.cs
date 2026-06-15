using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 圆形编织式迷宫字段（环形结构），所有位置均可用。
    /// 迷宫由 Rings 层同心环和 Sectors 个扇区组成，形成环形网格。
    ///
    /// 坐标系：x = 扇区索引（0 ~ Sectors-1），y = 环索引（0 ~ Rings-1）
    /// 方向定义：0=向内(Inward), 1=顺时针(CW), 2=向外(Outward), 3=逆时针(CCW)
    /// 顺时针/逆时针方向在扇区内环绕（wrap-around）。
    ///
    /// 顶点编号规则与基类一致：每个单元格 (x, y) 拥有两个顶点：
    ///   Lower 顶点索引 = (y * Width + x) * 2
    ///   Upper 顶点索引 = (y * Width + x) * 2 + 1
    /// </summary>
    public class CircularWeaveMazeField : WeaveMazeField
    {
        /// <summary>默认环数</summary>
        public const int DefaultRings = 15;

        /// <summary>默认扇区数</summary>
        public const int DefaultSectors = 24;

        /// <summary>
        /// 默认最小内弧占比：内弧长度占环宽的最小比例（0~1）。
        /// 当某环的内弧长度与环宽之比小于此值时，该环不参与迷宫生成。
        /// 默认 0.75，对 24 扇区恰好跳过内层 3 环。
        /// </summary>
        public const double DefaultMinInnerArcFrac = 0.75;

        /// <summary>环数（同心圆层数），即 Height</summary>
        public int Rings => Height;

        /// <summary>扇区数（每环的单元格数），即 Width</summary>
        public int Sectors => Width;

        /// <summary>
        /// 最小内弧占比（0~1）。内弧长度 = 2π × 环索引 × 环宽 / 扇区数，
        /// 当内弧长度 / 环宽 &lt; MinInnerArcFrac 时，该环被视为无效层。
        /// </summary>
        public double MinInnerArcFrac { get; }

        /// <summary>
        /// 根据 MinInnerArcFrac 和 Sectors 动态计算的跳过环数。
        /// 环 i 的内弧比 = 2πi/sectors，首个满足条件的 i 即为 SkipRings。
        /// </summary>
        public int SkipRings => Math.Max(0, (int)Math.Ceiling(MinInnerArcFrac * Sectors / (2.0 * Math.PI)));

        /// <summary>
        /// 创建圆形编织式迷宫字段，使用默认值
        /// </summary>
        public CircularWeaveMazeField()
            : this(DefaultRings, DefaultSectors, DefaultLoopFrac, DefaultCrossFrac, DefaultLongPassages, DefaultMinInnerArcFrac)
        {
        }

        /// <summary>
        /// 创建圆形编织式迷宫字段（兼容旧接口，使用默认最小内弧占比）
        /// </summary>
        public CircularWeaveMazeField(int rings, int sectors, double loopFrac, double crossFrac, bool longPassages)
            : this(rings, sectors, loopFrac, crossFrac, longPassages, DefaultMinInnerArcFrac)
        {
        }

        /// <summary>
        /// 创建圆形编织式迷宫字段
        /// </summary>
        /// <param name="rings">环数（同心圆层数）</param>
        /// <param name="sectors">扇区数（每环的单元格数）</param>
        /// <param name="loopFrac">环比例（0~1）</param>
        /// <param name="crossFrac">交叉比例（0~1）</param>
        /// <param name="longPassages">是否启用长通道模式</param>
        /// <param name="minInnerArcFrac">最小内弧占比（0~1），内弧长度占环宽的最小比例</param>
        public CircularWeaveMazeField(int rings, int sectors, double loopFrac, double crossFrac, bool longPassages, double minInnerArcFrac)
            : base(sectors, rings, loopFrac, crossFrac, longPassages)
        {
            MinInnerArcFrac = minInnerArcFrac;
        }

        /// <summary>
        /// 创建单元格白色遮罩。内弧长度不足的环标记为不可用（黑色），
        /// 其余环标记为可用（白色）。
        /// </summary>
        internal override bool[][] CreateCellWhiteMask()
        {
            int skip = SkipRings;
            var mask = new bool[Height][];
            for (int i = Height - 1; i >= 0; --i)
            {
                mask[i] = new bool[Width];
                for (int j = Width - 1; j >= 0; --j)
                {
                    mask[i][j] = i >= skip;
                }
            }
            return mask;
        }

        /// <summary>
        /// 圆形拓扑邻居计算。方向：0=向内, 1=顺时针, 2=向外, 3=逆时针
        /// 顺时针/逆时针方向在扇区内环绕（wrap-around），始终有效。
        /// </summary>
        internal override (int x, int y)? GetNeighbor(int x, int y, int direction) => direction switch
        {
            0 => y > 0 ? (x, y - 1) : null,                              // 向内
            1 => ((x + 1) % Width, y),                                     // 顺时针（环绕）
            2 => y < Height - 1 ? (x, y + 1) : null,                      // 向外
            3 => ((x - 1 + Width) % Width, y),                             // 逆时针（环绕）
            _ => null
        };

        /// <summary>
        /// 圆形内部单元格：不在最内层有效环（SkipRings）和最外环上的单元格。
        /// 最内层有效环的向内邻居为被跳过的环，不可作为环/交叉的中心。
        /// </summary>
        internal override bool IsInteriorCell(int x, int y) =>
            y > SkipRings && y < Height - 1;

        /// <summary>
        /// 圆形迷宫解法路径端点仅限外环（y = Height - 1），
        /// 避免出入口出现在内层较小的圆弧上。
        /// </summary>
        internal override bool IsValidSolutionEndpoint(int x, int y) =>
            y == Height - 1;

        /// <summary>
        /// 圆形迷宫出入口仅允许在外环的向外方向（方向2），
        /// 确保出入口清晰可见。
        /// </summary>
        internal override bool CanPlaceGate(int x, int y, int direction) =>
            direction == 2 && y == Height - 1;
    }
}
