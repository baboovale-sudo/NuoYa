using System;
using System.Threading;
using OLAPlug; // 引用插件命名空间

namespace OLA
{
    public class GameTask
    {
        private OLAPlugServer _ola;
        private long _hwnd;

        // 回调函数
        private Action<string> _log;
        private Action<string, string> _updateStatus;
        private Func<bool> _checkIsStopped;

        // 启动游戏的回调
        private Action _ensureGameStarted;

        public GameTask(OLAPlugServer ola, long hwnd, Action<string> log, Action<string, string> updateStatus, Func<bool> checkIsStopped, Action ensureGameStarted)
        {
            _ola = ola;
            _hwnd = hwnd;
            _log = log;
            _updateStatus = updateStatus;
            _checkIsStopped = checkIsStopped;
            _ensureGameStarted = ensureGameStarted;
        }

        /// <summary>
        /// 任务分发入口
        /// </summary>
        public void Execute(string taskName)
        {
            switch (taskName)
            {
                case "主线任务":
                    MainQuest();
                    break;
                case "每日活跃":
                    DailyActive();
                    break;
                case "自动签到":
                    AutoSign();
                    break;
                case "支线任务":
                    SideQuest();
                    break;
                case "挂机任务":
                    AfkTask();
                    break;
                default:
                    _updateStatus?.Invoke($"未知任务: {taskName}", _hwnd.ToString());
                    SmartSleep(1000);
                    break;
            }
        }

        // ==========================================
        // ⬇️ 任务逻辑 (无数据库版本)
        // ==========================================

        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke(); // 确保游戏启动

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("执行主线中...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return; // 检测停止信号

                // ----------------------------------------------------
                // 在这里写您的找图/找字逻辑 (使用本地路径或直接找字)
                // ----------------------------------------------------

                // 示例 1: 找字 (无需数据库，直接识别)
                // int x, y;
                // int ret = _ola.FindStr(0, 0, 1280, 720, "跳过剧情", "ffffff-000000", "本地字库名", 0.9, out x, out y);
                // if (ret != -1)
                // {
                //     _updateStatus?.Invoke("点击跳过", _hwnd.ToString());
                //     _ola.MoveTo(x, y);
                //     _ola.LeftClick();
                // }

                // 示例 2: 找图 (使用本地图片路径)
                // var result = _ola.MatchImageFromPath("screen", @"D:\Images\finish.bmp", 0.9, 0, 0, 1.0);
                // if (result.MatchState)
                // {
                //     // 点击...
                //     break; // 任务完成跳出
                // }

                // 占位符，防止编译警告
                if (false) break;
            }

            _updateStatus?.Invoke("主线任务结束", _hwnd.ToString());
        }

        private void DailyActive()
        {
            _updateStatus?.Invoke("做日常中...", _hwnd.ToString());
            while (true)
            {
                if (!SmartSleep(1000)) return;

                // 逻辑...

                if (false) break;
            }
        }

        private void AutoSign()
        {
            if (!SmartSleep(2000)) return;
            _updateStatus?.Invoke("点击签到", _hwnd.ToString());
            // _ola.LeftClick(); 
        }

        private void SideQuest()
        {
            while (true)
            {
                if (!SmartSleep(1000)) return;
                if (false) break;
            }
        }

        private void AfkTask()
        {
            _updateStatus?.Invoke("开始挂机...", _hwnd.ToString());
            while (true)
            {
                if (!SmartSleep(5000)) return;
            }
        }

        // ==========================================
        // 🛠️ 辅助方法
        // ==========================================
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