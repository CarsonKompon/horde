using System;
using System.Collections.Generic;
using Sandbox;

public sealed class PrefabSpawner : Component
{
	[Property] public float MinRespawnTime { get; set; } = 50f;
	[Property] public float MaxRespawnTime { get; set; } = 100f;
	[Property] List<GameObject> Prefabs { get; set; }

	float timer = 0;
	float respawnTime = 10f;
	GameObject lastSpawned = null;

	protected override void OnStart()
	{
		respawnTime = Random.Shared.Float( MinRespawnTime, MaxRespawnTime );
		timer = Random.Shared.Float( 0f, respawnTime );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;

		timer += Time.Delta;
		if ( timer > MaxRespawnTime )
		{
			timer = 0;
			respawnTime = Random.Shared.Float( MinRespawnTime, MaxRespawnTime );
			SpawnPrefab();
		}
	}

	void SpawnPrefab()
	{
		if ( lastSpawned.IsValid() ) return;
		var prefab = Prefabs[Random.Shared.Next( Prefabs.Count )];
		lastSpawned = prefab.Clone( Transform.Position );
		lastSpawned.NetworkSpawn( null );
	}
}