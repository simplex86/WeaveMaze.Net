using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫出入口（门）信息。记录迷宫中一个出入口的位置和方向。
    /// </summary>
    public class WeaveMazeGate
    {
        /// <summary>出入口所在的单元格列坐标（x）</summary>
        public int CellX { get; }

        /// <summary>出入口所在的单元格行坐标（y）</summary>
        public int CellY { get; }

        /// <summary>
        /// 出入口方向：0=北, 1=东, 2=南, 3=西
        /// </summary>
        public int Direction { get; }

        /// <summary>
        /// 创建迷宫出入口
        /// </summary>
        /// <param name="cellX">出入口所在的单元格列坐标</param>
        /// <param name="cellY">出入口所在的单元格行坐标</param>
        /// <param name="direction">出入口方向：0=北, 1=东, 2=南, 3=西</param>
        public WeaveMazeGate(int cellX, int cellY, int direction)
        {
            CellX = cellX;
            CellY = cellY;
            Direction = direction;
        }

        /// <summary>方向对应的位掩码：北=0b1000, 东=0b0100, 南=0b0010, 西=0b0001</summary>
        public int DirectionBit => Direction switch
        {
            0 => 0b1000,
            1 => 0b0100,
            2 => 0b0010,
            _ => 0b0001,
        };
    }
}
