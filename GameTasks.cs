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

        private Random _rnd = new Random();

        // ==========================================
        // 🛠️ 辅助方法区 (找图 + 找色 封装)
        // ==========================================

        /// <summary>
        /// 全能找图点击封装
        /// 参数：范围(4个) -> 图片名 -> 点击坐标(2个) -> [🔥必填]延迟时间 -> [可选]偏移
        /// </summary>
        private bool TryClickImage(
            int x1, int y1, int x2, int y2,
            string imgName,
            int targetX, int targetY,
            int delay,
            int offset = 5,
            double sim = 0.85,
            int type = 0,
            double angle = 0,
            double scale = 1.0
        )
        {
            var res = _ola.MatchWindowsFromPath(x1, y1, x2, y2, imgName, sim, type, angle, scale);
            if (res != null && res.MatchState)
            {
                ClickPoint(targetX, targetY, offset);
                SmartSleep(delay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 全能多点找色点击封装
        /// 参数：特征点串("x,y,color|...") -> 点击坐标(2个) -> [🔥必填]延迟时间 -> [可选]偏移
        /// </summary>
        private bool TryClickColorPoint(
            string pointsStr,
            int targetX, int targetY,
            int delay,
            int offset = 5
        )
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

                // 核心比色：Start和End传一样的值表示精确匹配
                if (_ola.CmpColor(x, y, color, color) == 0)
                {
                    return false;
                }
            }

            // 全部匹配成功则点击
            ClickPoint(targetX, targetY, offset);
            SmartSleep(delay);
            return true;
        }

        private void ClickPoint(int x, int y, int range = 5)
        {
            int rndX = x + _rnd.Next(-range, range + 1);
            int rndY = y + _rnd.Next(-range, range + 1);
            _ola.MoveTo(rndX, rndY);
            _ola.LeftClick();
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
        // 构造函数
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
        // 核心任务逻辑
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

        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke();

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("执行主线中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // 1. 退出条件
                var im = _ola.MatchWindowsFromPath(0, 0, 960, 540, "等级不足.bmp", 0.85, 0, 0, 1.0);
                if (im != null && im.MatchState)
                {
                    _log?.Invoke($"⛔ 发现等级不足，退出主线循环");
                    SmartSleep(1000);
                    break;
                }

                // ================== 【主线】找图示例 ==================
                if (TryClickImage(557, 166, 669, 204, "新手启程礼.bmp", 575, 359, 500)) continue;
                if (TryClickImage(0, 0, 960, 540, "立即启动.bmp", 478, 395, 3000)) continue;
                if (TryClickImage(445, 476, 516, 498, "开始游戏.bmp", 481, 485, 3000)) continue;

                // ================== 【主线】找色示例 ==================
                // 解释：如果(100,200)是黄色 且 (105,202)也是黄色 -> 点击(100,200) -> 等待2秒
                if (TryClickColorPoint("100,200,FFFF00|105,202,FFFF00", 100, 200, 2000)) continue;
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

                // ================== 【日常】找图/找色模版 ==================
                // 你可以直接在这里填入日常任务的按钮图片
                if (TryClickImage(0, 0, 1280, 720, "一键领取.bmp", 600, 600, 1000)) continue;

                // 或者直接填入日常任务红点的颜色坐标
                if (TryClickColorPoint("1100,200,FF0000", 1100, 200, 1000)) continue;


                var rewardRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"daily\get_reward.bmp", 0.9, 0, 0, 1.0);
                if (rewardRes.MatchState)
                {
                    _log?.Invoke("💰 领取日常奖励");
                    ClickPoint(rewardRes.MatchPoint.X, rewardRes.MatchPoint.Y);
                    SmartSleep(1500);
                }

                // 暂时用false防止死循环，你写好了可以把 false 改成 true 或去掉 break
                if (false) break;
            }
        }

        private void AutoSign()
        {
            _updateStatus?.Invoke("自动签到中...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            // ================== 【签到】找图/找色模版 ==================
            // 如果有特殊的活动弹窗，可以在这里先点掉
            TryClickImage(0, 0, 1280, 720, "关闭弹窗.bmp", 1200, 50, 1000);

            // 比如检测签到按钮颜色
            TryClickColorPoint("640,360,FFFFFF", 640, 360, 2000);


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
            else { _log?.Invoke("⚠️ 未找到签到图标"); }
        }

        private void SideQuest()
        {
            _updateStatus?.Invoke("执行支线中...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // ================== 【支线】找图/找色模版 ==================
                // 支线如果有固定的 "前往" 按钮，直接用找色最快
                if (TryClickColorPoint("200,300,00FF00|210,310,FFFFFF", 200, 300, 3000)) continue;

                if (TryClickImage(0, 0, 960, 540, "支线_前往.bmp", 500, 500, 2000)) continue;


                string ocrText = _ola.Ocr(50, 200, 350, 600);
                if (ocrText.Contains("支线"))
                {
                    _log?.Invoke($"🔍 发现任务文本: {ocrText}");
                    ClickPoint(100, 250, 15);
                    SmartSleep(5000);
                }
                else { _log?.Invoke("✅ 暂无支线任务"); break; }
            }
        }

        private void AfkTask()
        {
            _updateStatus?.Invoke("开始挂机...", _hwnd.ToString());
            _ensureGameStarted?.Invoke();
            if (!SmartSleep(3000)) return;

            // ================== 【挂机】找图/找色模版 ==================
            // 挂机前可能要先吃个药水？
            TryClickColorPoint("800,600,FF00FF", 800, 600, 500);

            var autoRes = _ola.MatchWindowsFromPath(0, 0, 1280, 720, @"afk\auto_fight.bmp", 0.9, 0, 0, 1.0);
            if (autoRes.MatchState)
            {
                _log?.Invoke("⚔️ 已开启自动战斗");
                ClickPoint(autoRes.MatchPoint.X, autoRes.MatchPoint.Y);
            }

            while (true)
            {
                if (!SmartSleep(5000)) return;

                // 挂机循环中检测异常弹窗
                if (TryClickImage(0, 0, 960, 540, "网络重连.bmp", 480, 360, 5000)) continue;
                if (TryClickColorPoint("480,360,FF0000", 480, 360, 1000)) continue;
            }
        }
    }
}