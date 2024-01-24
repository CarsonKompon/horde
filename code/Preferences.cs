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

    public static ChatSettings Chat
    {
        get
        {
            if ( _chatSettings is null )
            {
                var file = "/settings/chat.json";
                _chatSettings = FileSystem.Data.ReadJson( file, new ChatSettings() );
            }
            return _chatSettings;
        }
    }
    static ChatSettings _chatSettings;

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

public class ChatSettings
{
    public bool ShowAvatars { get; set; } = true;
    public int FontSize { get; set; } = 16;
    public bool ChatSounds { get; set; } = true;
}