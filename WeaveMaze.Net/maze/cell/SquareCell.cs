namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫单元格。每个单元格包含下层（lower）和上层（upper）两个节点。
    /// 平坦单元格只有 lower 节点参与连接；非平坦单元格（存在跨越结构）的 upper 节点也有连接。
    /// </summary>
    internal class SquareCell
    {
        /// <summary>下层节点（地面层）</summary>
        public SquareNode Lower { get; }

        /// <summary>上层节点（架空层，用于通道跨越）</summary>
        public SquareNode Upper { get; }

        /// <summary>单元格在迷宫中的列坐标（x）</summary>
        public int X { get; }

        /// <summary>单元格在迷宫中的行坐标（y）</summary>
        public int Y { get; }

        /// <summary>该单元格是否可用（白色区域）。用于 Mask 支持，false 表示该位置无通道</summary>
        public bool White { get; set; }

        public SquareCell(int x, int y, bool white = true)
        {
            X = x;
            Y = y;
            White = white;
            Lower = new SquareNode(this);
            Upper = new SquareNode(this);
        }

        /// <summary>备份 lower 和 upper 节点的连接状态</summary>
        public void Backup()
        {
            Lower.Backup();
            Upper.Backup();
        }

        /// <summary>恢复 lower 和 upper 节点的连接状态</summary>
        public void Restore()
        {
            Lower.Restore();
            Upper.Restore();
        }

        /// <summary>该单元格是否为平坦单元格（upper 节点无任何连接）</summary>
        public bool IsFlat() => !IsNotFlat();

        /// <summary>该单元格是否为非平坦单元格（upper 节点存在连接，即存在跨越结构）</summary>
        public bool IsNotFlat() =>
            Upper.North != null || Upper.East != null ||
            Upper.South != null || Upper.West != null;
    }
}
