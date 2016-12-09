using UnityEngine;
using System.Collections;

public class MapTile : MonoBehaviour {

	//Map coordinates in Level MapTile Array
	public int x;
	public int y;

	public Level level;

	public int maxHP = 10;
	public int HP = 10;

	public Sprite undamagedSprite;
	public Sprite dingedSprite;
	public Sprite damagedSprite;

	protected SpriteRenderer renderer;

	private TileType tileType = TileType.Block;

	// Use this for initialization
	public void Start () {
		renderer = this.GetComponent<SpriteRenderer> ();
		renderer.sprite = undamagedSprite;
	}

	// Update is called once per frame
	public void Update () {

	}

	public virtual void TestInheritance() {
		int i = 0;
	}

	public void Instantiate(int x, int y, Transform parent, Level level) {
		this.x = x;
		this.y = y;
		this.transform.parent = parent;
		this.level = level;
	}
		

	void OnCollisionEnter2D(Collision2D col) {
		//This isn't calling for some reason
		//Test for projectile collision
		IProjectile projectile = (IProjectile)col.gameObject.GetComponent (typeof(IProjectile));
		if (projectile != null) {
			projectile.OnMapTileHit(this);
		}
		//Check for player collision, blow him up and he loses or whatever
//		if (col.gameObject.tag == "Player")
//		{
//			ship = col.gameObject.GetComponent<Ship>();
//			ship.takeHit();
//			kill();
//
//		}
	}

	public void DestroyTile() {
		//remove itself from the maptile array
		this.level.mapTiles [x, y] = null;
		Destroy (this.gameObject);
	}

	public void TakeDamage(int damage) {
		HP = HP - damage;
		if (HP <= 0) {
			DestroyTile ();
		} else {
			float healthPercentage = ((float)HP / (float)maxHP) * 100;
			if (healthPercentage > 66) {
				renderer.sprite = undamagedSprite;
			} else if (healthPercentage > 33) {
				renderer.sprite = dingedSprite;
			} else {
				renderer.sprite = damagedSprite;
			}
		}
	}

	public bool CheckTileUp() {
		return y > 0 && level.mapTiles [x, y - 1] != null;
	}

	public bool CheckTileRight() {
		return x < level.mapTiles.GetLength(0)-1 && level.mapTiles [x + 1, y] != null;
	}

	public bool CheckTileDown() {
		return y < level.mapTiles.GetLength(1)-1 && level.mapTiles [x, y + 1] != null;
	}

	public bool CheckTileLeft() {
		return x > 0 && level.mapTiles [x - 1, y] != null;
	}

	public bool IsOuterTile() {
		return !CheckTileUp () || !CheckTileRight() || !CheckTileDown() || !CheckTileLeft();
	}

	public TileType getTileType() {
		return tileType;
	}
}
