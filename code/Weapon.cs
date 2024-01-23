using Sandbox;

public sealed class Weapon : Component
{
	[Property] public bool IsDefault { get; set; } = false;
	[Property, Category( "Bullet" )] GameObject BulletPrefab { get; set; }
	[Property, Category( "Bullet" )] GameObject BulletSpawnPos { get; set; }
	[Property, Category( "Weapon" )] public int ClipSize { get; set; } = 60;
	[Property, Category( "Weapon" )] public float FireRate { get; set; } = 0.1f;
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

		if ( FireSound is not null )
		{
			Sound.Play( FireSound, Transform.Position );
		}

		var bullet = BulletPrefab.Clone( BulletSpawnPos.Transform.World );
		bullet.NetworkSpawn();
		Ammo--;

		if ( Ammo <= 0 && ClipSize > 0 )
		{
			GameObject.Destroy();
		}

		timer = FireRate;
	}
}