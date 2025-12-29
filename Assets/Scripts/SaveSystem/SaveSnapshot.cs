using System;
using System.Collections.Generic;

namespace ThatGameJam.SaveSystem
{
    [Serializable]
    public class SaveSnapshot
    {
        public int version = SaveKeys.Version;
        public string timestamp;
        public List<SaveBlock> blocks = new List<SaveBlock>();

        public bool TryGetBlock(string key, out string json)
        {
            if (blocks != null)
            {
                for (var i = 0; i < blocks.Count; i++)
                {
                    var block = blocks[i];
                    if (block != null && block.key == key)
                    {
                        json = block.json;
                        return true;
                    }
                }
            }

            json = null;
            return false;
        }
    }

    [Serializable]
    public class SaveBlock
    {
        public string key;
        public string json;
    }
}
