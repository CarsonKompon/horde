using System;
using Sandbox;

public sealed class Pickup : Component, Component.ITriggerListener
{
	[Property] GameObject Body { get; set; }
	[Property] GameObject WeaponPrefab { get; set; }

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
		Log.Info( $"OnTriggerEnter: {other}" );
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			if ( player.Network.IsProxy ) return;

			foreach ( var obj in player.HoldObject.Children )
			{
				if ( obj.Components.Get<Weapon>() is Weapon wep && !wep.IsDefault )
				{
					obj.Destroy();
				}
			}

			var weapon = WeaponPrefab.Clone( player.HoldObject.Transform.World );
			weapon.SetParent( player.HoldObject );
			weapon.NetworkSpawn();
			GameObject.Destroy();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}

}