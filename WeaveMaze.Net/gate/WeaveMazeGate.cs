using System;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫出入口（门）信息。记录迷宫中一个出入口的位置和方向。
    /// </summary>
    public class WeaveMazeGate
    {
        /// <summary>出入口所在的单元格</summary>
        internal SquareCell Cell { get; }

        /// <summary>
        /// 出入口方向：0=北, 1=东, 2=南, 3=西
        /// </summary>
        internal int Direction { get; }

        /// <summary>
        /// 创建迷宫出入口
        /// </summary>
        /// <param name="cell">出入口所在的单元格</param>
        /// <param name="direction">出入口方向：0=北, 1=东, 2=南, 3=西</param>
        internal WeaveMazeGate(SquareCell cell, int direction)
        {
            Cell = cell;
            Direction = direction;
        }

        /// <summary>方向对应的位掩码：北=0b1000, 东=0b0100, 南=0b0010, 西=0b0001</summary>
        internal int DirectionBit => Direction switch
        {
            0 => 0b1000,
            1 => 0b0100,
            2 => 0b0010,
            _ => 0b0001,
        };
    }
}
