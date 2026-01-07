using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 导入资产命令
    /// 触发 AssetDatabase 刷新/导入
    /// </summary>
    public class Cmd_ImportAssets : IJobCommand
    {
        public string Name => "ImportAssets";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var pathsToken = args["paths"];
            if (pathsToken == null)
                return CommandExecResult.Fail("Missing 'paths' argument");

            var paths = pathsToken.ToObject<string[]>();
            if (paths == null || paths.Length == 0)
                return CommandExecResult.Fail("'paths' array is empty");

            var force = args["force"]?.ToObject<bool>() ?? false;

            ctx.Logger.Info($"Importing {paths.Length} assets (force={force})");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would import assets");
                return CommandExecResult.Ok("DryRun: would import assets");
            }

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    ctx.Logger.Warning("Skipping empty path");
                    continue;
                }

                // 验证路径在 Assets/ 下
                if (!PathUtil.IsUnderAssets(path))
                {
                    return CommandExecResult.Fail($"Path not under Assets/: {path}");
                }

                ctx.Logger.Debug($"Importing: {path}");

                try
                {
                    if (force)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    else
                    {
                        AssetDatabase.ImportAsset(path);
                    }
                }
                catch (System.Exception e)
                {
                    return CommandExecResult.Fail($"Failed to import {path}: {e.Message}", e);
                }
            }

            return CommandExecResult.Ok($"Imported {paths.Length} assets");
        }
    }
}
