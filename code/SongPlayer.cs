using System.Collections.Generic;
using Sandbox;

public sealed class SongPlayer : Component
{
	[Property] List<SoundEvent> Songs { get; set; }

	SoundHandle playing = null;
	int index = 0;

	protected override void OnUpdate()
	{

		if ( !playing.IsValid() )
		{
			NextSong();
		}
		else if ( !playing.IsPlaying )
		{
			NextSong();
		}

		if ( playing.IsValid() )
		{
			playing.Volume = HordePreferences.Settings.Volume / 100f;
		}

	}

	protected override void OnDestroy()
	{
		playing?.Stop();
	}

	void NextSong()
	{
		var song = Songs[index];
		playing = Sound.Play( song );
		index = (index + 1) % Songs.Count;
	}
}