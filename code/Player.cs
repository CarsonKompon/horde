using Sandbox;
using Sandbox.ActionGraphs;
using Sandbox.Citizen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

public sealed class Player : Component
{
	public static Player Local => Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( p => p.Network.IsOwner );
	[Sync] public float Health { get; set; } = 100f;
	[Property] public float Speed { get; set; } = 100f;
	[Property] public bool GodMode { get; set; } = false;
	[Property] public GameObject Body { get; set; }
	[Property] public CharacterController CharacterController { get; private set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property] GameObject FlashlightObject { get; set; }
	[Property] public GameObject HoldObject { get; set; }
	[Property] GameObject MainCollider { get; set; }
	[Property] GameObject Tombstone { get; set; }
	[Property] public GameObject DefaultBulletSpawn { get; set; }
	[Property] ParticleSphereEmitter SlideParticleEmitter { get; set; }
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
		HordeManager.Instance.IsStarting = false;
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
					AimPosition = WorldPosition + Forward * 128f;
				}
			}
			else
			{
				var mouseRay = Scene.Camera.ScreenPixelToRay( Mouse.Position );
				var mouseTrace = Scene.Trace.Ray( mouseRay.Position, mouseRay.Position + mouseRay.Forward * 10000 )
					.WithTag( "aimplane" )
					.Run();
				var mousePos = mouseTrace.HitPosition;
				Forward = (mousePos - WorldPosition.WithZ( mousePos.z )).Normal;
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

			var camPos = WorldPosition + Vector3.Backward * 192f + Vector3.Up * 512f + (AimPosition - WorldPosition.WithZ( AimPosition.z )) / 4f;
			// var camTrace = Scene.Trace.Ray( WorldPosition + Vector3.Up * 128f, camPos )
			// 	.WithoutTags( "player", "enemy", "trigger" )
			// 	.Run();
			// if ( camTrace.Hit ) camPos = camTrace.HitPosition + camTrace.Normal * 2f;
			Scene.Camera.WorldPosition = Scene.Camera.WorldPosition.LerpTo( camPos, 10 * Time.Delta );
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
		Body.WorldRotation = Rotation.Slerp( Body.WorldRotation, targetRot, 10 * Time.Delta );

		// Show/Hide Stuff
		Body.Enabled = Health > 0f;
		MainCollider.Enabled = Health > 0f;
		Tombstone.Enabled = Health <= 0f;
		SlideParticleEmitter.Enabled = IsSliding;

		UpdateAnimations();
	}

	void Slide()
	{
		if ( WishVelocity.Length < 0.1f ) WishVelocity = Forward * Speed;
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

	[Rpc.Broadcast]
	public void Hurt( float damage )
	{
		if ( IsProxy ) return;
		if ( timeSinceRespawn < 5f ) return;
		if ( GodMode ) return;
		Health -= damage;
		healTimer = 0f;
		if ( Health <= 0f )
		{
			Kill();
		}

		var sound = Sound.Play( "hurt.enemy", WorldPosition );
		sound.Pitch = 0.5f;
		PolyHud.Instance.Panel.FlashClass( "hurt", 0.2f );
	}

	public void Kill()
	{
		if ( IsProxy ) return;
		Health = 0f;
		BroadcastDeathEvent();
	}

	[Rpc.Broadcast]
	public void IncrementKills()
	{
		if ( IsProxy ) return;
		Kills++;

		string statId = "";
		switch ( CurrentWeapon.Name )
		{
			case "USP":
			case "Revolver":
				{
					statId = "kills_pistol";
					break;
				}
			case "AK-47":
			case "MP5":
				{
					statId = "kills_rifle";
					break;
				}
			case "Pipe Shotgun":
			case "Auto Shotgun":
				{
					statId = "kills_shotgun";
					break;
				}
			case "Sword":
			case "Bat":
				{
					statId = "kills_melee";
					break;
				}
			case "RPG":
				{
					statId = "kills_explosive";
					break;
				}
		}
		Sandbox.Services.Stats.Increment( "kills", 1 );
		if ( !string.IsNullOrEmpty( statId ) )
			Sandbox.Services.Stats.Increment( statId, 1 );
	}

	[Rpc.Broadcast]
	public void Respawn( bool notify = true )
	{
		if ( IsProxy || Health > 0f ) return;
		Health = 50f;
		timeSinceRespawn = 0f;
		ResetWeapons();
		if ( notify )
			BroadcastRespawnEvent();
	}

	[Rpc.Broadcast]
	public void GrantRevive()
	{
		Sandbox.Services.Stats.Increment( "revives", 1 );
	}

	public void GiveStartingWeapon()
	{
		if ( StartingWeaponPrefab is null ) return;
		if ( HeldWeapons.Count > 0 ) return;

		var weapon = StartingWeaponPrefab.Clone( HoldObject.Transform.World );
		weapon.SetParent( HoldObject );
		weapon.NetworkSpawn( Network.Owner );
	}

	[Rpc.Broadcast]
	public void GiveWeapon( Guid pickupObjectId )
	{
		if ( IsProxy ) return;

		var pickupObject = Scene.Children.FirstOrDefault( x => x.Id == pickupObjectId );
		if ( pickupObject is null ) return;
		var pickup = pickupObject.Components.Get<Pickup>();
		if ( pickup is null ) return;
		var prefab = pickup.WeaponPrefab;
		if ( prefab is null ) return;

		Sound.Play( "pickup.weapon", WorldPosition );

		foreach ( var obj in HoldObject.Children )
		{
			if ( obj.Components.Get<Weapon>() is Weapon wep && !wep.IsDefault )
			{
				obj.Destroy();
			}
		}

		var weapon = prefab.Clone( HoldObject.Transform.World );
		weapon.SetParent( HoldObject );
		weapon.NetworkSpawn();

		var weaponScript = weapon.Components.Get<Weapon>();
		PolyHud.Instance.AddNotification( $"Picked up {weaponScript.Name}!" );

		pickup.DestroyMe();
	}

	public void ResetWeapons()
	{
		foreach ( var weapon in HeldWeapons )
		{
			weapon.GameObject.Destroy();
		}

		GiveStartingWeapon();
	}

	[Rpc.Broadcast]
	public void FillHealth()
	{
		if ( IsProxy ) return;
		Health = 100f;
	}

	[Rpc.Broadcast]
	public void BroadcastAttackEvent()
	{
		AnimationHelper.Target.Set( "holdtype_attack", 1 );
		AnimationHelper.Target.Set( "b_attack", true );
	}

	[Rpc.Broadcast]
	void BroadcastDeathEvent()
	{
		if ( Network.Owner is null ) return;
		PolyHud.Instance.AddNotification( $"{Network.Owner.DisplayName} has died!" );
	}

	[Rpc.Broadcast]
	void BroadcastRespawnEvent()
	{
		PolyHud.Instance.AddNotification( $"{Network.Owner.DisplayName} has been revived!" );
	}
}