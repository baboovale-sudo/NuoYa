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

            // 1. 移动后停顿一下（模拟人眼定位，比如 10~30毫秒）
            Thread.Sleep(_rnd.Next(30, 100));

            _ola.LeftDown(); // 按下

            // 2. 按下和抬起之间停顿一下（模拟点击力度，比如 50~100毫秒）
            Thread.Sleep(_rnd.Next(50, 200));

            _ola.LeftUp();   // 抬起
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

                // 1. 异常退出条件：等级不足
                var im = _ola.MatchWindowsFromPath(0, 0, 960, 540, "等级不足.bmp", 0.85, 0, 0, 1.0);
                if (im != null && im.MatchState)
                {
                    _log?.Invoke($"⛔ 发现等级不足，退出主线循环");
                    SmartSleep(1000);
                    break;
                }

                // =======================================================================
                // 🔥 加点小循环：抓住入口，死磕到底
                // =======================================================================

                // 技能加点分配小循环
                // if (TryClickColorPoint("684,446,d1d1cf|669,449,c7c7bf|648,444,c7c7c3|651,449,cdcdc7|707,362,edd9b3", 668, 447, 500))
                if (TryClickImage(641, 442, 694, 457, "立即加点.bmp", 666, 449, 500)) 
                {        
                    SmartSleep(1000); // 稍微等一下界面打开

                    // 进入加点专用小循环 🔄
                    // 进入加点专用小循环 🔄
                    while (true)
                    {
                        if (_checkIsStopped()) return;
                        //在属性页面但是可用加点为0---点击退出
                        if (TryClickColorPoint("514,116,a37b20|520,116,8d6d21|520,117,916f21|840,22,d7e1eb", 939, 22, 500)) break;
                        // 1. 【退出条件】智力+敏捷+力量+体力都没有可用加点---点击退出
                        if (TryClickColorPoint("410,209,1c1d23|412,245,1d1f26|410,282,1f2228|418,319,20232a|427,181,9bc925|432,179,a1cd26", 940, 22, 500)) continue;
                        // 调整点数页面---智力判断---确认
                        if (TryClickColorPoint("410,282,917e52|417,282,85724a|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        // 调整点数页面---敏捷判断---确认
                        if (TryClickColorPoint("412,245,978156|418,245,8f7a4e|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        // 调整点数页面---力量判断---确认
                        if (TryClickColorPoint("410,209,957f54|418,209,8f7a4e|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        // 调整点数页面---体力判断---确认
                        if (TryClickColorPoint("419,319,8f784a|410,319,937f52|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        // 调整点数页面---点击推荐加点
                        if (TryClickColorPoint("544,209,b7a372|547,280,9f875a|568,178,e9e9e3|491,133,d1dde7|490,399,efefe7", 577, 179, 500)) continue;
                        // 首次选择有职业主攻方向推荐---智法
                        if (TryClickColorPoint("250,203,f17c01|273,355,ef7b01|579,90,c5a55c|379,90,c3a35a|739,190,e1dfd7", 760, 189, 500)) continue;
                        //存在加点---点击
                        if (TryClickColorPoint("554,116,a78f60|563,22,d1dbe3|840,23,d1dde7|469,146,a7a7a7", 554, 117, 500)) continue;
                        // 3. 【防空转】
                        SmartSleep(1000);
                    }
                }
                // =======================================================================


                // 主线任务使用新手套装---点击使用 
                if (TryClickColorPoint("855,162,451511|833,157,42120e|856,153,78231e|536,478,fd6e52|397,474,fbf3af", 841, 160, 500))
                {
                    SmartSleep(1000); // 稍微等一下界面打开
                    while (true)
                    {
                        if (_checkIsStopped()) return;
                        // 主线任务使用新手套装---确认打开  
                        if (TryClickColorPoint("508,159,19bb1e|484,161,18c71d|510,399,e3e5e7|549,398,6d87a7|827,19,e9e3d7", 503, 396, 1000)) continue;
                        ClickPoint(713, 99); // 1
                        SmartSleep (1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(779, 104); // 2
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(841, 101); // 3
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(909, 105); // 4
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(715, 167); // 5
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(778, 168); // 6
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(843, 170); // 7
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(908, 164); // 8
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(716, 225); // 9
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(779, 223); // 10
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(843, 226); // 11
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(907, 225); // 12
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(712, 291); // 13
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(778, 289); // 14
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(845, 289); // 15
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(907, 288); // 16
                        SmartSleep(1000);
                        ClickPoint(558, 455);
                        SmartSleep(500);
                        ClickPoint(942, 20); //退出背包
                        SmartSleep(1000);
                        break;
                     
                    }
                }
                // =======================================================================





                // --- 原有的其他主线逻辑 ---





                // 通用新手奖励引导---奖励领取    
                if (TryClickColorPoint("776,21,32435c|794,12,efe1d3|793,30,e1d9cf|819,12,f3e7db|783,475,e9ebeb|783,484,edefef|837,485,efefef|934,18,919187", 810, 478, 500)) continue;

                // 通用新手奖励引导1---奖励领取    
                if (TryClickColorPoint("807,475,ffffff|805,482,f7f7f7|794,13,f1e7db|819,12,f3e7db|933,16,959587", 807, 477, 500)) continue;

                // 购买新手宝箱---奖励领取    
                if (TryClickColorPoint("783,430,d90505|841,430,0000e3|759,72,fbfbfb|786,70,f3f3f3|934,16,959587", 810, 477, 500)) continue;
                // 检测是否购买成功---关闭页面
                if (TryClickColorPoint("546,159,c3b76a|547,161,811702|534,155,561711|560,191,859db3|387,470,b3b5b7", 942, 18, 500)) continue;
                // 购买新手套装循环----点击购买
                if (TryClickColorPoint("762,117,d5b97f|764,124,dbbf83|764,120,f7d58f|784,120,f9d78f|743,80,ededeb", 878, 113, 500)) continue;
                // 属性点的使用奖励---领取
                if (TryClickColorPoint("841,430,0000e3|780,428,f10000|802,70,fdfdfb|801,22,ede7db|828,20,ede7db", 810, 478, 500)) continue;
                // 领取新手福利---领取
                if (TryClickColorPoint("424,403,62e303|424,436,6cf903|466,459,f7b164|775,511,b92e2c|905,520,edefef", 910, 519, 500)) continue;
                // 领取奖励
                if (TryClickColorPoint("819,13,ede7db|827,476,efefef|790,480,f1f3f3|824,437,dbdbdb", 807, 478, 500)) continue;

                // 找图/找色移动
                if (TryClickImage(557, 166, 669, 204, "新手启程礼.bmp", 575, 359, 500)) continue;
                if (TryClickImage(0, 0, 960, 540, "立即启动.bmp", 478, 395, 3000)) continue;
                if (TryClickImage(445, 476, 516, 498, "开始游戏.bmp", 481, 485, 3000)) continue;

                // 通用主线点击
                if (TryClickColorPoint("41,115,bd972c|41,113,bd972c|41,110,bf972c", 100, 111, 2000)) continue;
                if (TryClickColorPoint("235,174,dfd5a3|156,207,fff3bf|73,126,f1e7b7|759,184,b5afa3", 782, 476, 2000)) continue;
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