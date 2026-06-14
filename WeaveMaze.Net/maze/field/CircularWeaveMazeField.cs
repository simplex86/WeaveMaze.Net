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

        /// <summary>环数（同心圆层数），即 Height</summary>
        public int Rings => Height;

        /// <summary>扇区数（每环的单元格数），即 Width</summary>
        public int Sectors => Width;

        /// <summary>
        /// 创建圆形编织式迷宫字段，使用默认值
        /// </summary>
        public CircularWeaveMazeField()
            : this(DefaultRings, DefaultSectors, DefaultLoopFrac, DefaultCrossFrac, DefaultLongPassages)
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
        public CircularWeaveMazeField(int rings, int sectors, double loopFrac, double crossFrac, bool longPassages)
            : base(sectors, rings, loopFrac, crossFrac, longPassages)
        {
        }

        /// <summary>
        /// 创建单元格白色遮罩。无遮罩时所有单元格均为白色。
        /// </summary>
        internal override bool[][] CreateCellWhiteMask()
        {
            var mask = new bool[Height][];
            for (int i = Height - 1; i >= 0; --i)
            {
                mask[i] = new bool[Width];
                for (int j = Width - 1; j >= 0; --j)
                {
                    mask[i][j] = true;
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
        /// 圆形内部单元格：不在最内环和最外环上的单元格
        /// </summary>
        internal override bool IsInteriorCell(int x, int y) =>
            y >= 1 && y < Height - 1;
    }
}
