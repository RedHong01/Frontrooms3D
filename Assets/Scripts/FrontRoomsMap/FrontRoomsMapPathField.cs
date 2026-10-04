using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// A bounded, player-rooted map distance field for Relay Pursuit v2 §7.
/// The field reads only already-built cells, so asking for a distance never
/// causes streaming to generate a chunk. Distances are metres; opening costs
/// are kept separately for the designer-facing warning measurements.
/// </summary>
public sealed class FrontRoomsMapPathField
{
    public const float Orthogonal = 3f;
    public const float Diagonal = 4.2426407f;
    public const float AjarOpening = 4f;
    public const float ShutOpening = 9f;

    static readonly int[] SideX = { 1, -1, 0, 0 };
    static readonly int[] SideY = { 0, 0, 1, -1 };

    public readonly float Cap;
    readonly int radius, side;
    readonly float[] distance, openingCost, heapKey;
    readonly int[] reached, settled, heapNode;
    int heapCount, stamp;

    public GridCoord Root { get; private set; }
    public int Visited { get; private set; }

    public FrontRoomsMapPathField(float capMetres)
    {
        Cap = Mathf.Max(0f, capMetres);
        radius = Mathf.CeilToInt(Cap / Orthogonal) + 1;
        side = radius * 2 + 1;
        var cells = side * side;
        distance = new float[cells];
        openingCost = new float[cells];
        reached = new int[cells];
        settled = new int[cells];
        heapNode = new int[cells * 8 + 1];
        heapKey = new float[heapNode.Length];
    }

    /// <summary>Distance from the last root, or infinity when it was not reached within the cap.</summary>
    public float DistanceTo(GridCoord cell)
    {
        var i = Index(cell);
        return i >= 0 && settled[i] == stamp ? distance[i] : float.PositiveInfinity;
    }

    /// <summary>Opening cost along the shortest-distance route to a cell, or infinity when unreachable.</summary>
    public float OpeningCostTo(GridCoord cell)
    {
        var i = Index(cell);
        return i >= 0 && settled[i] == stamp ? openingCost[i] : float.PositiveInfinity;
    }

    public void Build(FrontRoomsMapWorld map, GridCoord root, GridCoord? secondRoot = null)
    {
        stamp++;
        if (stamp == int.MaxValue)
        {
            System.Array.Clear(reached, 0, reached.Length);
            System.Array.Clear(settled, 0, settled.Length);
            stamp = 1;
        }
        Root = root;
        Visited = 0;
        heapCount = 0;
        if (map == null || !map.IsBuilt(root)) return;

        Reach(Index(root), 0f, 0f);
        if (secondRoot.HasValue && secondRoot.Value != root && map.IsBuilt(secondRoot.Value))
            Reach(Index(secondRoot.Value), 0f, 0f);
        var cache = map.Cache;
        while (heapCount > 0)
        {
            var node = Pop(out var d);
            if (settled[node] == stamp || d > distance[node]) continue;
            settled[node] = stamp;
            Visited++;

            var cell = new GridCoord(node % side - radius + Root.x, node / side - radius + Root.y);
            var plain = 0;
            for (var k = 0; k < 4; k++)
            {
                var next = new GridCoord(cell.x + SideX[k], cell.y + SideY[k]);
                if (!map.IsBuilt(next)) continue;
                var edge = cache.Edge(cell, next);
                if (edge == EdgeKind.Wall) continue;
                if (edge == EdgeKind.Open) plain |= 1 << k;

                var cost = map.OpeningCostBetween(cell, next);
                if (float.IsPositiveInfinity(cost)) continue;
                var i = Index(next);
                if (i >= 0 && settled[i] != stamp)
                    Reach(i, d + Orthogonal + cost, openingCost[node] + cost);
            }

            // A diagonal is allowed only when both orthogonal sides are plain
            // open room edges. This prevents cutting through a door, window,
            // arch or wall corner.
            for (var k = 0; k < 4; k++)
            {
                var dx = k < 2 ? 1 : -1;
                var dy = (k & 1) == 0 ? 1 : -1;
                var xSide = dx > 0 ? 0 : 1;
                var ySide = dy > 0 ? 2 : 3;
                if ((plain & (1 << xSide)) == 0 || (plain & (1 << ySide)) == 0) continue;

                var corner = new GridCoord(cell.x + dx, cell.y + dy);
                var i = Index(corner);
                if (i < 0 || settled[i] == stamp || !map.IsBuilt(corner)) continue;
                if (cache.Edge(new GridCoord(cell.x + dx, cell.y), corner) != EdgeKind.Open
                    || cache.Edge(new GridCoord(cell.x, cell.y + dy), corner) != EdgeKind.Open) continue;
                Reach(i, d + Diagonal, openingCost[node]);
            }
        }
    }

    int Index(GridCoord cell)
    {
        var x = cell.x - Root.x + radius;
        var y = cell.y - Root.y + radius;
        return x < 0 || y < 0 || x >= side || y >= side ? -1 : x + y * side;
    }

    void Reach(int i, float d, float openings)
    {
        if (i < 0 || d > Cap) return;
        if (reached[i] == stamp && (d > distance[i] + 1e-4f
            || (Mathf.Abs(d - distance[i]) <= 1e-4f && openings >= openingCost[i] - 1e-4f))) return;
        reached[i] = stamp;
        distance[i] = d;
        openingCost[i] = openings;

        var at = heapCount++;
        while (at > 0)
        {
            var parent = (at - 1) / 2;
            if (heapKey[parent] <= d) break;
            heapNode[at] = heapNode[parent];
            heapKey[at] = heapKey[parent];
            at = parent;
        }
        heapNode[at] = i;
        heapKey[at] = d;
    }

    int Pop(out float d)
    {
        var top = heapNode[0];
        d = heapKey[0];
        var lastNode = heapNode[--heapCount];
        var lastKey = heapKey[heapCount];
        var at = 0;
        while (true)
        {
            var child = at * 2 + 1;
            if (child >= heapCount) break;
            if (child + 1 < heapCount && heapKey[child + 1] < heapKey[child]) child++;
            if (heapKey[child] >= lastKey) break;
            heapNode[at] = heapNode[child];
            heapKey[at] = heapKey[child];
            at = child;
        }
        heapNode[at] = lastNode;
        heapKey[at] = lastKey;
        return top;
    }
}
