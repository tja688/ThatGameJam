using System;
using System.Diagnostics;
using System.IO;
using AutoGenJobs.Commands;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// AutoGen Job Runner
    /// 核心运行器，负责定期扫描和执行 Job
    /// </summary>
    [InitializeOnLoad]
    public static class JobRunner
    {
        private static JobQueue _queue;
        private static double _lastTickTime;
        private static bool _initialized;
        private static int _jobsProcessedThisTick;

        static JobRunner()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化 Runner
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _queue = new JobQueue();
            _lastTickTime = EditorApplication.timeSinceStartup;

            // 确保目录存在
            AutoGenSettings.EnsureDirectoriesExist();

            // 恢复 working 中的 Job
            var recovered = _queue.RecoverWorkingJobs();
            if (recovered > 0)
            {
                AutoGenLog.Info($"Recovered {recovered} jobs from working directory");
            }

            // 注册 Tick
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            _initialized = true;
            AutoGenLog.Info($"JobRunner initialized (version {AutoGenSettings.RUNNER_VERSION})");
        }

        /// <summary>
        /// 停止 Runner
        /// </summary>
        public static void Shutdown()
        {
            EditorApplication.update -= Tick;
            _initialized = false;
            AutoGenLog.Info("JobRunner shutdown");
        }

        /// <summary>
        /// 主循环 Tick
        /// </summary>
        private static void Tick()
        {
            if (!AutoGenSettings.EnableRunner) return;

            // 检查 Tick 间隔
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastTickTime < AutoGenSettings.TickIntervalMs / 1000.0)
                return;

            _lastTickTime = now;
            _jobsProcessedThisTick = 0;

            // 检查编译状态
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            // 处理 Job
            var stopwatch = Stopwatch.StartNew();
            var pendingJobs = _queue.GetPendingJobs();

            foreach (var jobFile in pendingJobs)
            {
                // 检查时间和数量限制
                if (stopwatch.ElapsedMilliseconds >= AutoGenSettings.MaxMsPerTick)
                    break;
                if (_jobsProcessedThisTick >= AutoGenSettings.MaxJobsPerTick)
                    break;

                ProcessJob(jobFile);
                _jobsProcessedThisTick++;
            }
        }

        /// <summary>
        /// 处理单个 Job
        /// </summary>
        private static void ProcessJob(string jobFilePath)
        {
            string workingPath = null;
            JobData job = null;
            JobLogger logger = null;
            JobResult result = null;

            try
            {
                // 读取 Job 文件
                var json = File.ReadAllText(jobFilePath);
                job = JobData.FromJson(json);

                if (job == null)
                {
                    AutoGenLog.Error($"Failed to parse job file: {jobFilePath}");
                    _queue.MoveToDead(jobFilePath);
                    return;
                }

                logger = new JobLogger(job.jobId);
                result = new JobResult(job.jobId);

                logger.Info($"Processing job: {job.jobId}");

                // 检查是否已完成（幂等检查）
                if (_queue.IsJobCompleted(job.jobId))
                {
                    logger.Info("Job already completed, skipping");
                    File.Delete(jobFilePath);
                    return;
                }

                // 检查 schema 版本
                if (job.schemaVersion != 1)
                {
                    logger.Error($"Unsupported schema version: {job.schemaVersion}");
                    result.SetFailed("SCHEMA_ERROR", $"Unsupported schema version: {job.schemaVersion}");
                    _queue.WriteResult(result);
                    _queue.WriteLog(logger);
                    _queue.MoveToDead(jobFilePath);
                    return;
                }

                // 执行门槛检查
                var check = Guards.CheckJobCanExecute(job);
                if (!check.CanProceed)
                {
                    if (check.IsWaiting)
                    {
                        // WAITING 状态：不移动文件，等待下次处理
                        logger.Info($"Job waiting: {check.FailReason}");
                        result.SetWaiting(check.WaitReason.Value, check.FailReason);
                        _queue.WriteResult(result);
                        return;
                    }
                    else
                    {
                        // FAILED 状态
                        logger.Error($"Job failed precondition: {check.FailReason}");
                        result.SetFailed("PRECONDITION_FAILED", check.FailReason);
                        _queue.WriteResult(result);
                        _queue.WriteLog(logger);
                        _queue.MoveToDead(jobFilePath);
                        return;
                    }
                }

                // 移动到 working
                if (!_queue.MoveToWorking(jobFilePath, out workingPath))
                {
                    logger.Warning("Failed to move job to working (may be locked by another process)");
                    return;
                }

                // 创建执行上下文
                var ctx = new CommandContext(job, logger);

                // 执行命令
                bool allSuccess = true;
                for (int i = 0; i < job.commands.Count; i++)
                {
                    var cmdData = job.commands[i];
                    ctx.CurrentCommandIndex = i;
                    logger.SetCurrentCommand(i);

                    // 获取命令
                    var command = CommandRegistry.GetCommand(cmdData.cmd);
                    if (command == null)
                    {
                        logger.Error($"Unknown command: {cmdData.cmd}");
                        result.AddCommandResult(new CommandResult(i, cmdData.cmd) { status = "FAILED", message = "Unknown command" });
                        result.SetFailed("UNKNOWN_COMMAND", $"Unknown command: {cmdData.cmd}");
                        allSuccess = false;
                        break;
                    }

                    logger.Info($"Executing: {cmdData.cmd}");

                    // 执行命令
                    var cmdResult = new CommandResult(i, cmdData.cmd);
                    try
                    {
                        var execResult = command.Execute(ctx, cmdData.args);

                        if (execResult.Success)
                        {
                            // 处理输出变量
                            var outputVars = cmdData.GetOutputVars();
                            foreach (var kvp in outputVars)
                            {
                                if (execResult.Outputs.TryGetValue(kvp.Key, out var outputObj))
                                {
                                    ctx.SetVar(kvp.Value, outputObj);
                                }
                            }

                            cmdResult.SetDone(execResult.GetOutputStrings(), execResult.Message);
                            logger.Info($"Command {cmdData.cmd} completed");
                        }
                        else
                        {
                            cmdResult.SetFailed(execResult.Message);
                            result.SetFailed("CMD_EXEC_ERROR", execResult.Message, execResult.Exception?.StackTrace);
                            allSuccess = false;
                            result.AddCommandResult(cmdResult);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Command execution error: {e.Message}\n{e.StackTrace}");
                        cmdResult.SetFailed(e.Message);
                        result.SetFailed("CMD_EXCEPTION", e.Message, e.StackTrace);
                        allSuccess = false;
                        result.AddCommandResult(cmdResult);
                        break;
                    }

                    result.AddCommandResult(cmdResult);
                }

                // 设置最终状态
                if (allSuccess)
                {
                    result.SetDone();
                    logger.Info("Job completed successfully");
                }

                // 写入结果
                _queue.WriteResult(result);
                _queue.WriteLog(logger);

                // 移动到 done
                if (workingPath != null)
                {
                    _queue.MoveToDone(workingPath);
                }
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Job processing error: {e.Message}\n{e.StackTrace}");

                if (result != null)
                {
                    result.SetFailed("RUNNER_ERROR", e.Message, e.StackTrace);
                    _queue.WriteResult(result);
                }

                if (logger != null)
                {
                    logger.Error($"Runner error: {e.Message}");
                    _queue.WriteLog(logger);
                }

                // 移动到 dead
                if (workingPath != null && File.Exists(workingPath))
                {
                    _queue.MoveToDead(workingPath);
                }
                else if (File.Exists(jobFilePath))
                {
                    _queue.MoveToDead(jobFilePath);
                }
            }
        }

        /// <summary>
        /// 手动触发处理一个 Job（用于测试）
        /// </summary>
        public static void ProcessNextJob()
        {
            var pendingJobs = _queue.GetPendingJobs();
            if (pendingJobs.Count > 0)
            {
                ProcessJob(pendingJobs[0]);
            }
        }

        /// <summary>
        /// 获取 Runner 状态
        /// </summary>
        public static string GetStatus()
        {
            if (!AutoGenSettings.EnableRunner)
                return "Disabled";
            if (EditorApplication.isCompiling)
                return "Compiling...";
            if (EditorApplication.isUpdating)
                return "Updating...";
            return "Running";
        }
    }
}
