using UnityEngine;
using System.Collections;

public class Rocket : MonoBehaviour, IProjectile {

	public GameObject explosionEffect;

	public int damage = 20;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void OnMapTileHit (MapTile mapTile) {
		Level level = mapTile.level;
		//Start destroying a radius
		DestroyMapTileRadius(level, mapTile.x, mapTile.y, 8);

		//refresh colliders
		level.refreshCollidersOnOuterTiles();

		Instantiate (explosionEffect, mapTile.transform.position, Quaternion.identity);
		//Destory the projectile
		Destroy (this.gameObject);
	}

	public void OnActorHit() {
		// DO damage to them or something
	}

	public void DestroyMapTileArea(int x, int y, Level level, int radius) {
		if (level.mapTiles[x,y] != null) {
			level.mapTiles[x,y].DestroyTile ();
		}
		if (radius > 0) {
			if (y > 0) {
				DestroyMapTileArea (x, y - 1, level, radius - 1);
			}
			if (x < level.mapTiles.GetLength (0) - 1) {
				DestroyMapTileArea (x + 1, y, level, radius - 1);
			}
			if (y < level.mapTiles.GetLength (1) - 1) {
				DestroyMapTileArea (x, y + 1, level, radius - 1);
			}
			if (x > 0) {
				DestroyMapTileArea (x - 1, y, level, radius - 1);
			}
		}
	}

	public void DestroyMapTileRadiusAlgorithm(Level level, int centerX, int centerY, int radius)
	{
		//This is an implementation of Midpoint Circle Algorithm here: http://rosettacode.org/wiki/Bitmap/Midpoint_circle_algorithm
		//It doesn't effectively fill in the circle, Many other solutions to do that are here: http://stackoverflow.com/questions/10878209/midpoint-circle-algorithm-for-filled-circles

		if (radius == 0) {
			if (level.mapTiles [centerX, centerY] != null) {
				level.mapTiles [centerX, centerY].DestroyTile ();
			}
			return;
		}

		int d = (5 - radius * 4) / 4;
		int x = 0;
		int y = radius;

		do
		{
			// ensure index is in range before setting (depends on your image implementation)
			// in this case we check if the pixel location is within the bounds of the image before setting the pixel

			if ((centerX + x < level.mapTiles.GetLength (0) - 1) && (centerY + y < level.mapTiles.GetLength (1) - 1)) {
				if (level.mapTiles[centerX + x,centerY + y] != null) {
					level.mapTiles[centerX + x,centerY + y].DestroyTile ();
				}
			}
			if ((centerX + x < level.mapTiles.GetLength (0) - 1) && (centerY - y > 0)) {
				if (level.mapTiles[centerX + x,centerY - y] != null) {
					level.mapTiles[centerX + x,centerY - y].DestroyTile ();
				}
			}
			if ((centerX - x > 0) && (centerY + y < level.mapTiles.GetLength (1) - 1)) {
				if (level.mapTiles[centerX - x,centerY + y] != null) {
					level.mapTiles[centerX - x,centerY + y].DestroyTile ();
				}
			}
			if ((centerX - x > 0) && (centerY - y > 0)) {
				if (level.mapTiles[centerX - x,centerY - y] != null) {
					level.mapTiles[centerX - x,centerY - y].DestroyTile ();
				}
			}
			if ((centerX + y < level.mapTiles.GetLength (0) - 1) && (centerY + x < level.mapTiles.GetLength (1) - 1)) {
				if (level.mapTiles[centerX + y,centerY + x] != null) {
					level.mapTiles[centerX + y,centerY + x].DestroyTile ();
				}
			}
			if ((centerX + y < level.mapTiles.GetLength (0) - 1) && (centerY - x > 0)) {
				if (level.mapTiles[centerX + y,centerY - x] != null) {
					level.mapTiles[centerX + y,centerY - x].DestroyTile ();
				}
			}
			if ((centerX - y > 0) && (centerY + x < level.mapTiles.GetLength (1) - 1)) {
				if (level.mapTiles[centerX - y,centerY + x] != null) {
					level.mapTiles[centerX - y,centerY + x].DestroyTile ();
				}
			}
			if ((centerX - y > 0) && (centerY - x > 0)) {
				if (level.mapTiles[centerX - y,centerY - x] != null) {
					level.mapTiles[centerX - y,centerY - x].DestroyTile ();
				}
			}

			if (d < 0)
			{
				d += 2 * x + 1;
			}
			else
			{
				d += 2 * (x - y) + 1;
				y--;
			}
			x++;
		} while (x <= y);

		DestroyMapTileRadius (level, centerX, centerY, radius - 1);
	}

	public void DestroyMapTileRadius(Level level, int centerX, int centerY, int radius) {
		//This is a more brute force algorithm. Would want to figure out distance to center for damage fall off
		for (int y = -radius; y <= radius; y++) {
			for (int x = -radius; x <= radius; x++) {
				if ((x * x + y * y <= radius * radius) && centerX + x >= 0 && centerX + x < level.mapTiles.GetLength (0) - 1 && centerY + y >= 0 && centerY + y < level.mapTiles.GetLength (1) - 1) {
					if (level.mapTiles[centerX + x,centerY + y] != null) {
						float dmgMod = 1 - ((float)(x * x + y * y) / (float)(radius*radius));
						int damageToTile = Mathf.RoundToInt(damage * dmgMod);
						level.mapTiles [centerX + x, centerY + y].TakeDamage (damageToTile);
					}
				}
			}
		}
	}
}
