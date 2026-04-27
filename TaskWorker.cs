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
                _ola = new OLAPlugServer("OLA.dll");
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
        // 🔥🔥🔥 OL_SDK 标准封装方法区 (官方文档级注释) 🔥🔥🔥
        // =======================================================================

        /// <summary>
        /// [封装] 范围找图并点击 (OL_MatchWindowsFromPath)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>在指定区域内查找指定图片，找到后自动点击指定坐标并执行延迟。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>图片文件需放置在 Output 目录下。支持 bmp, png, jpg 格式。</para>
        /// <para><b>参数链式:</b> (x1, y1, x2, y2, "图片名", 点击x, 点击y, 延迟)</para>
        /// <para><b>延迟类型:</b> 这里延迟是执行该代码后延迟执行 下一步</para>
        /// <para><b>参数定义:</b></para>
        /// <para>x1 (整型数): 查找区域左上角X坐标</para>
        /// <para>y1 (整型数): 查找区域左上角Y坐标</para>
        /// <para>x2 (整型数): 查找区域右下角X坐标</para>
        /// <para>y2 (整型数): 查找区域右下角Y坐标</para>
        /// <para>imgName (字符串): 图片文件名 (如 "test.bmp")</param>
        /// <para>targetX (整型数): 找到后点击的X坐标</para>
        /// <para>targetY (整型数): 找到后点击的Y坐标</para>
        /// <para>delay (整型数): 点击后的延迟时间(ms)</para>
        /// <para>offset (整型数): 点击坐标的随机偏移量(默认5)</param>
        /// <para>sim (双精度浮点数): 图片相似度(默认0.85)</para>
        /// <para><b>返回值:</b></para>
        /// <para>布尔值 - true: 成功(找到并点击); false: 失败(未找到)</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
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
        /// [封装] 多点找色并点击 (OL_CmpColor)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>对比指定窗口坐标的颜色是否符合指定的颜色值。支持多点比色，全部符合才执行点击。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>此函数是多点找色，如果需要找多个色彩的话请用|作为分隔符。</para>
        /// <para>比如 "69,340,e1d7a7|141,299,fbf1bf"</para>
        /// <para><b>参数链式:</b> ("颜色串", 点击x, 点击y, 延迟)</para>
        /// <para><b>延迟类型:</b> 这里延迟是执行该代码后延迟执行 下一步</para>
        /// <para><b>参数定义:</b></para>
        /// <para>pointsStr (字符串): 多点颜色特征串，格式 "x,y,color|x,y,color"</para>
        /// <para> - x (整型数): 要对比颜色的X坐标</para>
        /// <para> - y (整型数): 要对比颜色的Y坐标</para>
        /// <para> - color (字符串): 颜色格式 RRGGBB</para>
        /// <para>targetX (整型数): 成功后点击的X坐标</para>
        /// <para>targetY (整型数): 成功后点击的Y坐标</para>
        /// <para>delay (整型数): 点击后的延迟时间(ms)</para>
        /// <para>offset (整型数): 点击坐标的随机偏移量(默认5)</para>
        /// <para><b>返回值:</b></para>
        /// <para>布尔值 - true: 成功(所有点颜色匹配); false: 失败(任意点不匹配)</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
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
        /// [封装] 找字并点击该字坐标 (OL_FindStr 重载1)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>在指定区域内查找指定的文字，找到后点击文字所在的坐标。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>需要配合字库文件使用 (默认无尽黑暗.txt)。适用于点击文字本身的场景。</para>
        /// <para><b>参数链式:</b> (x1, y1, x2, y2, "找字内容", "颜色-色差", 延迟)</para>
        /// <para><b>延迟类型:</b> 这里延迟是执行该代码后延迟执行 下一步</para>
        /// <para><b>参数定义:</b></para>
        /// <para>x1 (整型数): 查找区域左上角X</para>
        /// <para>y1 (整型数): 查找区域左上角Y</para>
        /// <para>x2 (整型数): 查找区域右下角X</para>
        /// <para>y2 (整型数): 查找区域右下角Y</para>
        /// <para>text (字符串): 要查找的文字内容</para>
        /// <para>color (字符串): 颜色格式 "RRGGBB-DRDGDB" (颜色-偏色)</para>
        /// <para>delay (整型数): 点击后的延迟时间(ms)</para>
        /// <para><b>返回值:</b></para>
        /// <para>布尔值 - true: 成功(找到文字并点击); false: 失败(未找到)</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int delay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"找到[{text}] -> 坐标({x},{y}) -> 点击自身");
                OL_LeftClick(x, y);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// [封装] 找字并点击指定位置 (OL_FindStr 重载2)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>在指定区域内查找指定的文字，找到后点击指定的坐标(非文字坐标)。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>适用于通过文字判断界面状态，但实际需要点击其他按钮或位置的场景。</para>
        /// <para><b>参数链式:</b> (x1, y1, x2, y2, "找字内容", "颜色-色差", 点击x, 点击y, 延迟)</para>
        /// <para><b>延迟类型:</b> 这里延迟是执行该代码后延迟执行 下一步</para>
        /// <para><b>参数定义:</b></para>
        /// <para>x1 (整型数): 查找区域左上角X</para>
        /// <para>y1 (整型数): 查找区域左上角Y</para>
        /// <para>x2 (整型数): 查找区域右下角X</para>
        /// <para>y2 (整型数): 查找区域右下角Y</para>
        /// <para>text (字符串): 要查找的文字内容</para>
        /// <para>color (字符串): 颜色格式 "RRGGBB-DRDGDB"</para>
        /// <para>clickX (整型数): 指定点击的X坐标</para>
        /// <para>clickY (整型数): 指定点击的Y坐标</para>
        /// <para>delay (整型数): 点击后的延迟时间(ms)</para>
        /// <para><b>返回值:</b></para>
        /// <para>布尔值 - true: 成功(找到文字并点击指定位置); false: 失败(未找到)</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
        public bool OL_FindStr(int x1, int y1, int x2, int y2, string text, string color, int clickX, int clickY, int delay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"找到[{text}] -> 点击指定位置({clickX},{clickY})");
                OL_LeftClick(clickX, clickY);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// [封装] 区域OCR识字 (OL_OcrFromDict)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>使用字库对指定区域进行文字识别，返回识别到的字符串。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>仅进行识别，不执行任何点击操作。</para>
        /// <para><b>参数链式:</b> (x1, y1, x2, y2, "颜色-色差")</para>
        /// <para><b>参数定义:</b></para>
        /// <para>x1 (整型数): 区域左上角X</para>
        /// <para>y1 (整型数): 区域左上角Y</para>
        /// <para>x2 (整型数): 区域右下角X</para>
        /// <para>y2 (整型数): 区域右下角Y</para>
        /// <para>color (字符串): 颜色格式 "RRGGBB-DRDGDB"</para>
        /// <para><b>返回值:</b></para>
        /// <para>字符串 - 返回识别到的文本。如未识别到，返回空字符串。</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
        public string OL_OcrFromDict(int x1, int y1, int x2, int y2, string color)
        {
            string text = _ola!.OcrFromDict(x1, y1, x2, y2, color, "无尽黑暗.txt", 0.8);
            return text ?? "";
        }

        /// <summary>
        /// [封装] 鼠标移动并左键点击 (OL_LeftClick)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>模拟鼠标移动到指定坐标，并执行左键按下和弹起的操作。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>内部包含随机偏移和按键延迟，用于模拟真实用户操作。</para>
        /// <para><b>参数链式:</b> (点击x, 点击y, 随机偏移)</para>
        /// <para><b>参数定义:</b></para>
        /// <para>x (整型数): 目标X坐标</para>
        /// <para>y (整型数): 目标Y坐标</para>
        /// <para>range (整型数): 随机偏移范围(默认5像素)</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
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
        /// [封装] 智能延迟 (SmartSleep)
        /// <para>-------------------------------------------------------</para>
        /// <para><b>函数简介:</b></para>
        /// <para>执行指定时间的延迟，期间会持续检测任务的暂停或停止状态。</para>
        /// <para><b>使用说明:</b></para>
        /// <para>替代 Thread.Sleep，确保脚本可以随时响应用户的停止指令。</para>
        /// <para><b>参数链式:</b> (延迟时间)</para>
        /// <para><b>延迟类型:</b> 这里延迟是执行该代码后延迟执行 下一步</para>
        /// <para><b>参数定义:</b></para>
        /// <para>ms (整型数): 延迟时间(毫秒)</para>
        /// <para><b>返回值:</b></para>
        /// <para>布尔值 - true: 延迟正常结束; false: 任务被停止或中断</para>
        /// <para>-------------------------------------------------------</para>
        /// </summary>
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
        // 4. 内部辅助方法
        // =======================================================================
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
        private void LogError(string msg) { LogCallback?.Invoke($"{msg}"); UpdateStatus("错误", "0"); UpdateException(msg); }
        private void UpdateStatus(string status, string hwnd) { if (_lastStatusMsg != status) { _lastStatusMsg = status; StatusCallback?.Invoke(RowIndex, status, hwnd); } }
        private void UpdateException(string msg) { if (_lastExceptionMsg != msg) { _lastExceptionMsg = msg; ExceptionCallback?.Invoke(RowIndex, msg); } }
        private void Cleanup() { if (_ola != null) { _ola.UnBindWindow(); _ola.ReleaseObj(); _ola = null; } if (RunState == 4) { UpdateStatus("已停止", "0"); UpdateException(""); } }
        #endregion
    }
}