using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Citizen;

public sealed class Enemy : Component, Component.ITriggerListener
{
	public static int Count = 0;

	[Property, Sync] public float Health { get; set; } = 25f;
	[Property] float Speed { get; set; } = 80f;
	[Property] GameObject Body { get; set; }
	[Property] CharacterController CharacterController { get; set; }
	[Property] CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] GameObject LeftForward { get; set; }
	[Property] GameObject RightForward { get; set; }
	float ForwardDistance = 50f;

	public Vector3 WishVelocity { get; set; }

	[Sync] Vector3 Target { get; set; } = Vector3.Zero;
	[Sync] Guid LastHurt { get; set; } = Guid.Empty;
	TimeSince timeSinceLastHurt = 0f;
	TimeSince targetTimer = 0f;
	float startingHealth = 10f;
	int hurtChain = 0;

	List<Player> InRange = new();

	protected override void OnStart()
	{
		startingHealth = Health;
		Count++;
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			if ( targetTimer > 0.5f )
			{
				Target = WorldPosition + Vector3.Random.Normal.WithZ( 0 ) * 100f;
				if ( InRange.Count > 0 )
				{
					foreach ( var player in InRange )
					{
						if ( HasLineOfSight( player.WorldPosition ) && player.Health > 0 )
						{
							Target = player.WorldPosition;
							break;
						}
					}
				}
				targetTimer = 0f;
			}

			BuildWishVelocity();
			UpdateMovement();

			// using ( Gizmo.Scope( "navtest" ) )
			// {
			// 	Gizmo.Draw.Color = Color.Blue;
			// 	Gizmo.Draw.LineThickness = 2f;
			// 	Gizmo.Draw.Line( LeftForward.WorldPosition, LeftForward.WorldPosition + LeftForward.WorldRotation.Forward * ForwardDistance );
			// 	Gizmo.Draw.Line( RightForward.WorldPosition, RightForward.WorldPosition + RightForward.WorldRotation.Forward * ForwardDistance );
			// }
		}

		UpdateAnimations();
	}

	protected override void OnDestroy()
	{
		Count--;
	}

	[Rpc.Broadcast]
	public void Hurt( float damage, Guid hurtBy = default )
	{
		if ( IsProxy ) return;
		if ( Health <= 0 ) return;
		if ( hurtBy != default ) LastHurt = hurtBy;

		BroadcastHurtEvent( damage );

		Health -= damage;
		if ( Health <= 0f )
		{
			Kill();
		}
	}

	public void Kill()
	{
		var pickups = HordeManager.Instance.WeaponPrefabs;
		if ( pickups is not null && pickups.Count > 0 && Random.Shared.Float() < 0.15f )
		{
			var pickup = pickups[Random.Shared.Next( pickups.Count )];
			pickup.Clone( WorldPosition ).NetworkSpawn( null );
		}

		if ( LastHurt != Guid.Empty )
		{
			var player = Scene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.OwnerId == LastHurt );
			if ( player is not null )
			{
				player.IncrementKills();
			}
		}

		BroadcastKillEvent();

		GameObject.Destroy();
	}

	void BuildWishVelocity()
	{
		WishVelocity = (Target - WorldPosition).Normal;

		var canSee = HasLineOfSight( Target );
		if ( !canSee )
		{
			var tr1 = Scene.Trace.Ray( LeftForward.WorldPosition, LeftForward.WorldPosition + LeftForward.WorldRotation.Forward * ForwardDistance )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "trigger", "player" )
				.Run();
			var tr2 = Scene.Trace.Ray( RightForward.WorldPosition, RightForward.WorldPosition + RightForward.WorldRotation.Forward * ForwardDistance )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "trigger", "player" )
				.Run();
			if ( tr1.Hit )
			{
				Body.WorldRotation *= Rotation.FromYaw( 180f * Time.Delta );
				WishVelocity = Body.WorldRotation.Forward;
			}
			else if ( tr2.Hit )
			{
				Body.WorldRotation *= Rotation.FromYaw( -180f * Time.Delta );
				WishVelocity = Body.WorldRotation.Forward;
			}
		}

		WishVelocity = WishVelocity.Normal;
		WishVelocity = WishVelocity * Speed;

		if ( !canSee ) WishVelocity *= 0.1f;
	}

	void UpdateMovement()
	{
		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;
		CharacterController.ApplyFriction( GetFriction() );
		if ( CharacterController.IsOnGround )
		{
			CharacterController.Accelerate( WishVelocity );
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
		{
			CharacterController.Velocity += halfGravity;
			CharacterController.Accelerate( WishVelocity );
		}

		CharacterController.Move();

		if ( !CharacterController.IsOnGround )
		{
			CharacterController.Velocity += halfGravity;
		}
		else
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
	}

	float GetFriction()
	{
		if ( CharacterController.IsOnGround ) return 6.0f;

		return 0.2f;
	}

	void UpdateAnimations()
	{
		// Rotate body towards target
		if ( Target != Vector3.Zero )
		{
			var targetRot = Rotation.LookAt( Target.WithZ( WorldPosition.z ) - WorldPosition, Vector3.Up );
			Body.WorldRotation = Rotation.Slerp( Body.WorldRotation, targetRot, Time.Delta * 10f );
		}

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.WithLook( Body.WorldRotation.Forward );
	}

	bool HasLineOfSight( Vector3 pos )
	{
		var tr = Scene.Trace.Ray( WorldPosition + Vector3.Up * 16f, pos + Vector3.Up * 16f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "trigger", "player" )
			.Run();

		return !tr.Hit;
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player && player.Health > 0 )
		{
			InRange.Add( player );
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			InRange.Remove( player );
		}

	}

	[Rpc.Broadcast]
	void BroadcastHurtEvent( float damage )
	{
		var obj = HordeManager.Instance.DamageNumberPrefab.Clone( WorldPosition + Vector3.Random.WithZ( 0f ) * 4f + Vector3.Up * 64f );
		var dmgNumber = obj.Components.Get<DamageNumber>();
		dmgNumber.Damage = (int)damage;
		if ( damage >= startingHealth )
		{
			dmgNumber.TextRenderer.Color = Color.Red;
		}
		else
		{
			dmgNumber.TextRenderer.Color = Color.Yellow;
		}
		if ( timeSinceLastHurt > 3f )
		{
			hurtChain = 0;
		}
		var sound = Sound.Play( "hurt.enemy", WorldPosition );
		sound.Pitch = 1f + (hurtChain / 12f) * 2f;
		hurtChain++;
		timeSinceLastHurt = 0f;
	}

	[Rpc.Broadcast]
	void BroadcastKillEvent()
	{
		Sound.Play( "zombie.death", WorldPosition );
		var obj = HordeManager.Instance.BloodBurstPrefab.Clone( WorldPosition + Vector3.Up * 32f * (AnimationHelper?.Height ?? 1f) );
	}
}