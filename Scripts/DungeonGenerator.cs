using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DungeonData;

public class DungeonGenerator : MonoBehaviour
{
    /// <summary>
    /// 4�_�Ԃ̋�`�������N���X
    /// </summary>
    public class Rect
    {
        public int Left { get; protected set; }   //�����̓_
        public int Right { get; protected set; }  //�E���̓_
        public int Bottom { get; protected set; } //�����̓_
        public int Top { get; protected set; }    //�㑤�̓_

        public int Width { get { return Right - Left + 1; } }  //���̒���
        public int Height { get { return Top - Bottom + 1; } } //�c�̒���

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public Rect(int left, int bottom, int right, int top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        /// <summary>
        /// �}�b�v�f�[�^�ɑ΂��Ă��̋�`���͈̔͂̃O���b�h�̎�ނ�ς���
        /// </summary>
        /// <param name="mapData">�}�b�v�f�[�^</param>
        /// <param name="tile">�ύX����O���b�h</param>
        public void FillTile(GridType[,] mapData, GridType tile)
        {
            for (int y = Bottom; y <= Top; y++)
            {
                for (int x = Left; x <= Right; x++)
                {
                    mapData[x, y] = tile;
                }
            }
        }
    }

    /// <summary>
    /// �G���A�̋�`�͈̔�
    /// </summary>
    public class Area : Rect
    {
        public SplitData splitData { get; private set; } //�G���A�𕪂��鎲�̃f�[�^

        public Area parentArea { get; private set; } //���̃G���A�����e�̃G���A
        public Room room { get; private set; } //���̃G���A���������̋�`�f�[�^

        public List<Vector2Int> entranceList = new List<Vector2Int>(); //������̍��W

        public int areaTreeIndex; //��ԍŏ�����݂Ă��̃G���A�̕���

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public Area(Area parentArea, int left, int bottom, int right, int top, int areaTreeIndex) : base(left, bottom, right, top)
        {
            this.parentArea = parentArea;
            this.areaTreeIndex = areaTreeIndex;

            splitData = null;
            room = null;
            areaTreeIndex = -1;
        }

        /// <summary>
        /// �������̃f�[�^��ݒ肷��
        /// </summary>
        /// <param name="splitData">�������̃f�[�^</param>
        public void SetSplitData(SplitData splitData)
        {
            this.splitData = splitData;
        }

        /// <summary>
        /// �����̃f�[�^��ݒ肷��
        /// </summary>
        /// <param name="room">�����̃f�[�^</param>
        public void SetRoom(Room room)
        {
            this.room = room;
        }

        /// <summary>
        /// �����̃f�[�^���폜����
        /// </summary>
        public void RemoveRoom()
        {
            room = null;
        }

        /// <summary>
        /// �����̓�����̍��W��ǉ�����
        /// </summary>
        /// <param name="pos">���W</param>
        public void AddEntrance(Vector2Int pos)
        {
            entranceList.Add(pos);
        }
    }

    /// <summary>
    /// �����̋�`�͈̔̓f�[�^
    /// </summary>
    public class Room : Rect
    {
        public RoomType roomType; //�����̎��

        public Room(int left, int bottom, int right, int top) : base(left, bottom, right, top)
        {
        }
    }

    /// <summary>
    /// �G���A�𕪂��鎲�̃f�[�^
    /// </summary>
    public class SplitData
    {
        /// <summary>
        /// ���̕���
        /// </summary>
        public enum SplitAxis
        {
            X,
            Y,
        }

        public int splitPos; //���̈ʒu
        public SplitAxis splitAxis; //���̕���
    }

    [SerializeField] Vector2Int areaMinSize; //�G���A�̍ŏ��T�C�Y
    [SerializeField] Vector2Int roomMinSize; //�����̍ŏ��T�C�Y
    [SerializeField] Vector2Int roomMaxSize; //�����̍ő�T�C�Y
    [SerializeField] int mapPadding;    //�}�b�v�̊O�ǂ̃T�C�Y
    [SerializeField] int roomPadding;   //�����̊O�ǂ̃T�C�Y
    [SerializeField] int wallHeight;    //�ǂ̍���
    [SerializeField] int wallPadding;   //�ǂ̊Ԋu
    [SerializeField] int maxAStarRoute; //�ǉ��̓��̐�
    [SerializeField] int splitCountMin; //�����̕����̍ŏ���
    [SerializeField] int splitCountMax; //�����̕����̍ő��
    [SerializeField] float splitEndParcent;    //�����̕������I������m��
    [SerializeField] float removeRoomMinRatio; //���������Ȃ����̍ŏ�����
    [SerializeField] float removeRoomMaxRatio; //���������Ȃ����̍ő劄��

