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

			if ( !living )
			{
				InGame = false;
			}
		}
	}
}