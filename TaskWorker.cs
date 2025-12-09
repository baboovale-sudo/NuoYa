using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OLAPlug; // 确保引用了 SDK 命名空间

namespace OLA
{
    public class TaskWorker
    {
        public int RowIndex { get; set; }
        public string EmulatorName { get; set; }
        public string EmulatorClass { get; set; }
        public string EmulatorBasePath { get; set; }

        // 🔥 新增：包名属性 (默认值防止为空)
        public string PackageName { get; set; } = "com.syyx.nuoya.idle";

        public List<string> TaskList { get; set; } = new List<string>();

        public int RunState { get; private set; } = 0;
        public DateTime LastStartTime { get; private set; }

        private OLAPlugServer? _ola = null;
        private CancellationTokenSource? _logicTokenSource;

        private string _lastStatusMsg = "";
        private string _lastExceptionMsg = "";

        public Action<string>? LogCallback;
        public Action<int, string, string>? StatusCallback;
        public Action<int, string>? ExceptionCallback;

        // 🔥 修改构造函数：增加 packageName 参数
        public TaskWorker(int row, string name, string className, string path, string packageName = "")
        {
            this.RowIndex = row;
            this.EmulatorName = name;
            this.EmulatorClass = className;
            this.EmulatorBasePath = path;

            // 如果传入了包名就更新，没传就用默认值
            if (!string.IsNullOrEmpty(packageName))
            {
                this.PackageName = packageName;
            }
        }

        public void Start()
        {
            if (RunState == 1) return;
            RunState = 1;
            LastStartTime = DateTime.Now;
            UpdateException("等待60秒监控介入...");
            _logicTokenSource = new CancellationTokenSource();
            var token = _logicTokenSource.Token;
            Task.Run(() => RunLogicThread(token), token);
        }

        public void Stop()
        {
            RunState = 4;
            _logicTokenSource?.Cancel();
            UpdateStatus("已停止", "0");
            UpdateException("");
        }

        public void Pause()
        {
            if (RunState == 1) { RunState = 2; UpdateStatus("已暂停", ""); }
        }
        public void Resume()
        {
            if (RunState == 2) { RunState = 3; }
        }

        public bool IsAlive()
        {
            if (_ola is null) return false;
            return FindWindowWithPlugin() != 0;
        }

        public void MarkAsMonitored()
        {
            if (_lastExceptionMsg.Contains("等待") || _lastExceptionMsg.Contains("监控"))
            {
                UpdateException("监控中");
            }
        }

        public void PerformRestart()
        {
            Task.Run(() =>
            {
                UpdateStatus("掉线重连", "0");
                UpdateException("检测掉线，正在重启...");
                _logicTokenSource?.Cancel();
                RunState = 0;
                CloseEmulator();
                Thread.Sleep(3000);
                LogCallback?.Invoke("🔄 执行重启...");
                Start();
            });
        }

        private void RunLogicThread(CancellationToken token)
        {
            try
            {
                _ola = new OLAPlugServer();
                if (_ola.OLAObject == 0) { LogError("插件接口创建失败"); return; }

                long parentHwnd = 0;
                parentHwnd = FindWindowWithPlugin();

                if (parentHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;
                    UpdateStatus("启动中...", "0");
                    if (!LaunchEmulator()) { LogError("启动失败"); return; }

                    UpdateStatus("等待画面10s", "0");
                    try { Task.Delay(10000, token).Wait(); } catch { return; }

                    UpdateException("等待60秒监控介入...");
                    int retry = 0;
                    while (parentHwnd == 0 && retry < 30)
                    {
                        if (token.IsCancellationRequested) return;
                        parentHwnd = FindWindowWithPlugin();
                        if (parentHwnd != 0) break;
                        Thread.Sleep(1000);
                        retry++;
                    }
                }

                if (parentHwnd == 0) { LogError("启动超时"); return; }

                UpdateStatus("等待画面", parentHwnd.ToString());
                long childHwnd = 0;
                while (RunState != 4 && childHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;
                    childHwnd = _ola!.GetWindow(parentHwnd, 1);
                    if (childHwnd != 0) break;
                    Thread.Sleep(1000);
                }

                int ret = _ola!.BindWindowEx(childHwnd, Form1.OLAConfig.Bind_Display, Form1.OLAConfig.Bind_Mouse, Form1.OLAConfig.Bind_Keypad, "", Form1.OLAConfig.Bind_Mode);

                if (ret == 1)
                {
                    UpdateStatus("运行中", childHwnd.ToString());
                    LogCallback?.Invoke($"✅ 成功绑定窗口: 0x{childHwnd:X}");
                    try
                    {
                        DoGameLogic(token, childHwnd);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested) LogError($"逻辑异常:{ex.Message}");
                    }
                    RunState = 4;
                }
                else { LogError($"绑定失败:{ret}"); }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) LogError($"异常:{ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private void DoGameLogic(CancellationToken token, long currentHwnd)
        {
            if (TaskList == null || TaskList.Count == 0)
            {
                LogCallback?.Invoke("⚠️ 未分配任务");
                Thread.Sleep(2000);
                return;
            }

            // 🔥 初始化任务执行类
            // 传入 EnsureGameRunning 方法，以便在任务中随时拉起游戏
            var gameTask = new GameTask(
                _ola!,
                currentHwnd,
                (msg) => LogCallback?.Invoke(msg),
                (status, hwnd) => UpdateStatus(status, hwnd),
                () => CheckLoopState(token),
                () => EnsureGameRunning() // 传入启动逻辑
            );

            foreach (var taskName in TaskList)
            {
                CheckPauseState(token);
                if (RunState == 4) break;

                UpdateStatus($"执行中-{taskName}", currentHwnd.ToString());
                LogCallback?.Invoke($"👉 开始执行: {taskName}");

                try
                {
                    gameTask.Execute(taskName);
                }
                catch (Exception ex)
                {
                    LogCallback?.Invoke($"❌ 任务[{taskName}]出错: {ex.Message}");
                }

                if (RunState == 4) break;

                LogCallback?.Invoke($"✅ {taskName} 已完成");
                Thread.Sleep(1000);
            }

            if (RunState != 4)
            {
                UpdateStatus("任务已全部完成", currentHwnd.ToString());
                LogCallback?.Invoke("🎉 所有任务已完成");
            }
        }

        private bool CheckLoopState(CancellationToken token)
        {
            if (token.IsCancellationRequested) return true;
            CheckPauseState(token);
            return RunState == 4;
        }

        private void CheckPauseState(CancellationToken token)
        {
            while (RunState == 2)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(500);
            }
            if (RunState == 3) { RunState = 1; }
            token.ThrowIfCancellationRequested();
        }

