using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using OLAPlug;

namespace OLA
{
    public class GameTask
    {
        private OLAPlugServer _ola;
        private long _hwnd;

        private Action<string> _log;
        private Action<string, string> _updateStatus;
        private Func<bool> _checkIsStopped;
        private Action _ensureGameStarted;

        private readonly string _imageBasePath;

        // 🔥【新增】随机数生成器
        private Random _rnd = new Random();

        public GameTask(OLAPlugServer ola, long hwnd, Action<string> log, Action<string, string> updateStatus, Func<bool> checkIsStopped, Action ensureGameStarted)
        {
            _ola = ola;
            _hwnd = hwnd;
            _log = log;
            _updateStatus = updateStatus;
            _checkIsStopped = checkIsStopped;
            _ensureGameStarted = ensureGameStarted;

            _imageBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            _ola.SetPath(_imageBasePath);
        }

        public void Execute(string taskName)
        {
            switch (taskName)
            {
                case "主线任务": MainQuest(); break;
                case "每日活跃": DailyActive(); break;
                case "自动签到": AutoSign(); break;
                case "支线任务": SideQuest(); break;
                case "挂机任务": AfkTask(); break;
                default:
                    _updateStatus?.Invoke($"未知任务: {taskName}", _hwnd.ToString());
                    SmartSleep(1000);
                    break;
            }
        }

        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke();

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("执行主线中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // 找图：登录历史 (点击固定坐标)
                var imgRes = _ola.MatchWindowsFromPath(0, 0, 960, 540, "进入游戏.bmp", 0.85, 0, 0, 1.0);

                if (imgRes != null && imgRes.MatchState)
                {
                    _log?.Invoke($"👉 发现登录历史，随机偏移点击固定坐标(476, 319)");

                    // 🔥【修改】使用带随机偏移的点击方法 (默认偏移范围5像素)
                    ClickPoint(476, 319, 5);

                    SmartSleep(1000);
                    continue;
                }

                // 找字：主线
                int x, y;
                int ret = _ola.FindStr(0, 0, 1280, 720, "主线", "ffffff-000000", "my_dict.txt", 0.9, out x, out y);
                if (ret != -1)
                {
                    _log?.Invoke("👉 发现主线任务，点击追踪");

                    // 这里也可以用 ClickPoint
                    ClickPoint(x, y, 5);

                    SmartSleep(5000);
                }
            }

            _updateStatus?.Invoke("主线任务结束", _hwnd.ToString());
        }

        private void DailyActive()
        {
            _updateStatus?.Invoke("准备日常...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            _updateStatus?.Invoke("做日常中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                var rewardRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"daily\get_reward.bmp", 0.9, 0, 0, 1.0);

                if (rewardRes.MatchState)
                {
                    _log?.Invoke("💰 领取日常奖励");
                    ClickPoint(rewardRes.MatchPoint.X, rewardRes.MatchPoint.Y);
                    SmartSleep(1500);
                }

                if (false) break;
            }
        }

        private void AutoSign()
        {
            _updateStatus?.Invoke("自动签到中...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            var iconRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"sign\icon.bmp", 0.9, 0, 0, 1.0);

            if (iconRes.MatchState)
            {
                ClickPoint(iconRes.MatchPoint.X, iconRes.MatchPoint.Y);
                SmartSleep(2000);

                _updateStatus?.Invoke("点击签到按钮", _hwnd.ToString());
                int cx, cy;
                if (_ola.FindStr(0, 0, 1280, 720, "签到", "ffffff-202020", "font", 0.8, out cx, out cy) != -1)
                {
                    ClickPoint(cx, cy);
                    SmartSleep(1000);
                }
            }
            else
            {
                _log?.Invoke("⚠️ 未找到签到图标");
            }
        }

        private void SideQuest()
        {
            _updateStatus?.Invoke("执行支线中...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            while (true)
            {
                if (!SmartSleep(1000)) return;

                string ocrText = _ola.Ocr(50, 200, 350, 600);
                if (ocrText.Contains("支线"))
                {
                    _log?.Invoke($"🔍 发现任务文本: {ocrText}");
                    ClickPoint(100, 250, 15); // 随机范围大一点
                    SmartSleep(5000);
                }
                else
                {
                    _log?.Invoke("✅ 暂无支线任务");
                    break;
                }
            }
        }

        private void AfkTask()
        {
            _updateStatus?.Invoke("开始挂机...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            var autoRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"afk\auto_fight.bmp", 0.9, 0, 0, 1.0);

            if (autoRes.MatchState)
            {
                _log?.Invoke("⚔️ 已开启自动战斗");
                ClickPoint(autoRes.MatchPoint.X, autoRes.MatchPoint.Y);
            }

            while (true)
            {
                if (!SmartSleep(5000)) return;
            }
        }

        // ==========================================
        // 🛠️ 核心辅助方法：仿真点击
        // ==========================================

        /// <summary>
        /// 🔥【仿真点击】移动+点击，包含随机延迟和随机偏移
        /// </summary>
        /// <param name="x">目标中心X</param>
        /// <param name="y">目标中心Y</param>
        /// <param name="range">随机偏移范围(默认5像素)</param>
        private void ClickPoint(int x, int y, int range = 5)
        {
            // 1. 计算随机坐标：在 (x-range) 到 (x+range) 之间
            int rndX = x + _rnd.Next(-range, range + 1);
            int rndY = y + _rnd.Next(-range, range + 1);

            // 2. 移动前随机延迟 (50-200ms) - 模拟人手反应
            // int preDelay = _rnd.Next(50, 201); 
            // SmartSleep(preDelay); 
            // (如果需要更快的连点，可以注释掉上面两行前摇)

            // 3. 移动鼠标到随机偏移后的位置
            _ola.MoveTo(rndX, rndY);

            // 4. 左键点击
            _ola.LeftClick();

            // 5. 点击后随机延迟 (100-300ms) - 模拟按键回弹
            // int postDelay = _rnd.Next(100, 301);
            // SmartSleep(postDelay);
        }

        private bool SmartSleep(int ms)
        {
            int slice = 100;
            int count = ms / slice;
            int remain = ms % slice;

            for (int i = 0; i < count; i++)
            {
                if (_checkIsStopped()) return false;
                Thread.Sleep(slice);
            }

            if (remain > 0)
            {
                if (_checkIsStopped()) return false;
                Thread.Sleep(remain);
            }

            return true;
        }
    }
}