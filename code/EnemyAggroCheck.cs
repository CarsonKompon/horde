using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyAggroCheck : Component, Component.ITriggerListener
{
	[Property] Enemy Enemy { get; set; }

	TimeSince targetTimer = 0f;
	List<Player> InRange = new();

	protected override void OnFixedUpdate()
	{
		if ( !IsProxy )
		{
			if ( targetTimer > 0.5f )
			{
				if ( InRange.Count > 0 )
				{
					foreach ( var player in InRange )
					{
						if ( Enemy.HasLineOfSight( player.WorldPosition ) && player.Health > 0 )
						{
							Enemy.Target = player.WorldPosition;
							break;
						}
					}
				}
				targetTimer = 0f;
			}
		}

	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player && player.Health > 0 )
		{
			InRange.Add( player );
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			InRange.Remove( player );
		}

	}
}