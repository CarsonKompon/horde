using Sandbox;

public sealed class BulletTrace : Component
{
	public float Damage { get; set; } = 10f;
	public float Range { get; set; } = 1000f;
	[Property, Category( "Particles" )] ParticleSystem MuzzleFlash { get; set; }
	TimeSince timeSinceSpawn = 0f;

	protected override void OnStart()
	{
		var tr = Scene.Trace.Ray( Transform.Position, Transform.Position + Transform.Rotation.Forward * Range )
			.WithoutTags( "trigger", "player" )
			.Run();

		var startPos = Transform.Position;
		var endPos = tr.Hit ? tr.HitPosition : tr.EndPosition;
		var distance = (endPos - startPos).Length;

		if ( Networking.IsHost && tr.Hit && tr.GameObject.Components.GetInParentOrSelf<Enemy>() is Enemy enemy )
		{
			enemy.Hurt( Damage );
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