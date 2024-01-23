using Sandbox;
using Sandbox.ActionGraphs;
using Sandbox.Citizen;
using System.Linq;

public sealed class Player : Component
{
	public static Player Local => GameManager.ActiveScene.GetAllComponents<Player>().FirstOrDefault( p => p.Network.IsOwner );
	[Property] public float Speed { get; set; } = 100f;
	[Property] public GameObject Body { get; set; }
	[Property] public CharacterController CharacterController { get; private set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property] public GameObject AimObject { get; set; }
	[Property] public GameObject HoldObject { get; set; }
	[Property] GameObject StartingWeaponPrefab { get; set; }

	public Weapon CurrentWeapon => HoldObject.Components.GetAll<Weapon>().OrderBy( x => x.IsDefault ).FirstOrDefault();

	public Vector3 WishVelocity { get; set; }
	public Vector3 Forward { get; set; }

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
			BuildWishVelocity();

			if ( Input.UsingController )
			{
				var look = Input.AnalogLook;
				var aim = new Vector2( -look.pitch, look.yaw );
				if ( aim.Length > 0.1f )
				{
					Forward = new Vector3( aim.x, aim.y, 0 );
					AimObject.Transform.Position = Transform.Position + Forward * 256f;
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
				AimObject.Transform.Position = mousePos;
			}

			// Move
			UpdateMovement();

			// Lerp Body Rotation
			var targetRot = Rotation.LookAt( Forward, Vector3.Up );
			Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, 10 * Time.Delta );

			var camPos = Transform.Position + Vector3.Up * 512f + (AimObject.Transform.Position - Transform.Position.WithZ( AimObject.Transform.Position.z )) / 4f;
			Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camPos, 10 * Time.Delta );
		}

		UpdateAnimations();
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
		AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Pistol;
	}
}