    List<Area> allAreas = new List<Area>(); //���ׂẴG���A�̃��X�g
    List<Area> mapAreas = new List<Area>(); //�������������ꂽ�G���A�̃��X�g

    Vector2Int mapSize;  //�}�b�v�̃T�C�Y
    GridType[,] mapData; //�}�b�v�̃}�X�̏��

    public Vector2Int GetMapSize() { return mapSize; } //�}�b�v�̑傫���̎擾
    public GridType[,] GetMapData() { return mapData; } //�}�b�v�f�[�^�̎擾
    public List<Area> GetMapAreas() { return mapAreas; } //�}�b�v�̃G���A�̎擾

    /// <summary>
    /// �_���W�����̐���
    /// </summary>
    /// <param name="mapSize">�_���W�����̃T�C�Y</param>
    /// <returns>������̃f�[�^</returns>
    public GridType[,] Generate(Vector2Int mapSize)
    {
        //�}�b�v�̑傫����ݒ�
        this.mapSize = mapSize;
        mapData = new GridType[mapSize.x, mapSize.y];

        //�G���A�����X�g��������
        allAreas.Clear();
        mapAreas.Clear();

        //�}�b�v�S�̂̃G���A
        Area rootArea = new Area(null, 0, 0, mapSize.x - 1, mapSize.y - 1, 0);

        //�G���A�̕���
        SplitArea(rootArea);

        //�}�b�v�z��̏�����
        rootArea.FillTile(mapData, GridType.Celling);

        //�G���A���Ƃɕ����̐���
        CreateRoom();

        //���̐���
        CreateRoute();

        //�ǉ��̓�����
        for(int i=0;i<maxAStarRoute;i++)
        CreateAStarRoute();

        //�ǂ̐���
        CreateWalls();

        //�����̎�ނ̐ݒ�
        SetRoomType();

        return mapData;
    }

    /// <summary>
    /// �G���A�̕���
    /// </summary>
    /// <param name="area">��������G���A</param>
    /// <param name="splitCount">���݂̕�����</param>
    void SplitArea(Area area, int splitCount = 0)
    {
        //�������I�����邩�ǂ���
        bool splitEnd = (splitCount >= splitCountMin && splitEndParcent > Random.Range(0.0f, 100.0f)) || splitCount >= splitCountMax;

        //����ȏ㕪���ł��Ȃ��ꍇ�A�܂��͕������I������ꍇ�͕������I������
        if ((area.Width < areaMinSize.x * 2 + 1 && area.Height < areaMinSize.y * 2 + 1) || splitEnd)
        {
            mapAreas.Add(area);
            return;
        }

        //�������@�̃f�[�^
        SplitData splitData = new SplitData();

        //����������������߂�
        splitData.splitAxis = (area.Width, area.Height) switch
        {
            (var _w, var _h) when _w == _h => (SplitData.SplitAxis)Random.Range(0, 1 + 1),  //�G���A�̗]�肪�����Ƃ������Ȃ�΃����_���Ɍ��߂�
            (var _w, var _h) when _w > _h && _w >= areaMinSize.x => SplitData.SplitAxis.X, //�G���A�̗]�肪X�����������Ȃ��X���ɕ�������悤�ɂ���
            (var _w, var _h) when _w < _h && _h >= areaMinSize.y => SplitData.SplitAxis.Y, //�G���A�̗]�肪Y�����������Ȃ��Y���ɕ�������悤�ɂ���
            _ => throw new System.NotImplementedException(),                                //��L�̂ǂ̏ꍇ�ɂ����Ă͂܂�Ȃ��Ƃ���O�������s��
        };

        //���������_��ݒ肷��
        splitData.splitPos = (splitData.splitAxis) switch
        {
            SplitData.SplitAxis.X => Random.Range(area.Left + areaMinSize.x, area.Right - areaMinSize.x), //�������鎲��X�����̏ꍇ
            SplitData.SplitAxis.Y => Random.Range(area.Bottom + areaMinSize.y, area.Top - areaMinSize.y),   //�������鎲��Y�����̏ꍇ
            _ => throw new System.NotImplementedException(),                                                //�������鎲�����݂��Ȃ��ꍇ�͗�O�������s��
        };

        //�G���A�𕪊����s��
        Area[] splitAreas = (splitData.splitAxis) switch
        {
            //�������鎲��X�����̏ꍇ
            SplitData.SplitAxis.X => new Area[2]
            {
                new Area(area, area.Left, area.Bottom, splitData.splitPos - 1, area.Top, splitCount), //�����̕����̕���
		        new Area(area, splitData.splitPos + 1, area.Bottom, area.Right, area.Top, splitCount) //�E���̕����̕���
			},

            //�������鎲��Y�����̏ꍇ
            SplitData.SplitAxis.Y => new Area[2]
            {
                new Area(area, area.Left, area.Bottom, area.Right, splitData.splitPos - 1, splitCount), //�����̕����̕���
				new Area(area, area.Left, splitData.splitPos + 1, area.Right, area.Top, splitCount)     //�㑤�̕����̕���
			},

            //�������鎲�����݂��Ȃ��ꍇ�͗�O�������s��
            _ => throw new System.NotImplementedException(),
        };

        //�G���A�ɂ��ꂼ��f�[�^�����蓖�Ă�
        area.SetSplitData(splitData);

        //���ꂼ��̃G���A������ɕ�������
        splitCount++;
        SplitArea(splitAreas[0], splitCount);
        SplitArea(splitAreas[1], splitCount);

        allAreas.Add(area);
    }

