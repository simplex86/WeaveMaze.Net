using System;
using System.Collections.Generic;

namespace SimplexLab.WeaveMaze
{
    internal static class ExList
    {
        /// <summary>
        /// Fisher-Yates 洗牌
        /// </summary>
        public static void Shuffle<T>(this List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
