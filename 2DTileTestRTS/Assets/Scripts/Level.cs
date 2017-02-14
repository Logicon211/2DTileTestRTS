using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;
using Algorithms;

[System.Serializable]
public enum TileType
{
	Empty,
	Block,
	OneWay
}

public class Level : MonoBehaviour {

	public TextAsset levelXml;
	public bool loadAtStartUp = true;
	public MapTile[,] mapTiles;

	public GameObject groundTilePrefab;

	public enum Direction {Up, Right, Down, Left};

	//Static reference to itself (Singleton)
	public static Level mainLevel;

	//Temporary
	public GameObject player;

	//PATHFINDER STUFF
	public List<Sprite> mDirtSprites;

	/// <summary>
	/// The width of the map in tiles.
	/// </summary>
	public int mWidth = 50;
	/// <summary>
	/// The height of the map in tiles.
	/// </summary>
	public int mHeight = 42;
	/// <summary>
	/// The path finder.
	/// </summary>
	public PathFinderFast mPathFinder;
	/// <summary>
	/// The size of a tile in pixels.
	/// </summary>
	static public int cTileSize = 1;

	private LineRenderer lineRenderer;
	public LayerMask playerLayerMask;

	void Awake() {
		mainLevel = this;
	}

	// Use this for initialization
	void Start () {
		lineRenderer = GetComponent<LineRenderer> ();
		if(loadAtStartUp) {
			//Destroy all existing Children and reload
			ClearLevel();
			InstantiateLevel();
		}

		mainLevel = this;
		//PATHFINDER STUFF
		InitPathFinder();
	}

	// Update is called once per frame
	void Update () {
		//right mouse button click
		if(Input.GetMouseButtonDown(1)) {

			Vector3 mouseScreenPosition = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
			Vector2 mousePosition = new Vector2(mouseScreenPosition.x, mouseScreenPosition.y);

			RaycastHit2D hit = Physics2D.Raycast(mousePosition, new Vector2(0, -1), Mathf.Infinity, playerLayerMask);

			//Draws a line for a frame
			//Debug.DrawRay(mousePosition, new Vector2(0, -1), Color.green);


			Vector2 clickPosition = new Vector2();
			Vector2 playerPosition = new Vector2();
			bool foundClick = false;
			bool foundPlayer = false;


			if (hit.collider != null) {
				MapTile tile = hit.collider.gameObject.GetComponent<MapTile> ();
				if (tile != null) {
					//Instead of making an explosion. We want to save this point as the waypoint for the unit to move. Will need to figure out how to do pathfinding this way
					//Instantiate(Resources.Load ("Explosion") as GameObject , new Vector3(hit.point.x, hit.point.y, 0), transform.rotation);
					clickPosition = new Vector2(tile.x , tile.y + 1);
					foundClick = true;
				}
			}

			Vector2 player2DPosition = new Vector2 (player.transform.position.x, player.transform.position.y);
			RaycastHit2D playerHit = Physics2D.Raycast(player2DPosition, new Vector2(0, -1), Mathf.Infinity, playerLayerMask);

//			if (playerHit.collider != null) {
//				MapTile tile = playerHit.collider.gameObject.GetComponent<MapTile> ();
//				if (tile != null) {
//					playerPosition = new Vector2(tile.x , tile.y + 1);
//					foundPlayer = true;
//				}
//			}

			if (foundClick/* && foundPlayer*/) {
				Vector2i start = new Vector2i(Convert.ToInt32(playerPosition.x), Convert.ToInt32(playerPosition.y));
				Vector2i end = new Vector2i(Convert.ToInt32(clickPosition.x), Convert.ToInt32(clickPosition.y));
				//List<Vector2i> path = mPathFinder.FindPath (start, end, player.GetComponent<Unit>().width, player.GetComponent<Unit>().height, (short)player.GetComponent<Unit>().maxJumpHeight);
				//DrawPathLines(path);
				player.GetComponent<Unit>().MoveTo(end);
			}
		}
	}

	void FixedUpdate() {
		//PATHFINDER STUFF
		//player.BotUpdate();
	}

	//Singleton implementation, Should probably check if this is how you do it in Unity
	public static Level getLevel() {
		if (mainLevel == null) {
			mainLevel = new Level ();
		}
		return mainLevel;
	}

	public void ClearLevel() {
		foreach(Transform child in transform) {
			Destroy(child.gameObject);
		}
	}

