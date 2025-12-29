using System;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.StoryTasks.Commands;
using ThatGameJam.Features.StoryTasks.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class StoryFlagsSaveData
    {
        public List<string> flags = new List<string>();
    }

    public class StoryFlagsSaveAdapter : SaveParticipant<StoryFlagsSaveData>, IController
    {
        [SerializeField] private string saveKey = "story.flags";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "story.flags";
        }

        protected override StoryFlagsSaveData Capture()
        {
            var model = (StoryFlagsModel)this.GetModel<IStoryFlagsModel>();
            var data = new StoryFlagsSaveData();
            data.flags.AddRange(model.GetAllFlags());
            return data;
        }

        protected override void Restore(StoryFlagsSaveData data)
        {
            if (data == null)
            {
                return;
            }

            var model = (StoryFlagsModel)this.GetModel<IStoryFlagsModel>();
            model.ClearFlags();

            if (data.flags == null)
            {
                return;
            }

            for (var i = 0; i < data.flags.Count; i++)
            {
                var flag = data.flags[i];
                if (string.IsNullOrEmpty(flag))
                {
                    continue;
                }

                this.SendCommand(new SetFlagCommand(flag));
            }
        }
    }
}
