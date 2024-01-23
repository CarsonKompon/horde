using Sandbox;
using Sandbox.ActionGraphs;
using Sandbox.Citizen;
using System.Linq;

public sealed class Player : Component
{
	public static Player Local => GameManager.ActiveScene.GetAllComponents<Player>().FirstOrDefault( p => p.Network.IsOwner );
	public float Health { get; set; } = 100f;
	[Property] public float Speed { get; set; } = 100f;
	[Property] public GameObject Body { get; set; }
	[Property] public CharacterController CharacterController { get; private set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property] public GameObject HoldObject { get; set; }
	[Property] GameObject StartingWeaponPrefab { get; set; }

	public Weapon CurrentWeapon => HoldObject.Components.GetAll<Weapon>().OrderBy( x => x.IsDefault ).FirstOrDefault();

	[Sync] public Vector3 WishVelocity { get; set; }
	[Sync] public Vector3 Forward { get; set; }
	[Sync] public Vector3 AimPosition { get; set; }
	[Sync] TimeSince SlideTimer { get; set; } = 1f;
	public bool IsSliding => SlideTimer < 0.6f;

	protected override void OnStart()
	{
		if ( CurrentWeapon is not null && CurrentWeapon.IsDefault ) return;

		var weapon = StartingWeaponPrefab.Clone( HoldObject.Transform.World );
		weapon.SetParent( HoldObject );
		weapon.NetworkSpawn( Network.OwnerConnection );
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

			if ( Input.Pressed( "attack2" ) && SlideTimer > 2f )
			{
				Slide();
			}

			if ( !IsSliding )
				BuildWishVelocity();

			// Move
			UpdateMovement();

			var camPos = Transform.Position + Vector3.Backward * 192f + Vector3.Up * 512f + (AimPosition - Transform.Position.WithZ( AimPosition.z )) / 4f;
			Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camPos, 10 * Time.Delta );
		}


		// Lerp Body Rotation
		var targetRot = Rotation.LookAt( Forward, Vector3.Up );
		Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, 10 * Time.Delta );


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
	public void BroadcastAttackEvent()
	{
		AnimationHelper.Target.Set( "b_attack", true );
	}
}