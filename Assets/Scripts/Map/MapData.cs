using System.Collections.Generic;
using UnityEngine;

// new values go only to the end, the map may be saved to disk later
public enum MapNodeType { Fight, Elite, Rest, Treasure, Boss }

// one room of the map. Links are stored as indices, not references,
// so the whole map can be turned into JSON with JsonUtility
[System.Serializable]
public class MapNode
{
    public int Floor;
    public int Column;
    public MapNodeType Type;
    // indices of the nodes on the next floor this node leads to
    public List<int> Next = new List<int>();
    // small random shift so the map does not look like a perfect grid
    public Vector2 Offset;
}

// generated map of one run plus the player's position on it, pure data without MonoBehaviour
[System.Serializable]
public class MapData
{
    public int Seed;
    public int FloorCount;
    public int ColumnCount;
    public List<MapNode> Nodes = new List<MapNode>();
    public int BossIndex = -1;

    // -1 means the player has not entered the first floor yet
    public int CurrentIndex = -1;
    public List<int> VisitedPath = new List<int>();

    public MapNode CurrentNode => CurrentIndex >= 0 ? Nodes[CurrentIndex] : null;
    public bool IsBossReached => CurrentIndex >= 0 && CurrentIndex == BossIndex;

    // nodes the player can move to right now
    public List<int> GetAvailableNodes()
    {
        List<int> result = new List<int>();

        if (CurrentIndex < 0)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Floor == 0) result.Add(i);
            }
            return result;
        }

        result.AddRange(Nodes[CurrentIndex].Next);
        return result;
    }

    public bool IsAvailable(int index)
    {
        return GetAvailableNodes().Contains(index);
    }

    public bool IsVisited(int index)
    {
        return VisitedPath.Contains(index);
    }

    public void MoveTo(int index)
    {
        CurrentIndex = index;
        VisitedPath.Add(index);
    }
}