	public void ClearLevelFromEditor() {
		List<Transform> tempList = transform.Cast<Transform>().ToList();
		foreach(Transform child in tempList) {
			DestroyImmediate(child.gameObject);
		}
	}

	public void InstantiateLevel() {
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		xmlDoc.LoadXml(levelXml.text);
		XmlNode levelNode = xmlDoc.FirstChild;
		XmlNodeList levelsList = xmlDoc.GetElementsByTagName("level"); // array of the level nodes.

		mWidth = int.Parse(levelNode.Attributes ["width"].Value);
		mHeight = int.Parse(levelNode.Attributes ["height"].Value);
		mapTiles = new MapTile[mWidth,mHeight];
		foreach(XmlNode levelInfo in levelsList) {
			XmlNodeList levelContent = levelInfo.ChildNodes;
			
			foreach(XmlNode levelItems in levelContent) {
				if(levelItems.Name == "Tiles") {
					//get Attribuites for level
					string tilesetName = levelItems.Attributes["tileset"].Value;
					
					foreach (XmlNode levelTile in levelItems.ChildNodes) {
						if(levelTile.Name == "tile") {
							
							int tileX = int.Parse(levelTile.Attributes["x"].Value);
							//-y values because OGMO's axis starts in the upper left and not lower left.
							int tileY = mHeight - int.Parse(levelTile.Attributes["y"].Value);
							int id = int.Parse(levelTile.Attributes["id"].Value);


							//convert these to cases?
							//More possible tiles
							//Note, in order to use Resources.load, the prefab needs to be in the Resources folder
							MapTile tile = null;


							//Ground Tile
							if (id == 0) {
								tile = (Instantiate(groundTilePrefab, new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject).GetComponent<MapTile> ();
								tile.Instantiate (tileX, tileY, transform, this);
							} 
							//More tiles to check for in here

							mapTiles [tileX,tileY] = tile;	
						}
					}
				}
				
				if(levelItems.Name == "Entities") {
					foreach(XmlNode levelEntities in levelItems) {
						//Do something with entities
						//obj.Add ("entities", levelEntities.InnerXml);
					}
				}

				refreshCollidersOnOuterTiles ();

				//This method generates 1 collider over the entire map
//				gameObject.AddComponent<PolygonCollider2D>(); //collider for itself
//				PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();
//
//				List<Vector2> path = new List<Vector2> ();
//				//Make collider?
//				//simple solution to draw a collider around the first block we encounter. Will need to handle breaks and more complete maps
//				int x = 0;
//				int y = 0;
//				for (x = 0; x < mapTiles.GetLength (0); x++) {
//					bool breakLoop = false;
//					for (y = 0; y < mapTiles.GetLength (1); y++) {
//						if(mapTiles[x,y] != null) {
//							breakLoop = true;
//							break;
//							//we should have coordinates of first tile hit
//						}
//						//	path.Add (node);
//						//	Vector2 node = new Vector2(x,-y);
//					}
//					if (breakLoop) {
//						break;
//					}
//				}
//				Vector2 startingNode = new Vector2 (x, -y);
//				path.Add(startingNode);
//				path = BuildPath(path, startingNode, x, y, Direction.Up);
//				collider.SetPath(0, path.ToArray());
//
				//This example sets up 2 paths, 2 boxes
//				collider.SetPath(0, path.ToArray());
//				collider.SetPath(1, path2.ToArray());
			}
		}
	}

	public void refreshCollidersOnOuterTiles() {
		for (int x = 0; x < mapTiles.GetLength (0); x++) {
			for (int y = 0; y < mapTiles.GetLength (1); y++) {
				if(mapTiles[x,y] != null && mapTiles[x, y].IsOuterTile()) {
					//Enable Collider
					mapTiles [x, y].gameObject.GetComponent<BoxCollider2D> ().enabled = true;
				}
			}
		}
	}

	//THIS FUNCTION WAS A TEST. CURRENTLY WON'T WORK AT THE MOMENT DUE TO IT EXPECTING THE MAPTILE ARRAY TO BE GAMEOBJECTS
	public List<Vector2> BuildPath(List<Vector2> path, Vector2 startPoint, int x, int y, Direction direction) {
		//Assuming the coordinates passed into this function equates to a non-null map tile
		//Will try to traverse the tiles clockwise (up - right - down - left)

		//currently putting path nodes for a polygon collider on the center of each node
		//Next will just try enabling collision boxes on just the edges
		//Also need to be able to put edges around multiple blocks of tiles, and on the inner empty spaces

		bool tileUp = mapTiles[x, y].CheckTileUp ();
		bool tileRight = mapTiles[x, y].CheckTileRight ();
		bool tileDown = mapTiles[x, y].CheckTileDown ();
		bool tileLeft = mapTiles[x, y].CheckTileLeft();

		if (startPoint.Equals (new Vector2 (x, -y)) && path.Count > 1) {
			//We're back to the beginning, return the path
			return path;
		}
		if (direction == Direction.Up) {
			if (tileLeft) {
				//If the tile to the left
				AddNodeToPath(path, x - 1, -y);
				path = BuildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us 
				AddNodeToPath(path, x, -(y - 1));
				path = BuildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				AddNodeToPath(path, x + 1, -y);
				path = BuildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				AddNodeToPath(path, x, -(y + 1));
				path = BuildPath (path, startPoint, x, y+1, Direction.Down);
			}
		} else if (direction == Direction.Right) {
			if (tileUp) {
				//If there's a tile above us
				AddNodeToPath(path, x, -(y - 1));
				path = BuildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				AddNodeToPath(path, x + 1, -y);
				path = BuildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				AddNodeToPath(path, x, -(y + 1));
				path = BuildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				AddNodeToPath(path, x - 1, -y);
				path = BuildPath (path, startPoint, x - 1, y, Direction.Left);
			}
		} else if (direction == Direction.Down) {
			if (tileRight) {
				//if there's a tile to the right
				AddNodeToPath(path, x + 1, -y);
				path = BuildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				AddNodeToPath(path, x, -(y + 1));
				path = BuildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				AddNodeToPath(path, x - 1, -y);
				path = BuildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us
				AddNodeToPath(path, x, -(y - 1));
				path = BuildPath (path, startPoint, x, y - 1, Direction.Up);
			}
		} else if (direction == Direction.Left) {
			if (tileDown) {
				//If there's a tile below us
				AddNodeToPath(path, x, -(y + 1));
				path = BuildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				AddNodeToPath(path, x - 1, -y);
				path = BuildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us
				AddNodeToPath(path, x, -(y - 1));
				path = BuildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				AddNodeToPath(path, x + 1, -y);
				path = BuildPath (path, startPoint, x + 1, y, Direction.Right);
			}
		} 

		//Should never really end here;
		return path;
	}

	private void AddNodeToPath(List<Vector2> path, int x, int y) {
		Vector2 node = new Vector2 (x, y);
		path.Add (node);
	}



	/******************************** PATHFINDER CODE MAY NEED TO MOVE ********************************************/



	public TileType GetTile(int x, int y) 
	{
		if (x < 0 || x >= mWidth
			|| y < 0 || y >= mHeight)
			return TileType.Block;

		if (mapTiles [x, y] == null) {
			return TileType.Empty;
		}

		return mapTiles[x, y].getTileType(); 
	}

	public bool IsOneWayPlatform(int x, int y)
	{
		if (x < 0 || x >= mWidth
			|| y < 0 || y >= mHeight || mapTiles[x, y] == null)
			return false;

		return (mapTiles[x, y].getTileType() == TileType.OneWay);
	}

	public bool IsGround(int x, int y)
	{
		if (x < 0 || x >= mWidth
			|| y < 0 || y >= mHeight || mapTiles[x, y] == null)
			return false;

		return (mapTiles[x, y].getTileType() == TileType.OneWay || mapTiles[x, y].getTileType() == TileType.Block);
	}

	public bool IsObstacle(int x, int y)
	{
		if (x < 0 || x >= mWidth
			|| y < 0 || y >= mHeight)
			return true;

		if (mapTiles[x, y] == null) {
			return false;
		}

		return (mapTiles[x, y].getTileType() == TileType.Block);
	}

	public bool IsNotEmpty(int x, int y)
	{
		if (x < 0 || x >= mWidth
			|| y < 0 || y >= mHeight || mapTiles[x, y] == null)
			return true;

		return (mapTiles[x, y].getTileType() != TileType.Empty);
	}

	public void InitPathFinder()
	{
		mPathFinder = new PathFinderFast(this);

		mPathFinder.Formula                 = HeuristicFormula.Manhattan;
		//if false then diagonal movement will be prohibited
		mPathFinder.Diagonals               = false;
		//if true then diagonal movement will have higher cost
		mPathFinder.HeavyDiagonals          = false;
		//estimate of path length
		mPathFinder.HeuristicEstimate       = 6;
		mPathFinder.PunishChangeDirection   = false;
		mPathFinder.TieBreaker              = false;
		mPathFinder.SearchLimit             = 10000;
		mPathFinder.DebugProgress           = false;
		mPathFinder.DebugFoundPath          = false;
	}

	public void GetMapTileAtPoint(Vector2 point, out int tileIndexX, out int tileIndexY)
	{
		//position was originally assumed to be the worlds map position starting from bottom left corner. I don't know if what I'm doing will fix that
		tileIndexY =(int)((point.y - transform.position.y/* + cTileSize/2.0f*/)/(float)(cTileSize));
		tileIndexX =(int)((point.x - transform.position.x/* + cTileSize/2.0f*/)/(float)(cTileSize));
	}

	public Vector2i GetMapTileAtPoint(Vector2 point)
	{
		return new Vector2i ((int)(point.x - transform.position.x), (int)(point.y - transform.position.y));
//		return new Vector2i((int)((point.x - transform.position.x + cTileSize/2.0f)/(float)(cTileSize)),
//			(int)((point.y - transform.position.y + cTileSize/2.0f)/(float)(cTileSize)));
	}

	public Vector2 GetMapTilePosition(int tileIndexX, int tileIndexY)
	{
		return new Vector2(
			(float) (tileIndexX * cTileSize) + transform.position.x,
			(float) (tileIndexY * cTileSize) + transform.position.y
		);
	}

	public Vector2 GetMapTilePosition(Vector2i tileCoords)
	{
		return new Vector2(
			(float) (tileCoords.x * cTileSize) + transform.position.x,
			(float) (tileCoords.y * cTileSize) + transform.position.y
		);
	}

	public bool CollidesWithMapTile(AABB aabb, int tileIndexX, int tileIndexY)
	{
		var tilePos = GetMapTilePosition (tileIndexX, tileIndexY);

		return aabb.Overlaps(tilePos, new Vector2( (float)(cTileSize)/2.0f, (float)(cTileSize)/2.0f));
	}

	public bool AnySolidBlockInRectangle(Vector2 start, Vector2 end)
	{
		return AnySolidBlockInRectangle(GetMapTileAtPoint(start), GetMapTileAtPoint(end));
	}

	public bool AnySolidBlockInStripe(int x, int y0, int y1)
	{
		int startY, endY;

		if (y0 <= y1)
		{
			startY = y0;
			endY = y1;
		}
		else
		{
			startY = y1;
			endY = y0;
		}

		for (int y = startY; y <= endY; ++y)
		{
			if (GetTile(x, y) == TileType.Block)
				return true;
		}

		return false;
	}

	public bool AnySolidBlockInRectangle(Vector2i start, Vector2i end)
	{
		int startX, startY, endX, endY;

		if (start.x <= end.x)
		{
			startX = start.x;
			endX = end.x;
		}
		else
		{
			startX = end.x;
			endX = start.x;
		}

		if (start.y <= end.y)
		{
			startY = start.y;
			endY = end.y;
		}
		else
		{
			startY = end.y;
			endY = start.y;
		}

		for (int y = startY; y <= endY; ++y)
		{
			for (int x = startX; x <= endX; ++x)
			{
				if (GetTile(x, y) == TileType.Block)
					return true;
			}
		}

		return false;
	}

	protected void DrawPathLines(List<Vector2i> path) {
		if (path != null && path.Count > 0) {
			lineRenderer.enabled = true;
			lineRenderer.SetVertexCount (path.Count);
			lineRenderer.SetWidth (0.3f, 0.3f);

			for (var i = 0; i < path.Count; ++i) {
				lineRenderer.SetColors (Color.red, Color.red);
				//Commenting out tile size because I've manually made the tiles big
				lineRenderer.SetPosition (i, transform.position + new Vector3 (path [i].x/* * cTileSize*/, path [i].y/* * cTileSize*/, -5.0f));
			}
		} else {
			lineRenderer.enabled = false;
		}
	}

//	public void SetTile(int x, int y, TileType type)
//	{
//		if (x <= 1 || x >= mWidth - 2 || y <= 1 || y >= mHeight - 2)
//			return;
//
//		tiles[x, y] = type;
//
//		if (type == TileType.Block)
//		{
//			mGrid[x, y] = 0;
//			AutoTile(type, x, y, 1, 8, 4, 4, 4, 4);
//			tilesSprites[x, y].enabled = true;
//		}
//		else if (type == TileType.OneWay)
//		{
//			mGrid[x, y] = 1;
//			tilesSprites[x, y].enabled = true;
//
//			tilesSprites[x, y].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
//			tilesSprites[x, y].transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
//			tilesSprites[x, y].sprite = mDirtSprites[25];
//		}
//		else
//		{
//			mGrid[x, y] = 1;
//			tilesSprites[x, y].enabled = false;
//		}
//
//		AutoTile(type, x - 1, y, 1, 8, 4, 4, 4, 4);
//		AutoTile(type, x + 1, y, 1, 8, 4, 4, 4, 4);
//		AutoTile(type, x, y - 1, 1, 8, 4, 4, 4, 4);
//		AutoTile(type, x, y + 1, 1, 8, 4, 4, 4, 4);
//	}

	/*public void Start()
	{
		mRandomNumber = new System.Random();

		Application.targetFrameRate = 60;

		inputs = new bool[(int)KeyInput.Count];
		prevInputs = new bool[(int)KeyInput.Count];

		//set the position
		position = transform.position;

		mWidth = mapRoom.width;
		mHeight = mapRoom.height;

		tiles = new TileType[mWidth, mHeight];
		tilesSprites = new SpriteRenderer[mapRoom.width, mapRoom.height];

		mGrid = new byte[Mathf.NextPowerOfTwo((int)mWidth), Mathf.NextPowerOfTwo((int)mHeight)];
		InitPathFinder();

		Camera.main.orthographicSize = Camera.main.pixelHeight / 2;

		for (int y = 0; y < mHeight; ++y)
		{
			for (int x = 0; x < mWidth; ++x)
			{
				tilesSprites[x, y] = Instantiate<SpriteRenderer>(tilePrefab);
				tilesSprites[x, y].transform.parent = transform;
				tilesSprites[x, y].transform.position = position + new Vector3(cTileSize * x, cTileSize * y, 10.0f);

				SetTile(x, y, mapRoom.tileData[y * mWidth + x] == TileType.Empty ? TileType.Empty : TileType.Block);
			}
		}

		for (int y = 0; y < mHeight; ++y)
		{
			tiles[1, y] = TileType.Block;
			tiles[mWidth - 2, y] = TileType.Block;
		}

		for (int x = 0; x < mWidth; ++x)
		{
			tiles[x, 1] = TileType.Block;
			tiles[x, mHeight - 2] = TileType.Block;
		}

		/*for (int y = 2; y < mHeight - 2; ++y)
        {
            for (int x = 2; x < mWidth - 2; ++x)
            {
                if (y < mHeight/4)
                    SetTile(x, y, TileType.Block);
            }
        }*/

		/*player.BotInit(inputs, prevInputs);
		player.mMap = this;
		player.mPosition = new Vector2(2 * Map.cTileSize, (mHeight / 2) * Map.cTileSize + player.mAABB.HalfSizeY);
	}*/

//	void Update()
//	{
//		inputs[(int)KeyInput.GoRight] = Input.GetKey(goRightKey);
//		inputs[(int)KeyInput.GoLeft] = Input.GetKey(goLeftKey);
//		inputs[(int)KeyInput.GoDown] = Input.GetKey(goDownKey);
//		inputs[(int)KeyInput.Jump] = Input.GetKey(goJumpKey);
//
//		if (Input.GetKeyUp(KeyCode.Math))
//			lastMouseTileX = lastMouseTileY = -1;
//
//		Vector2 mousePos = Input.mousePosition;
//		Vector2 cameraPos = Camera.main.transform.position;
//		var mousePosInWorld = cameraPos + mousePos - new Vector2(gameCamera.pixelWidth / 2, gameCamera.pixelHeight / 2);
//
//		int mouseTileX, mouseTileY;
//		GetMapTileAtPoint(mousePosInWorld, out mouseTileX, out mouseTileY);
//
//		if (Input.GetKeyDown(KeyCode.Math))
//		{
//			player.TappedOnTile(new Vector2i(mouseTileX, mouseTileY));
//		}
//
//		if (Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.Mouse2))
//		{
//			if (mouseTileX != lastMouseTileX || mouseTileY != lastMouseTileY || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Mouse2))
//			{
//				if (!IsNotEmpty(mouseTileX, mouseTileY))
//					SetTile(mouseTileX, mouseTileY, TileType.Block );
//				else
//					SetTile(mouseTileX, mouseTileY, TileType.Empty);
//
//				lastMouseTileX = mouseTileX;
//				lastMouseTileY = mouseTileY;
//			}
//		}
//	}

//	System.Random mRandomNumber;
//
//	void AutoTile(TileType type, int x, int y, int rand4NeighbourTiles, int rand3NeighbourTiles,
//		int rand2NeighbourPipeTiles, int rand2NeighbourCornerTiles, int rand1NeighbourTiles, int rand0NeighbourTiles)
//	{
//		if (x >= mWidth || x < 0 || y >= mHeight || y < 0)
//			return;
//
//		if (tiles[x, y] != TileType.Block)
//			return;
//
//		int tileOnLeft = tiles[x - 1, y] == tiles[x, y] ? 1 : 0;
//		int tileOnRight = tiles[x + 1, y] == tiles[x, y] ? 1 : 0;
//		int tileOnTop = tiles[x, y + 1] == tiles[x, y] ? 1 : 0;
//		int tileOnBottom = tiles[x, y - 1] == tiles[x, y] ? 1 : 0;
//
//		float scaleX = 1.0f;
//		float scaleY = 1.0f;
//		float rot = 0.0f;
//		int id = 0;
//
//		int sum = tileOnLeft + tileOnRight + tileOnTop + tileOnBottom;
//
//		switch (sum)
//		{
//		case 0:
//			id = 1 + mRandomNumber.Next(rand0NeighbourTiles);
//
//			break;
//		case 1:
//			id = 1 + rand0NeighbourTiles + mRandomNumber.Next(rand1NeighbourTiles);
//
//			if (tileOnRight == 1)
//				scaleX = -1;
//			else if (tileOnTop == 1)
//				rot = -1;
//			else if (tileOnBottom == 1)
//			{
//				rot = 1;
//				scaleY = -1;
//			}
//
//			break;
//		case 2:
//
//			if (tileOnLeft + tileOnBottom == 2)
//			{
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//					+ mRandomNumber.Next(rand2NeighbourCornerTiles);
//			}
//			else if (tileOnRight + tileOnBottom == 2)
//			{
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//					+ mRandomNumber.Next(rand2NeighbourCornerTiles);
//				scaleX = -1;
//			}
//			else if (tileOnTop + tileOnLeft == 2)
//			{
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//					+ mRandomNumber.Next(rand2NeighbourCornerTiles);
//				scaleY = -1;
//			}
//			else if (tileOnTop + tileOnRight == 2)
//			{
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//					+ mRandomNumber.Next(rand2NeighbourCornerTiles);
//				scaleX = -1;
//				scaleY = -1;
//			}
//			else if (tileOnTop + tileOnBottom == 2)
//			{
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + mRandomNumber.Next(rand2NeighbourPipeTiles);
//				rot = 1;
//			}
//			else if (tileOnRight + tileOnLeft == 2)
//				id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + mRandomNumber.Next(rand2NeighbourPipeTiles);
//
//			break;
//		case 3:
//			id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//				+ rand2NeighbourCornerTiles + mRandomNumber.Next(rand3NeighbourTiles);
//
//			if (tileOnLeft == 0)
//			{
//				rot = 1;
//				scaleX = -1;
//			}
//			else if (tileOnRight == 0)
//			{
//				rot = 1;
//				scaleY = -1;
//			}
//			else if (tileOnBottom == 0)
//				scaleY = -1;
//
//			break;
//
//		case 4:
//			id = 1 + rand0NeighbourTiles + rand1NeighbourTiles + rand2NeighbourPipeTiles
//				+ rand2NeighbourCornerTiles + rand3NeighbourTiles + mRandomNumber.Next(rand4NeighbourTiles);
//
//			break;
//		}
//
//		tilesSprites[x, y].transform.localScale = new Vector3(scaleX, scaleY, 1.0f);
//		tilesSprites[x, y].transform.eulerAngles = new Vector3(0.0f, 0.0f, rot * 90.0f);
//		tilesSprites[x, y].sprite = mDirtSprites[id - 1];
//	}

}