    /// <summary>
    /// �����̐���
    /// </summary>
    void CreateRoom()
    {
        Debug.Log("�G���A�̑��� => " + mapAreas.Count);

        //�������Ȃ������̐�
        int removeRoomAmount = Mathf.FloorToInt(Random.Range(removeRoomMinRatio, removeRoomMaxRatio) * mapAreas.Count * 0.01f);
        List<Area> removeAreas = RandomSelectOrderArea(mapAreas).GetRange(0, removeRoomAmount);

        foreach (Area area in mapAreas)
        {
            //�����𐶐����ɂȂ��ꍇ�͎��s���Ȃ�
            if (removeAreas.Contains(area)) continue;

            //�G���A�ɍ��镔���̍ő�̑傫��
            int areaOfRoomWidth  = area.Width - roomPadding * 2;
            int areaOfRoomHeight = area.Height - roomPadding * 2 - wallHeight * 2;

            //��镔���̑傫��
            int roomWidth  = Random.Range(Mathf.Min(roomMinSize.x, areaOfRoomWidth) , areaOfRoomWidth  + 1);
            int roomHeight = Random.Range(Mathf.Min(roomMinSize.y, areaOfRoomHeight), areaOfRoomHeight + 1);

            //�����̍ő�T�C�Y���ݒ肢��ꍇ�͐�������
            if (roomMaxSize.x > 0) roomWidth  = Mathf.Min(roomWidth, roomMaxSize.x);
            if (roomMaxSize.y > 0) roomHeight = Mathf.Min(roomHeight, roomMaxSize.y);

            //�����̌��Ԃ̑傫��
            int spaceWidth  = areaOfRoomWidth  - roomWidth;
            int spaceHeight = areaOfRoomHeight - roomHeight;

            //��_����̗���Ă��鋗��
            int marginWidth  = Random.Range(0, spaceWidth + 1);
            int marginHeight = Random.Range(0, spaceHeight + 1);

            //�����̂��ꂼ��̍��W
            int left   = area.Left   + marginWidth  + roomPadding;
            int bottom = area.Bottom + marginHeight + roomPadding + wallHeight;
            int right  = left        + roomWidth    - 1;
            int top    = bottom      + roomHeight   - 1;

            //�G���A�ɕ��������
            area.SetRoom(new Room(left, bottom, right, top));

            //�����Ƀ^�C����~��
            area.FillTile(mapData, GridType.RoomPadding);
            area.room.FillTile(mapData, GridType.Floor);
        }
    }

