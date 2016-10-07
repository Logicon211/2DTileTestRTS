using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

public class LoadLevel : MonoBehaviour {

	public TextAsset levelXml;
	public bool loadAtStartUp;
	public GameObject[,] mapTiles;

	public enum Direction {Up, Right, Down, Left};

	// Use this for initialization
	void Start () {
		if(loadAtStartUp) {
			//Destroy all existing Children and reload
			ClearLevel();
			InstantiateLevel();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
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

		int width = int.Parse(levelNode.Attributes ["width"].Value);
		int height = int.Parse(levelNode.Attributes ["height"].Value);
		mapTiles = new GameObject[width,height];
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
							int tileY = -int.Parse(levelTile.Attributes["y"].Value);
							int id = int.Parse(levelTile.Attributes["id"].Value);


							//convert these to cases?
							//More possible tiles
							//Note, in order to use Resources.load, the prefab needs to be in the Resources folder
							GameObject tile = null;

							if (id == 0) {
								tile = Instantiate(Resources.Load("BlackParticleTile"), new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject;
								tile.transform.parent = transform;
							} else if(id == 7) {
								tile = Instantiate(Resources.Load("BasicTile"), new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject;
								tile.transform.parent = transform;
							} else if(id == 20) {
								tile = Instantiate(Resources.Load("GrassTile"), new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject;
								tile.transform.parent = transform;
							} else if(id == 30) {
								tile = Instantiate(Resources.Load("RoundedTile"), new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation) as GameObject;
								tile.transform.parent = transform;
							}
							mapTiles [tileX,-tileY] = tile;	
						}
					}
				}
				
				if(levelItems.Name == "Entities") {
					foreach(XmlNode levelEntities in levelItems) {
						//Do something with entities
						//obj.Add ("entities", levelEntities.InnerXml);
					}
				}

				gameObject.AddComponent<PolygonCollider2D>(); //collider for itself
				PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();

				List<Vector2> path = new List<Vector2> ();
				//Make collider?
				//simple solution to draw a collider around the first block we encounter. Will need to handle breaks and more complete maps
				int x = 0;
				int y = 0;
				for (x = 0; x < mapTiles.GetLength (0); x++) {
					bool breakLoop = false;
					for (y = 0; y < mapTiles.GetLength (1); y++) {
						if(mapTiles[x,y] != null) {
							breakLoop = true;
							break;
							//we should have coordinates of first tile hit
						}
						//	path.Add (node);
						//	Vector2 node = new Vector2(x,-y);
					}
					if (breakLoop) {
						break;
					}
				}
				Vector2 startingNode = new Vector2 (x, -y);
				path.Add(startingNode);
				path = buildPath(path, startingNode, x, y, Direction.Up);
				collider.SetPath(0, path.ToArray());
				//This example sets up 2 paths, 2 boxes
//				Vector2 node = new Vector2(0,0);
//				path.Add (node);
//				node = new Vector2(1,0);
//				path.Add (node);
//				node = new Vector2(1,-1);
//				path.Add (node);
//				node = new Vector2(0,-1);
//				path.Add (node);
//				node = new Vector2(0,0);
//				path.Add (node);
//
//				List<Vector2> path2 = new List<Vector2> ();
//   				node = new Vector2(4,0);
//				path2.Add (node);
//				node = new Vector2(5,0);
//				path2.Add (node);
//				node = new Vector2(5,-1);
//				path2.Add (node);
//				node = new Vector2(4,-1);
//				path2.Add (node);
//				node = new Vector2(4,0);
//				path2.Add (node);
//
//				collider.pathCount = 2;
//				collider.SetPath(0, path.ToArray());
//				collider.SetPath(1, path2.ToArray());
			}
		}
	}

	public List<Vector2> buildPath(List<Vector2> path, Vector2 startPoint, int x, int y, Direction direction) {
		//Assuming the coordinates passed into this function equates to a non-null map tile
		//Will try to traverse the tiles clockwise (up - right - down - left)

		//currently putting path nodes for a polygon collider on the center of each node
		//Next will just try enabling collision boxes on just the edges
		//Also need to be able to put edges around multiple blocks of tiles, and on the inner empty spaces

		bool tileUp = checkTileUp (x, y);
		bool tileRight = checkTileRight (x, y);
		bool tileDown = checkTileDown (x, y);
		bool tileLeft = checkTileLeft(x, y);

		if (startPoint.Equals (new Vector2 (x, -y)) && path.Count > 1) {
			//We're back to the beginning, return the path
			return path;
		}
		if (direction == Direction.Up) {
			if (tileLeft) {
				//If the tile to the left
				addNodeToPath(path, x - 1, -y);
				path = buildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us 
				addNodeToPath(path, x, -(y - 1));
				path = buildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				addNodeToPath(path, x + 1, -y);
				path = buildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				addNodeToPath(path, x, -(y + 1));
				path = buildPath (path, startPoint, x, y+1, Direction.Down);
			}
		} else if (direction == Direction.Right) {
			if (tileUp) {
				//If there's a tile above us
				addNodeToPath(path, x, -(y - 1));
				path = buildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				addNodeToPath(path, x + 1, -y);
				path = buildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				addNodeToPath(path, x, -(y + 1));
				path = buildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				addNodeToPath(path, x - 1, -y);
				path = buildPath (path, startPoint, x - 1, y, Direction.Left);
			}
		} else if (direction == Direction.Down) {
			if (tileRight) {
				//if there's a tile to the right
				addNodeToPath(path, x + 1, -y);
				path = buildPath (path, startPoint, x + 1, y, Direction.Right);
			} else if (tileDown) {
				//If there's a tile below us
				addNodeToPath(path, x, -(y + 1));
				path = buildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				addNodeToPath(path, x - 1, -y);
				path = buildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us
				addNodeToPath(path, x, -(y - 1));
				path = buildPath (path, startPoint, x, y - 1, Direction.Up);
			}
		} else if (direction == Direction.Left) {
			if (tileDown) {
				//If there's a tile below us
				addNodeToPath(path, x, -(y + 1));
				path = buildPath (path, startPoint, x, y+1, Direction.Down);
			} else if (tileLeft) {
				//If the tile to the left
				addNodeToPath(path, x - 1, -y);
				path = buildPath (path, startPoint, x - 1, y, Direction.Left);
			} else if (tileUp) {
				//If there's a tile above us
				addNodeToPath(path, x, -(y - 1));
				path = buildPath (path, startPoint, x, y - 1, Direction.Up);
			} else if (tileRight) {
				//if there's a tile to the right
				addNodeToPath(path, x + 1, -y);
				path = buildPath (path, startPoint, x + 1, y, Direction.Right);
			}
		} 

		//Should never really end here;
		return path;
	}

	private void addNodeToPath(List<Vector2> path, int x, int y) {
		Vector2 node = new Vector2 (x, y);
		path.Add (node);
	}

	private bool checkTileUp(int x, int y) {
		return y > 0 && mapTiles [x, y - 1] != null;
	}

	private bool checkTileRight(int x, int y) {
		return x < mapTiles.GetLength(0)-1 && mapTiles [x + 1, y] != null;
	}

	private bool checkTileDown(int x, int y) {
		return y < mapTiles.GetLength(1)-1 && mapTiles [x, y + 1] != null;
	}

	private bool checkTileLeft(int x, int y) {
		return x > 0 && mapTiles [x - 1, y] != null;
	}
}
