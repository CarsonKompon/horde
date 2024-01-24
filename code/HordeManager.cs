using System.Collections.Generic;
using Sandbox;

public sealed class HordeManager : Component
{
	public static HordeManager Instance { get; private set; }

	[Property] public GameObject DamageNumberPrefab { get; set; }
	[Property] public GameObject BloodBurstPrefab { get; set; }
	[Property] public List<GameObject> WeaponPrefabs { get; set; }

	protected override void OnAwake()
	{
		Instance = this;
	}
}