using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 迷宫解法数据。存储解路径信息，同时将解路径写入节点的 xxx2 字段供渲染器使用。
    /// </summary>
    public struct WeaveMazeSolution
    {
        /// <summary>解路径上的节点列表（从起点到终点）</summary>
        internal List<SquareNode> Path { get; set; }

        /// <summary>解路径长度（节点数）</summary>
        public int Length => Path?.Count ?? 0;

        /// <summary>解路径是否有效</summary>
        internal bool IsValid => Path != null && Path.Count > 0;
    }
}
