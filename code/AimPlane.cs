using Sandbox;

public sealed class AimPlane : Component
{
	protected override void OnUpdate()
	{
		var player = Player.Local;
		if ( player is not null && player.Health > 0 )
		{
			WorldPosition = player.WorldPosition.WithZ( player.CurrentWeapon.WorldPosition.z );
		}
	}
}