using Sandbox;
using Sandbox.ActionGraphs;
using Sandbox.Citizen;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

public sealed class Player : Component
{
	public static Player Local => GameManager.ActiveScene.GetAllComponents<Player>().FirstOrDefault( p => p.Network.IsOwner );
	[Sync] public float Health { get; set; } = 100f;
	[Property] public float Speed { get; set; } = 100f;
	[Property] public GameObject Body { get; set; }
	[Property] public CharacterController CharacterController { get; private set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property] GameObject FlashlightObject { get; set; }
	[Property] public GameObject HoldObject { get; set; }
	[Property] GameObject MainCollider { get; set; }
	[Property] GameObject Tombstone { get; set; }
	[Property] GameObject StartingWeaponPrefab { get; set; }

	public Weapon CurrentWeapon => HoldObject.Components.GetAll<Weapon>().OrderBy( x => x.IsDefault ).FirstOrDefault();
	public List<Weapon> HeldWeapons => HoldObject.Components.GetAll<Weapon>().OrderBy( x => x.IsDefault ).ToList();

	[Sync] public Vector3 WishVelocity { get; set; }
	[Sync] public Vector3 Forward { get; set; }
	[Sync] public Vector3 AimPosition { get; set; }
	[Sync] TimeSince SlideTimer { get; set; } = 1f;
	[Sync] bool Flashlight { get; set; } = true;
	[Sync] public int Kills { get; set; } = 0;
	public bool IsSliding => SlideTimer < 0.6f;

	float healTimer = 0f;
	TimeSince timeSinceRespawn = 0f;

	protected override void OnStart()
	{
		if ( CurrentWeapon is not null && CurrentWeapon.IsDefault ) return;

		GiveStartingWeapon();
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			if ( Input.UsingController )
			{
				var look = Input.AnalogLook;
				var aim = new Vector2( -look.pitch, look.yaw );
				if ( aim.Length > 0.1f )
				{
					Forward = new Vector3( aim.x, aim.y, 0 );
					AimPosition = Transform.Position + Forward * 128f;
				}
			}
			else
			{
				var mouseRay = Scene.Camera.ScreenPixelToRay( Mouse.Position );
				var mouseTrace = Scene.Trace.Ray( mouseRay.Position, mouseRay.Position + mouseRay.Forward * 10000 )
					.WithTag( "aimplane" )
					.Run();
				var mousePos = mouseTrace.HitPosition;
				Forward = (mousePos - Transform.Position.WithZ( mousePos.z )).Normal;
				AimPosition = mousePos;
			}

			if ( Health > 0f )
			{
				if ( Input.Pressed( "Flashlight" ) )
				{
					Flashlight = !Flashlight;
					Sound.Play( Flashlight ? "flashlight_on" : "flashlight_off" );
				}

				if ( Input.Pressed( "attack2" ) && SlideTimer > 2f )
				{
					Slide();
				}

				if ( !IsSliding )
					BuildWishVelocity();

				// Move
				UpdateMovement();

				// Heal
				healTimer += Time.Delta;
				if ( healTimer >= 8f && Health < 100f )
				{
					Health += 5 * Time.Delta;
					if ( Health > 100f ) Health = 100f;
				}
			}

			var camPos = Transform.Position + Vector3.Backward * 192f + Vector3.Up * 512f + (AimPosition - Transform.Position.WithZ( AimPosition.z )) / 4f;
			// var camTrace = Scene.Trace.Ray( Transform.Position + Vector3.Up * 128f, camPos )
			// 	.WithoutTags( "player", "enemy", "trigger" )
			// 	.Run();
			// if ( camTrace.Hit ) camPos = camTrace.HitPosition + camTrace.Normal * 2f;
			Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camPos, 10 * Time.Delta );
		}
		else
		{
			foreach ( var weapon in HeldWeapons )
			{
				weapon.Body.Enabled = weapon == CurrentWeapon;
			}
		}

		// Enable Flashlight
		FlashlightObject.Enabled = Flashlight;

		// Lerp Body Rotation
		var targetRot = Rotation.LookAt( Forward, Vector3.Up );
		Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, 10 * Time.Delta );

		Body.Enabled = Health > 0f;
		MainCollider.Enabled = Health > 0f;
		Tombstone.Enabled = Health <= 0f;

		UpdateAnimations();
	}

	void Slide()
	{
		if ( WishVelocity.Length < Speed )
		{
			WishVelocity = WishVelocity.Normal * Speed;
		}
		WishVelocity *= 1.75f;
		if ( WishVelocity.Length > Speed * 2f )
		{
			WishVelocity = WishVelocity.Normal * Speed * 2f;
		}

		SlideTimer = 0f;
	}

	void BuildWishVelocity()
	{
		var move = Input.AnalogMove;
		WishVelocity = new Vector3( move.x, move.y, 0 );
		WishVelocity = WishVelocity.Normal;

		WishVelocity = WishVelocity * Speed;
		if ( Input.Down( "Run" ) ) WishVelocity *= 1.5f;
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
		AnimationHelper.WithLook( Forward );
		AnimationHelper.HoldType = CurrentWeapon?.HoldType ?? CitizenAnimationHelper.HoldTypes.None;
		AnimationHelper.SpecialMove = IsSliding ? CitizenAnimationHelper.SpecialMoveStyle.Slide : CitizenAnimationHelper.SpecialMoveStyle.None;
	}

	[Broadcast]
	public void Hurt( float damage )
	{
		if ( IsProxy ) return;
		if ( timeSinceRespawn < 5f ) return;
		Health -= damage;
		healTimer = 0f;
		if ( Health <= 0f )
		{
			Kill();
		}
	}

	public void Kill()
	{
		if ( IsProxy ) return;
		Health = 0f;
		BroadcastDeathEvent();
	}

	[Broadcast]
	public void IncrementKills()
	{
		if ( IsProxy ) return;
		Kills++;
	}

	[Broadcast]
	public void Respawn()
	{
		if ( IsProxy || Health > 0f ) return;
		Health = 100f;
		timeSinceRespawn = 0f;
		ResetWeapons();
		BroadcastRespawnEvent();
	}

	public void GiveStartingWeapon()
	{
		if ( StartingWeaponPrefab is null ) return;
		if ( HeldWeapons.Count > 0 ) return;

		var weapon = StartingWeaponPrefab.Clone( HoldObject.Transform.World );
		weapon.SetParent( HoldObject );
		weapon.NetworkSpawn( Network.OwnerConnection );
	}

	public void ResetWeapons()
	{
		foreach ( var weapon in HeldWeapons )
		{
			weapon.GameObject.Destroy();
		}

		GiveStartingWeapon();
	}

	[Broadcast]
	public void BroadcastAttackEvent()
	{
		AnimationHelper.Target.Set( "b_attack", true );
	}

	[Broadcast]
	void BroadcastDeathEvent()
	{
		PolyHud.Instance.AddNotification( $"{Network.OwnerConnection.DisplayName} has died!" );
	}

	[Broadcast]
	void BroadcastRespawnEvent()
	{
		PolyHud.Instance.AddNotification( $"{Network.OwnerConnection.DisplayName} has been revived!" );
	}
}