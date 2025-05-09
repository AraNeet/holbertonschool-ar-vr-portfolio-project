using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public Vector3Int position;
    public GameObject roomObject;
    public List<Room> connectedRooms = new List<Room>();
} 