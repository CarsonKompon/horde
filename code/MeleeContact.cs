using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sandbox;
using Sandbox.Citizen;

public sealed class MeleeContact : Component
{
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float Range { get; set; } = 20f;
	[Property] public float DestroyAfter { get; set; } = 0.1f;
	[Property] public SoundEvent HitSound { get; set; }

	TimeSince timeSinceCreated = 0f;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		bool didHit = false;

		var enemies = Scene.Components.GetAll<Enemy>().ToList();
		for ( int i = enemies.Count - 1; i >= 0; i-- )
		{
			var enemy = enemies[i];
			if ( enemy is null || enemy.Health <= 0 ) continue;
			Log.Info( Range );
			if ( Vector3.DistanceBetween( enemy.Transform.Position + Vector3.Up * 42f, Transform.Position ) <= Range )
			{
				enemy.Hurt( Damage, Network.OwnerId );
				didHit = true;
			}
		}

		if ( didHit )
		{
			Sound.Play( HitSound, Transform.Position );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( timeSinceCreated > DestroyAfter )
		{
			GameObject.Destroy();
		}
	}
}