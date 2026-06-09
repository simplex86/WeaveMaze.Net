using System;
using System.Threading.Tasks;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 矩形编织式迷宫字段，用于存储迷宫的生成参数和结果数据。
    /// 渲染时可完全基于此结构中的参数和数据完成。
    /// </summary>
    public struct RectangularWeaveMazeField
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

        /// <summary>
        /// 可选的遮罩（Mask）。为 null 时表示标准矩形迷宫（所有位置均可用）。
        /// 通过 RectangularWeaveMazeMaskLoader 从图片加载。
        /// </summary>
        public RectangularWeaveMazeMask? Mask { get; set; }

        #endregion

        #region 生成结果

        /// <summary>
        /// 生成的迷宫单元格数据。索引为 [y][x]，即 [行][列]。
        /// 在调用 Generate 之前为 null。
        /// 每个单元格的 Lower 和 Upper 节点的四方向连接（North/East/South/West）
        /// 描述了迷宫的通道结构；xxx2 字段描述了解路径。
        /// </summary>
        public SquareCell[][]? Cells { get; set; }

        #endregion

        /// <summary>
        /// 创建矩形编织式迷宫字段，使用默认值
        /// </summary>
        /// <param name="width">迷宫宽度（列数），默认 30</param>
        /// <param name="height">迷宫高度（行数），默认 30</param>
        /// <param name="loopFrac">环比例（0~1），默认 0.05</param>
        /// <param name="crossFrac">交叉比例（0~1），默认 0.25</param>
        /// <param name="longPassages">是否启用长通道模式，默认 false</param>
        /// <param name="mask">可选遮罩，null 表示标准矩形</param>
        public RectangularWeaveMazeField()
            : this(DefaultSize, DefaultSize, DefaultLoopFrac, DefaultCrossFrac, DefaultLongPassages)
        {

        }

        /// <summary>
        /// 创建矩形编织式迷宫字段，使用默认值
        /// </summary>
        /// <param name="width">迷宫宽度（列数），默认 30</param>
        /// <param name="height">迷宫高度（行数），默认 30</param>
        /// <param name="loopFrac">环比例（0~1），默认 0.05</param>
        /// <param name="crossFrac">交叉比例（0~1），默认 0.25</param>
        /// <param name="longPassages">是否启用长通道模式，默认 false</param>
        public RectangularWeaveMazeField(int width, int height, double loopFrac, double crossFrac, bool longPassages)
        {
            Width = width;
            Height = height;
            LoopFrac = loopFrac;
            CrossFrac = crossFrac;
            LongPassages = longPassages;
            Mask = null;
            Cells = null;
        }

        /// <summary>
        /// 创建矩形编织式迷宫字段
        /// </summary>
        /// <param name="mask">遮罩</param>
        /// <param name="loopFrac">环比例（0~1）</param>
        /// <param name="crossFrac">交叉比例（0~1）</param>
        /// <param name="longPassages">是否启用长通道模式</param>
        public RectangularWeaveMazeField(RectangularWeaveMazeMask mask,
                                         double loopFrac,
                                         double crossFrac,
                                         bool longPassages)
        {
            Mask = mask;
            Width = mask.Width;
            Height = mask.Height;
            LoopFrac = loopFrac;
            CrossFrac = crossFrac;
            LongPassages = longPassages;
            Cells = null;
        }
    }
}
