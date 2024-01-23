using Sandbox;

public sealed class AimPlane : Component
{
	protected override void OnUpdate()
	{
		var player = Player.Local;
		if ( player is not null )
		{
			Transform.Position = player.Transform.Position.WithZ( player.Transform.Position.z + 50f );
		}
	}
}