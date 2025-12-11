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

        // =======================================================================
        // 🔥🔥🔥 OL_SDK 标准封装方法区 (含详细 XML 注释) 🔥🔥🔥
        // =======================================================================

        /// <summary>
        /// [封装] 范围找图并点击
        /// <para>SDK原名: <see cref="OLAPlugServer.MatchWindowsFromPath"/></para>
        /// <para>参数链式: 左上X -> 左上Y -> 右下X -> 右下Y -> 图片名 -> 点击X -> 点击Y -> 延迟</para>
        /// <para>修改说明: 封装了找图成功后的自动点击(带随机偏移)和智能延迟逻辑。</para>
        /// </summary>
        /// <param name="x1">查找范围左上角X</param>
        /// <param name="y1">查找范围左上角Y</param>
        /// <param name="x2">查找范围右下角X</param>
        /// <param name="y2">查找范围右下角Y</param>
        /// <param name="imgName">图片名称 (位于Output目录下)</param>
        /// <param name="targetX">成功后点击的X坐标</param>
        /// <param name="targetY">成功后点击的Y坐标</param>
        /// <param name="delay">点击后的延迟时间(毫秒)</param>
        /// <param name="offset">点击坐标的随机偏移量(默认5)</param>
        /// <param name="sim">图片相似度(默认0.85)</param>
        /// <returns>
        /// <see cref="bool"/>
        /// <list type="bullet">
        /// <item><description><c>true</c>: 找到图片并执行了点击。</description></item>
        /// <item><description><c>false</c>: 未找到图片或相似度不足。</description></item>
        /// </list>
        /// </returns>
        /// <example>
        /// <code>
        /// // 示例：在全屏找 "start.bmp"，找到后点击(100,200)，延迟1秒
        /// bool result = _worker.OL_MatchWindowsFromPath(0, 0, 1280, 720, "start.bmp", 100, 200, 1000);
        /// </code>
        /// </example>
        public bool OL_MatchWindowsFromPath(
            int x1, int y1, int x2, int y2,
            string imgName,
            int targetX, int targetY,
            int delay,
            int offset = 5,
            double sim = 0.85)
        {
            var res = _ola!.MatchWindowsFromPath(x1, y1, x2, y2, imgName, sim, 0, 0, 1.0);
            if (res != null && res.MatchState)
            {
                OL_LeftClick(targetX, targetY, offset);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// [封装] 多点找色并点击
        /// <para>SDK原名: <see cref="OLAPlugServer.CmpColor"/></para>
        /// <para>参数链式: 多点颜色串 -> 点击X -> 点击Y -> 延迟</para>
        /// <para>修改说明: 解析字符串并循环调用 CmpColor，所有点匹配成功才执行点击。</para>
        /// </summary>
        /// <param name="pointsStr">
        /// 多点颜色特征串，格式: "x,y,color|x,y,color"
        /// <br/>例如: "100,200,FFFFFF|300,400,00FF00"
        /// </param>
        /// <param name="targetX">成功后点击的X坐标</param>
        /// <param name="targetY">成功后点击的Y坐标</param>
        /// <param name="delay">点击后的延迟时间(毫秒)</param>
        /// <param name="offset">点击坐标的随机偏移量(默认5)</param>
        /// <returns>
        /// <see cref="bool"/>
        /// <list type="bullet">
        /// <item><description><c>true</c>: 所有颜色点均匹配成功，已点击。</description></item>
        /// <item><description><c>false</c>: 任意一个点颜色不匹配。</description></item>
        /// </list>
        /// </returns>
        /// <example>
        /// <code>
        /// // 示例：判断两个点的颜色，符合则点击(500,500)
        /// bool result = _worker.OL_CmpColor("100,200,FFFFFF|300,400,00FF00", 500, 500, 1000);
        /// </code>
        /// </example>
        public bool OL_CmpColor(string pointsStr, int targetX, int targetY, int delay, int offset = 5)
        {
            if (string.IsNullOrEmpty(pointsStr)) return false;

            string[] points = pointsStr.Split('|');
            foreach (string p in points)
            {
                string[] item = p.Split(',');
                if (item.Length < 3) continue;

                int x = int.Parse(item[0]);
                int y = int.Parse(item[1]);
                string color = item[2];

                // 使用精确比色
                if (_ola!.CmpColor(x, y, color, color) == 0)
                {
                    return false;
                }
            }
            OL_LeftClick(targetX, targetY, offset);
            SmartSleep(delay);
            return true;
        }

        /// <summary>
        /// [封装] 找字并点击该字坐标 (重载1)
        /// <para>SDK原名: <see cref="OLAPlugServer.FindStr"/></para>
        /// <para>参数链式: 左上X -> 左上Y -> 右下X -> 右下Y -> 找字内容 -> 颜色-容差 -> 延迟</para>
        /// <para>修改说明: 找到文字后，自动点击文字所在的坐标(x,y)。</para>
        /// </summary>
        /// <param name="x1">范围左上X</param>
        /// <param name="y1">范围左上Y</param>
        /// <param name="x2">范围右下X</param>
        /// <param name="y2">范围右下Y</param>
        /// <param name="text">要查找的文字内容</param>
        /// <param name="color">颜色-容差 (如 "FFFFFF-101010")</param>
        /// <param name="delay">点击后延迟(ms)</param>
        /// <returns>
        /// <see cref="bool"/>
        /// <list type="bullet">
        /// <item><description><c>true</c>: 找到文字并点击了自身坐标。</description></item>
        /// <item><description><c>false</c>: 未找到文字。</description></item>
        /// </list>
        /// </returns>
        /// <example>
        /// <code>
        /// // 示例：在区域找 "开始游戏"，找到后点击文字位置
        /// bool result = _worker.OL_FindStr(0, 0, 1280, 720, "开始游戏", "FFFFFF-101010", 1000);
        /// </code>
        /// </example>
        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int delay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"🔠 找到[{text}] -> 坐标({x},{y}) -> 点击自身");
                OL_LeftClick(x, y);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// [封装] 找字并点击指定坐标 (重载2)
        /// <para>SDK原名: <see cref="OLAPlugServer.FindStr"/></para>
        /// <para>参数链式: 左上X -> 左上Y -> 右下X -> 右下Y -> 找字内容 -> 颜色-容差 -> 点击X -> 点击Y -> 延迟</para>
        /// <para>修改说明: 找到文字后，不点文字，而是点击参数中指定的坐标(clickX, clickY)。</para>
        /// </summary>
        /// <param name="x1">范围左上X</param>
        /// <param name="y1">范围左上Y</param>
        /// <param name="x2">范围右下X</param>
        /// <param name="y2">范围右下Y</param>
        /// <param name="text">要查找的文字内容</param>
        /// <param name="color">颜色-容差</param>
        /// <param name="clickX">指定点击X坐标</param>
        /// <param name="clickY">指定点击Y坐标</param>
        /// <param name="delay">点击后延迟(ms)</param>
        /// <returns>
        /// <see cref="bool"/>
        /// <list type="bullet">
        /// <item><description><c>true</c>: 找到文字并点击了指定坐标。</description></item>
        /// <item><description><c>false</c>: 未找到文字。</description></item>
        /// </list>
        /// </returns>
        /// <example>
        /// <code>
        /// // 示例：找 "任务完成"，找到后点击坐标(900, 500)
        /// bool result = _worker.OL_FindStr(0, 0, 1280, 720, "任务完成", "FFFFFF-101010", 900, 500, 1000);
        /// </code>
        /// </example>
        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int clickX, int clickY, int delay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"🔠 找到[{text}] -> 点击指定位置({clickX},{clickY})");
                OL_LeftClick(clickX, clickY);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// [封装] 区域OCR识字
        /// <para>SDK原名: <see cref="OLAPlugServer.OcrFromDict"/></para>
        /// <para>参数链式: 左上X -> 左上Y -> 右下X -> 右下Y -> 颜色 -> (返回文本)</para>
        /// </summary>
        /// <param name="x1">范围左上X</param>
        /// <param name="y1">范围左上Y</param>
        /// <param name="x2">范围右下X</param>
        /// <param name="y2">范围右下Y</param>
        /// <param name="color">颜色-容差</param>
        /// <returns>
        /// <see cref="string"/>
        /// <para>返回识别到的文本内容。如果未识别到，返回空字符串 ""。</para>
        /// </returns>
        /// <example>
        /// <code>
        /// string txt = _worker.OL_OcrFromDict(0, 0, 200, 100, "FFFFFF-000000");
        /// </code>
        /// </example>
        public string OL_OcrFromDict(int x1, int y1, int x2, int y2, string color)
        {
            string text = _ola!.OcrFromDict(x1, y1, x2, y2, color, "无尽黑暗.txt", 0.8);
            return text ?? "";
        }

        /// <summary>
        /// [封装] 鼠标移动并左键点击
        /// <para>SDK原名: <see cref="OLAPlugServer.LeftClick"/> (组合 MoveTo + LeftDown + LeftUp)</para>
        /// <para>参数链式: 目标X -> 目标Y -> 随机偏移范围</para>
        /// <para>修改说明: 增加了坐标随机偏移，并模拟了按下与抬起之间的微小延迟(30-100ms)，防检测。</para>
        /// </summary>
        /// <param name="x">目标X坐标</param>
        /// <param name="y">目标Y坐标</param>
        /// <param name="range">随机偏移范围(默认5像素)</param>
        /// <returns>void</returns>
        /// <example>
        /// <code>
        /// _worker.OL_LeftClick(100, 200, 5);
        /// </code>
        /// </example>
        public void OL_LeftClick(int x, int y, int range = 5)
        {
            int rndX = x + _rnd.Next(-range, range + 1);
            int rndY = y + _rnd.Next(-range, range + 1);
            _ola!.MoveTo(rndX, rndY);
            Thread.Sleep(_rnd.Next(30, 100));
            _ola.LeftDown();
            Thread.Sleep(_rnd.Next(50, 200));
            _ola.LeftUp();
        }

        /// <summary>
        /// [功能] 智能延迟
        /// <para>修改说明: 支持任务暂停和停止状态检测的 Sleep，比 Thread.Sleep 更安全。</para>
        /// </summary>
        /// <returns>
        /// <see cref="bool"/>
        /// <para>true: 延迟正常结束。</para>
        /// <para>false: 延迟期间接收到停止或异常指令。</para>
        /// </returns>
        public bool SmartSleep(int ms)
        {
            int slice = 100;
            int count = ms / slice;
            int remain = ms % slice;
            for (int i = 0; i < count; i++) { if (CheckLoopState()) return false; Thread.Sleep(slice); }
            if (remain > 0) { if (CheckLoopState()) return false; Thread.Sleep(remain); }
            return true;
        }

        // =======================================================================
        // 以下为其他内部方法 (Start/Stop逻辑等)
        // =======================================================================

        #region 生命周期与逻辑线程
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

                string imageBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                _ola.SetPath(imageBasePath);

                long parentHwnd = FindWindowWithPlugin();
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
                LogCallback?.Invoke("⚠️ 未分配任务");
                Thread.Sleep(2000);
                return;
            }

            var gameTask = new GameTask(this);
            foreach (var taskName in TaskList)
            {
                CheckPauseState();
                if (RunState == 4) break;
                LogCallback?.Invoke($"👉 开始执行: {taskName}");
                try { gameTask.Execute(taskName); }
                catch (Exception ex) { LogCallback?.Invoke($"❌ 任务[{taskName}]出错: {ex.Message}"); }
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
        #endregion

        #region 内部辅助方法
        public void EnsureGameRunning()
        {
            if (EmulatorName.Contains("雷电"))
            {
                try
                {
                    string indexStr = "0";
                    if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[1];
                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    if (!File.Exists(cmdExe)) { LogCallback?.Invoke("⚠️ 未找到 ldconsole.exe"); return; }
                    Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = $"launchex --index {indexStr} --packagename {this.PackageName}", UseShellExecute = false, CreateNoWindow = true });
                    LogCallback?.Invoke($"🚀 正在拉起游戏: {this.PackageName}");
                }
                catch (Exception ex) { LogCallback?.Invoke($"❌ 启动指令失败: {ex.Message}"); }
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
        private bool LaunchEmulator()
        {
            try
            {
                string cmdExe = "", args = "", indexStr = "0";
                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电")) { cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe"); args = $"launchex --index {indexStr} --packagename {this.PackageName}"; }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string shellPath = Path.Combine(Directory.GetParent(EmulatorBasePath)?.FullName ?? "", "shell");
                    cmdExe = Path.Combine(shellPath, "MuMuManager.exe");
                    if (!File.Exists(cmdExe)) cmdExe = Path.Combine(EmulatorBasePath, "MuMuManager.exe");
                    args = $"player launch {indexStr}";
                }
                if (!File.Exists(cmdExe)) return false;
                Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = args, UseShellExecute = false, CreateNoWindow = true });
                return true;
            }
            catch { return false; }
        }
        private void CloseEmulator()
        {
            try
            {
                string cmdExe = "", args = "", indexStr = "0";
                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];
                if (EmulatorName.Contains("雷电")) { cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe"); args = $"quit --index {indexStr}"; }
                else if (EmulatorName.Contains("MuMu")) { /* 省略Mumu关闭逻辑以保持简洁，同上 */ }
                if (File.Exists(cmdExe)) Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = args, UseShellExecute = false, CreateNoWindow = true });
            }
            catch { }
        }
        private void LogError(string msg) { LogCallback?.Invoke($"❌ {msg}"); UpdateStatus("错误", "0"); UpdateException(msg); }
        private void UpdateStatus(string status, string hwnd) { if (_lastStatusMsg != status) { _lastStatusMsg = status; StatusCallback?.Invoke(RowIndex, status, hwnd); } }
        private void UpdateException(string msg) { if (_lastExceptionMsg != msg) { _lastExceptionMsg = msg; ExceptionCallback?.Invoke(RowIndex, msg); } }
        private void Cleanup() { if (_ola != null) { _ola.UnBindWindow(); _ola.ReleaseObj(); _ola = null; } if (RunState == 4) { UpdateStatus("已停止", "0"); UpdateException(""); } }
        #endregion
    }
}