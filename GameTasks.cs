using System;
using System.Threading;
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

        public GameTask(OLAPlugServer ola, long hwnd, Action<string> log, Action<string, string> updateStatus, Func<bool> checkIsStopped, Action ensureGameStarted)
        {
            _ola = ola;
            _hwnd = hwnd;
            _log = log;
            _updateStatus = updateStatus;
            _checkIsStopped = checkIsStopped;
            _ensureGameStarted = ensureGameStarted;
        }

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

        private void MainQuest()
        {
            _updateStatus?.Invoke("启动/检查游戏", _hwnd.ToString());
            _ensureGameStarted?.Invoke();

            if (!SmartSleep(5000)) return;

            _updateStatus?.Invoke("主线升级...", _hwnd.ToString());

            while (true)
            {
                if (!SmartSleep(1000)) return;

                // 示例:
                // int x, y;
                // if (_ola.FindStr(..., out x, out y) != -1) ...

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
            _updateStatus?.Invoke("自动签到中...", _hwnd.ToString());

            if (!SmartSleep(2000)) return;
            _updateStatus?.Invoke("点击签到", _hwnd.ToString());
            // _ola.LeftClick(); 
        }

        private void SideQuest()
        {
            _updateStatus?.Invoke("支线任务...", _hwnd.ToString());

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