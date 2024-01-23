using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Citizen;

public sealed class Enemy : Component, Component.ITriggerListener
{
	[Property] float Speed { get; set; } = 80f;
	[Property] GameObject Body { get; set; }
	[Property] CharacterController CharacterController { get; set; }
	[Property] CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] GameObject LeftForward { get; set; }
	[Property] GameObject RightForward { get; set; }
	float ForwardDistance = 50f;

	public Vector3 WishVelocity { get; set; }

	[Sync] Vector3 Target { get; set; } = Vector3.Zero;
	TimeSince targetTimer = 0f;

	List<GameObject> InRange = new();

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			if ( targetTimer > 0.5f || Vector3.DistanceBetween( Target, Transform.Position ) < 10f )
			{
				Target = Transform.Position + Vector3.Random.Normal.WithZ( 0 ) * 100f;
				if ( InRange.Count > 0 )
				{
					foreach ( var obj in InRange )
					{
						var tr = Scene.Trace.Ray( Transform.Position + Vector3.Up * 16f, obj.Transform.Position + Vector3.Up * 16f )
							.IgnoreGameObjectHierarchy( GameObject )
							.WithoutTags( "trigger", "player" )
							.Run();

						if ( HasLineOfSight( obj.Transform.Position ) )
						{
							Target = obj.Transform.Position;
							break;
						}
					}
				}
				targetTimer = 0f;
			}

			BuildWishVelocity();
			UpdateMovement();

			// Rotate body towards target
			if ( Target != Vector3.Zero )
			{
				var targetRot = Rotation.LookAt( Target - Transform.Position, Vector3.Up );
				Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, Time.Delta * 10f );
			}

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
		if ( !Networking.IsHost ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			InRange.Add( player.GameObject );
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( !Networking.IsHost ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			InRange.Remove( player.GameObject );
		}

	}
}