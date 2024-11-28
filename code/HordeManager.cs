using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed class HordeManager : Component
{
	public static HordeManager Instance { get; private set; }

	[Property] public GameObject DamageNumberPrefab { get; set; }
	[Property] public GameObject BloodBurstPrefab { get; set; }
	[Property] public List<GameObject> WeaponPrefabs { get; set; }

	[Sync] public bool InGame { get; set; } = true;
	public bool IsStarting { get; set; } = true;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( InGame )
		{
			var living = false;
			foreach ( var player in Scene.Components.GetAll<Player>() )
			{
				if ( player.Health > 0 )
				{
					living = true;
					break;
				}
			}

			if ( !living && !IsStarting )
			{
				InGame = false;
			}
		}
	}

	public void ResetGame()
	{
		if ( !Networking.IsHost ) return;

		var players = Scene.Components.GetAll<Player>().ToList();
		for ( int i = players.Count - 1; i >= 0; i-- )
		{
			var player = players[i];
			player.Respawn( false );
			player.Health = 100f;
			player.Kills = 0;
		}

		// Destroy all Enemy objects
		var enemies = Scene.Components.GetAll<Enemy>().ToList();
		for ( int i = enemies.Count - 1; i >= 0; i-- )
		{
			var enemy = enemies[i];
			enemies.RemoveAt( i );
			enemy.GameObject.Destroy();
		}

		// Destroy all EnemySpawner objects
		var spawners = Scene.Components.GetAll<EnemySpawner>().ToList();
		for ( int i = spawners.Count - 1; i >= 0; i-- )
		{
			var spawner = spawners[i];
			spawners.RemoveAt( i );
			spawner.GameObject.Destroy();
		}

		// Destroy all Pickup objects
		var pickups = Scene.Components.GetAll<Pickup>().ToList();
		for ( int i = pickups.Count - 1; i >= 0; i-- )
		{
			var pickup = pickups[i];
			pickups.RemoveAt( i );
			pickup.GameObject.Destroy();
		}

		MapManager.Instance.OnMapLoaded();

		InGame = true;
	}
}