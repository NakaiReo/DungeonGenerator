using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DungeonData;

[RequireComponent(typeof(DungeonGenerator))]
public class MapGenerator : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;

	[SerializeField] Vector2Int mapSize;     //マップの大きさ
	[SerializeField] Tilemap groundTilemap;  //地面のタイルマップ
	[SerializeField] Tilemap routeTilemap;   //道のタイルマップ	
	[SerializeField] Tilemap wallTilemap;    //壁のタイルマップ
	[SerializeField] Tilemap cellingTilemap; //天井のタイルマップ
	[SerializeField] Tilemap doorTilemap;    //ドアのタイルマップ

	[Space]
	[SerializeField] TileBase floorTile;   //地面のタイル
	[SerializeField] TileBase routeTile;   //道のタイル
	[SerializeField] TileBase cellingTile; //天井のタイル
	[SerializeField] TileBase wallTile;    //壁のタイル
	[SerializeField] TileBase doorTile;    //ドアのタイル

	public GridType[,] MapData { get { return dungeonGenerator.GetMapData(); } } 
	public Vector2 GetTileWorldPos(Vector2Int tilePos) { tilePos.y *= -1; return groundTilemap.GetCellCenterWorld((Vector3Int)tilePos) + Vector3.down * 0.2f; } //タイルのワールド座標

	private void Awake()
	{
		dungeonGenerator = GetComponent<DungeonGenerator>();
	}

	private void Start()
	{
		Generate();
	}

	/// <summary>
	/// 地形の生成
	/// </summary>
	public void Generate()
	{
		//ダンジョンのデータを生成
		dungeonGenerator.Generate(mapSize);
		
		//ダンジョンを描画
		for(int y = 0; y < mapSize.y; y++)
		{
			for(int x = 0; x < mapSize.x; x++)
			{
				GridType gridType = MapData[x, y];

				Tilemap tilemap  = GetGridTypeOfTileMap(gridType);
				TileBase tileBase = GetGridTypeOfTileBase(gridType);
				tilemap?.SetTile(new Vector3Int(x, -y, 0), tileBase);
			}
		}
	}

	/// <summary>
	/// マップデータに対するタイルマップを取得
	/// </summary>
	public Tilemap GetGridTypeOfTileMap(GridType gridType) => gridType switch
	{
		GridType.None => null,
		GridType.Floor => groundTilemap,
		GridType.Route => routeTilemap,
		GridType.Wall => wallTilemap,
		GridType.Celling => cellingTilemap,
		GridType.RoomPadding => cellingTilemap,
		_ => null,
	};

	/// <summary>
	/// マップデータに対するタイルを取得
	/// </summary>
	public TileBase GetGridTypeOfTileBase(GridType gridType) => gridType switch
	{
		GridType.None => null,
		GridType.Floor => floorTile,
		GridType.Route => routeTile,
		GridType.Wall => wallTile,
		GridType.Celling => cellingTile,
		GridType.RoomPadding => cellingTile,
		_ => null,
	};
}