    /// <summary>
    /// �����ƕ������q���������
    /// </summary>
    void CreateRoute()
    {
        //���ݑ��݂��Ă��镔�����q����
        foreach (Area parentArea in allAreas)
        {
            SplitData splitData = parentArea.splitData;

            //�G���A�����ԋ߂��G���A��T��
            Area[] areas = SearchNearArea(parentArea);
            if (areas == null) continue;

            //���̃G���A�̕������ꂽ�G���A���擾
            Area area1 = areas[0];
            Area area2 = areas[1];

            //���̃^�C��
            GridType TempGridType = GridType.Route;

            //�������鎲�ɉ�����
            switch (splitData.splitAxis)
            {
                case SplitData.SplitAxis.X:

                    //��������ʒu�����߂�
                    int leftX  = area1.room.Right + 1;
                    int rightX = area2.room.Left  - 1;

                    int leftDoorPos  = GetAvailableEntrancePosY(area1, leftX).y;
                    int rightDoorPos = GetAvailableEntrancePosY(area2, rightX).y;

                    //���̐���
                    for (int x = area1.room.Right + 1; x <= splitData.splitPos; x++)
                    {
                        mapData[x, leftDoorPos] = TempGridType;
                    }
                    for (int x = splitData.splitPos; x <= area2.room.Left - 1; x++)
                    {
                        mapData[x, rightDoorPos] = TempGridType;
                    }
                    for (int y = Mathf.Min(leftDoorPos, rightDoorPos); y <= Mathf.Max(leftDoorPos, rightDoorPos); y++)
                    {
                        mapData[splitData.splitPos, y] = TempGridType;
                    }

                    break;
                case SplitData.SplitAxis.Y:

                    //��������ʒu�����߂�
                    int bottomY = area1.room.Top + 1;
                    int topY    = area2.room.Bottom - 1;

                    int bottomDoorPos = GetAvailableEntrancePosX(area1, bottomY).x;
                    int topDoorPos    = GetAvailableEntrancePosX(area2, topY).x;

                    //���̐���
                    for (int y = area1.room.Top + 1; y <= splitData.splitPos; y++)
                    {
                        mapData[bottomDoorPos, y] = TempGridType;
                    }
                    for (int y = splitData.splitPos; y <= area2.room.Bottom - 1; y++)
                    {
                        mapData[topDoorPos, y] = TempGridType;
                    }
                    for (int x = Mathf.Min(bottomDoorPos, topDoorPos); x <= Mathf.Max(bottomDoorPos, topDoorPos); x++)
                    {
                        mapData[x, splitData.splitPos] = TempGridType;
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// 2�_�Ԃ��q�����̃p�X���擾
    /// </summary>
    /// <param name="startPos">�J�n�ʒu</param>
    /// <param name="endPos">�I���ʒu</param>
    /// <returns>���̃p�X</returns>
    public List<AStar.AStarNode> AStar(Vector2Int startPos, Vector2Int endPos)
    {
        AStar aStar = new AStar();
        return aStar.GetRoutePath(mapData, startPos, endPos, new GridType[] { GridType.Celling, GridType.Route});
    }

    /// <summary>
    /// AStar�A���S���Y���ɂ��V�K�ʘH�̐���
    /// </summary>
    /// <returns></returns>
    public bool CreateAStarRoute()
    {
        Vector2Int startPos;
        Vector2Int endPos;
        List<Area> hasRoomAreas = RandomSelectOrderAreaIsHasRoom(mapAreas);

        //�J�n�ʒu�ƏI���ʒu���擾
        startPos = AreaSplitRandomPos(hasRoomAreas[0]);
        endPos   = AreaSplitRandomPos(hasRoomAreas[1]);

        //�擾�ł��Ȃ���Ύ��s���Ȃ�
        if (startPos == new Vector2Int(-1, -1)) return false;
        if (endPos   == new Vector2Int(-1, -1)) return false;

        //�ʘH�̃p�X���擾
        List<AStar.AStarNode> path = AStar(startPos, endPos);

        Debug.Log(path.Count);

        //�p�X���擾�ł��Ȃ������ꍇ��
        if (path == null || path.Count == 0)
        {
            return false;
        }

        //�p�X��H��Ȃ��瓹�ɒu��������
        foreach (AStar.AStarNode node in path)
        {
            Vector2Int pos = node.pos;
            mapData[pos.x, pos.y] = GridType.Route;
        }

        //�����Ɠ����q����
        AreaSplitRoute(hasRoomAreas[0], startPos);
        AreaSplitRoute(hasRoomAreas[1], endPos);

        return true;
    }

    /// <summary>
    /// �ǂ̐���
    /// </summary>
    void CreateWalls()
    {
        for(int y = 0; y < mapSize.y; y++) 
        {
            for(int x = 0; x < mapSize.x; x++)
            {
                //���W�������n�ʂłȂ���Ύ���
                if (mapData[x, y]  != GridType.Floor && mapData[x, y] != GridType.Route) continue;

                //�ǂ̐���
                for(int h = 1; h <= wallHeight; h++)
                {
                    int dy = y - h;
                    if (dy < 0) break;
                    if (mapData[x, dy] != GridType.Celling && mapData[x, dy] != GridType.RoomPadding) continue;

                    mapData[x, dy] = GridType.Wall;
                }
            }
        }
    }

    /// <summary>
    /// �����̎�ނ����蓖�Ă�
    /// </summary>
    void SetRoomType()
    {
        //�����̃��X�g�������_���ɕ��ёւ��Ď擾
        List<Room> rooms = mapAreas.Select(n => n.room).Where(n => n != null).OrderBy(n => System.Guid.NewGuid()).ToList();

        //�����̎�ނ��擾
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomType roomType = i switch
            {
                (int n) when (n == 0) => RoomType.StartRoom,  //�ŏ��̈�̓v���C���[�̗N���ꏊ�ɂ���
                (int n) when (n == 1) => RoomType.StaierRoom, //���̕����͎��̃t���A�����镔���ɂ���
                _ => RoomType.Normal,                         //����ȊO�͉����Ȃ�������
            };

            rooms[i].roomType = roomType; //�����̎�ނ̊��蓖��
        }
	}

    /// <summary>
    /// �������ԋ߂��G���A���擾
    /// </summary>
    /// <param name="parentArea">�e�ƂȂ�I�u�W�F�N�g</param>
    /// <returns>���E�E�㉺�̂����Ƃ��߂�����</returns>
    Area[] SearchNearArea(Area parentArea)
    {
        SplitData splitData = parentArea.splitData;

        int bottomMin = int.MaxValue;
        int topMin = int.MaxValue;

        Area bottomOrderArea = null;
        Area topOrderArea = null;

        foreach (Area area in mapAreas)
        {
            //�������Ă���G���A�����ƂȂ�G���A���[����Β��ׂȂ�
            if (parentArea.areaTreeIndex >= area.areaTreeIndex) continue;
            if (area.room == null) continue;

            //�G���A���͈͊O�������ꍇ���ׂȂ�
            Area pArea = parentArea;
            Room room = area.room;
            if ((room.Left >= pArea.Left && room.Bottom >= pArea.Bottom && room.Right <= pArea.Right && room.Top <= pArea.Top) == false) continue;

            switch (splitData.splitAxis)
            {
                case SplitData.SplitAxis.X:

                    //���E�̂ǂ��炩���߂���΋߂����������蓖�Ă�
                    int left = splitData.splitPos - room.Right;
                    int right = room.Left - splitData.splitPos;

                    if (left < bottomMin && left > 0)
                    {
                        bottomMin = left;
                        bottomOrderArea = area;
                    }
                    if (right < topMin && right > 0)
                    {
                        topMin = right;
                        topOrderArea = area;
                    }

                    break;

                case SplitData.SplitAxis.Y:

                    //�㉺�̂ǂ��炩���߂���΋߂����������蓖�Ă�
                    int bottom = splitData.splitPos - room.Top;
                    int top = room.Bottom - splitData.splitPos;

                    if (bottom < bottomMin && bottom > 0)
                    {
                        bottomMin = bottom;
                        bottomOrderArea = area;
                    }
                    if (top < topMin && top > 0)
                    {
                        topMin = top;
                        topOrderArea = area;
                    }
                    break;

                default:
                    throw new System.NotImplementedException();
            }
        }

        //�ǂ������G���A�����蓖�Ă��Ȃ��ꍇ�̓G���A��Ԃ��Ȃ�
        if (bottomOrderArea == null || topOrderArea == null) return null;

        return new Area[2] { bottomOrderArea, topOrderArea };
    }

    /// <summary>
    /// ���̃G���A�������������������ɂ��邩�ǂ���
    /// </summary>
    /// <param name="area">���ׂ�G���A</param>
    /// <returns>�G���A�̕���</returns>
    Direction SplitAxisDirection(Area area)
    {
        if (area.room == null) throw new System.NullReferenceException();
        if (area.parentArea == null) throw new System.NullReferenceException();

        SplitData splitData = area.parentArea.splitData;
        Room room = area.room;
        
        //���̕�����Ԃ�
        switch (splitData.splitAxis)
        {
            case SplitData.SplitAxis.X:
                int left = splitData.splitPos - room.Right;
                int right = room.Left - splitData.splitPos;

                if (left > 0)  return Direction.Right;
                if (right > 0) return Direction.Left;
                break;
            case SplitData.SplitAxis.Y:
                int bottom = splitData.splitPos - room.Top;
                int top = room.Bottom - splitData.splitPos;

                if (bottom > 0) return Direction.Top;
                if (top > 0)    return Direction.Bottom;
                break;
            default:
                throw new System.NullReferenceException();
        }

        throw new System.NotImplementedException();
    }

    /// <summary>
    /// �G���A�����̕����ɑ΂��ďd�������ʒu��Ԃ��Ȃ��悤�Ƀ����_���Ȉʒu��Ԃ�
    /// </summary>
    /// <param name="area">�擾�����G���A</param>
    /// <returns>���W</returns>
    Vector2Int AreaSplitRandomPos(Area area)
    {
        if (area.room == null) throw new System.NullReferenceException();
        if (area.parentArea == null) throw new System.NullReferenceException();

        Direction splitDirection = SplitAxisDirection(area);

        //���̃G���A����������������������d�������ʒu��Ԃ��Ȃ��悤�Ƀ����_���Ȉʒu���擾
        return splitDirection switch
        {
            Direction.Left   => GetAvailableEntrancePosY(area, area.room.Left   - 1, area.Left - 1),
            Direction.Right  => GetAvailableEntrancePosY(area, area.room.Right  + 1, area.Right + 1),
            Direction.Bottom => GetAvailableEntrancePosX(area, area.room.Bottom - 1, area.Bottom - 1),
            Direction.Top    => GetAvailableEntrancePosX(area, area.room.Top    + 1, area.Top + 1),
            _ => throw new System.NotImplementedException(),
        };
    }

    /// <summary>
    /// �G���A�̓���̍��W���玲�Ɍ������ē����q����
    /// </summary>
    /// <param name="area">�G���A</param>
    /// <param name="pos">���W</param>
    void AreaSplitRoute(Area area, Vector2Int pos)
    {
        if (area.room == null) throw new System.NullReferenceException();
        if (area.parentArea == null) throw new System.NullReferenceException();

        Direction splitDirection = SplitAxisDirection(area);

        GridType gridType = GridType.Route;

        switch (splitDirection)
        {
            case Direction.Left:
                for (int x = pos.x + 1; x < area.room.Left; x++) mapData[x, pos.y] = gridType;
                break;
            case Direction.Bottom:
                for (int y = pos.y + 1; y < area.room.Bottom; y++) mapData[pos.x, y] = gridType;
                break;
            case Direction.Right:
                for (int x = area.room.Right + 1; x < pos.x; x++) mapData[x, pos.y] = gridType;
                break;
            case Direction.Top:
                for (int y = area.room.Top + 1; y < pos.y; y++) mapData[pos.x, y] = gridType;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// �����̃G���A�̃��X�g�������_���ɓ���ւ��ĕԂ�
    /// </summary>
    /// <param name="areas">���̃��X�g</param>
    /// <returns>�����_���ɓ���ւ������X�g</returns>
    List<Area> RandomSelectOrderArea(List<Area> areas)
    {
        List<Area> result = new List<Area>(areas);

        for (int i = 0; i < result.Count - 1; i++)
        {
            int r = Random.Range(i, result.Count);

            Area temp = result[r];
            result[r] = result[i];
            result[i] = temp;
        }

        return result;
    }

    /// <summary>
    /// �����̕����̃��X�g�������_���ɓ���ւ��ĕԂ�
    /// </summary>
    /// <param name="areas">���̃��X�g</param>
    /// <returns>�����_���ɓ���ւ������X�g</returns>
    List<Room> RandomSelectOrderRoom(List<Area> areas)
    {
        List<Room> result = new List<Area>(areas).Select(a => a.room).Where(a => a != null).ToList();

        for (int i = 0; i < result.Count - 1; i++)
        {
            int r = Random.Range(i, result.Count);

            Room temp = result[r];
            result[r] = result[i];
            result[i] = temp;
        }

        return result;
    }

    /// <summary>
    /// �����̕����̃��X�g�𕔉������f�[�^�ɍi��A�����_���ɓ���ւ��ĕԂ�
    /// </summary>
    /// <param name="areas">���̃��X�g</param>
    /// <returns>�����_���ɓ���ւ������X�g</returns>
    List<Area> RandomSelectOrderAreaIsHasRoom(List<Area> areas)
    {
        List<Area> result = new List<Area>();

        foreach (Area area in areas)
        {
            if (area.room == null) continue;

            result.Add(area);
        }

        for (int i = 0; i < result.Count - 1; i++)
        {
            int r = Random.Range(i, result.Count);

            Area temp = result[r];
            result[r] = result[i];
            result[i] = temp;
        }

        return result;
    }


    /// <summary>
    /// �����̃����_���Ȉʒu��Ԃ�
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetRandomFloorPos()
    {
        Room room = RandomSelectOrderRoom(mapAreas)[0];

        int x = Random.Range(room.Left, room.Right + 1);
        int y = Random.Range(room.Bottom, room.Top + 1);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// �����̓�������d�Ȃ�Ȃ��悤�ɐ�������
    /// </summary>
    /// <param name="area">�G���A</param>
    /// <param name="y">��ƂȂ�Y���W</param>
    /// <param name="replaceY">�u��������l</param>
    /// <returns></returns>
    public Vector2Int GetAvailableEntrancePosX(Area area, int y, int replaceY = -1)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        List<Vector2Int> remove = new List<Vector2Int>();

        int height = wallHeight + wallPadding; //�ǂ̍���

        //�������X�g��
        for (int x = area.room.Left + wallPadding; x <= area.room.Right - wallPadding; x++)
        {
            list.Add(new Vector2Int(x, y));
        }

        //���O������W�����X�g��
        foreach (Vector2Int pos in area.entranceList)
        {
            for (int h = -height; h <= height; h++)
            {
                remove.Add(new Vector2Int(pos.x - h, pos.y));
            }
        }

        List<Vector2Int> resultList = list.Except<Vector2Int>(remove).ToList(); //��₩�珜�O���s��
        if (resultList.Count <= 0) return new Vector2Int(-1, -1);               //��₪�Ȃ���΍��W��Ԃ��Ȃ�

        Vector2Int resultPos = resultList.OrderBy(n => System.Guid.NewGuid()).First(); //���O���X�g���烉���_���Ɉ�擾
        area.AddEntrance(resultPos); //�����̓������ǉ�

        if (replaceY >= 0) resultPos.y = replaceY;

        return resultPos;
    }

    public Vector2Int GetAvailableEntrancePosY(Area area, int x, int replaceX = -1)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        List<Vector2Int> remove = new List<Vector2Int>();

        int height = wallHeight + wallPadding; //�ǂ̍���

        //�������X�g��
        for (int y = area.room.Bottom + wallPadding; y <= area.room.Top - wallPadding; y++)
        {
            list.Add(new Vector2Int(x, y));
        }

        //���O������W�����X�g��
        foreach (Vector2Int pos in area.entranceList)
        {
            for (int h = -height; h <= height; h++)
            {
                remove.Add(new Vector2Int(pos.x, pos.y - h));
            }
        }

        List<Vector2Int> resultList = list.Except<Vector2Int>(remove).ToList(); //���O������W�����X�g��
        if (resultList.Count <= 0) return new Vector2Int(-1, -1);               //��₪�Ȃ���΍��W��Ԃ��Ȃ�

        Vector2Int resultPos = resultList.OrderBy(n => System.Guid.NewGuid()).First(); //���O���X�g���烉���_���Ɉ�擾
        area.AddEntrance(resultPos); //�����̓������ǉ�

        if (replaceX >= 0) resultPos.x = replaceX;

        return resultPos;
    }
}