        // 🔥 核心方法：确保游戏运行 (使用动态包名 PackageName)
        private void EnsureGameRunning()
        {
            if (EmulatorName.Contains("雷电"))
            {
                try
                {
                    string indexStr = "0";
                    if (EmulatorName.Contains("-"))
                    {
                        string[] parts = EmulatorName.Split('-');
                        indexStr = parts[parts.Length - 1];
                    }

                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    if (!File.Exists(cmdExe))
                    {
                        LogCallback?.Invoke("⚠️ 未找到 ldconsole.exe");
                        return;
                    }

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = cmdExe;
                    // 🔥 使用 launchex 和 this.PackageName 启动/置顶游戏
                    psi.Arguments = $"launchex --index {indexStr} --packagename {this.PackageName}";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process.Start(psi);

                    LogCallback?.Invoke($"🚀 正在拉起游戏: {this.PackageName}");
                }
                catch (Exception ex)
                {
                    LogCallback?.Invoke($"❌ 启动指令失败: {ex.Message}");
                }
            }
        }

        private long FindWindowWithPlugin()
        {
            if (_ola is null) return 0;
            long hwnd = _ola.FindWindow(EmulatorClass, EmulatorName);
            if (hwnd == 0) { hwnd = _ola.FindWindow(EmulatorClass, EmulatorName + "(64)"); }
            if (hwnd == 0 && EmulatorName.EndsWith("-0"))
            {
                string altName = EmulatorName.Replace("-0", "");
                hwnd = _ola.FindWindow(EmulatorClass, altName);
                if (hwnd == 0) hwnd = _ola.FindWindow(EmulatorClass, altName + "(64)");
            }
            return hwnd;
        }

        private bool LaunchEmulator()
        {
            try
            {
                string cmdExe = "";
                string args = ""; string indexStr = "0";
                if (EmulatorName.Contains("-")) { string[] parts = EmulatorName.Split('-'); indexStr = parts[parts.Length - 1]; }

                if (EmulatorName.Contains("雷电"))
                {
                    cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    // 启动时也带上包名 (launchex)
                    args = $"launchex --index {indexStr} --packagename {this.PackageName}";
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string parentDir = Directory.GetParent(EmulatorBasePath)?.FullName ?? "";
                    string shellPath = Path.Combine(parentDir, "shell");
                    cmdExe = Path.Combine(shellPath, "MuMuManager.exe");
                    if (!File.Exists(cmdExe)) cmdExe = Path.Combine(EmulatorBasePath, "MuMuManager.exe");
                    args = $"player launch {indexStr}";
                }
                if (!File.Exists(cmdExe)) return false;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cmdExe; psi.Arguments = args; psi.UseShellExecute = false; psi.CreateNoWindow = true;
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        private void CloseEmulator()
        {
            try
            {
                string cmdExe = "";
                string args = ""; string indexStr = "0";
                if (EmulatorName.Contains("-")) { string[] parts = EmulatorName.Split('-'); indexStr = parts[parts.Length - 1]; }
                if (EmulatorName.Contains("雷电")) { cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe"); args = $"quit --index {indexStr}"; }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string parentDir = Directory.GetParent(EmulatorBasePath)?.FullName ?? "";
                    string shellPath = Path.Combine(parentDir, "shell");
                    cmdExe = Path.Combine(shellPath, "MuMuManager.exe");
                    if (!File.Exists(cmdExe)) cmdExe = Path.Combine(EmulatorBasePath, "MuMuManager.exe");
                    args = $"player shutdown {indexStr}";
                }
                if (File.Exists(cmdExe)) { Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = args, UseShellExecute = false, CreateNoWindow = true }); }
            }
            catch { }
        }

        private void LogError(string msg)
        {
            LogCallback?.Invoke($"❌ {msg}");
            UpdateStatus("错误", "0");
            UpdateException(msg);
        }

        private void UpdateStatus(string status, string hwnd)
        {
            if (_lastStatusMsg != status)
            {
                _lastStatusMsg = status;
                StatusCallback?.Invoke(RowIndex, status, hwnd);
            }
        }

        private void UpdateException(string msg)
        {
            if (_lastExceptionMsg != msg)
            {
                _lastExceptionMsg = msg;
                ExceptionCallback?.Invoke(RowIndex, msg);
            }
        }

        private void Cleanup()
        {
            if (_ola != null)
            {
                _ola.UnBindWindow();
                _ola.ReleaseObj();
                _ola = null;
            }
            if (RunState == 4)
            {
                UpdateStatus("已停止", "0");
                UpdateException("");
            }
        }
    }
}