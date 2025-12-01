using UnityEngine;
using System.Collections.Generic;

public enum NodeType
{
    Walkable,
    Door,
    Stair,
    Corner,
    Junction
}

[System.Serializable]
public class Node
{
    public Vector3 position;
    public bool isWalkable = true;
    public NodeType type = NodeType.Walkable;
    
    [System.NonSerialized]
    public List<Node> neighbors = new List<Node>();

    public Node(Vector3 pos, NodeType nodeType = NodeType.Walkable)
    {
        position = pos;
        type = nodeType;
    }
}