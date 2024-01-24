using Sandbox;

public sealed class MapManager : Component
{
	[Property] MapInstance Map { get; set; }
	[Property] GameObject EnemySpawnerPrefab { get; set; }

	protected override void OnStart()
	{
		Map.OnMapLoaded += OnMapLoaded;

		if ( Map.IsLoaded )
		{
			OnMapLoaded();
		}
	}

	void OnMapLoaded()
	{

		foreach ( var obj in Map.GameObject.Children )
		{

			if ( obj.Name == "sd_weaponspawn_random" )
			{
				var weaponSpawner = EnemySpawnerPrefab.Clone( obj.Transform.Position );
				weaponSpawner.NetworkSpawn( null );
			}
		}
	}
}