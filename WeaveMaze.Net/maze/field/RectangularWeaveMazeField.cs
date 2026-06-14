using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 矩形编织式迷宫字段（无遮罩），所有位置均可用。
    /// </summary>
    public class RectangularWeaveMazeField : WeaveMazeField
    {
        /// <summary>
        /// 创建矩形编织式迷宫字段，使用默认值
        /// </summary>
        public RectangularWeaveMazeField()
            : this(DefaultSize, DefaultSize, DefaultLoopFrac, DefaultCrossFrac, DefaultLongPassages)
        {
        }

        /// <summary>
        /// 创建矩形编织式迷宫字段
        /// </summary>
        /// <param name="width">迷宫宽度（列数）</param>
        /// <param name="height">迷宫高度（行数）</param>
        /// <param name="loopFrac">环比例（0~1）</param>
        /// <param name="crossFrac">交叉比例（0~1）</param>
        /// <param name="longPassages">是否启用长通道模式</param>
        public RectangularWeaveMazeField(int width, int height, double loopFrac, double crossFrac, bool longPassages)
            : base(width, height, loopFrac, crossFrac, longPassages)
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
        /// 矩形拓扑邻居计算。方向：0=北, 1=东, 2=南, 3=西
        /// </summary>
        internal override (int x, int y)? GetNeighbor(int x, int y, int direction) => direction switch
        {
            0 => y > 0 ? (x, y - 1) : null,
            1 => x < Width - 1 ? (x + 1, y) : null,
            2 => y < Height - 1 ? (x, y + 1) : null,
            3 => x > 0 ? (x - 1, y) : null,
            _ => null
        };

        /// <summary>
        /// 矩形内部单元格：不在边界上的单元格
        /// </summary>
        internal override bool IsInteriorCell(int x, int y) =>
            x >= 1 && x < Width - 1 && y >= 1 && y < Height - 1;
    }
}
