using System;
using Sandbox;

public sealed class DamageNumber : Component
{
	[Property] public TextRenderer TextRenderer { get; set; }
	public int Damage = 10;

	float alpha = 1f;
	Vector3 velocity = Vector3.Random.WithZ( Random.Shared.Float( 64f, 200f ) );
	protected override void OnUpdate()
	{
		TextRenderer.Text = Damage.ToString();

		alpha -= Time.Delta * 0.5f;
		TextRenderer.Color = TextRenderer.Color.WithAlpha( alpha );

		velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		WorldPosition += velocity * Time.Delta;

		WorldRotation = Rotation.LookAt( WorldPosition - Scene.Camera.WorldPosition, Vector3.Up );

		if ( alpha <= 0f )
		{
			GameObject.Destroy();
		}
	}
}