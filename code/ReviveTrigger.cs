using Sandbox;

public sealed class ReviveTrigger : Component, Component.ITriggerListener
{
	[Property] Player Player { get; set; }
	protected override void OnUpdate()
	{

	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;

		if ( other.Components.GetInParentOrSelf<Player>() is Player player && player != Player )
		{
			Player.Respawn();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}
}