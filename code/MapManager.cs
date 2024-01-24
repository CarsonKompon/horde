using System;
using System.Linq;
using Sandbox;

public sealed class MapManager : Component
{
	public static MapManager Instance { get; private set; }

	[Property] MapInstance Map { get; set; }
	[Property] GameObject EnemySpawnerPrefab { get; set; }

	protected override void OnStart()
	{
		Map.OnMapLoaded += OnMapLoaded;

		if ( Map.IsLoaded )
		{
			OnMapLoaded();
		}

		Instance = this;
	}

	public void OnMapLoaded()
	{
		if ( !Networking.IsHost ) return;

		Scene.Title = Map.MapName;

		bool createdSpawns = false;

		foreach ( var obj in Map.GameObject.Children )
		{

			if ( obj.Name == "sd_weaponspawn_random" || obj.Name.StartsWith( "tf_healthkit" ) || obj.Name.StartsWith( "tf_ammopack" ) )
			{
				var weaponSpawner = EnemySpawnerPrefab.Clone( obj.Transform.Position );
				weaponSpawner.NetworkSpawn( null );
				createdSpawns = true;
			}
		}

		if ( !createdSpawns )
		{
			foreach ( var obj in Map.GameObject.Children )
			{

				if ( obj.Name == "info_player_start" )
				{
					var weaponSpawner = EnemySpawnerPrefab.Clone( obj.Transform.Position );
					weaponSpawner.NetworkSpawn( null );
				}
			}
		}

		var spawnPoints = Scene.Components.GetAll<SpawnPoint>().OrderBy( x => new Guid() ).ToList();
		var spawnIndex = 0;
		foreach ( var player in Scene.Components.GetAll<Player>() )
		{
			player.CharacterController.Velocity = Vector3.Zero;
			player.Transform.Position = spawnPoints[spawnIndex].Transform.Position + Vector3.Up * 4f;
			spawnIndex = (spawnIndex + 1) % spawnPoints.Count;
		}
	}
}