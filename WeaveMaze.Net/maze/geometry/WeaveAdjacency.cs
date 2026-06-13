namespace SimplexLab.WeaveMaze
{
    /// <summary>
    /// 邻接表项，表示图中一条从源顶点到目标顶点的有向边。
    /// 每个顶点的邻接表存储该顶点的所有出边。
    /// </summary>
    public struct WeaveAdjacency
    {
        /// <summary>目标顶点索引</summary>
        public int Neighbor;

        /// <summary>
        /// 边的方向：0=北, 1=东, 2=南, 3=西
        /// </summary>
        public int Direction;

        public WeaveAdjacency(int neighbor, int direction)
        {
            Neighbor = neighbor;
            Direction = direction;
        }
    }
}
