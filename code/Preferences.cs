using Sandbox;

public static class HordePreferences
{

    public static HordeSettings Settings
    {
        get
        {
            if ( _settings is null )
            {
                var file = "/settings.json";
                _settings = FileSystem.Data.ReadJson( file, new HordeSettings() );
            }
            return _settings;
        }
    }
    static HordeSettings _settings;

    public static void Save()
    {
        var file = "/settings.json";
        FileSystem.Data.WriteJson( file, Settings );
    }

}

public class HordeSettings
{
    public float Volume { get; set; } = 40f;
}