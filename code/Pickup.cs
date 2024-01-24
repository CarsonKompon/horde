using System;
using Sandbox;

public sealed class Pickup : Component, Component.ITriggerListener
{
	[Property] GameObject Body { get; set; }
	[Property] public GameObject WeaponPrefab { get; set; }

	Vector3 offset;

	protected override void OnStart()
	{
		offset = Body.Transform.LocalPosition;
	}

	protected override void OnUpdate()
	{
		Body.Transform.LocalPosition = offset + Vector3.Up * 3f * MathF.Sin( Time.Now * 2f );
		Body.Transform.LocalRotation = Rotation.FromYaw( Time.Now * 100f );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( !Networking.IsHost ) return;

		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			player.GiveWeapon( GameObject.Id );
			GameObject.Destroy();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}

	[Broadcast]
	public void DestroyMe()
	{
		GameObject.Destroy();
	}

}