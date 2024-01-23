using System;
using Sandbox;
using Sandbox.Citizen;

public sealed class Weapon : Component
{
	[Property] public bool IsDefault { get; set; } = false;
	[Property] GameObject Body { get; set; }
	[Property, Category( "Bullet" )] GameObject BulletPrefab { get; set; }
	[Property, Category( "Bullet" )] GameObject BulletSpawnPos { get; set; }

	[Property, Category( "Weapon" )] public string Name { get; set; } = "Item";
	[Property, Category( "Weapon" )] public int ClipSize { get; set; } = 60;
	[Property, Category( "Weapon" )] public float FireRate { get; set; } = 0.1f;
	[Property, Category( "Weapon" )] public float Damage { get; set; } = 10f;
	[Property, Category( "Weapon" )] public float Spread { get; set; } = 0f;
	[Property, Category( "Weapon" )] public float Range { get; set; } = 1000f;
	[Property, Category( "Weapon" ), Range( 1, 64, 1, true, false )] public int BulletsPerShot { get; set; } = 1;
	[Property, Category( "Weapon" )] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;

	[Property, Category( "Assets" )] public string Icon { get; set; } = "ui/weapons/pistol.png";
	[Property, Category( "Assets" )] public SoundEvent FireSound { get; set; }

	public int Ammo { get; set; } = 0;
	float timer = 0f;

	protected override void OnStart()
	{
		Ammo = ClipSize;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		Body.Enabled = Player.Local.CurrentWeapon == this;

		if ( Player.Local.CurrentWeapon != this ) return;

		timer -= Time.Delta;

		if ( timer <= 0 && Input.Down( "attack1" ) )
		{
			Fire();
		}
	}

	public void Fire()
	{
		if ( Ammo <= 0 && ClipSize > 0 ) return;

		Ammo--;

		Player.Local.BroadcastAttackEvent();
		BroadcastFireEvent();

		if ( Ammo <= 0 && ClipSize > 0 )
		{
			GameObject.Destroy();
		}

		timer = FireRate;
	}

	[Broadcast]
	void BroadcastFireEvent()
	{
		for ( int i = 0; i < BulletsPerShot; i++ )
		{
			var transform = BulletSpawnPos.Transform.World;
			transform.Rotation *= Rotation.FromYaw( Random.Shared.Float( -Spread, Spread ) );
			transform.Scale = 1f;
			var bulletObj = BulletPrefab.Clone( transform );
			var bullet = bulletObj.Components.Get<BulletTrace>();
			bullet.Damage = Damage;
			bullet.Range = Range;
		}

		if ( FireSound is not null )
		{
			Sound.Play( FireSound, Transform.Position );
		}
	}
}