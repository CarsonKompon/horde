using System;
using System.Linq;
using Sandbox;

public sealed class BulletTrace : Component
{
	public float Damage { get; set; } = 10f;
	public float Range { get; set; } = 1000f;
	[Property, Category( "Particles" )] ParticleSystem MuzzleFlash { get; set; }
	TimeSince timeSinceSpawn = 0f;

	protected override void OnStart()
	{
		var tr = Scene.Trace.Ray( WorldPosition, WorldPosition + WorldRotation.Forward * Range )
			.WithoutTags( "trigger", "player" )
			.Run();

		var startPos = WorldPosition;
		var endPos = tr.Hit ? tr.HitPosition : tr.EndPosition;
		var distance = (endPos - startPos).Length;

		if ( !IsProxy && tr.Hit && tr.GameObject.Components.GetInParent<Enemy>() is Enemy enemy )
		{
			enemy.Hurt( Damage, Network.OwnerId );

			if ( !enemy.IsValid() )
			{
				var player = Scene.Components.GetAll<Player>().FirstOrDefault( p => p.Network.OwnerId == Network.OwnerId );
				player.IncrementKills();
			}
		}

		// Trace Particles
		var p = new SceneParticles( Scene.SceneWorld, "particles/tracer/trail_smoke.vpcf" );
		p.SetControlPoint( 0, startPos );
		p.SetControlPoint( 1, endPos );
		p.SetControlPoint( 2, distance );
		p.PlayUntilFinished( Task );

		// Muzzle Flash Particles
		if ( MuzzleFlash is not null )
		{
			p = new( Scene.SceneWorld, MuzzleFlash );
			p.SetControlPoint( 0, Transform.World );
			p.PlayUntilFinished( Task );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( timeSinceSpawn > 0.1f )
		{
			GameObject.Destroy();
		}
	}
}