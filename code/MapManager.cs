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