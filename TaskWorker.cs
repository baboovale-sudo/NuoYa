using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OLAPlug;

namespace OLA
{
    public class TaskWorker
    {
        public int RowIndex { get; set; }
        public string EmulatorName { get; set; }
        public string EmulatorClass { get; set; }
        public string EmulatorBasePath { get; set; }
        public string PackageName { get; set; } = "com.xy.sh.wjsy5774";
        public List<string> TaskList { get; set; } = new List<string>();
        public int RunState { get; private set; } = 0;
        public DateTime LastStartTime { get; private set; }

        public OLAPlugServer Ola => _ola!;
        public long CurrentBindHwnd { get; private set; } = 0;

        private OLAPlugServer? _ola = null;
        private CancellationTokenSource? _logicTokenSource;
        private CancellationToken _currentToken;

        private string _lastStatusMsg = "";
        private string _lastExceptionMsg = "";
        private Random _rnd = new Random();

        public Action<string>? LogCallback;
        public Action<int, string, string>? StatusCallback;
        public Action<int, string>? ExceptionCallback;

        public TaskWorker(int row, string name, string className, string path, string packageName = "")
        {
            this.RowIndex = row;
            this.EmulatorName = name;
            this.EmulatorClass = className;
            this.EmulatorBasePath = path;
            if (!string.IsNullOrEmpty(packageName)) this.PackageName = packageName;
        }

        #region 生命周期控制
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

        public void Pause() { if (RunState == 1) { RunState = 2; UpdateStatus("已暂停", ""); } }
        public void Resume() { if (RunState == 2) { RunState = 3; } }
        public bool IsAlive() => _ola != null && FindWindowWithPlugin() != 0;

