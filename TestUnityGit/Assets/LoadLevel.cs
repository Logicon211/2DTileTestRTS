using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

public class LoadLevel : MonoBehaviour {

	public TextAsset levelXml;
	public GameObject[] tilePrefabs;

	public int gridSize = 16;

	Dictionary<string,string> obj;

	// Use this for initialization
	void Start () {
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		xmlDoc.LoadXml(levelXml.text);
		XmlNodeList levelsList = xmlDoc.GetElementsByTagName("level"); // array of the level nodes.
		List<Vector2> tileList = new List<Vector2>();

		foreach(XmlNode levelInfo in levelsList) {
			XmlNodeList levelContent = levelInfo.ChildNodes;
			obj = new Dictionary<string,string>(); // Create a object(Dictionary) to collect the both nodes inside the level node and then

			foreach(XmlNode levelItems in levelContent) {
				if(levelItems.Name == "Tiles") {

					string tilesetName = levelItems.Attributes["tileset"].Value;

					foreach (XmlNode levelTile in levelItems.ChildNodes) {
						if(levelTile.Name == "tile") {
							int tileX = int.Parse(levelTile.Attributes["x"].Value);
							//-y values because OGMO's axis starts in the upper left and not lower left.
							int tileY = -int.Parse(levelTile.Attributes["y"].Value);
							int id = int.Parse(levelTile.Attributes["id"].Value);
							//Get individual child info?

							//convert these to cases?
							//This also doesn't seem to get pixel perfect position
							if(id == 7) {
								Instantiate(tilePrefabs[0], new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation);
							} else if(id == 20) {
								Instantiate(tilePrefabs[1], new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation);
							} else if(id == 30) {
								Instantiate(tilePrefabs[2], new Vector3(transform.position.x +(tileX), transform.position.y +(tileY), 0), transform.rotation);
							}

							tileList.Add(new Vector2(tileX, tileY));
                   		}
					}
				}

				if(levelItems.Name == "Entities") {
					foreach(XmlNode levelEntities in levelItems) {
						//Do something with entities
						//obj.Add ("entities", levelEntities.InnerXml);
					}
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
