using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DungeonData;
using UnityEngine.Events;

public class AStar
{
	public class AStarNode
	{
		public enum Status
		{
			None,
			Open,
			Closed,
		}

		public AStarNode parentNode; //ノードが持つ親ノード
		public Vector2Int pos { get; private set; } //ノードの位置情報
		public Status status { get; private set; } //ノードの解放情報
		public GridType gridType { get; private set; } //ノードの持つマップ当たり判定

		public int realCost { get; private set; } //実際のコスト
		public int estimatedCost { get; private set; } //仮想コスト
		
		public int score { get { return realCost + estimatedCost; } } //スコア

		public AStarNode(Vector2Int pos, GridType gridType)
		{
			this.pos = pos;
			this.gridType = gridType;
			status = Status.None;

			realCost = 0;
			estimatedCost = 0;
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public static void Init(ref List<AStarNode> openNodes)
		{
			openNodes.Clear();
		}
		 
		/// <summary>
		/// ノードを開放する
		/// </summary>
		public void NodeOpen(AStarNode parentNode, Vector2Int endPos, ref List<AStarNode> openNodes)
		{
			//親ノードの設定
			this.parentNode = parentNode;

			//実際のコストの計算
			realCost = parentNode != null ? parentNode.realCost + 1 : 0;

			//仮想のコストの計算
			Vector2Int vector = endPos - pos;
			vector = new Vector2Int(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
			estimatedCost = vector.x + vector.y;

			//ノード解放
			openNodes.Add(this);
			status = Status.Open;
		}

		/// <summary>
		/// ノードを閉じる
		/// </summary>
		public void NodeClose(ref List<AStarNode> openNodes)
		{
			//ノードを閉じる
			openNodes.Remove(this);
			status = Status.Closed;
		}

		/// <summary>
		/// このノードは通貨可能か
		/// </summary>
		/// <returns></returns>
		public bool IsPass(GridType[] passGridType)
		{
			foreach(GridType passGrid in passGridType)
			{
				if (gridType == passGrid) return true;
			}

			return false;
		}
	}

	List<AStarNode> openNodes = new List<AStarNode>(); //ステータスがOpenになっているノード一覧
	GridType[] passGridType; //マップの当たり判定一覧
	Vector2Int endPos; //経路探査の最終地点

	int width;  //マップデータの幅
	int height; //マップデータの高さ
	AStarNode[,] nodeData; //ノードマップ
	bool isEndSearch; //経路探査終了フラグ
	List<AStarNode> path = new List<AStarNode>(); //経路ノード

	/// <summary>
	/// 経路探査
	/// </summary>
	/// <param name="mapData">ダンジョンデータ</param>
	/// <param name="startPos">経路開始位置</param>
	/// <param name="endPos">経路終了位置</param>
	/// <param name="passGridType">通過可能なグリッド</param>
	/// <returns>経路ノード</returns>
	public List<AStarNode> GetRoutePath(GridType[,] mapData, Vector2Int startPos, Vector2Int endPos, GridType[] passGridType)
	{
		//幅と高さの取得
		width  = mapData.GetLength(0);
		height = mapData.GetLength(1);

		this.endPos = endPos;
		this.passGridType = passGridType;

		//ノードデータの初期化
		nodeData = new AStarNode[width, height];
		AStarNode.Init(ref openNodes);

		for(int y = 0; y < height; y++)
		{
			for(int x = 0; x < width; x++)
			{
				nodeData[x, y] = new AStarNode(new Vector2Int(x, y), mapData[x, y]);
			}
		}

		Debug.Log("Path Search Start");

		//初期地点のノードを開放
		nodeData[startPos.x, startPos.y].NodeOpen(null, endPos, ref openNodes);
		AStarNode startNode = nodeData[startPos.x, startPos.y];
		path.Clear();
		isEndSearch = false;
		SearchPath(startNode);

		//経路探査終了まで検査する
		while (!isEndSearch)
		{
			AStarNode nextNode = SearchMinScoreNode();

			if (nextNode == null) break;

			SearchPath(nextNode);
		}

		Debug.Log("Path Search End => " + path.Count);

		return path;
	}

	/// <summary>
	/// ノードを解放する
	/// </summary>
	void SearchPath(AStarNode node)
	{
		//ノードが最終地点であれば検索を終了する
		if(node.pos == endPos)
		{
			path = GetNodePath(node);
			isEndSearch = true;
			return;
		}

		//調べたノードを閉じる
		Vector2Int currentPos = node.pos;
		nodeData[currentPos.x, currentPos.y].NodeClose(ref openNodes);

		//新たなノード⒲お解放していく
		for (int dx = -1, dy = 0, i = 0; i < 4; dx += dy, dy = dx - dy, dx = dy - dx, i++)
		{
			Vector2Int movePos = currentPos + new Vector2Int(dx, dy);
			if (movePos.x < 0 || movePos.x >= width) continue;
			if (movePos.y < 0 || movePos.y >= height) continue;
			if (nodeData[movePos.x, movePos.y].status != AStarNode.Status.None) continue;
			if (!nodeData[movePos.x, movePos.y].IsPass(passGridType)) continue;

			nodeData[movePos.x, movePos.y].NodeOpen(node, endPos, ref openNodes);
		}
	}

	/// <summary>
	/// ノードを辿りパスを取得する
	/// </summary>
	List<AStarNode> GetNodePath(AStarNode node, List<AStarNode> path = null)
	{
		if (path == null) path = new List<AStarNode>();

		path.Add(node);

		if(node.parentNode != null)
		{
			return GetNodePath(node.parentNode, path);
		}
		else
		{
			path.Reverse();

			return path;
		}
	}

	/// <summary>
	/// 開いているノードから一番スコアの低いノードを取得
	/// </summary>
	/// <returns></returns>
	AStarNode SearchMinScoreNode()
	{
		int  minScore = int.MaxValue;
		int  minCost  = int.MaxValue;
		AStarNode minNode  = null;

		//開いているノードを全て検索
		foreach (AStarNode node in openNodes)
		{
			int score = node.score;
			if (score > minScore) continue;
			if (score == minCost && node.realCost >= minCost) continue;

			minScore = score;
			minCost = node.realCost;
			minNode = node;
		}

		if(minNode == null)
		{
			Debug.LogWarning("最小値が存在しません!");
			return null;
		}

		return minNode;
	}
}
