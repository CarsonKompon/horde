using Sandbox;

public sealed class LookAtCamera : Component
{
	[Property] Angles Offset { get; set; } = Angles.Zero;
	protected override void OnUpdate()
	{
		WorldRotation = Rotation.LookAt( Scene.Camera.WorldPosition - WorldPosition ) * Rotation.From( Offset );
	}
}