using UnityEngine;
using System.Collections;

public class MapTile : MonoBehaviour {

	//Map coordinates in Level MapTile Array
	public int x;
	public int y;

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	public virtual void TestInheritance() {
		int i = 0;
	}

	public void Instantiate(int x, int y, Transform parent) {
		this.x = x;
		this.y = y;
		this.transform.parent = parent;
	}

}
