using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sandbox;
using Sandbox.Citizen;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float Cooldown { get; set; } = 1f;
	[Property] CitizenAnimationHelper CitizenAnimator { get; set; }

	[Sync] TimeSince TimeSinceLastDamage { get; set; } = 0f;

	List<Player> canHurt = new();

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( TimeSinceLastDamage >= Cooldown && canHurt.Count > 0 )
		{
			bool didHurt = false;
			foreach ( var player in canHurt )
			{
				if ( player is null || player.Health <= 0 ) continue;
				player.Hurt( Damage );
				didHurt = true;
			}

			if ( didHurt )
			{
				TimeSinceLastDamage = 0f;
				BroadcastAttackEvent();
			}
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			canHurt.Add( player );
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy ) return;
		if ( other.Components.GetInParentOrSelf<Player>() is Player player )
		{
			canHurt.Remove( player );
		}
	}

	[Broadcast]
	void BroadcastAttackEvent()
	{
		PlayAttackAnim();
	}

	async void PlayAttackAnim()
	{
		if ( CitizenAnimator is null ) return;
		if ( Random.Shared.Float() < .5f )
			CitizenAnimator.HoldType = CitizenAnimationHelper.HoldTypes.Swing;
		else
			CitizenAnimator.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		CitizenAnimator.Target.Set( "b_attack", true );
		await GameTask.DelayRealtimeSeconds( Cooldown / 2f );
		CitizenAnimator.HoldType = CitizenAnimationHelper.HoldTypes.None;
	}
}