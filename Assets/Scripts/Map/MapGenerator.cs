using System.Collections.Generic;
using UnityEngine;

// builds a Slay the Spire style map: random walks from the bottom to the top of a grid,
// then a room type for every node that the walks touched.
// the same seed always gives the same map
public static class MapGenerator
{
    private static readonly MapNodeType[] RandomTypes =
    {
        MapNodeType.Fight, MapNodeType.Elite, MapNodeType.Rest, MapNodeType.Treasure
    };

    public static MapData Generate(MapConfig config, int seed)
    {
        System.Random rng = new System.Random(seed);
        int floors = config.FloorCount;
        int columns = config.ColumnCount;

        // links[floor, column] = columns on the next floor this cell leads to
        List<int>[,] links = new List<int>[floors, columns];
        bool[,] used = new bool[floors, columns];
        for (int floor = 0; floor < floors; floor++)
        {
            for (int column = 0; column < columns; column++)
            {
                links[floor, column] = new List<int>();
            }
        }

        BuildPaths(config.PathCount, floors, columns, rng, links, used);

        MapData map = new MapData { Seed = seed, FloorCount = floors, ColumnCount = columns };
        CreateNodes(map, links, used, rng);
        AssignTypes(map, config, rng);
        return map;
    }

    // ---------- step 1: paths ----------

    static void BuildPaths(int pathCount, int floors, int columns, System.Random rng, List<int>[,] links, bool[,] used)
    {
        int firstStart = -1;

        for (int path = 0; path < pathCount; path++)
        {
            int column = rng.Next(columns);

            // the first two paths start in different columns, so the very first choice is real
            if (path == 1)
            {
                while (column == firstStart) column = rng.Next(columns);
            }
            if (path == 0) firstStart = column;

            used[0, column] = true;

            for (int floor = 0; floor < floors - 1; floor++)
            {
                int next = PickNextColumn(floor, column, columns, rng, links);
                if (!links[floor, column].Contains(next)) links[floor, column].Add(next);

                used[floor + 1, next] = true;
                column = next;
            }
        }
    }

    // up-left, up or up-right, but never across an existing diagonal link
    static int PickNextColumn(int floor, int column, int columns, System.Random rng, List<int>[,] links)
    {
        List<int> options = new List<int>();
        for (int next = column - 1; next <= column + 1; next++)
        {
            if (next >= 0 && next < columns) options.Add(next);
        }
        Shuffle(options, rng);

        foreach (int next in options)
        {
            // a move to the side crosses a link going from the neighbour back over to our column.
            // going straight up never crosses, so some option is always found
            if (next != column && links[floor, next].Contains(column)) continue;
            return next;
        }
        return column;
    }

    // ---------- step 2: nodes and links ----------

    static void CreateNodes(MapData map, List<int>[,] links, bool[,] used, System.Random rng)
    {
        int floors = map.FloorCount;
        int columns = map.ColumnCount;
        int[,] index = new int[floors, columns];

        // nodes go floor by floor, left to right, AssignTypes relies on this order
        for (int floor = 0; floor < floors; floor++)
        {
            for (int column = 0; column < columns; column++)
            {
                index[floor, column] = -1;
                if (!used[floor, column]) continue;

                MapNode node = new MapNode { Floor = floor, Column = column };
                node.Offset = new Vector2(RandomSigned(rng), RandomSigned(rng));

                index[floor, column] = map.Nodes.Count;
                map.Nodes.Add(node);
            }
        }

        for (int floor = 0; floor < floors - 1; floor++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (!used[floor, column]) continue;

                MapNode node = map.Nodes[index[floor, column]];
                foreach (int next in links[floor, column])
                {
                    node.Next.Add(index[floor + 1, next]);
                }
            }
        }

        // the boss is one more floor on top, every node of the last floor leads to it
        MapNode boss = new MapNode { Floor = floors, Column = columns / 2, Type = MapNodeType.Boss };
        map.BossIndex = map.Nodes.Count;
        map.Nodes.Add(boss);

        for (int column = 0; column < columns; column++)
        {
            if (used[floors - 1, column]) map.Nodes[index[floors - 1, column]].Next.Add(map.BossIndex);
        }
    }

    // ---------- step 3: room types ----------

    static void AssignTypes(MapData map, MapConfig config, System.Random rng)
    {
        List<int>[] parents = new List<int>[map.Nodes.Count];
        for (int i = 0; i < parents.Length; i++)
        {
            parents[i] = new List<int>();
        }
        for (int i = 0; i < map.Nodes.Count; i++)
        {
            foreach (int next in map.Nodes[i].Next) parents[next].Add(i);
        }

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            if (i == map.BossIndex) continue;
            map.Nodes[i].Type = ChooseType(map, i, parents, config, rng);
        }
    }

    static MapNodeType ChooseType(MapData map, int nodeIndex, List<int>[] parents, MapConfig config, System.Random rng)
    {
        int floor = map.Nodes[nodeIndex].Floor;

        if (floor == 0) return MapNodeType.Fight;
        if (floor == map.FloorCount - 1) return MapNodeType.Rest;
        if (floor == config.TreasureFloor) return MapNodeType.Treasure;

        // weighted random among the types the rules allow, a fight is always allowed
        List<MapNodeType> allowed = new List<MapNodeType>();
        int totalWeight = 0;
        foreach (MapNodeType type in RandomTypes)
        {
            int weight = config.GetWeight(type);
            if (weight <= 0 || !IsAllowed(map, nodeIndex, type, parents, config)) continue;

            allowed.Add(type);
            totalWeight += weight;
        }

        if (totalWeight == 0) return MapNodeType.Fight;

        int roll = rng.Next(totalWeight);
        foreach (MapNodeType type in allowed)
        {
            roll -= config.GetWeight(type);
            if (roll < 0) return type;
        }
        return MapNodeType.Fight;
    }

    static bool IsAllowed(MapData map, int nodeIndex, MapNodeType type, List<int>[] parents, MapConfig config)
    {
        if (type == MapNodeType.Fight) return true;

        int floor = map.Nodes[nodeIndex].Floor;

        // the first floors teach the basics, no elites and no need to rest yet
        if ((type == MapNodeType.Elite || type == MapNodeType.Rest) && floor < config.MinEliteRestFloor) return false;

        // a rest right before the fixed rest floor would be wasted
        if (type == MapNodeType.Rest && floor == map.FloorCount - 2) return false;

        foreach (int parent in parents[nodeIndex])
        {
            // no two special rooms of the same type in a row
            if (map.Nodes[parent].Type == type) return false;

            // two exits of one node should offer different rooms, otherwise the choice means nothing.
            // only siblings with a smaller index already have their type
            foreach (int sibling in map.Nodes[parent].Next)
            {
                if (sibling < nodeIndex && map.Nodes[sibling].Type == type) return false;
            }
        }
        return true;
    }

    // ---------- helpers ----------

    static void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static float RandomSigned(System.Random rng)
    {
        return (float)(rng.NextDouble() * 2.0 - 1.0);
    }
}
