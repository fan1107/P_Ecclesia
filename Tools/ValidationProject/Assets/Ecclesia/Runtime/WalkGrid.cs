using System.Collections.Generic;
using UnityEngine;

namespace Ecclesia
{
    // A small deterministic four-way grid keeps visitors out of walls and pew backs.
    public sealed class WalkGrid
    {
        public const int Width = 32, Height = 18;
        readonly bool[,] walkable = new bool[Width, Height];
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public bool IsOpen(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height && walkable[p.x, p.y];
        public void Set(int x, int y, bool value) => walkable[x, y] = value;
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!IsOpen(start) || !IsOpen(end)) return null;
            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (p == end)
                {
                    var path = new List<Vector2Int>();
                    while (p != start) { path.Add(p); p = previous[p]; }
                    path.Reverse(); return path;
                }
                foreach (var d in Directions)
                {
                    var next = p + d;
                    if (!IsOpen(next) || previous.ContainsKey(next)) continue;
                    previous[next] = p; queue.Enqueue(next);
                }
            }
            return null;
        }
    }
}
