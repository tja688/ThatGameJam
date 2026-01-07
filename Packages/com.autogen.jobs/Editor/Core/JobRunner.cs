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

        // 崩溃恢复：最大重试次数和超时阈值
        private const int MAX_ATTEMPTS = 3;
        private static readonly TimeSpan STALE_JOB_TIMEOUT = TimeSpan.FromMinutes(5);

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

            // 恢复 working 中的 Job（带状态检查）
            RecoverWorkingJobsWithState();

            // 注册 Tick
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            _initialized = true;
            AutoGenLog.Info($"JobRunner initialized (version {AutoGenSettings.RUNNER_VERSION})");
        }

        /// <summary>
        /// 带状态检查的崩溃恢复
        /// </summary>
        private static void RecoverWorkingJobsWithState()
        {
            var workingJobs = _queue.GetWorkingJobs();
            int recovered = 0;
            int deadLettered = 0;

            foreach (var jobFile in workingJobs)
            {
                var fileName = Path.GetFileNameWithoutExtension(jobFile);
                var stateFile = Path.Combine(AutoGenSettings.WorkingPath, $"{fileName}.state.json");

                // 检查状态文件
                if (File.Exists(stateFile))
                {
                    try
                    {
                        var stateJson = File.ReadAllText(stateFile);
                        var state = JobState.FromJson(stateJson);

                        // 检查重试次数
                        if (state.attempt >= MAX_ATTEMPTS)
                        {
                            AutoGenLog.Warning($"Job {state.jobId} exceeded max attempts ({MAX_ATTEMPTS}), moving to dead");
                            WriteRecoveryFailResult(state.jobId, $"Exceeded max attempts ({MAX_ATTEMPTS})");
                            _queue.MoveToDead(jobFile);
                            File.Delete(stateFile);
                            deadLettered++;
                            continue;
                        }

                        // 检查是否超时（可能死锁）
                        if (state.startedAtUtc != null)
                        {
                            var started = DateTime.Parse(state.startedAtUtc);
                            if (DateTime.UtcNow - started > STALE_JOB_TIMEOUT)
                            {
                                AutoGenLog.Warning($"Job {state.jobId} timed out (started at {state.startedAtUtc}), moving to dead");
                                WriteRecoveryFailResult(state.jobId, $"Job timed out after {STALE_JOB_TIMEOUT.TotalMinutes} minutes");
                                _queue.MoveToDead(jobFile);
                                File.Delete(stateFile);
                                deadLettered++;
                                continue;
                            }
                        }

                        // 增加重试次数并恢复
                        state.attempt++;
                        File.WriteAllText(stateFile, state.ToJson());

                        if (_queue.MoveBackToInbox(jobFile))
                        {
                            // 状态文件也移动
                            var inboxStateFile = Path.Combine(AutoGenSettings.InboxPath, $"{fileName}.state.json");
                            if (File.Exists(stateFile))
                                File.Move(stateFile, inboxStateFile);

                            recovered++;
                            AutoGenLog.Info($"Recovered job {state.jobId} (attempt {state.attempt})");
                        }
                    }
                    catch (Exception e)
                    {
                        AutoGenLog.Error($"Failed to process state file for {jobFile}: {e.Message}");
                        _queue.MoveToDead(jobFile);
                        deadLettered++;
                    }
                }
                else
                {
                    // 没有状态文件，创建一个并恢复
                    var state = new JobState { attempt = 1 };

                    if (_queue.MoveBackToInbox(jobFile))
                    {
                        recovered++;
                        AutoGenLog.Info($"Recovered job without state: {Path.GetFileName(jobFile)}");
                    }
                }
            }

            if (recovered > 0 || deadLettered > 0)
            {
                AutoGenLog.Info($"Recovery: {recovered} jobs recovered, {deadLettered} dead-lettered");
            }
        }

        /// <summary>
        /// 写入恢复失败的结果
        /// </summary>
        private static void WriteRecoveryFailResult(string jobId, string reason)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            var result = new JobResult(jobId);
            result.SetFailed("RECOVERY_FAILED", reason);
            _queue.WriteResult(result);
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
        /// 关键修复：先认领（move）再读取，避免半写入问题
        /// </summary>
        private static void ProcessJob(string jobFilePath)
        {
            string workingPath = null;
            JobData job = null;
            JobLogger logger = null;
            JobResult result = null;
            string stateFilePath = null;

            try
            {
                // ============================================================
                // 修复 A: 先认领（move 到 working），再读取文件
                // 这样可以避免读取到半写入的文件
                // ============================================================
                if (!_queue.MoveToWorking(jobFilePath, out workingPath))
                {
                    // 移动失败 - 可能被其他进程锁定（文件正在写入）或已被处理
                    AutoGenLog.Debug($"Failed to claim job (likely still being written): {Path.GetFileName(jobFilePath)}");
                    return;
                }

                // 从 working 目录读取文件（而非原 inbox）
                string json;
                try
                {
                    json = File.ReadAllText(workingPath);
                }
                catch (Exception e)
                {
                    AutoGenLog.Error($"Failed to read job file from working: {e.Message}");
                    _queue.MoveToDead(workingPath);
                    return;
                }

                // 解析 JSON
                try
                {
                    job = JobData.FromJson(json);
                }
                catch (Exception e)
                {
                    AutoGenLog.Error($"Failed to parse job JSON: {e.Message}");
                    // 写入解析失败的结果
                    var parseResult = new JobResult("unknown_" + Path.GetFileNameWithoutExtension(workingPath));
                    parseResult.SetFailed("JSON_PARSE_ERROR", e.Message);
                    _queue.WriteResult(parseResult);
                    _queue.MoveToDead(workingPath);
                    return;
                }

                if (job == null)
                {
                    AutoGenLog.Error($"Parsed job is null: {workingPath}");
                    _queue.MoveToDead(workingPath);
                    return;
                }

                logger = new JobLogger(job.jobId);
                result = new JobResult(job.jobId);

                logger.Info($"Processing job: {job.jobId}");

                // ============================================================
                // 修复 C: 写入状态文件，用于崩溃恢复
                // ============================================================
                stateFilePath = Path.Combine(AutoGenSettings.WorkingPath, $"{job.jobId}.state.json");
                var existingState = LoadJobState(stateFilePath);
                var state = existingState ?? new JobState();
                state.jobId = job.jobId;
                state.status = "RUNNING";
                state.startedAtUtc = DateTime.UtcNow.ToString("o");
                state.currentCommandIndex = 0;
                SaveJobState(stateFilePath, state);

                // 检查是否已完成（幂等检查）
                if (_queue.IsJobCompleted(job.jobId))
                {
                    logger.Info("Job already completed, skipping");
                    CleanupStateFile(stateFilePath);
                    File.Delete(workingPath);
                    return;
                }

                // 检查 schema 版本
                if (job.schemaVersion != 1)
                {
                    logger.Error($"Unsupported schema version: {job.schemaVersion}");
                    result.SetFailed("SCHEMA_ERROR", $"Unsupported schema version: {job.schemaVersion}");
                    _queue.WriteResult(result);
                    _queue.WriteLog(logger);
                    CleanupStateFile(stateFilePath);
                    _queue.MoveToDead(workingPath);
                    return;
                }

                // 执行门槛检查
                var check = Guards.CheckJobCanExecute(job);
                if (!check.CanProceed)
                {
                    if (check.IsWaiting)
                    {
                        // WAITING 状态：移回 inbox，等待下次处理
                        logger.Info($"Job waiting: {check.FailReason}");
                        result.SetWaiting(check.WaitReason.Value, check.FailReason);
                        _queue.WriteResult(result);

                        // 保持状态文件，移回 inbox
                        state.status = "WAITING";
                        SaveJobState(stateFilePath, state);
                        _queue.MoveBackToInbox(workingPath);

                        // 状态文件也移回
                        var inboxStatePath = Path.Combine(AutoGenSettings.InboxPath, $"{job.jobId}.state.json");
                        if (File.Exists(stateFilePath))
                        {
                            try { File.Move(stateFilePath, inboxStatePath); } catch { }
                        }
                        return;
                    }
                    else
                    {
                        // FAILED 状态
                        logger.Error($"Job failed precondition: {check.FailReason}");
                        result.SetFailed("PRECONDITION_FAILED", check.FailReason);
                        _queue.WriteResult(result);
                        _queue.WriteLog(logger);
                        CleanupStateFile(stateFilePath);
                        _queue.MoveToDead(workingPath);
                        return;
                    }
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

                    // 更新状态文件中的当前命令索引
                    state.currentCommandIndex = i;
                    SaveJobState(stateFilePath, state);

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

                // 清理状态文件并移动到 done
                CleanupStateFile(stateFilePath);
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

                // 清理状态文件
                CleanupStateFile(stateFilePath);

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
        /// 加载 Job 状态文件
        /// </summary>
        private static JobState LoadJobState(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                return JobState.FromJson(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存 Job 状态文件
        /// </summary>
        private static void SaveJobState(string path, JobState state)
        {
            try
            {
                File.WriteAllText(path, state.ToJson());
            }
            catch (Exception e)
            {
                AutoGenLog.Warning($"Failed to save job state: {e.Message}");
            }
        }

        /// <summary>
        /// 清理状态文件
        /// </summary>
        private static void CleanupStateFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
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

    /// <summary>
    /// Job 状态（用于崩溃恢复）
    /// </summary>
    [Serializable]
    public class JobState
    {
        public string jobId;
        public string status;
        public string startedAtUtc;
        public int currentCommandIndex;
        public int attempt;

        public string ToJson()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }

        public static JobState FromJson(string json)
        {
            return UnityEngine.JsonUtility.FromJson<JobState>(json);
        }
    }
}
