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
	TimeSince targetTimer = 0f;
	float startingHealth = 10f;

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
				Target = Transform.Position + Vector3.Random.Normal.WithZ( 0 ) * 100f;
				if ( InRange.Count > 0 )
				{
					foreach ( var player in InRange )
					{
						if ( HasLineOfSight( player.Transform.Position ) && player.Health > 0 )
						{
							Target = player.Transform.Position;
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
			// 	Gizmo.Draw.Line( LeftForward.Transform.Position, LeftForward.Transform.Position + LeftForward.Transform.Rotation.Forward * ForwardDistance );
			// 	Gizmo.Draw.Line( RightForward.Transform.Position, RightForward.Transform.Position + RightForward.Transform.Rotation.Forward * ForwardDistance );
			// }
		}

		UpdateAnimations();
	}

	protected override void OnDestroy()
	{
		Count--;
	}

	[Broadcast]
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
			pickup.Clone( Transform.Position ).NetworkSpawn( null );
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
		WishVelocity = (Target - Transform.Position).Normal;

		var canSee = HasLineOfSight( Target );
		if ( !canSee )
		{
			var tr1 = Scene.Trace.Ray( LeftForward.Transform.Position, LeftForward.Transform.Position + LeftForward.Transform.Rotation.Forward * ForwardDistance )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "trigger", "player" )
				.Run();
			var tr2 = Scene.Trace.Ray( RightForward.Transform.Position, RightForward.Transform.Position + RightForward.Transform.Rotation.Forward * ForwardDistance )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "trigger", "player" )
				.Run();
			if ( tr1.Hit )
			{
				Body.Transform.Rotation *= Rotation.FromYaw( 180f * Time.Delta );
				WishVelocity = Body.Transform.Rotation.Forward;
			}
			else if ( tr2.Hit )
			{
				Body.Transform.Rotation *= Rotation.FromYaw( -180f * Time.Delta );
				WishVelocity = Body.Transform.Rotation.Forward;
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
			var targetRot = Rotation.LookAt( Target.WithZ( Transform.Position.z ) - Transform.Position, Vector3.Up );
			Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, Time.Delta * 10f );
		}

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.WithLook( Body.Transform.Rotation.Forward );
	}

	bool HasLineOfSight( Vector3 pos )
	{
		var tr = Scene.Trace.Ray( Transform.Position + Vector3.Up * 16f, pos + Vector3.Up * 16f )
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

	[Broadcast]
	void BroadcastHurtEvent( float damage )
	{
		var obj = HordeManager.Instance.DamageNumberPrefab.Clone( Transform.Position + Vector3.Random.WithZ( 0f ) * 4f + Vector3.Up * 64f );
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
	}

	[Broadcast]
	void BroadcastKillEvent()
	{
		Sound.Play( "zombie.death", Transform.Position );
		var obj = HordeManager.Instance.BloodBurstPrefab.Clone( Transform.Position + Vector3.Up * 32f * (AnimationHelper?.Height ?? 1f) );
	}
}