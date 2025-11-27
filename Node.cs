using UnityEngine;

[System.Serializable]
public class Node
{
    public Vector3 position;
    public bool isWalkable = true;
    [System.NonSerialized]
    public Node[] neighbors;

    public Node(Vector3 pos)
    {
        position = pos;
    }
}