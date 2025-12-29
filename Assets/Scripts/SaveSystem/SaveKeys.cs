using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    public static class SaveKeys
    {
        public const string FileName = "save.es3";
        public const string SnapshotKey = "snapshot";
        public const int Version = 1;

        public static ES3Settings Settings => new ES3Settings(FileName)
        {
            location = ES3.Location.File,
            directory = ES3.Directory.PersistentDataPath
        };
    }
}
