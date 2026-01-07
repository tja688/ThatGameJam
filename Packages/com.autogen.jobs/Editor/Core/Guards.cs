using System;
using UnityEditor;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// 执行门槛检查（Guards）
    /// 在执行 Job 前进行各种前置条件检查
    /// </summary>
    public static class Guards
    {
        /// <summary>
        /// 检查结果
        /// </summary>
        public class CheckResult
        {
            public bool CanProceed { get; private set; }
            public WaitingReason? WaitReason { get; private set; }
            public string FailReason { get; private set; }
            public bool IsFailed => !CanProceed && !WaitReason.HasValue;
            public bool IsWaiting => !CanProceed && WaitReason.HasValue;

            private CheckResult() { }

            public static CheckResult Ok() => new CheckResult { CanProceed = true };

            public static CheckResult Wait(WaitingReason reason, string message = null) => new CheckResult
            {
                CanProceed = false,
                WaitReason = reason,
                FailReason = message ?? reason.ToString()
            };

            public static CheckResult Fail(string reason) => new CheckResult
            {
                CanProceed = false,
                FailReason = reason
            };
        }

        /// <summary>
        /// 检查 Unity 是否正在编译或更新
        /// </summary>
        public static CheckResult CheckNotCompiling()
        {
            if (EditorApplication.isCompiling)
            {
                return CheckResult.Wait(WaitingReason.WAITING_COMPILING, "Unity is compiling");
            }
            if (EditorApplication.isUpdating)
            {
                return CheckResult.Wait(WaitingReason.WAITING_COMPILING, "Unity is updating");
            }
            return CheckResult.Ok();
        }

        /// <summary>
        /// 检查 Runner 版本
        /// </summary>
        public static CheckResult CheckRunnerVersion(int requiredVersion)
        {
            if (AutoGenSettings.RUNNER_VERSION < requiredVersion)
            {
                return CheckResult.Wait(
                    WaitingReason.WAITING_RUNNER_VERSION,
                    $"Runner version {AutoGenSettings.RUNNER_VERSION} < required {requiredVersion}"
                );
            }
            return CheckResult.Ok();
        }

        /// <summary>
        /// 检查所需类型是否存在
        /// </summary>
        public static CheckResult CheckRequiredTypes(System.Collections.Generic.List<string> typeNames)
        {
            if (typeNames == null || typeNames.Count == 0)
                return CheckResult.Ok();

            foreach (var typeName in typeNames)
            {
                if (!TypeResolver.TypeExists(typeName))
                {
                    return CheckResult.Wait(
                        WaitingReason.WAITING_TYPES_MISSING,
                        $"Required type not found: {typeName}"
                    );
                }
            }
            return CheckResult.Ok();
        }

        /// <summary>
        /// 检查写入根目录是否合法
        /// </summary>
        public static CheckResult CheckWriteRoot(string projectWriteRoot)
        {
            if (string.IsNullOrEmpty(projectWriteRoot))
            {
                return CheckResult.Fail("projectWriteRoot is empty");
            }

            var normalized = PathUtil.NormalizeAssetPath(projectWriteRoot);

            // 必须在 Assets 目录下
            if (!PathUtil.IsUnderAssets(normalized))
            {
                return CheckResult.Fail($"projectWriteRoot must be under Assets/: {projectWriteRoot}");
            }

            // 必须在允许的根目录下
            bool allowed = false;
            foreach (var root in AutoGenSettings.AllowedWriteRoots)
            {
                var normalizedRoot = PathUtil.NormalizeAssetPath(root);
                if (normalized.StartsWith(normalizedRoot) || normalizedRoot.StartsWith(normalized))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
            {
                return CheckResult.Fail($"projectWriteRoot not in allowed roots: {projectWriteRoot}");
            }

            return CheckResult.Ok();
        }

        /// <summary>
        /// 检查资产路径是否允许写入
        /// </summary>
        public static CheckResult CheckAssetPathAllowed(string assetPath, string projectWriteRoot)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return CheckResult.Fail("Asset path is empty");
            }

            var normalized = PathUtil.NormalizeAssetPath(assetPath);
            var normalizedRoot = PathUtil.NormalizeAssetPath(projectWriteRoot);

            if (!normalized.StartsWith(normalizedRoot))
            {
                return CheckResult.Fail($"Asset path '{assetPath}' is not under projectWriteRoot '{projectWriteRoot}'");
            }

            return CheckResult.Ok();
        }

        /// <summary>
        /// 综合检查 Job 是否可执行
        /// </summary>
        public static CheckResult CheckJobCanExecute(JobData job)
        {
            // 检查编译状态
            var compileCheck = CheckNotCompiling();
            if (!compileCheck.CanProceed)
                return compileCheck;

            // 检查 Runner 版本
            var versionCheck = CheckRunnerVersion(job.runnerMinVersion);
            if (!versionCheck.CanProceed)
                return versionCheck;

            // 检查所需类型
            var typesCheck = CheckRequiredTypes(job.requiresTypes);
            if (!typesCheck.CanProceed)
                return typesCheck;

            // 检查写入根目录
            var writeRootCheck = CheckWriteRoot(job.projectWriteRoot);
            if (!writeRootCheck.CanProceed)
                return writeRootCheck;

            return CheckResult.Ok();
        }
    }
}
