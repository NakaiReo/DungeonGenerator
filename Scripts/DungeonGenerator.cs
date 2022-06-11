using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DungeonData;

public class DungeonGenerator : MonoBehaviour
{
    /// <summary>
    /// 4点間の矩形を扱うクラス
    /// </summary>
    public class Rect
    {
        public int Left { get; protected set; }   //左側の点
        public int Right { get; protected set; }  //右側の点
        public int Bottom { get; protected set; } //下側の点
        public int Top { get; protected set; }    //上側の点

        public int Width { get { return Right - Left + 1; } }  //横の長さ
        public int Height { get { return Top - Bottom + 1; } } //縦の長さ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Rect(int left, int bottom, int right, int top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        /// <summary>
        /// マップデータに対してこの矩形分の範囲のグリッドの種類を変える
        /// </summary>
        /// <param name="mapData">マップデータ</param>
        /// <param name="tile">変更するグリッド</param>
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
    /// エリアの矩形の範囲
    /// </summary>
    public class Area : Rect
    {
        public SplitData splitData { get; private set; } //エリアを分ける軸のデータ

        public Area parentArea { get; private set; } //このエリアが持つ親のエリア
        public Room room { get; private set; } //このエリアが持つ部屋の矩形データ

        public List<Vector2Int> entranceList = new List<Vector2Int>(); //入り口の座標

        public int areaTreeIndex; //一番最初からみてこのエリアの分岐数

        /// <summary>
        /// コンストラクタ
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
        /// 分割軸のデータを設定する
        /// </summary>
        /// <param name="splitData">分割軸のデータ</param>
        public void SetSplitData(SplitData splitData)
        {
            this.splitData = splitData;
        }

        /// <summary>
        /// 部屋のデータを設定する
        /// </summary>
        /// <param name="room">部屋のデータ</param>
        public void SetRoom(Room room)
        {
            this.room = room;
        }

        /// <summary>
        /// 部屋のデータを削除する
        /// </summary>
        public void RemoveRoom()
        {
            room = null;
        }

        /// <summary>
        /// 部屋の入り口の座標を追加する
        /// </summary>
        /// <param name="pos">座標</param>
        public void AddEntrance(Vector2Int pos)
        {
            entranceList.Add(pos);
        }
    }

    /// <summary>
    /// 部屋の矩形の範囲データ
    /// </summary>
    public class Room : Rect
    {
        public RoomType roomType; //部屋の種類

        public Room(int left, int bottom, int right, int top) : base(left, bottom, right, top)
        {
        }
    }

    /// <summary>
    /// エリアを分ける軸のデータ
    /// </summary>
    public class SplitData
    {
        /// <summary>
        /// 軸の方向
        /// </summary>
        public enum SplitAxis
        {
            X,
            Y,
        }

        public int splitPos; //軸の位置
        public SplitAxis splitAxis; //軸の方向
    }

    [SerializeField] Vector2Int areaMinSize; //エリアの最小サイズ
    [SerializeField] Vector2Int roomMinSize; //部屋の最小サイズ
    [SerializeField] Vector2Int roomMaxSize; //部屋の最大サイズ
    [SerializeField] int mapPadding;    //マップの外壁のサイズ
    [SerializeField] int roomPadding;   //部屋の外壁のサイズ
    [SerializeField] int wallHeight;    //壁の高さ
    [SerializeField] int wallPadding;   //壁の間隔
    [SerializeField] int maxAStarRoute; //追加の道の数
    [SerializeField] int splitCountMin; //部屋の分割の最小回数
    [SerializeField] int splitCountMax; //部屋の分割の最大回数
    [SerializeField] float splitEndParcent;    //部屋の分割を終了する確率
    [SerializeField] float removeRoomMinRatio; //部屋を作らない数の最小割合
    [SerializeField] float removeRoomMaxRatio; //部屋を作らない数の最大割合

    List<Area> allAreas = new List<Area>(); //すべてのエリアのリスト
    List<Area> mapAreas = new List<Area>(); //部屋が生成されたエリアのリスト

    Vector2Int mapSize;  //マップのサイズ
    GridType[,] mapData; //マップのマスの情報

    public Vector2Int GetMapSize() { return mapSize; } //マップの大きさの取得
    public GridType[,] GetMapData() { return mapData; } //マップデータの取得
    public List<Area> GetMapAreas() { return mapAreas; } //マップのエリアの取得

