using Sandbox;

public sealed class ReviveTrigger : Component, Component.ITriggerListener
{
	[Property] Player Player { get; set; }

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Components.GetInParentOrSelf<Player>() is Player player && player != Player && player.Health > 0f )
		{
			using ( Rpc.FilterInclude( player.Network.Owner ) )
			{
				player.GrantRevive();
			}
			Player.Respawn();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}
}