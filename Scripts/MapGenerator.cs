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

	[SerializeField] Vector2Int mapSize;     //�}�b�v�̑傫��
	[SerializeField] Tilemap groundTilemap;  //�n�ʂ̃^�C���}�b�v
	[SerializeField] Tilemap routeTilemap;   //���̃^�C���}�b�v	
	[SerializeField] Tilemap wallTilemap;    //�ǂ̃^�C���}�b�v
	[SerializeField] Tilemap cellingTilemap; //�V��̃^�C���}�b�v
	[SerializeField] Tilemap doorTilemap;    //�h�A�̃^�C���}�b�v

	[Space]
	[SerializeField] TileBase floorTile;   //�n�ʂ̃^�C��
	[SerializeField] TileBase routeTile;   //���̃^�C��
	[SerializeField] TileBase cellingTile; //�V��̃^�C��
	[SerializeField] TileBase wallTile;    //�ǂ̃^�C��
	[SerializeField] TileBase doorTile;    //�h�A�̃^�C��

	public GridType[,] MapData { get { return dungeonGenerator.GetMapData(); } } 
	public Vector2 GetTileWorldPos(Vector2Int tilePos) { tilePos.y *= -1; return groundTilemap.GetCellCenterWorld((Vector3Int)tilePos) + Vector3.down * 0.2f; } //�^�C���̃��[���h���W

	private void Awake()
	{
		dungeonGenerator = GetComponent<DungeonGenerator>();
	}

	private void Start()
	{
		Generate();
	}

	/// <summary>
	/// �n�`�̐���
	/// </summary>
	public void Generate()
	{
		//�_���W�����̃f�[�^�𐶐�
		dungeonGenerator.Generate(mapSize);
		
		//�_���W������`��
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
	/// �}�b�v�f�[�^�ɑ΂���^�C���}�b�v���擾
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
	/// �}�b�v�f�[�^�ɑ΂���^�C�����擾
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
