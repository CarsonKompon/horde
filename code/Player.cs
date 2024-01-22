using Sandbox;
using Sandbox.Citizen;

public sealed class Player : Component
{
	[Property] public float Speed { get; set; } = 100f;
	[Property] public GameObject Body { get; set; }
	[Property] public CharacterController CharacterController { get; private set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; private set; }
	[Property] GameObject AimObject { get; set; }

	public Vector3 WishVelocity { get; set; }
	public Vector3 Forward { get; set; }

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			BuildWishVelocity();

			var mouseRay = Scene.Camera.ScreenPixelToRay( Mouse.Position );
			var mouseTrace = Scene.Trace.Ray( mouseRay.Position, mouseRay.Position + mouseRay.Forward * 10000 )
				.WithTag( "aimplane" )
				.Run();
			var mousePos = mouseTrace.HitPosition;
			Forward = (mousePos - Transform.Position.WithZ( mousePos.z )).Normal;
			AimObject.Transform.Position = mousePos;

			// Move
			UpdateMovement();

			// Lerp Body Rotation
			var targetRot = Rotation.LookAt( Forward, Vector3.Up );
			Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, targetRot, 10 * Time.Delta );

			var camPos = Transform.Position + Vector3.Up * 512f + (mousePos - Transform.Position.WithZ( mousePos.z )) / 4f;
			Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( camPos, 10 * Time.Delta );
		}

		UpdateAnimations();
	}

	void BuildWishVelocity()
	{
		WishVelocity = Vector3.Zero;
		if ( Input.Down( "Forward" ) ) WishVelocity += Vector3.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += Vector3.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += Vector3.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += Vector3.Right;
		WishVelocity = WishVelocity.Normal;

		WishVelocity = WishVelocity * Speed;
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
	}
}