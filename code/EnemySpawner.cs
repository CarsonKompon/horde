using System;
using System.Collections.Generic;
using Sandbox;

public sealed class EnemySpawner : Component
{
	[Property, Sync] public int MaxEnemies { get; set; } = 1;
	[Property, Sync] public float MinRespawnTime { get; set; } = 50f;
	[Property, Sync] public float MaxRespawnTime { get; set; } = 100f;
	[Property] List<GameObject> Prefabs { get; set; }

	float timer = 0;
	float respawnTime = 10f;
	List<GameObject> spawned = new();
	int spawnCount = 0;
	int goalCount = 0;

	int Increment = 15;
	int GoalIncrement = 4;
	float Stunt = 0f;

	protected override void OnStart()
	{
		MaxEnemies = (int)MathF.Ceiling( Random.Shared.Float( 0.01f, 2.025f ) );
		respawnTime = Random.Shared.Float( MinRespawnTime, MaxRespawnTime );
		timer = Random.Shared.Float( 0f, respawnTime );
		Increment = Random.Shared.Int( 10, 15 );
		GoalIncrement = Random.Shared.Int( 2, 4 );
		Stunt = Random.Shared.Float();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

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
		goalCount++;
		if ( goalCount % GoalIncrement == 0 )
		{
			MinRespawnTime -= 0.5f;
			MaxRespawnTime -= 0.5f;
		}
		var inc = (Increment * MaxEnemies);
		if ( MaxEnemies <= 2 ) inc = 10;
		if ( goalCount >= inc )
		{
			MaxEnemies++;
			goalCount = 0;
			MinRespawnTime = 10 - Stunt * MaxEnemies;
			MaxRespawnTime = 20 - Stunt * MaxEnemies;
		}
		MinRespawnTime = Math.Max( MinRespawnTime, 2f );
		MaxRespawnTime = Math.Max( MaxRespawnTime, 5f );


		for ( int i = 0; i < spawned.Count; i++ )
		{
			if ( !spawned[i].IsValid() )
			{
				spawned.RemoveAt( i );
				i--;
			}
		}

		if ( spawned.Count >= MaxEnemies ) return;
		var prefab = Prefabs[Random.Shared.Next( Prefabs.Count )];
		var newSpawn = prefab.Clone( WorldPosition );
		newSpawn.NetworkSpawn( null );
		spawned.Add( newSpawn );
		spawnCount++;
	}
}