    /// <summary>
    /// ダンジョンの生成
    /// </summary>
    /// <param name="mapSize">ダンジョンのサイズ</param>
    /// <returns>生成後のデータ</returns>
    public GridType[,] Generate(Vector2Int mapSize)
    {
        //マップの大きさを設定
        this.mapSize = mapSize;
        mapData = new GridType[mapSize.x, mapSize.y];

        //エリアをリストを初期化
        allAreas.Clear();
        mapAreas.Clear();

        //マップ全体のエリア
        Area rootArea = new Area(null, 0, 0, mapSize.x - 1, mapSize.y - 1, 0);

        //エリアの分割
        SplitArea(rootArea);

        //マップ配列の初期化
        rootArea.FillTile(mapData, GridType.Celling);

        //エリアごとに部屋の生成
        CreateRoom();

        //道の生成
        CreateRoute();

        //追加の道生成
        for(int i=0;i<maxAStarRoute;i++)
        CreateAStarRoute();

        //壁の生成
        CreateWalls();

        //部屋の種類の設定
        SetRoomType();

        return mapData;
    }

    /// <summary>
    /// エリアの分割
    /// </summary>
    /// <param name="area">分割するエリア</param>
    /// <param name="splitCount">現在の分割数</param>
    void SplitArea(Area area, int splitCount = 0)
    {
        //分割を終了するかどうか
        bool splitEnd = (splitCount >= splitCountMin && splitEndParcent > Random.Range(0.0f, 100.0f)) || splitCount >= splitCountMax;

        //これ以上分割できない場合、または分割を終了する場合は分割を終了する
        if ((area.Width < areaMinSize.x * 2 + 1 && area.Height < areaMinSize.y * 2 + 1) || splitEnd)
        {
            mapAreas.Add(area);
            return;
        }

        //分割方法のデータ
        SplitData splitData = new SplitData();

        //分割する方向を決める
        splitData.splitAxis = (area.Width, area.Height) switch
        {
            (var _w, var _h) when _w == _h => (SplitData.SplitAxis)Random.Range(0, 1 + 1),  //エリアの余りが両軸とも同じならばランダムに決める
            (var _w, var _h) when _w > _h && _w >= areaMinSize.x => SplitData.SplitAxis.X, //エリアの余りがX軸側が長いならばX軸に分割するようにする
            (var _w, var _h) when _w < _h && _h >= areaMinSize.y => SplitData.SplitAxis.Y, //エリアの余りがY軸側が長いならばY軸に分割するようにする
            _ => throw new System.NotImplementedException(),                                //上記のどの場合にも当てはまらないとき例外処理を行う
        };

        //分割する基準点を設定する
        splitData.splitPos = (splitData.splitAxis) switch
        {
            SplitData.SplitAxis.X => Random.Range(area.Left + areaMinSize.x, area.Right - areaMinSize.x), //分割する軸がX方向の場合
            SplitData.SplitAxis.Y => Random.Range(area.Bottom + areaMinSize.y, area.Top - areaMinSize.y),   //分割する軸がY方向の場合
            _ => throw new System.NotImplementedException(),                                                //分割する軸が存在しない場合は例外処理を行う
        };

        //エリアを分割を行う
        Area[] splitAreas = (splitData.splitAxis) switch
        {
            //分割する軸がX方向の場合
            SplitData.SplitAxis.X => new Area[2]
            {
                new Area(area, area.Left, area.Bottom, splitData.splitPos - 1, area.Top, splitCount), //左側の部屋の分割
		        new Area(area, splitData.splitPos + 1, area.Bottom, area.Right, area.Top, splitCount) //右側の部屋の分割
			},

            //分割する軸がY方向の場合
            SplitData.SplitAxis.Y => new Area[2]
            {
                new Area(area, area.Left, area.Bottom, area.Right, splitData.splitPos - 1, splitCount), //下側の部屋の分割
				new Area(area, area.Left, splitData.splitPos + 1, area.Right, area.Top, splitCount)     //上側の部屋の分割
			},

            //分割する軸が存在しない場合は例外処理を行う
            _ => throw new System.NotImplementedException(),
        };

        //エリアにそれぞれデータを割り当てる
        area.SetSplitData(splitData);

        //それぞれのエリアをさらに分割する
        splitCount++;
        SplitArea(splitAreas[0], splitCount);
        SplitArea(splitAreas[1], splitCount);

        allAreas.Add(area);
    }

