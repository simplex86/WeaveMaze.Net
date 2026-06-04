namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫节点。每个单元格包含 lower（下层）和 upper（上层）两个节点，
    /// 上层节点用于表示编织式迷宫中通道跨越时的架空通道。
    /// </summary>
    public class Node
    {
        /// <summary>北向连接</summary>
        public Node? North { get; set; }

        /// <summary>东向连接</summary>
        public Node? East { get; set; }

        /// <summary>南向连接</summary>
        public Node? South { get; set; }

        /// <summary>西向连接</summary>
        public Node? West { get; set; }

        /// <summary>北向备份/解路径连接</summary>
        public Node? North2 { get; set; }

        /// <summary>东向备份/解路径连接</summary>
        public Node? East2 { get; set; }

        /// <summary>南向备份/解路径连接</summary>
        public Node? South2 { get; set; }

        /// <summary>西向备份/解路径连接</summary>
        public Node? West2 { get; set; }

        /// <summary>搜索时记录父节点</summary>
        public Node? VisitedBy { get; set; }

        /// <summary>区域标识（区域标记阶段使用），也复用于求解阶段的距离记录</summary>
        public int Region { get; set; } = -1;

        /// <summary>所属单元格</summary>
        public Cell Cell { get; }

        public Node(Cell cell)
        {
            Cell = cell;
        }

        /// <summary>备份当前四方向连接到 xxx2 字段</summary>
        public void Backup()
        {
            North2 = North;
            East2 = East;
            South2 = South;
            West2 = West;
        }

        /// <summary>从 xxx2 字段恢复四方向连接</summary>
        public void Restore()
        {
            North = North2;
            East = East2;
            South = South2;
            West = West2;
        }
    }
}
