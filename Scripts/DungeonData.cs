using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonData
{
    public enum GridType
    {
        None,
        Floor,
        Wall,
        Celling,
        Route,
        RoomPadding,
    }

    public enum RoomType
	{
        None,
        Normal,
        StartRoom,
        StaierRoom,
        Shop,
	}

    public enum Direction
    {
        Left,
        Bottom,
        Right,
        Top,
    }

    public enum ErrorCord
    {
        NotFound = -1,
        OutOfRange = -2,
    }
}