    /// <summary>
    /// 部屋の生成
    /// </summary>
    void CreateRoom()
    {
        Debug.Log("エリアの総数 => " + mapAreas.Count);

        //生成しない部屋の数
        int removeRoomAmount = Mathf.FloorToInt(Random.Range(removeRoomMinRatio, removeRoomMaxRatio) * mapAreas.Count * 0.01f);
        List<Area> removeAreas = RandomSelectOrderArea(mapAreas).GetRange(0, removeRoomAmount);

        foreach (Area area in mapAreas)
        {
            //部屋を生成しにない場合は実行しない
            if (removeAreas.Contains(area)) continue;

            //エリアに作れる部屋の最大の大きさ
            int areaOfRoomWidth  = area.Width - roomPadding * 2;
            int areaOfRoomHeight = area.Height - roomPadding * 2 - wallHeight * 2;

            //作る部屋の大きさ
            int roomWidth  = Random.Range(Mathf.Min(roomMinSize.x, areaOfRoomWidth) , areaOfRoomWidth  + 1);
            int roomHeight = Random.Range(Mathf.Min(roomMinSize.y, areaOfRoomHeight), areaOfRoomHeight + 1);

            //部屋の最大サイズが設定いる場合は制限する
            if (roomMaxSize.x > 0) roomWidth  = Mathf.Min(roomWidth, roomMaxSize.x);
            if (roomMaxSize.y > 0) roomHeight = Mathf.Min(roomHeight, roomMaxSize.y);

            //部屋の隙間の大きさ
            int spaceWidth  = areaOfRoomWidth  - roomWidth;
            int spaceHeight = areaOfRoomHeight - roomHeight;

            //基準点からの離れている距離
            int marginWidth  = Random.Range(0, spaceWidth + 1);
            int marginHeight = Random.Range(0, spaceHeight + 1);

            //部屋のそれぞれの座標
            int left   = area.Left   + marginWidth  + roomPadding;
            int bottom = area.Bottom + marginHeight + roomPadding + wallHeight;
            int right  = left        + roomWidth    - 1;
            int top    = bottom      + roomHeight   - 1;

            //エリアに部屋を作る
            area.SetRoom(new Room(left, bottom, right, top));

            //部屋にタイルを敷く
            area.FillTile(mapData, GridType.RoomPadding);
            area.room.FillTile(mapData, GridType.Floor);
        }
    }

