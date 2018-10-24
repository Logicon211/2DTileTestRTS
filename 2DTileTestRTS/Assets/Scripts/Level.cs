using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;
using Algorithms;
using CreativeSpore.SuperTilemapEditor;

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

	public Texture2D selectionTexture;
	public LayerMask playerLayerMask;
	private LineRenderer lineRenderer;

	private bool dragging = false;
	private Vector2 dragStartPosition;
	private Vector2 dragCurrentPosition;

	//Tilemap editor references
	public STETilemap tilemap;

	void Awake() {
		mainLevel = this;
	}

	// Use this for initialization
	void Start () {
		lineRenderer = GetComponent<LineRenderer> ();
		if(loadAtStartUp) {
			//Destroy all existing Children and reload
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
					//Set the clicked position to one tile above the ground (over where you clicked)
					clickPosition = new Vector2(tile.x , tile.y + 1);
					foundClick = true;
				}
			}

			Vector2 player2DPosition = new Vector2 (player.transform.position.x, player.transform.position.y);
			RaycastHit2D playerHit = Physics2D.Raycast(player2DPosition, new Vector2(0, -1), Mathf.Infinity, playerLayerMask);

			//If we found the clicked area, get the start and endpoints and Move there.
			if (foundClick/* && foundPlayer*/) {
				Vector2i start = new Vector2i(Convert.ToInt32(playerPosition.x), Convert.ToInt32(playerPosition.y));
				Vector2i end = new Vector2i(Convert.ToInt32(clickPosition.x), Convert.ToInt32(clickPosition.y));
				player.GetComponent<Unit>().MoveTo(end);
			}
		}

		//Left Mouse Button Down
		if (Input.GetMouseButtonDown (0)) {
			dragging = true;
			//Vector3 mouseScreenPosition = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
			//dragStartPosition = new Vector2(mouseScreenPosition.x, mouseScreenPosition.y);
			dragStartPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			dragCurrentPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		}

		if (Input.GetMouseButton (0)) {
			dragCurrentPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		}

		if (Input.GetMouseButtonUp (0)) {
			dragging = false;
		}
	}

	void FixedUpdate() {
		//PATHFINDER STUFF
		//player.BotUpdate();
	}

	void OnGUI()
	{
		if(dragging)
		{
			// Create a rectangle object out of the start and end position while transforming it
			// to the screen's cordinates.
			var rect = new Rect(dragStartPosition.x, Screen.height - dragStartPosition.y ,dragCurrentPosition.x - dragStartPosition.x, -1 * (dragCurrentPosition.y - dragStartPosition.y));
			// Draw the texture.
			GUI.DrawTexture(rect, selectionTexture);
		}
	}

	//Singleton implementation, Should probably check if this is how you do it in Unity
	public static Level getLevel() {
		if (mainLevel == null) {
			mainLevel = new Level ();
		}
		return mainLevel;
	}

	public void InstantiateLevel() {

		//Tilemap MUST enforce a non-negative index grid, and size of height and width need to be a power of 2
		//Tilemap must be at map coordinate x = -0.5, and y = -0.5 so that the offset allows the pathfinder to place nodes in the middle of tiles and not in the top left corner
		mWidth = tilemap.GridWidth;
		mHeight = tilemap.GridHeight;
		mapTiles = new MapTile[mWidth,mHeight];

		for(int x=0; x < mWidth; x++) {
			for(int y=0; y < mHeight; y++) {
				//Null check?
				GameObject tileObject = tilemap.GetTileObject(x, y);
				if(tileObject) {
					if(tileObject.GetComponent<MapTile>() != null) {
						MapTile tile = tileObject.GetComponent<MapTile>();
						tile.Instantiate (x, y, tile.transform, this);
						mapTiles[x, y] = tile;
					} else if (tileObject.GetComponent<Building>() != null) {
						Debug.Log("BUILDING TILE");
						//TODO: instantiate the building in some way, maybe destroy it and recreate it at the spot?
						//TODO: use the building's height and width to determine which map naps nodes it occupies starting from the bottom right corner of it
					}
				}
			}
		}

		refreshCollidersOnOuterTiles ();

		//**************************************************************************************** */

		// XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		// xmlDoc.LoadXml(levelXml.text);
		// XmlNode levelNode = xmlDoc.FirstChild;
		// XmlNodeList levelsList = xmlDoc.GetElementsByTagName("level"); // array of the level nodes.

		// mWidth = int.Parse(levelNode.Attributes ["width"].Value);
		// mHeight = int.Parse(levelNode.Attributes ["height"].Value);
		// mapTiles = new MapTile[mWidth,mHeight];
		// foreach(XmlNode levelInfo in levelsList) {
		// 	XmlNodeList levelContent = levelInfo.ChildNodes;
			
		// 	foreach(XmlNode levelItems in levelContent) {
		// 		if(levelItems.Name == "Tiles") {
		// 			//get Attribuites for level
		// 			string tilesetName = levelItems.Attributes["tileset"].Value;
					
		// 			foreach (XmlNode levelTile in levelItems.ChildNodes) {
		// 				if(levelTile.Name == "tile") {
							
		// 					int tileX = int.Parse(levelTile.Attributes["x"].Value);
		// 					//-y values because OGMO's axis starts in the upper left and not lower left.
		// 					int tileY = mHeight - int.Parse(levelTile.Attributes["y"].Value);
		// 					int id = int.Parse(levelTile.Attributes["id"].Value);


		// 					//convert these to cases?
		// 					//More possible tiles
		// 					//Note, in order to use Resources.load, the prefab needs to be in the Resources folder
		// 					MapTile tile = null;


		// 					//Ground Tile
		// 					if (id == 0) {
		// 						tile = (Instantiate(groundTilePrefab, new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject).GetComponent<MapTile> ();
		// 						tile.Instantiate (tileX, tileY, transform, this);
		// 					} 
		// 					//More tiles to check for in here

		// 					mapTiles [tileX,tileY] = tile;	
		// 				}
		// 			}
		// 		}
				
		// 		if(levelItems.Name == "Entities") {
		// 			foreach(XmlNode levelEntities in levelItems) {
		// 				//Do something with entities
		// 				//obj.Add ("entities", levelEntities.InnerXml);
		// 			}
		// 		}

		// 		refreshCollidersOnOuterTiles ();
		// 		//An alternative method to generate 1 collider over the entire map is unfinished in the UnfinishedFunctions.txt
		// 		//This method generates 1 collider over the entire map

		// 	}
		// }
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

		//EuclideanNoSQR seemed to work the best, although it sometimes provides a little hop off ledges that is unneeded
		mPathFinder.Formula                 = HeuristicFormula.EuclideanNoSQR;
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
}
