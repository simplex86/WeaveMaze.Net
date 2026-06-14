using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 自定义形状编织式迷宫字段（带遮罩），Width/Height 由遮罩决定。
    /// </summary>
    public class CustomizedWeaveMazeField : WeaveMazeField
    {
        /// <summary>
        /// 遮罩数据。描述迷宫中哪些位置可用（白色）哪些被屏蔽（黑色）。
        /// </summary>
        public CustomizedWeaveMazeMask Mask { get; }

        /// <summary>
        /// 创建自定义形状编织式迷宫字段
        /// </summary>
        /// <param name="mask">遮罩，Width/Height 由遮罩决定</param>
        /// <param name="loopFrac">环比例（0~1）</param>
        /// <param name="crossFrac">交叉比例（0~1）</param>
        /// <param name="longPassages">是否启用长通道模式</param>
        public CustomizedWeaveMazeField(CustomizedWeaveMazeMask mask,
                                        double loopFrac,
                                        double crossFrac,
                                        bool longPassages)
            : base(mask.Width, mask.Height, loopFrac, crossFrac, longPassages)
        {
            Mask = mask;
        }

        /// <summary>
        /// 创建单元格白色遮罩。根据遮罩决定每个单元格是否为白色。
        /// </summary>
        internal override bool[][] CreateCellWhiteMask()
        {
            var mask = new bool[Height][];
            for (int i = Height - 1; i >= 0; --i)
            {
                mask[i] = new bool[Width];
                for (int j = Width - 1; j >= 0; --j)
                {
                    mask[i][j] = Mask[i, j];
                }
            }
            return mask;
        }

        /// <summary>
        /// 矩形拓扑邻居计算（与 RectangularWeaveMazeField 相同）。
        /// 方向：0=北, 1=东, 2=南, 3=西
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
