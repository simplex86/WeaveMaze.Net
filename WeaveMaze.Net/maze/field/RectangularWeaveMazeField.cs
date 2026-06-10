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
        /// 创建单元格数组。无遮罩时所有单元格均为白色。
        /// </summary>
        public override SquareCell[][] CreateCells()
        {
            var cells = new SquareCell[Height][];
            for (int i = Height - 1; i >= 0; --i)
            {
                cells[i] = new SquareCell[Width];
                for (int j = Width - 1; j >= 0; --j)
                {
                    cells[i][j] = new SquareCell(j, i, true);
                }
            }
            return cells;
        }
    }
}
