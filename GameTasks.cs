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

        // ==========================================
        // 🛠️ 辅助方法区 (根据用户要求，封装和核心点击方法前置)
        // ==========================================

        /// <summary>
        /// 找图点击封装 (范围必须手动指定，偏移默认为 5)
        /// </summary>
        private bool TryClickImage(string imgName, int targetX, int targetY, int x1, int y1, int x2, int y2, int offset = 5)
        {
            // 调用找图接口
            var res = _ola.MatchWindowsFromPath(x1, y1, x2, y2, imgName, 0.85, 0, 0, 1.0);

            if (res != null && res.MatchState)
            {
                // 找到了，执行带随机偏移的点击
                ClickPoint(targetX, targetY, offset);
                SmartSleep(1000);
                return true;
            }

            return false;
        }

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

        // ==========================================
        // 构造函数 (Constructor)
        // ==========================================

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

        // ==========================================
        // 核心任务逻辑 (Task Logic)
        // ==========================================

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

        // 🔥 修正后的 MainQuest 方法 (使用 TryClickImage)
        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke();

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("执行主线中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // -----------------------------------------------------------
                // 1. 【退出条件】等级不足
                // -----------------------------------------------------------
                var im = _ola.MatchWindowsFromPath(0, 0, 960, 540, "等级不足.bmp", 0.85, 0, 0, 1.0);
                if (im != null && im.MatchState)
                {
                    _log?.Invoke($"⛔ 发现等级不足，退出主线循环");
                    SmartSleep(1000);
                    break;
                }

                // -----------------------------------------------------------
                // 2. 【找图逻辑】使用封装函数
                // -----------------------------------------------------------

                // 找 "进入游戏"，点 (482, 421)，找图范围 (0, 0, 960, 540)
                if (TryClickImage("进入游戏.bmp", 482, 421, 0, 0, 960, 540)) continue;

                // 找 "入游戏"，点 (100, 200)，找图范围 (0, 0, 960, 540)
                if (TryClickImage("入游戏.bmp", 100, 200, 0, 0, 960, 540)) continue;

                // 找 "游戏"，点 (888, 666)，找图范围 (0, 0, 960, 540)
                if (TryClickImage("游戏.bmp", 888, 666, 0, 0, 960, 540)) continue;

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
    }
}