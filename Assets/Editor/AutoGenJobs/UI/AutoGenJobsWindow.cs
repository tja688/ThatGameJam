using System.Linq;
using AutoGenJobs.Commands;
using AutoGenJobs.Core;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.UI
{
    /// <summary>
    /// AutoGen Jobs 编辑器窗口
    /// 提供可视化的状态监控和操作界面
    /// </summary>
    public class AutoGenJobsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string[] _pendingJobs;
        private string[] _workingJobs;
        private double _lastRefreshTime;

        public static void ShowWindow()
        {
            var window = GetWindow<AutoGenJobsWindow>("AutoGen Jobs");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshJobLists();
        }

        private void OnGUI()
        {
            // 自动刷新
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > 1.0)
            {
                RefreshJobLists();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawStatusSection();
            EditorGUILayout.Space(10);

            DrawSettingsSection();
            EditorGUILayout.Space(10);

            DrawJobsSection();
            EditorGUILayout.Space(10);

            DrawCommandsSection();
            EditorGUILayout.Space(10);

            DrawActionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Runner Status:", GUILayout.Width(100));
                var status = JobRunner.GetStatus();
                var statusColor = status == "Running" ? Color.green :
                                  status == "Disabled" ? Color.gray : Color.yellow;
                var oldColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(status, EditorStyles.boldLabel);
                GUI.color = oldColor;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Version:", GUILayout.Width(100));
                EditorGUILayout.LabelField(AutoGenSettings.RUNNER_VERSION.ToString());
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Pending Jobs:", GUILayout.Width(100));
                EditorGUILayout.LabelField(_pendingJobs?.Length.ToString() ?? "0");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Working Jobs:", GUILayout.Width(100));
                EditorGUILayout.LabelField(_workingJobs?.Length.ToString() ?? "0");
            }
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Enable Runner:", GUILayout.Width(100));
                var newValue = EditorGUILayout.Toggle(AutoGenSettings.EnableRunner);
                if (newValue != AutoGenSettings.EnableRunner)
                {
                    AutoGenSettings.EnableRunner = newValue;
                    if (newValue)
                        JobRunner.Initialize();
                    else
                        JobRunner.Shutdown();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Verbose:", GUILayout.Width(100));
                AutoGenSettings.VerboseLogging = EditorGUILayout.Toggle(AutoGenSettings.VerboseLogging);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Tick Interval:", GUILayout.Width(100));
                AutoGenSettings.TickIntervalMs = EditorGUILayout.IntSlider(AutoGenSettings.TickIntervalMs, 50, 1000);
                EditorGUILayout.LabelField("ms", GUILayout.Width(30));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Jobs Root:", GUILayout.Width(100));
                EditorGUILayout.LabelField(AutoGenSettings.JobRootAbsolute, EditorStyles.miniLabel);
            }
        }

        private void DrawJobsSection()
        {
            EditorGUILayout.LabelField("Jobs", EditorStyles.boldLabel);

            if (_pendingJobs != null && _pendingJobs.Length > 0)
            {
                EditorGUILayout.LabelField("Pending:", EditorStyles.miniBoldLabel);
                foreach (var job in _pendingJobs.Take(5))
                {
                    EditorGUILayout.LabelField($"  • {System.IO.Path.GetFileName(job)}", EditorStyles.miniLabel);
                }
                if (_pendingJobs.Length > 5)
                {
                    EditorGUILayout.LabelField($"  ... and {_pendingJobs.Length - 5} more", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No pending jobs", EditorStyles.miniLabel);
            }

            if (_workingJobs != null && _workingJobs.Length > 0)
            {
                EditorGUILayout.LabelField("Working:", EditorStyles.miniBoldLabel);
                foreach (var job in _workingJobs)
                {
                    EditorGUILayout.LabelField($"  • {System.IO.Path.GetFileName(job)}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCommandsSection()
        {
            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);

            var commands = CommandRegistry.GetAllCommandNames();
            EditorGUILayout.LabelField($"Registered: {commands.Length}", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var displayCommands = string.Join(", ", commands.Take(10));
                if (commands.Length > 10)
                    displayCommands += $" ... (+{commands.Length - 10})";
                EditorGUILayout.LabelField(displayCommands, EditorStyles.miniLabel);
            }
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Inbox"))
                {
                    MenuItems.OpenInboxFolder();
                }

                if (GUILayout.Button("Open Results"))
                {
                    MenuItems.OpenResultsFolder();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Process Next Job"))
                {
                    JobRunner.ProcessNextJob();
                    RefreshJobLists();
                }

                if (GUILayout.Button("Refresh"))
                {
                    RefreshJobLists();
                    CommandRegistry.Refresh();
                }
            }
        }

        private void RefreshJobLists()
        {
            var queue = new JobQueue();
            _pendingJobs = queue.GetPendingJobs().ToArray();
            _workingJobs = queue.GetWorkingJobs().ToArray();
        }
    }
}