    /// <summary>
    /// 部屋と部屋を繋ぐ道を作る
    /// </summary>
    void CreateRoute()
    {
        //現在存在している部屋を繋げる
        foreach (Area parentArea in allAreas)
        {
            SplitData splitData = parentArea.splitData;

            //エリアから一番近いエリアを探す
            Area[] areas = SearchNearArea(parentArea);
            if (areas == null) continue;

            //そのエリアの分割されたエリアを取得
            Area area1 = areas[0];
            Area area2 = areas[1];

            //道のタイル
            GridType TempGridType = GridType.Route;

            //分割する軸に応じて
            switch (splitData.splitAxis)
            {
                case SplitData.SplitAxis.X:

                    //分割する位置を決める
                    int leftX  = area1.room.Right + 1;
                    int rightX = area2.room.Left  - 1;

                    int leftDoorPos  = GetAvailableEntrancePosY(area1, leftX).y;
                    int rightDoorPos = GetAvailableEntrancePosY(area2, rightX).y;

                    //道の生成
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

                    //分割する位置を決める
                    int bottomY = area1.room.Top + 1;
                    int topY    = area2.room.Bottom - 1;

                    int bottomDoorPos = GetAvailableEntrancePosX(area1, bottomY).x;
                    int topDoorPos    = GetAvailableEntrancePosX(area2, topY).x;

                    //道の生成
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
    /// 2点間を繋ぐ道のパスを取得
    /// </summary>
    /// <param name="startPos">開始位置</param>
    /// <param name="endPos">終了位置</param>
    /// <returns>道のパス</returns>
    public List<AStar.AStarNode> AStar(Vector2Int startPos, Vector2Int endPos)
    {
        AStar aStar = new AStar();
        return aStar.GetRoutePath(mapData, startPos, endPos, new GridType[] { GridType.Celling, GridType.Route});
    }

    /// <summary>
    /// AStarアルゴリズムによる新規通路の生成
    /// </summary>
    /// <returns></returns>
    public bool CreateAStarRoute()
    {
        Vector2Int startPos;
        Vector2Int endPos;
        List<Area> hasRoomAreas = RandomSelectOrderAreaIsHasRoom(mapAreas);

        //開始位置と終了位置を取得
        startPos = AreaSplitRandomPos(hasRoomAreas[0]);
        endPos   = AreaSplitRandomPos(hasRoomAreas[1]);

        //取得できなければ実行しない
        if (startPos == new Vector2Int(-1, -1)) return false;
        if (endPos   == new Vector2Int(-1, -1)) return false;

        //通路のパスを取得
        List<AStar.AStarNode> path = AStar(startPos, endPos);

        Debug.Log(path.Count);

        //パスが取得できなかった場合は
        if (path == null || path.Count == 0)
        {
            return false;
        }

        //パスを辿りながら道に置き換える
        foreach (AStar.AStarNode node in path)
        {
            Vector2Int pos = node.pos;
            mapData[pos.x, pos.y] = GridType.Route;
        }

        //部屋と道を繋げる
        AreaSplitRoute(hasRoomAreas[0], startPos);
        AreaSplitRoute(hasRoomAreas[1], endPos);

        return true;
    }

    /// <summary>
    /// 壁の生成
    /// </summary>
    void CreateWalls()
    {
        for(int y = 0; y < mapSize.y; y++) 
        {
            for(int x = 0; x < mapSize.x; x++)
            {
                //座標が道か地面でなければ次へ
                if (mapData[x, y]  != GridType.Floor && mapData[x, y] != GridType.Route) continue;

                //壁の生成
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
    /// 部屋の種類を割り当てる
    /// </summary>
    void SetRoomType()
    {
        //部屋のリストをランダムに並び替えて取得
        List<Room> rooms = mapAreas.Select(n => n.room).Where(n => n != null).OrderBy(n => System.Guid.NewGuid()).ToList();

        //部屋の種類を取得
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomType roomType = i switch
            {
                (int n) when (n == 0) => RoomType.StartRoom,  //最初の一つはプレイヤーの湧き場所にする
                (int n) when (n == 1) => RoomType.StaierRoom, //次の部屋は次のフロアを昇る部屋にする
                _ => RoomType.Normal,                         //それ以外は何もない部屋に
            };

            rooms[i].roomType = roomType; //部屋の種類の割り当て
        }
	}

    /// <summary>
    /// 軸から一番近いエリアを取得
    /// </summary>
    /// <param name="parentArea">親となるオブジェクト</param>
    /// <returns>左右・上下のもっとも近い部屋</returns>
    Area[] SearchNearArea(Area parentArea)
    {
        SplitData splitData = parentArea.splitData;

        int bottomMin = int.MaxValue;
        int topMin = int.MaxValue;

        Area bottomOrderArea = null;
        Area topOrderArea = null;

        foreach (Area area in mapAreas)
        {
            //検索しているエリアが元となるエリアより深ければ調べない
            if (parentArea.areaTreeIndex >= area.areaTreeIndex) continue;
            if (area.room == null) continue;

            //エリアが範囲外だった場合調べない
            Area pArea = parentArea;
            Room room = area.room;
            if ((room.Left >= pArea.Left && room.Bottom >= pArea.Bottom && room.Right <= pArea.Right && room.Top <= pArea.Top) == false) continue;

            switch (splitData.splitAxis)
            {
                case SplitData.SplitAxis.X:

                    //左右のどちらかが近ければ近い部屋を割り当てる
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

                    //上下のどちらかが近ければ近い部屋を割り当てる
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

        //どちかがエリアを割り当てられない場合はエリアを返さない
        if (bottomOrderArea == null || topOrderArea == null) return null;

        return new Area[2] { bottomOrderArea, topOrderArea };
    }

    /// <summary>
    /// このエリアが分割した軸が方向にあるかどうか
    /// </summary>
    /// <param name="area">調べるエリア</param>
    /// <returns>エリアの方向</returns>
    Direction SplitAxisDirection(Area area)
    {
        if (area.room == null) throw new System.NullReferenceException();
        if (area.parentArea == null) throw new System.NullReferenceException();

        SplitData splitData = area.parentArea.splitData;
        Room room = area.room;
        
        //軸の方向を返す
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
    /// エリアを軸の方向に対して重複した位置を返さないようにランダムな位置を返す
    /// </summary>
    /// <param name="area">取得したエリア</param>
    /// <returns>座標</returns>
    Vector2Int AreaSplitRandomPos(Area area)
    {
        if (area.room == null) throw new System.NullReferenceException();
        if (area.parentArea == null) throw new System.NullReferenceException();

        Direction splitDirection = SplitAxisDirection(area);

        //このエリアが分割した軸が方向から重複した位置を返さないようにランダムな位置を取得
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
    /// エリアの特定の座標から軸に向かって道を繋げる
    /// </summary>
    /// <param name="area">エリア</param>
    /// <param name="pos">座標</param>
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
    /// 引数のエリアのリストをランダムに入れ替えて返す
    /// </summary>
    /// <param name="areas">元のリスト</param>
    /// <returns>ランダムに入れ替えたリスト</returns>
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
    /// 引数の部屋のリストをランダムに入れ替えて返す
    /// </summary>
    /// <param name="areas">元のリスト</param>
    /// <returns>ランダムに入れ替えたリスト</returns>
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
    /// 引数の部屋のリストを部屋を持つデータに絞り、ランダムに入れ替えて返す
    /// </summary>
    /// <param name="areas">元のリスト</param>
    /// <returns>ランダムに入れ替えたリスト</returns>
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
    /// 部屋のランダムな位置を返す
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
    /// 部屋の入り口が重ならないように生成する
    /// </summary>
    /// <param name="area">エリア</param>
    /// <param name="y">基準となるY座標</param>
    /// <param name="replaceY">置き換える値</param>
    /// <returns></returns>
    public Vector2Int GetAvailableEntrancePosX(Area area, int y, int replaceY = -1)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        List<Vector2Int> remove = new List<Vector2Int>();

        int height = wallHeight + wallPadding; //壁の高さ

        //候補をリスト化
        for (int x = area.room.Left + wallPadding; x <= area.room.Right - wallPadding; x++)
        {
            list.Add(new Vector2Int(x, y));
        }

        //除外する座標をリスト化
        foreach (Vector2Int pos in area.entranceList)
        {
            for (int h = -height; h <= height; h++)
            {
                remove.Add(new Vector2Int(pos.x - h, pos.y));
            }
        }

        List<Vector2Int> resultList = list.Except<Vector2Int>(remove).ToList(); //候補から除外を行う
        if (resultList.Count <= 0) return new Vector2Int(-1, -1);               //候補がなければ座標を返さない

        Vector2Int resultPos = resultList.OrderBy(n => System.Guid.NewGuid()).First(); //除外リストからランダムに一つ取得
        area.AddEntrance(resultPos); //部屋の入り口を追加

        if (replaceY >= 0) resultPos.y = replaceY;

        return resultPos;
    }

    public Vector2Int GetAvailableEntrancePosY(Area area, int x, int replaceX = -1)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        List<Vector2Int> remove = new List<Vector2Int>();

        int height = wallHeight + wallPadding; //壁の高さ

        //候補をリスト化
        for (int y = area.room.Bottom + wallPadding; y <= area.room.Top - wallPadding; y++)
        {
            list.Add(new Vector2Int(x, y));
        }

        //除外する座標をリスト化
        foreach (Vector2Int pos in area.entranceList)
        {
            for (int h = -height; h <= height; h++)
            {
                remove.Add(new Vector2Int(pos.x, pos.y - h));
            }
        }

        List<Vector2Int> resultList = list.Except<Vector2Int>(remove).ToList(); //除外する座標をリスト化
        if (resultList.Count <= 0) return new Vector2Int(-1, -1);               //候補がなければ座標を返さない

        Vector2Int resultPos = resultList.OrderBy(n => System.Guid.NewGuid()).First(); //除外リストからランダムに一つ取得
        area.AddEntrance(resultPos); //部屋の入り口を追加

        if (replaceX >= 0) resultPos.x = replaceX;

        return resultPos;
    }
}
