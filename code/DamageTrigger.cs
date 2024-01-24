using Sandbox;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float Cooldown { get; set; } = 1f;

	[Sync] TimeSince TimeSinceLastDamage { get; set; } = 0f;

	public void OnTriggerEnter( Collider other )
	{
		if ( TimeSinceLastDamage < Cooldown ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			if ( player.IsProxy ) return;

			player.Hurt( Damage );
			TimeSinceLastDamage = 0f;
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}
}