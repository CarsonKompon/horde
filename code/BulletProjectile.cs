using System.Linq;
using Sandbox;

public sealed class BulletProjectile : Component, Component.ITriggerListener
{
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float Speed { get; set; } = 20f;
	[Property] public float Lifetime { get; set; } = 10f;
	[Property] SoundEvent ImpactSound { get; set; }
	[Property] GameObject DestroyPrefab { get; set; }
	[Property] GameObject DestroySpawnPosition { get; set; }
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var lastPos = WorldPosition;
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;
		Lifetime -= Time.Delta;

		var tr = Scene.Trace.Ray( lastPos, WorldPosition )
			.WithoutTags( "trigger", "bullet", "enemy", "bullet" )
			.Run();

		if ( tr.Hit || Lifetime <= 0 )
		{
			Kill();
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( !other.GameObject.Tags.Has( "trigger" ) && other.Components.GetInParent<Enemy>() is Enemy enemy )
		{
			enemy.Hurt( Damage, Network.OwnerId );
			if ( !enemy.IsValid() )
			{
				var player = Scene.Components.GetAll<Player>().FirstOrDefault( p => p.Network.OwnerId == Network.OwnerId );
				player.IncrementKills();
			}
			Kill();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}

	void Kill()
	{
		if ( IsProxy ) return;

		if ( DestroyPrefab is not null )
		{
			var transform = Transform.World;
			if ( DestroySpawnPosition is not null )
			{
				transform = DestroySpawnPosition.Transform.World;
			}
			var go = DestroyPrefab.Clone( transform );
			go.NetworkSpawn();

			if ( go.Components.Get<MeleeContact>() is MeleeContact meleeContact )
			{
				meleeContact.Damage = Damage;
			}
		}

		DestroyMe();
	}

	[Rpc.Broadcast]
	void DestroyMe()
	{
		if ( ImpactSound is not null )
			Sound.Play( ImpactSound, WorldPosition );
		GameObject.Destroy();
	}
}