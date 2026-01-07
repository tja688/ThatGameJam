using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 保存资产命令
    /// </summary>
    public class Cmd_SaveAssets : IJobCommand
    {
        public string Name => "SaveAssets";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            var refresh = args?["refresh"]?.ToObject<bool>() ?? false;

            ctx.Logger.Info($"Saving assets (refresh={refresh})");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would save assets");
                return CommandExecResult.Ok("DryRun: would save assets");
            }

            AssetDatabase.SaveAssets();

            if (refresh)
            {
                AssetDatabase.Refresh();
            }

            ctx.Logger.Info("Assets saved");

            return CommandExecResult.Ok("Assets saved");
        }
    }
}
