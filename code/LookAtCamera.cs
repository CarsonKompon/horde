using Sandbox;

public sealed class LookAtCamera : Component
{
	[Property] Angles Offset { get; set; } = Angles.Zero;
	protected override void OnUpdate()
	{
		Transform.Rotation = Rotation.LookAt( Scene.Camera.Transform.Position - Transform.Position ) * Rotation.From( Offset );
	}
}