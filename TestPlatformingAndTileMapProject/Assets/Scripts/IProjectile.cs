using UnityEngine;
using System.Collections;

public interface IProjectile
{
	void OnMapTileHit (MapTile mapTile);
	void OnActorHit();
}


