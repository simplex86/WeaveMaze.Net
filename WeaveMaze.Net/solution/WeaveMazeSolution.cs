using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫解法数据。存储解路径信息。
    /// </summary>
    public struct WeaveMazeSolution
    {
        /// <summary>解路径上的顶点索引列表（从起点到终点）</summary>
        internal List<int> Path { get; set; }

        /// <summary>解路径长度（顶点数）</summary>
        public int Length => Path?.Count ?? 0;

        /// <summary>解路径是否有效</summary>
        internal bool IsValid => Path != null && Path.Count > 0;
    }
}