        public void MarkAsMonitored()
        {
            if (_lastExceptionMsg.Contains("等待") || _lastExceptionMsg.Contains("监控")) UpdateException("监控中");
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
                LogCallback?.Invoke("执行重启...");
                Start();
            });
        }
        #endregion

        #region 逻辑线程核心
        private void RunLogicThread(CancellationToken token)
        {
            try
            {
                _ola = new OLAPlugServer();
                if (_ola.OLAObject == 0) { LogError("插件接口创建失败"); return; }

                string imageBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                _ola.SetPath(imageBasePath);

                long parentHwnd = FindWindowWithPlugin();
                if (parentHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;
                    UpdateStatus("启动中...", "0");
                    if (!LaunchEmulator()) { LogError("启动失败: 请检查日志"); return; }
                    UpdateStatus("等待画面10s", "0");
                    try { Task.Delay(10000, token).Wait(); } catch { return; }

                    UpdateException("等待60秒监控介入...");
                    int retry = 0;
                    while (parentHwnd == 0 && retry < 60)
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
                    LogCallback?.Invoke($"成功绑定窗口: 0x{childHwnd:X}");
                    try { DoGameLogic(token, childHwnd); }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { if (!token.IsCancellationRequested) LogError($"逻辑异常:{ex.Message}"); }
                    RunState = 4;
                }
                else { LogError($"绑定失败:{ret}"); }
            }
            catch (Exception ex) { if (!token.IsCancellationRequested) LogError($"异常:{ex.Message}"); }
            finally { Cleanup(); }
        }

        private void DoGameLogic(CancellationToken token, long currentHwnd)
        {
            _currentToken = token;
            CurrentBindHwnd = currentHwnd;

            if (TaskList == null || TaskList.Count == 0)
            {
                LogCallback?.Invoke("未分配任务");
                Thread.Sleep(2000);
                return;
            }

            var gameTask = new GameTask(this);
            foreach (var taskName in TaskList)
            {
                CheckPauseState();
                if (RunState == 4) break;
                LogCallback?.Invoke($"开始执行: {taskName}");
                try { gameTask.Execute(taskName); }
                catch (Exception ex) { LogCallback?.Invoke($"任务[{taskName}]出错: {ex.Message}"); }
                if (RunState == 4) break;
                LogCallback?.Invoke($"{taskName} 已完成");
                Thread.Sleep(1000);
            }
            if (RunState != 4)
            {
                UpdateStatus("任务已全部完成", currentHwnd.ToString());
                LogCallback?.Invoke("所有任务已完成");
            }
        }
        #endregion

        // =======================================================================
        // 🔥🔥🔥 OL_SDK 标准封装方法区 🔥🔥🔥
        // =======================================================================
        public bool OL_MatchWindowsFromPath(int x1, int y1, int x2, int y2, string imgName, int targetX, int targetY, int delay, int offset = 5, double sim = 0.85)
        {
            var res = _ola!.MatchWindowsFromPath(x1, y1, x2, y2, imgName, sim, 0, 0, 1.0);
            if (res != null && res.MatchState) { OL_LeftClick(targetX, targetY, offset); SmartSleep(delay); return true; }
            return false;
        }

        public bool OL_CmpColor(string pointsStr, int targetX, int targetY, int delay, int offset = 5)
        {
            if (string.IsNullOrEmpty(pointsStr)) return false;
            string[] points = pointsStr.Split('|');
            foreach (string p in points)
            {
                string[] item = p.Split(','); if (item.Length < 3) continue;
                if (_ola!.CmpColor(int.Parse(item[0]), int.Parse(item[1]), item[2], item[2]) == 0) return false;
            }
            OL_LeftClick(targetX, targetY, offset); SmartSleep(delay); return true;
        }

        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int delay)
        {
            int x, y; if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1) { OL_LeftClick(x, y); SmartSleep(delay); return true; }
            return false;
        }

        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int clickX, int clickY, int delay)
        {
            int x, y; if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1) { OL_LeftClick(clickX, clickY); SmartSleep(delay); return true; }
            return false;
        }

        public string OL_OcrFromDict(int x1, int y1, int x2, int y2, string color) => _ola!.OcrFromDict(x1, y1, x2, y2, color, "无尽黑暗.txt", 0.8) ?? "";

        public void OL_LeftClick(int x, int y, int range = 5)
        {
            _ola!.MoveTo(x + _rnd.Next(-range, range + 1), y + _rnd.Next(-range, range + 1));
            Thread.Sleep(_rnd.Next(30, 100)); _ola.LeftDown(); Thread.Sleep(_rnd.Next(50, 200)); _ola.LeftUp();
        }

        public bool SmartSleep(int ms)
        {
            int slice = 100, count = ms / slice;
            for (int i = 0; i < count; i++) { if (CheckLoopState()) return false; Thread.Sleep(slice); }
            if ((ms % slice) > 0) { if (CheckLoopState()) return false; Thread.Sleep(ms % slice); }
            return true;
        }

        #region 内部辅助方法 (MuMu 专用修正版)

        public void EnsureGameRunning()
        {
            if (EmulatorName.Contains("雷电"))
            {
                try
                {
                    string indexStr = "0";
                    if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[1];
                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    if (!File.Exists(cmdExe)) { LogCallback?.Invoke("未找到 ldconsole.exe"); return; }
                    Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = $"launchex --index {indexStr} --packagename {this.PackageName}", UseShellExecute = false, CreateNoWindow = true });
                    LogCallback?.Invoke($"正在拉起游戏: {this.PackageName}");
                }
                catch (Exception ex) { LogCallback?.Invoke($"启动指令失败: {ex.Message}"); }
            }
        }

        private bool CheckLoopState()
        {
            if (_currentToken.IsCancellationRequested) return true;
            CheckPauseState();
            return RunState == 4;
        }

        private void CheckPauseState()
        {
            bool wasPaused = false;
            while (RunState == 2) { wasPaused = true; _currentToken.ThrowIfCancellationRequested(); Thread.Sleep(500); }
            if (RunState == 3) RunState = 1;
            if (wasPaused) UpdateStatus("运行中", CurrentBindHwnd.ToString());
            _currentToken.ThrowIfCancellationRequested();
        }

        // 保持原样：插件找标题
        private long FindWindowWithPlugin()
        {
            if (_ola is null) return 0;
            long hwnd = _ola.FindWindow(EmulatorClass, EmulatorName);
            if (hwnd == 0) hwnd = _ola.FindWindow(EmulatorClass, EmulatorName + "(64)");
            if (hwnd == 0 && EmulatorName.EndsWith("-0"))
            {
                string altName = EmulatorName.Replace("-0", "");
                hwnd = _ola.FindWindow(EmulatorClass, altName);
                if (hwnd == 0) hwnd = _ola.FindWindow(EmulatorClass, altName + "(64)");
            }
            return hwnd;
        }

        // ----------------------------------------------------------------------------------
        // [核心修正] MuMuManager 调用逻辑
        // ----------------------------------------------------------------------------------
        private string ExecuteMuMuManager(string args)
        {
            try
            {
                string managerPath = "";

                // 1. 优先尝试拼接 nx_main (针对用户只填了安装根目录的情况)
                string pathWithSub = Path.Combine(EmulatorBasePath, "nx_main", "MuMuManager.exe");

                // 2. 备选尝试直接拼接 (针对用户已经填了 nx_main 目录的情况)
                string pathDirect = Path.Combine(EmulatorBasePath, "MuMuManager.exe");

                // 智能判断用哪个
                if (File.Exists(pathWithSub))
                {
                    managerPath = pathWithSub;
                }
                else if (File.Exists(pathDirect))
                {
                    managerPath = pathDirect;
                }
                else
                {
                    // 都找不到，打印报错
                    LogCallback?.Invoke($"[路径错误] 找不到 MuMuManager.exe");
                    LogCallback?.Invoke($"已尝试: {pathWithSub}");
                    LogCallback?.Invoke($"已尝试: {pathDirect}");
                    return "";
                }

                LogCallback?.Invoke($"[调试] 运行工具: {managerPath} {args}");

                Process p = new Process();
                p.StartInfo.FileName = managerPath;
                p.StartInfo.Arguments = args;

                // 强制设置工作目录，防止缺少 DLL
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(managerPath);

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true; // 抓取错误输出
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd(); // 看看有没有报错
                p.WaitForExit();

                if (!string.IsNullOrEmpty(error)) LogCallback?.Invoke($"[CMD报错] {error}");

                return output;
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"CMD异常: {ex.Message}");
                return "";
            }
        }

        private bool LaunchEmulator()
        {
            try
            {
                string indexStr = "0";
                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    string args = $"launchex --index {indexStr} --packagename {this.PackageName}";
                    if (File.Exists(cmdExe))
                    {
                        Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = args, UseShellExecute = false, CreateNoWindow = true });
                        return true;
                    }
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    // MuMu 启动：直接发送指令，不判断返回值（防止静默启动误判）
                    ExecuteMuMuManager($"control -v {indexStr} launch");

                    if (!string.IsNullOrEmpty(this.PackageName))
                    {
                        Thread.Sleep(2000);
                        ExecuteMuMuManager($"control -v {indexStr} app launch -pkg {this.PackageName}");
                    }
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private void CloseEmulator()
        {
            try
            {
                string indexStr = "0";
                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    if (File.Exists(cmdExe)) Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = $"quit --index {indexStr}", UseShellExecute = false, CreateNoWindow = true });
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    ExecuteMuMuManager($"control -v {indexStr} shutdown");
                }
            }
            catch { }
        }

        private void LogError(string msg) { LogCallback?.Invoke($"{msg}"); UpdateStatus("错误", "0"); UpdateException(msg); }
        private void UpdateStatus(string status, string hwnd) { if (_lastStatusMsg != status) { _lastStatusMsg = status; StatusCallback?.Invoke(RowIndex, status, hwnd); } }
        private void UpdateException(string msg) { if (_lastExceptionMsg != msg) { _lastExceptionMsg = msg; ExceptionCallback?.Invoke(RowIndex, msg); } }
        private void Cleanup() { if (_ola != null) { _ola.UnBindWindow(); _ola.ReleaseObj(); _ola = null; } if (RunState == 4) { UpdateStatus("已停止", "0"); UpdateException(""); } }
        #endregion
    }
}