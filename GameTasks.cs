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

        // 全局图片目录（仅用于初始化检查）
        private readonly string _imageBasePath;

        public GameTask(OLAPlugServer ola, long hwnd, Action<string> log, Action<string, string> updateStatus, Func<bool> checkIsStopped, Action ensureGameStarted)
        {
            _ola = ola;
            _hwnd = hwnd;
            _log = log;
            _updateStatus = updateStatus;
            _checkIsStopped = checkIsStopped;
            _ensureGameStarted = ensureGameStarted;

            // 1. 确定 Output 文件夹的绝对路径
            _imageBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

            // 2. 🔥【核心】告诉插件：以后找图、找字，默认都去这个 Output 文件夹里找！
            // 只要设置了这一行，后面所有的 MatchWindowsFromPath、FindStr 都不用再写完整路径了。
            _ola.SetPath(_imageBasePath);
        }

        // ❌ 已删除 GetImgPath 方法，因为设置了 SetPath 后就不需要它了

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

        // ==========================================
        // ⬇️ 任务逻辑
        // ==========================================

        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke();

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("执行主线中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // --- 📷 案例：直接写文件名！ ---
                // 因为上面设置了 SetPath，这里只需写文件名 "登录历史.bmp"
                // 插件会自动去 Output 文件夹里找。
                // 如果文件在 Output\main_quest\ 里，就写 @"main_quest\登录历史.bmp"

                // 假设图片直接在 Output 根目录下：
                var imgRes = _ola.MatchWindowsFromPath(0, 0, 1920, 1080, "登录历史.bmp", 0.85, 0, 0, 1.0);

                if (imgRes != null && imgRes.MatchState)
                {
                    _log?.Invoke("👉 发现登录历史，点击固定坐标(476, 319)");

                    // 点击固定坐标
                    _ola.MoveTo(476, 319);
                    _ola.LeftClick();

                    SmartSleep(1000);
                    continue;
                }

                // --- 🅰️ 案例：找字 ---
                // 同样，字库文件也不需要写绝对路径了，直接写文件名
                int x, y;
                // 注意：正式使用前需要加载字库，如 _ola.SetDict(0, "my_dict.txt");
                int ret = _ola.FindStr(0, 0, 1280, 720, "主线", "ffffff-000000", "my_dict.txt", 0.9, out x, out y);
                if (ret != -1)
                {
                    _log?.Invoke("👉 发现主线任务，点击追踪");
                    _ola.MoveTo(x, y);
                    _ola.LeftClick();
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

                // 假设这个图片在 Output\daily\ 目录下，就写相对路径
                var rewardRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"daily\get_reward.bmp", 0.9, 0, 0, 1.0);

                if (rewardRes.MatchState)
                {
                    _log?.Invoke("💰 领取日常奖励");
                    _ola.MoveTo(rewardRes.MatchPoint.X, rewardRes.MatchPoint.Y);
                    _ola.LeftClick();
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

            // 假设图片在 Output\sign\ 目录下
            var iconRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"sign\icon.bmp", 0.9, 0, 0, 1.0);

            if (iconRes.MatchState)
            {
                _ola.MoveTo(iconRes.MatchPoint.X, iconRes.MatchPoint.Y);
                _ola.LeftClick();
                SmartSleep(2000);

                _updateStatus?.Invoke("点击签到按钮", _hwnd.ToString());
                // 找字
                int cx, cy;
                if (_ola.FindStr(0, 0, 1280, 720, "签到", "ffffff-202020", "font", 0.8, out cx, out cy) != -1)
                {
                    _ola.MoveTo(cx, cy);
                    _ola.LeftClick();
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
                    _ola.MoveTo(100, 250);
                    _ola.LeftClick();
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

            // 假设图片在 Output\afk\ 目录下
            var autoRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"afk\auto_fight.bmp", 0.9, 0, 0, 1.0);

            if (autoRes.MatchState)
            {
                _ola.MoveTo(autoRes.MatchPoint.X, autoRes.MatchPoint.Y);
                _ola.LeftClick();
                _log?.Invoke("⚔️ 已开启自动战斗");
            }

            while (true)
            {
                if (!SmartSleep(5000)) return;
            }
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