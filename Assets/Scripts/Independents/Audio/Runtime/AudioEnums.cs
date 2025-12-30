namespace ThatGameJam.Independents.Audio
{
    public enum AudioCategory
    {
        SFX = 0,
        Music = 1,
        UI = 2,
        Ambient = 3
    }

    public enum AudioBus
    {
        SFX = 0,
        Music = 1,
        UI = 2,
        Ambient = 3
    }

    public enum AudioPlayMode
    {
        Single = 0,
        RandomOne = 1,
        RandomNoRepeat = 2,
        Sequence = 3
    }

    public enum AudioSpatialMode
    {
        TwoD = 0,
        ThreeD = 1
    }

    public enum AudioStopPolicy
    {
        StopByEventId = 0,
        StopByOwner = 1,
        StopByHandle = 2
    }
}
