using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Concurrent;

namespace OLAPlug
{

    internal class FuncMapInfo
    {
        public string StandardName { get; set; }

        public string ExportedName { get; set; }

        public uint RVA { get; set; } = 0;

        public Delegate DelegateInstance { get; set; }

        public FuncMapInfo(string standardName)
        {
            this.StandardName = standardName;
            this.ExportedName = standardName;
        }

        public FuncMapInfo(string standardName, string exportedName, string rva)
        {
            this.StandardName = standardName;
            this.ExportedName = exportedName.Trim();
            this.RVA = ConvertRVAStringToUInt32(rva);
        }

        private uint ConvertRVAStringToUInt32(string rvaStr)
        {
            // 清理字符串：移除空格、前缀
            rvaStr = rvaStr.Trim()
                          .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                          .Replace(" ", "");

            try
            {
                return uint.Parse(rvaStr, NumberStyles.HexNumber);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"无效的RVA格式: {rvaStr}", ex);
            }
        }
    }

    /// <summary>
    /// OLA插件DLL动态加载辅助类（模板文件）
    /// 注意：此文件为模板，委托定义需要外部生成后插入到指定位置
    /// </summary>
    public static class OLAPlugDLLHelper
    {
        # region 数据类型定义

        public delegate void DrawGuiButtonCallback(long buttonHandle);
        public delegate void DrawGuiMouseCallback(long handle, int event_type, int x, int y);
        public delegate void DownloadCallback(long current, long total, long speed, long user_data);
        public delegate void TcpClientCallback(long client_handle, int event_type, long data, int data_len, long user_data);
        public delegate void TcpServerCallback(long server_handle, long conn_id, int event_type, long data, int data_len, long user_data);
        public delegate int HotkeyCallback(int keycode, int modifiers);
        public delegate void MouseCallback(int button, int x, int y, int clicks);
        public delegate void MouseWheelCallback(int x, int y, int amount, int rotation);
        public delegate void MouseMoveCallback(int x, int y);
        public delegate void MouseDragCallback(int x, int y);

        /// <summary>
        /// JSON操作错误码枚举
        /// </summary>
        public enum JSONError
        {
            JSON_SUCCESS = 0,              // 操作成功
            JSON_ERROR_INVALID_HANDLE,     // 无效的句柄
            JSON_ERROR_PARSE_FAILED,       // JSON解析失败
            JSON_ERROR_TYPE_MISMATCH,      // 类型不匹配
            JSON_ERROR_KEY_NOT_FOUND,      // 键不存在
            JSON_ERROR_INDEX_OUT_OF_RANGE, // 索引超出范围
            JSON_ERROR_UNKNOWN             // 未知错误
        }

        #endregion

        #region 接口委托定义
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateCOLAPlugInterFaceDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DestroyCOLAPlugInterFaceDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long VerDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetPlugInfoDelegate(int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetPathDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetPathDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetMachineCodeDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetBasePathDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegDelegate(string userCode, string softCode, string featureList);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int BindWindowDelegate(long instance, long hwnd, string display, string mouse, string keypad, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int BindWindowExDelegate(long instance, long hwnd, string display, string mouse, string keypad, string pubstr, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnBindWindowDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetBindWindowDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ReleaseWindowsDllDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeStringPtrDelegate(long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeMemoryPtrDelegate(long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetStringSizeDelegate(long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetStringFromPtrDelegate(long ptr, StringBuilder lpString, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelayDelegate(int millisecond);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelaysDelegate(int minMillisecond, int maxMillisecond);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetUACDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CheckUACDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RunAppDelegate(long instance, string appPath, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ExecuteCmdDelegate(long instance, string cmd, string current_dir, int time_out);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetConfigDelegate(long instance, string configKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetConfigDelegate(long instance, string configStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetConfigByKeyDelegate(long instance, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SendDropFilesDelegate(long instance, long hwnd, string file_path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetDefaultEncodeDelegate(int inputEncoding, int outputEncoding);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetLastErrorDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetLastErrorStringDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long HideModuleDelegate(long instance, string moduleName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnhideModuleDelegate(long instance, long ctx);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRandomNumberDelegate(long instance, int min, int max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetRandomDoubleDelegate(long instance, double min, double max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ExcludePosDelegate(long instance, string json, int type, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindNearestPosDelegate(long instance, string json, int type, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SortPosDistanceDelegate(long instance, string json, int type, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetDenseRectDelegate(long instance, long image, int width, int height, out int x1, out int y1, out int x2, out int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long PathPlanningDelegate(long instance, long image, int startX, int startY, int endX, int endY, double potentialRadius, double searchRadius);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateGraphDelegate(long instance, string json);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetGraphDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int AddEdgeDelegate(long instance, long graphPtr, string from, string to, double weight, bool isDirected);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetShortestPathDelegate(long instance, long graphPtr, string from, string to);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetShortestDistanceDelegate(long instance, long graphPtr, string from, string to);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ClearGraphDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DeleteGraphDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetNodeCountDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetEdgeCountDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetShortestPathToAllNodesDelegate(long instance, long graphPtr, string startNode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetMinimumSpanningTreeDelegate(long instance, long graphPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetDirectedPathToAllNodesDelegate(long instance, long graphPtr, string startNode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetMinimumArborescenceDelegate(long instance, long graphPtr, string root);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateGraphFromCoordinatesDelegate(long instance, string json, bool connectAll, double maxDistance, bool useEuclideanDistance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int AddCoordinateNodeDelegate(long instance, long graphPtr, string name, double x, double y, bool connectToExisting, double maxDistance, bool useEuclideanDistance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetNodeCoordinatesDelegate(long instance, long graphPtr, string name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetNodeConnectionDelegate(long instance, long graphPtr, string from, string to, bool canConnect, double weight);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetNodeConnectionStatusDelegate(long instance, long graphPtr, string from, string to);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AsmCallDelegate(long instance, long hwnd, string asmStr, int type, long baseAddr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AssembleDelegate(long instance, string asmStr, long baseAddr, int arch, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DisassembleDelegate(long instance, string asmCode, long baseAddr, int arch, int mode, int showType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LoginDelegate(string userCode, string softCode, string featureList, string softVersion, string dealerCode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ActivateDelegate(string userCode, string softCode, string softVersion, string dealerCode, string licenseKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaAddDeviceDelegate(long instance, int vmId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaAddDeviceExDelegate(long instance, string connectionString);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaRemoveDeviceDelegate(long instance, long deviceId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaGetPidFromNameDelegate(long instance, long deviceId, string processName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaGetPidListDelegate(long instance, long deviceId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaGetProcessInfoDelegate(long instance, long deviceId, int pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaGetModuleBaseDelegate(long instance, long deviceId, int pid, string moduleName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaGetModuleSizeDelegate(long instance, long deviceId, int pid, string moduleName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaGetProcAddressDelegate(long instance, long deviceId, int pid, string moduleName, string functionName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaScatterCreateDelegate(long instance, long deviceId, int pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaScatterPrepareDelegate(long instance, long scatterHandle, long address, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaScatterExecuteDelegate(long instance, long scatterHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaScatterReadDelegate(long instance, long scatterHandle, long address, long buffer, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaScatterClearDelegate(long instance, long scatterHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaScatterCloseDelegate(long instance, long scatterHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindDataDelegate(long instance, long deviceId, int pid, string addr_range, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindDataExDelegate(long instance, long deviceId, int pid, string addr_range, string data, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindDoubleDelegate(long instance, long deviceId, int pid, string addr_range, double double_value_min, double double_value_max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindDoubleExDelegate(long instance, long deviceId, int pid, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindFloatDelegate(long instance, long deviceId, int pid, string addr_range, float float_value_min, float float_value_max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindFloatExDelegate(long instance, long deviceId, int pid, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindIntDelegate(long instance, long deviceId, int pid, string addr_range, long int_value_min, long int_value_max, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindIntExDelegate(long instance, long deviceId, int pid, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindStringDelegate(long instance, long deviceId, int pid, string addr_range, string string_value, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaFindStringExDelegate(long instance, long deviceId, int pid, string addr_range, string string_value, int type, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadDataDelegate(long instance, long deviceId, int pid, string addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadDataAddrDelegate(long instance, long deviceId, int pid, long addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadDataAddrToBinDelegate(long instance, long deviceId, int pid, long addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadDataToBinDelegate(long instance, long deviceId, int pid, string addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double DmaReadDoubleDelegate(long instance, long deviceId, int pid, string addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double DmaReadDoubleAddrDelegate(long instance, long deviceId, int pid, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate float DmaReadFloatDelegate(long instance, long deviceId, int pid, string addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate float DmaReadFloatAddrDelegate(long instance, long deviceId, int pid, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadIntDelegate(long instance, long deviceId, int pid, string addr, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadIntAddrDelegate(long instance, long deviceId, int pid, long addr, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadStringDelegate(long instance, long deviceId, int pid, string addr, int type, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DmaReadStringAddrDelegate(long instance, long deviceId, int pid, long addr, int type, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDataDelegate(long instance, long deviceId, int pid, string addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDataFromBinDelegate(long instance, long deviceId, int pid, string addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDataAddrDelegate(long instance, long deviceId, int pid, long addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDataAddrFromBinDelegate(long instance, long deviceId, int pid, long addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDoubleDelegate(long instance, long deviceId, int pid, string addr, double double_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteDoubleAddrDelegate(long instance, long deviceId, int pid, long addr, double double_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteFloatDelegate(long instance, long deviceId, int pid, string addr, float float_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteFloatAddrDelegate(long instance, long deviceId, int pid, long addr, float float_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteIntDelegate(long instance, long deviceId, int pid, string addr, int type, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteIntAddrDelegate(long instance, long deviceId, int pid, long addr, int type, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteStringDelegate(long instance, long deviceId, int pid, string addr, int type, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DmaWriteStringAddrDelegate(long instance, long deviceId, int pid, long addr, int type, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiCleanupDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetGuiActiveDelegate(long instance, int active);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiIsGuiActiveDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetGuiClickThroughDelegate(long instance, int enabled);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiIsGuiClickThroughDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiRectangleDelegate(long instance, int x, int y, int width, int height, int mode, double lineThickness);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiCircleDelegate(long instance, int x, int y, int radius, int mode, double lineThickness);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiLineDelegate(long instance, int x1, int y1, int x2, int y2, double lineThickness);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiTextDelegate(long instance, string text, int x, int y, string fontPath, int fontSize, int align);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiImageDelegate(long instance, string imagePath, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiImagePtrDelegate(long instance, long imagePtr, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiWindowDelegate(long instance, string title, int x, int y, int width, int height, int style);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiPanelDelegate(long instance, long parentHandle, int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawGuiButtonDelegate(long instance, long parentHandle, string text, int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetPositionDelegate(long instance, long handle, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetSizeDelegate(long instance, long handle, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetColorDelegate(long instance, long handle, int r, int g, int b, int a);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetAlphaDelegate(long instance, long handle, int alpha);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetDrawModeDelegate(long instance, long handle, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetLineThicknessDelegate(long instance, long handle, double thickness);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetFontDelegate(long instance, long handle, string fontPath, int fontSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetTextAlignDelegate(long instance, long handle, int align);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetTextDelegate(long instance, long handle, string text);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetWindowTitleDelegate(long instance, long handle, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetWindowStyleDelegate(long instance, long handle, int style);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetWindowTopMostDelegate(long instance, long handle, int topMost);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetWindowTransparencyDelegate(long instance, long handle, int alpha);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiDeleteObjectDelegate(long instance, long handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiClearAllDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetVisibleDelegate(long instance, long handle, int visible);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetZOrderDelegate(long instance, long handle, int zOrder);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetParentDelegate(long instance, long handle, long parentHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetButtonCallbackDelegate(long instance, long handle, DrawGuiButtonCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiSetMouseCallbackDelegate(long instance, long handle, DrawGuiMouseCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiGetDrawObjectTypeDelegate(long instance, long handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiGetPositionDelegate(long instance, long handle, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiGetSizeDelegate(long instance, long handle, out int width, out int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DrawGuiIsPointInObjectDelegate(long instance, long handle, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetMemoryModeDelegate(long instance, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExportDriverDelegate(long instance, string driver_path, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LoadDriverDelegate(long instance, string driver_name, string driver_path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnloadDriverDelegate(long instance, string driver_name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DriverTestDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LoadPdbDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetPdbDownloadUrlsDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int HideProcessDelegate(long instance, long pid, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectProcessDelegate(long instance, long pid, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectProcess2Delegate(long instance, long pid, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int AddProtectPIDDelegate(long instance, long pid, long mode, long allow_pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveProtectPIDDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int AddAllowPIDDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveAllowPIDDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FakeProcessDelegate(long instance, long pid, long fake_pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectWindowDelegate(long instance, long hwnd, int flag);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeOpenProcessDelegate(long instance, long pid, out long process_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeOpenThreadDelegate(long instance, long thread_id, out long thread_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int StartSecurityGuardDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileTestDriverDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileEnableDriverDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileDisableDriverDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileStartFilterDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileStopFilterDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileAddProtectedPathDelegate(long instance, string path, int mode, int is_directory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileRemoveProtectedPathDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileClearProtectedPathsDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileQueryProtectedPathDelegate(long instance, string path, out int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileAddWhitelistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileRemoveWhitelistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileClearWhitelistDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileQueryWhitelistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileAddBlacklistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileRemoveBlacklistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileClearBlacklistDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ProtectFileQueryBlacklistDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int EnabletVtDriverDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtFakeWriteDataDelegate(long instance, long hwnd, string addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtFakeWriteDataFromBinDelegate(long instance, long hwnd, string addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtFakeWriteDataAddrDelegate(long instance, long hwnd, long addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtFakeWriteDataAddrFromBinDelegate(long instance, long hwnd, long addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtUnFakeMemoryAddrDelegate(long instance, long hwnd, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VtUnFakeMemoryDelegate(long instance, long hwnd, string addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectEnableDriverDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectDisableDriverDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectAddProtectDelegate(long instance, long pid, string path, int mode, int permission);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectRemoveProtectDelegate(long instance, long pid, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectClearAllDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectAddWhitelistDelegate(long instance, long pid, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectRemoveWhitelistDelegate(long instance, long pid, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectClearWhitelistDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectAddBlacklistDelegate(long instance, long pid, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectRemoveBlacklistDelegate(long instance, long pid, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VipProtectClearBlacklistDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GenerateRSAKeyDelegate(long instance, string publicKeyPath, string privateKeyPath, int type, int keySize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ConvertRSAPublicKeyDelegate(long instance, string publicKey, int inputType, int outputType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ConvertRSAPrivateKeyDelegate(long instance, string privateKey, int inputType, int outputType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EncryptWithRsaDelegate(long instance, string message, string publicKey, int paddingType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DecryptWithRsaDelegate(long instance, string cipher, string privateKey, int paddingType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SignWithRsaDelegate(long instance, string message, string privateCer, int shaType, int paddingType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VerifySignWithRsaDelegate(long instance, string message, string signature, int shaType, int paddingType, string publicCer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AESEncryptDelegate(long instance, string source, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AESDecryptDelegate(long instance, string source, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AESEncryptExDelegate(long instance, string source, string key, string iv, int mode, int paddingType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long AESDecryptExDelegate(long instance, string source, string key, string iv, int mode, int paddingType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MD5EncryptDelegate(long instance, string source);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SHAHashDelegate(long instance, string source, int shaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long HMACDelegate(long instance, string source, string key, int shaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GenerateRandomBytesDelegate(long instance, int length, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GenerateGuidDelegate(long instance, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long Base64EncodeDelegate(long instance, string source);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long Base64DecodeDelegate(long instance, string source);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long PBKDF2Delegate(long instance, string password, string salt, int iterations, int keyLength, int shaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MD5FileDelegate(long instance, string filePath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SHAFileDelegate(long instance, string filePath, int shaType);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateFolderDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DeleteFolderDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetFolderListDelegate(long instance, string path, string baseDir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsDirectoryDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsFileDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateFileDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DeleteFileDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CopyFileDelegate(long instance, string src, string dst);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MoveFileDelegate(long instance, string src, string dst);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RenameFileDelegate(long instance, string src, string dst);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetFileSizeDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetFileListDelegate(long instance, string path, string baseDir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetFileNameDelegate(long instance, string path, int withExtension);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ToAbsolutePathDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ToRelativePathDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FileOrDirectoryExistsDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadFileStringDelegate(long instance, string filePath, int encoding);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadBytesFromFileDelegate(long instance, string filePath, int offset, long size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteBytesToFileDelegate(long instance, string filePath, long dataAddr, int dataSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteStringToFileDelegate(long instance, string filePath, string data, int encoding);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int StartHotkeyHookDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int StopHotkeyHookDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegisterHotkeyDelegate(long instance, int keycode, int modifiers, HotkeyCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnregisterHotkeyDelegate(long instance, int keycode, int modifiers);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegisterMouseButtonDelegate(long instance, int button, int type, MouseCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnregisterMouseButtonDelegate(long instance, int button, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegisterMouseWheelDelegate(long instance, MouseWheelCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnregisterMouseWheelDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegisterMouseMoveDelegate(long instance, MouseMoveCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnregisterMouseMoveDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegisterMouseDragDelegate(long instance, MouseDragCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnregisterMouseDragDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InjectDelegate(long instance, long hwnd, string dll_path, int type, int bypassGuard);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InjectFromUrlDelegate(long instance, long hwnd, string url, int type, int bypassGuard);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InjectFromBufferDelegate(long instance, long hwnd, long bufferAddr, int bufferSize, int type, int bypassGuard);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonCreateObjectDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonCreateArrayDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonParseDelegate(string str, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonStringifyDelegate(long obj, int indent, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonFreeDelegate(long obj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonGetValueDelegate(long obj, string key, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonGetArrayItemDelegate(long arr, int index, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long JsonGetStringDelegate(long obj, string key, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double JsonGetNumberDelegate(long obj, string key, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonGetBoolDelegate(long obj, string key, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonGetSizeDelegate(long obj, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonSetValueDelegate(long obj, string key, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonArrayAppendDelegate(long arr, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonSetStringDelegate(long obj, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonSetNumberDelegate(long obj, string key, double value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonSetBoolDelegate(long obj, string key, int value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonDeleteKeyDelegate(long obj, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int JsonClearDelegate(long obj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ParseMatchImageJsonDelegate(string str, out int matchState, out int x, out int y, out int width, out int height, out double matchVal, out double angle, out int index);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetMatchImageAllCountDelegate(string str);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ParseMatchImageAllJsonDelegate(string str, int parseIndex, out int matchState, out int x, out int y, out int width, out int height, out double matchVal, out double angle, out int index);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetResultCountDelegate(string resultStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GenerateMouseTrajectoryDelegate(long instance, int startX, int startY, int endX, int endY);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyDownDelegate(long instance, int vk_code);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyUpDelegate(long instance, int vk_code);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyPressDelegate(long instance, int vk_code);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LeftDownDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LeftUpDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MoveToDelegate(long instance, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MoveToWithoutSimulatorDelegate(long instance, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RightClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RightDoubleClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RightDownDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RightUpDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetCursorShapeDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetCursorImageDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyPressStrDelegate(long instance, string keyStr, int delay);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SendStringDelegate(long instance, long hwnd, string str);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SendStringExDelegate(long instance, long hwnd, long addr, int len, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyPressCharDelegate(long instance, string keyStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyDownCharDelegate(long instance, string keyStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int KeyUpCharDelegate(long instance, string keyStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MoveRDelegate(long instance, int rx, int ry);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MiddleClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MoveToExDelegate(long instance, int x, int y, int w, int h);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetCursorPosDelegate(long instance, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MiddleUpDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MiddleDownDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MiddleDoubleClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LeftClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LeftDoubleClickDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WheelUpDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WheelDownDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WaitKeyDelegate(long instance, int vk_code, int time_out);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int EnableMouseAccuracyDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GenerateInvoluteMouseTrajectoryDelegate(long instance, int startX, int startY, int radius, int stepDistance, double curvature, double noiseAmplitude);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogShutdownDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetFilePathDelegate(long instance, long loggerHandle, string logFilePath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetPatternDelegate(long instance, long loggerHandle, string logPattern);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetMaxFileSizeDelegate(long instance, long loggerHandle, int maxFileSizeMb);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetMaxFilesDelegate(long instance, long loggerHandle, int maxFiles);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetLevelDelegate(long instance, long loggerHandle, int level);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogGetLevelDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetTargetDelegate(long instance, long loggerHandle, int targetFlags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetAsyncDelegate(long instance, long loggerHandle, int enableAsync);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetColorModeDelegate(long instance, long loggerHandle, int colorMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetLevelColorDelegate(long instance, long loggerHandle, int level, int color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogResetLevelColorsDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetFlushIntervalDelegate(long instance, long loggerHandle, int flushIntervalSeconds);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogTraceDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogDebugDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogInfoDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogWarnDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogErrorDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogCriticalDelegate(long instance, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogFlushDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LogCreateInstanceDelegate(long instance, string instanceName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogDestroyInstanceDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetBaseDirectoryDelegate(long instance, long loggerHandle, string baseDirectory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetDirModeDelegate(long instance, long loggerHandle, int dirMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetModuleNameDelegate(long instance, long loggerHandle, string moduleName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetFileNamePatternDelegate(long instance, long loggerHandle, string fileNamePattern);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetRotationModeDelegate(long instance, long loggerHandle, int rotationMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogSetAppendModeDelegate(long instance, long loggerHandle, int enableAppend);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogTraceExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogDebugExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogInfoExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogWarnExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogErrorExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogCriticalExDelegate(long instance, long loggerHandle, string message);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogRotateFileDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogCleanupOldFilesDelegate(long instance, long loggerHandle, int keepCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LogGetCurrentFilePathDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LogGetCurrentFileSizeDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LogGetTotalFilesCountDelegate(long instance, long loggerHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DoubleToDataDelegate(long instance, double double_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FloatToDataDelegate(long instance, float float_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long StringToDataDelegate(long instance, string string_value, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int Int64ToInt32Delegate(long instance, long v);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long Int32ToInt64Delegate(long instance, int v);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindDataDelegate(long instance, long hwnd, string addr_range, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindDataExDelegate(long instance, long hwnd, string addr_range, string data, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindDoubleDelegate(long instance, long hwnd, string addr_range, double double_value_min, double double_value_max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindDoubleExDelegate(long instance, long hwnd, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindFloatDelegate(long instance, long hwnd, string addr_range, float float_value_min, float float_value_max);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindFloatExDelegate(long instance, long hwnd, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindIntDelegate(long instance, long hwnd, string addr_range, long int_value_min, long int_value_max, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindIntExDelegate(long instance, long hwnd, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStringDelegate(long instance, long hwnd, string addr_range, string string_value, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStringExDelegate(long instance, long hwnd, string addr_range, string string_value, int type, int step, int multi_thread, int mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadDataDelegate(long instance, long hwnd, string addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadDataAddrDelegate(long instance, long hwnd, long addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadDataAddrToBinDelegate(long instance, long hwnd, long addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadDataToBinDelegate(long instance, long hwnd, string addr, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double ReadDoubleDelegate(long instance, long hwnd, string addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double ReadDoubleAddrDelegate(long instance, long hwnd, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate float ReadFloatDelegate(long instance, long hwnd, string addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate float ReadFloatAddrDelegate(long instance, long hwnd, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadIntDelegate(long instance, long hwnd, string addr, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadIntAddrDelegate(long instance, long hwnd, long addr, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadStringDelegate(long instance, long hwnd, string addr, int type, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadStringAddrDelegate(long instance, long hwnd, long addr, int type, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDataDelegate(long instance, long hwnd, string addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDataFromBinDelegate(long instance, long hwnd, string addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDataAddrDelegate(long instance, long hwnd, long addr, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDataAddrFromBinDelegate(long instance, long hwnd, long addr, long data, int len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDoubleDelegate(long instance, long hwnd, string addr, double double_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteDoubleAddrDelegate(long instance, long hwnd, long addr, double double_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteFloatDelegate(long instance, long hwnd, string addr, float float_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteFloatAddrDelegate(long instance, long hwnd, long addr, float float_value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteIntDelegate(long instance, long hwnd, string addr, int type, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteIntAddrDelegate(long instance, long hwnd, long addr, int type, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteStringDelegate(long instance, long hwnd, string addr, int type, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int WriteStringAddrDelegate(long instance, long hwnd, long addr, int type, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetMemoryHwndAsProcessIdDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeProcessMemoryDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetModuleBaseAddrDelegate(long instance, long hwnd, string module_name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetModuleSizeDelegate(long instance, long hwnd, string module_name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetRemoteApiAddressDelegate(long instance, long hwnd, string module_name, string fun_name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long VirtualAllocExDelegate(long instance, long hwnd, long addr, int size, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VirtualFreeExDelegate(long instance, long hwnd, long addr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int VirtualProtectExDelegate(long instance, long hwnd, long addr, int size, int newProtect, out int oldProtect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long VirtualQueryExDelegate(long instance, long hwnd, long addr, long pmbi);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateRemoteThreadDelegate(long instance, long hwnd, long lpStartAddress, long lpParameter, int dwCreationFlags, out long lpThreadId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CloseHandleDelegate(long instance, long handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int HookRemoteApiDelegate(long instance, long hwnd, long targetAddr, long size, long hook_proc);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int UnhookRemoteApiDelegate(long instance, long hwnd, long targetAddr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int HttpDownloadFileDelegate(long instance, string url, string save_path, DownloadCallback callback, long user_data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int HttpDownloadFileExDelegate(long instance, string url, string save_path, DownloadCallback callback, long user_data, int max_retries, int connect_timeout_sec, int read_timeout_sec);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long HttpGetDelegate(long instance, string url);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long HttpPostDelegate(long instance, string url, string body, string content_type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long HttpRequestExDelegate(long instance, string method, string url, string headers, string body, string content_type, out int status_code);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long TcpClientCreateDelegate(long instance, TcpClientCallback callback, long user_data, int enable_packet_protocol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpClientConnectDelegate(long instance, long client_handle, string host, int port);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpClientSendDelegate(long instance, long client_handle, long data, int data_len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpClientDisconnectDelegate(long instance, long client_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpClientDestroyDelegate(long instance, long client_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long TcpServerCreateDelegate(long instance, string bind_addr, int port, TcpServerCallback callback, long user_data, int enable_packet_protocol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpServerSendDelegate(long instance, long server_handle, long conn_id, long data, int data_len);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpServerDisconnectDelegate(long instance, long server_handle, long conn_id);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpServerStopDelegate(long instance, long server_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long TcpServerGetClientAddressDelegate(long instance, long server_handle, long conn_id);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long TcpServerGetAllConnectionIdsDelegate(long instance, long server_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TcpServerDestroyDelegate(long instance, long server_handle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrDelegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromPtrDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromBmpDataDelegate(long instance, long ptr, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrDetailsDelegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromPtrDetailsDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromBmpDataDetailsDelegate(long instance, long ptr, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrV5Delegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrV5DetailsDelegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrV5FromPtrDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrV5FromPtrDetailsDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetOcrConfigDelegate(long instance, string configKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetOcrConfigDelegate(long instance, string configStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetOcrConfigByKeyDelegate(long instance, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromDictDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromDictDetailsDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromDictPtrDelegate(long instance, long ptr, string colorJson, string dict_name, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OcrFromDictPtrDetailsDelegate(long instance, long ptr, string colorJson, string dict_name, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindStrDelegate(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal, out int outX, out int outY);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStrDetailDelegate(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStrAllDelegate(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStrFromPtrDelegate(long instance, long source, string str, string colorJson, string dict, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindStrFromPtrAllDelegate(long instance, long source, string str, string colorJson, string dict, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FastNumberOcrFromPtrDelegate(long instance, long source, string numbers, string colorJson, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FastNumberOcrDelegate(long instance, int x1, int y1, int x2, int y2, string numbers, string colorJson, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ImportTxtDictDelegate(long instance, string dictName, string dictPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExportTxtDictDelegate(long instance, string dictName, string dictPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CaptureDelegate(long instance, int x1, int y1, int x2, int y2, string file);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetScreenDataBmpDelegate(long instance, int x1, int y1, int x2, int y2, out long data, out int dataLen);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetScreenDataDelegate(long instance, int x1, int y1, int x2, int y2, out long data, out int dataLen, out int stride);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetScreenDataPtrDelegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CaptureGifDelegate(long instance, int x1, int y1, int x2, int y2, string file, int delay, int time);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int LockDisplayDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetSnapCacheTimeDelegate(long instance, int cacheTime);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetImageDataDelegate(long instance, long imgPtr, out long data, out int size, out int stride);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImageFromPathDelegate(long instance, string source, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImageFromPathAllDelegate(long instance, string source, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImagePtrFromPathDelegate(long instance, long source, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImagePtrFromPathAllDelegate(long instance, long source, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetColorDelegate(long instance, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetColorPtrDelegate(long instance, long source, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CopyImageDelegate(long instance, long sourcePtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeImagePathDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeImageAllDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LoadImageDelegate(long instance, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LoadImageFromBmpDataDelegate(long instance, long data, int dataSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long LoadImageFromRGBDataDelegate(long instance, int width, int height, long scan0, int stride);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeImagePtrDelegate(long instance, long screenPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsFromPtrDelegate(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImageFromPtrDelegate(long instance, long source, long templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchImageFromPtrAllDelegate(long instance, long source, long templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsFromPtrAllDelegate(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsFromPathDelegate(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsFromPathAllDelegate(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsThresholdFromPtrDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsThresholdFromPtrAllDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsThresholdFromPathDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchWindowsThresholdFromPathAllDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ShowMatchWindowDelegate(long instance, int flag);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double CalculateSSIMDelegate(long instance, long image1, long image2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double CalculateHistogramsDelegate(long instance, long image1, long image2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double CalculateMSEDelegate(long instance, long image1, long image2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SaveImageFromPtrDelegate(long instance, long ptr, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReSizeDelegate(long instance, long ptr, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorDelegate(long instance, int x1, int y1, int x2, int y2, string color1, string color2, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorListDelegate(long instance, int x1, int y1, int x2, int y2, string color1, string color2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorExDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorListExDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpMultiColorDelegate(long instance, string pointJson, double sim);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpMultiColorPtrDelegate(long instance, long image, string pointJson, double sim);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindMultiColorDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string pointJson, double sim, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindMultiColorListDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson, string pointJson, double sim);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindMultiColorFromPtrDelegate(long instance, long ptr, string colorJson, string pointJson, double sim, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindMultiColorListFromPtrDelegate(long instance, long ptr, string colorJson, string pointJson, double sim);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetImageSizeDelegate(long instance, long ptr, out int width, out int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorBlockDelegate(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorBlockPtrDelegate(long instance, long ptr, string colorList, int count, int width, int height, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorBlockListDelegate(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorBlockListPtrDelegate(long instance, long ptr, string colorList, int count, int width, int height, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorBlockExDelegate(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FindColorBlockPtrExDelegate(long instance, long ptr, string colorList, int count, int width, int height, int dir, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorBlockListExDelegate(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindColorBlockListPtrExDelegate(long instance, long ptr, string colorList, int count, int width, int height, int type, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetColorNumDelegate(long instance, int x1, int y1, int x2, int y2, string colorList);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetColorNumPtrDelegate(long instance, long ptr, string colorList);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CroppedDelegate(long instance, long image, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetThresholdImageFromMultiColorPtrDelegate(long instance, long ptr, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetThresholdImageFromMultiColorDelegate(long instance, int x1, int y1, int x2, int y2, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsSameImageDelegate(long instance, long ptr, long ptr2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ShowImageDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ShowImageFromFileDelegate(long instance, string file);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SetColorsToNewColorDelegate(long instance, long ptr, string colorJson, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RemoveOtherColorsDelegate(long instance, long ptr, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawRectangleDelegate(long instance, long ptr, int x1, int y1, int x2, int y2, int thickness, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawCircleDelegate(long instance, long ptr, int x, int y, int radius, int thickness, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DrawFillPolyDelegate(long instance, long ptr, string pointJson, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DecodeQRCodeDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateQRCodeDelegate(long instance, string str, int pixelsPerModule);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateQRCodeExDelegate(long instance, string str, int pixelsPerModule, int version, int correction_level, int mode, int structure_number);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchAnimationFromPtrDelegate(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MatchAnimationFromPathDelegate(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RemoveImageDiffDelegate(long instance, long image1, long image2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetImageBmpDataDelegate(long instance, long imgPtr, out long data, out int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetImagePngDataDelegate(long instance, long imgPtr, out long data, out int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FreeImageDataDelegate(long instance, long screenPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ScalePixelsDelegate(long instance, long ptr, int pixelsPerModule);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateImageDelegate(long instance, int width, int height, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetPixelDelegate(long instance, long image, int x, int y, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetPixelListDelegate(long instance, long image, string points, string color);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ConcatImageDelegate(long instance, long image1, long image2, int gap, string color, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CoverImageDelegate(long instance, long image1, long image2, int x, int y, double alpha);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RotateImageDelegate(long instance, long image, double angle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ImageToBase64Delegate(long instance, long image);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long Base64ToImageDelegate(long instance, string base64);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int Hex2ARGBDelegate(long instance, string hex, out int a, out int r, out int g, out int b);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int Hex2RGBDelegate(long instance, string hex, out int r, out int g, out int b);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ARGB2HexDelegate(long instance, int a, int r, int g, int b);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RGB2HexDelegate(long instance, int r, int g, int b);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long Hex2HSVDelegate(long instance, string hex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RGB2HSVDelegate(long instance, int r, int g, int b);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorDelegate(long instance, int x1, int y1, string colorStart, string colorEnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorPtrDelegate(long instance, long ptr, int x, int y, string colorStart, string colorEnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorExDelegate(long instance, int x1, int y1, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorPtrExDelegate(long instance, long ptr, int x, int y, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorHexExDelegate(long instance, string hex, string colorJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CmpColorHexDelegate(long instance, string hex, string colorStart, string colorEnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetConnectedComponentsDelegate(long instance, long ptr, string points, int tolerance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double DetectPointerDirectionDelegate(long instance, long ptr, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double DetectPointerDirectionByFeaturesDelegate(long instance, long ptr, long templatePtr, int x, int y, bool useTemplate);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FastMatchDelegate(long instance, long ptr, long templatePtr, double matchVal, int type, double angle, double scale);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FastROIDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetROIRegionDelegate(long instance, long ptr, out int x1, out int y1, out int x2, out int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetForegroundPointsDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ConvertColorDelegate(long instance, long ptr, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ThresholdDelegate(long instance, long ptr, double thresh, double maxVal, int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RemoveIslandsDelegate(long instance, long ptr, int minArea);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MorphGradientDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MorphTophatDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MorphBlackhatDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DilationDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ErosionDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GaussianBlurDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SharpenDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CannyEdgeDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FlipDelegate(long instance, long ptr, int flipCode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MorphOpenDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long MorphCloseDelegate(long instance, long ptr, int kernelSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long SkeletonizeDelegate(long instance, long ptr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ImageStitchFromPathDelegate(long instance, string path, out long trajectory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ImageStitchCreateDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ImageStitchAppendDelegate(long instance, long imageStitch, long image);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ImageStitchGetResultDelegate(long instance, long imageStitch, out long trajectory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ImageStitchFreeDelegate(long instance, long imageStitch);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long BitPackingDelegate(long instance, long image);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long BitUnpackingDelegate(long instance, string imageStr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetImageCacheDelegate(int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindImageFromPtrDelegate(long instance, long source, long templ, string deltaColor, double matchVal, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindImageFromPtrAllDelegate(long instance, long source, long templ, string deltaColor, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindImageFromPathDelegate(long instance, string source, string templ, string deltaColor, double matchVal, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindImageFromPathAllDelegate(long instance, string source, string templ, string deltaColor, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowsFromPtrDelegate(long instance, int x1, int y1, int x2, int y2, long templ, string deltaColor, double matchVal, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowsFromPtrAllDelegate(long instance, int x1, int y1, int x2, int y2, long templ, string deltaColor, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowsFromPathDelegate(long instance, int x1, int y1, int x2, int y2, string templ, string deltaColor, double matchVal, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowsFromPathAllDelegate(long instance, int x1, int y1, int x2, int y2, string templ, string deltaColor, double matchVal);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryOpenKeyDelegate(long instance, int rootKey, string subKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryCreateKeyDelegate(long instance, int rootKey, string subKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryCloseKeyDelegate(long instance, long key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryKeyExistsDelegate(long instance, int rootKey, string subKey);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryDeleteKeyDelegate(long instance, int rootKey, string subKey, int recursive);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistrySetStringDelegate(long instance, long key, string valueName, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetStringDelegate(long instance, long key, string valueName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistrySetDwordDelegate(long instance, long key, string valueName, int value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryGetDwordDelegate(long instance, long key, string valueName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistrySetQwordDelegate(long instance, long key, string valueName, long value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetQwordDelegate(long instance, long key, string valueName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryDeleteValueDelegate(long instance, long key, string valueName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryEnumSubKeysDelegate(long instance, long key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryEnumValuesDelegate(long instance, long key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistrySetEnvironmentVariableDelegate(long instance, string name, string value, int systemWide);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetEnvironmentVariableDelegate(long instance, string name, int systemWide);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetUserRegistryPathDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetSystemRegistryPathDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryBackupToFileDelegate(long instance, int rootKey, string subKey, string filePath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RegistryRestoreFromFileDelegate(long instance, string filePath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryCompareKeysDelegate(long instance, int rootKey1, string subKey1, int rootKey2, string subKey2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistrySearchKeysDelegate(long instance, int rootKey, string searchPath, string searchPattern, int recursive);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetInstalledSoftwareDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long RegistryGetWindowsVersionDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CreateDatabaseDelegate(long instance, string dbName, string password);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OpenDatabaseDelegate(long instance, string dbName, string password);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OpenMemoryDatabaseDelegate(long instance, long address, int size, string password);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetDatabaseErrorDelegate(long instance, long db);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CloseDatabaseDelegate(long instance, long db);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetAllTableNamesDelegate(long instance, long db);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetTableInfoDelegate(long instance, long db, string tableName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetTableInfoDetailDelegate(long instance, long db, string tableName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExecuteSqlDelegate(long instance, long db, string sql);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExecuteScalarDelegate(long instance, long db, string sql);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ExecuteReaderDelegate(long instance, long db, string sql);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ReadDelegate(long instance, long stmt);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetDataCountDelegate(long instance, long stmt);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetColumnCountDelegate(long instance, long stmt);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetColumnNameDelegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetColumnIndexDelegate(long instance, long stmt, string columnName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetColumnTypeDelegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int FinalizeDelegate(long instance, long stmt);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetDoubleDelegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetInt32Delegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetInt64Delegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetStringDelegate(long instance, long stmt, int iCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetDoubleByColumnNameDelegate(long instance, long stmt, string columnName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetInt32ByColumnNameDelegate(long instance, long stmt, string columnName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetInt64ByColumnNameDelegate(long instance, long stmt, string columnName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetStringByColumnNameDelegate(long instance, long stmt, string columnName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InitOlaDatabaseDelegate(long instance, long db);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InitOlaImageFromDirDelegate(long instance, long db, string dir, int cover);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveOlaImageFromDirDelegate(long instance, long db, string dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExportOlaImageDirDelegate(long instance, long db, string dir, string exportDir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ImportOlaImageDelegate(long instance, long db, string dir, string fileName, int cover);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetOlaImageDelegate(long instance, long db, string dir, string fileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveOlaImageDelegate(long instance, long db, string dir, string fileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetDbConfigDelegate(long instance, long db, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetDbConfigDelegate(long instance, long db, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveDbConfigDelegate(long instance, long db, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetDbConfigExDelegate(long instance, string key, string value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetDbConfigExDelegate(long instance, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveDbConfigExDelegate(long instance, string key);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InitDictFromDirDelegate(long instance, long db, string dict_name, string dict_path, int cover);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int InitDictFromTxtDelegate(long instance, long db, string dict_name, string dict_path, int cover);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ImportDictWordDelegate(long instance, long db, string dict_name, string pic_file_name, int cover);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExportDictDelegate(long instance, long db, string dict_name, string export_dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveDictDelegate(long instance, long db, string dict_name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int RemoveDictWordDelegate(long instance, long db, string dict_name, string word);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetDictImageDelegate(long instance, long db, string dict_name, string word, int gap, int dir);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OpenVideoDelegate(long instance, string videoPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long OpenCameraDelegate(long instance, int deviceIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CloseVideoDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsVideoOpenedDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetVideoInfoDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetVideoWidthDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetVideoHeightDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetVideoFPSDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetVideoTotalFramesDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetVideoDurationDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetCurrentFrameIndexDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetCurrentTimestampDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadNextFrameDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadFrameAtIndexDelegate(long instance, long videoHandle, int frameIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadFrameAtTimeDelegate(long instance, long videoHandle, double timestamp);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ReadCurrentFrameDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SeekToFrameDelegate(long instance, long videoHandle, int frameIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SeekToTimeDelegate(long instance, long videoHandle, double timestamp);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SeekToBeginningDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SeekToEndDelegate(long instance, long videoHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExtractFramesToFilesDelegate(long instance, long videoHandle, int startFrame, int endFrame, int step, string outputDir, string imageFormat, int jpegQuality);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExtractFramesByIntervalDelegate(long instance, long videoHandle, double intervalSeconds, string outputDir, string imageFormat);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ExtractKeyFramesDelegate(long instance, long videoHandle, double threshold, int maxFrames, string outputDir, string imageFormat);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SaveCurrentFrameDelegate(long instance, long videoHandle, string outputPath, int quality);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SaveFrameAtIndexDelegate(long instance, long videoHandle, int frameIndex, string outputPath, int quality);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FrameToBase64Delegate(long instance, long videoHandle, string format);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double CalculateFrameSimilarityDelegate(long instance, long frame1, long frame2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetVideoInfoFromPathDelegate(long instance, string videoPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsValidVideoFileDelegate(long instance, string videoPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ExtractSingleFrameDelegate(long instance, string videoPath, int frameIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long ExtractThumbnailDelegate(long instance, string videoPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ConvertVideoDelegate(long instance, string inputPath, string outputPath, string codec, double fps);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ResizeVideoDelegate(long instance, string inputPath, string outputPath, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TrimVideoDelegate(long instance, string inputPath, string outputPath, double startTime, double endTime);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateVideoFromImagesDelegate(long instance, string imageDir, string outputPath, double fps, string codec);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DetectSceneChangesDelegate(long instance, string videoPath, double threshold);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double CalculateAverageBrightnessDelegate(long instance, string videoPath);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long DetectMotionDelegate(long instance, string videoPath, double threshold);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetWindowStateDelegate(long instance, long hwnd, int state);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowDelegate(long instance, string class_name, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetClipboardDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetClipboardDelegate(long instance, string text);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SendPasteDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetWindowDelegate(long instance, long hwnd, int flag);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetWindowTitleDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetWindowClassDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetWindowRectDelegate(long instance, long hwnd, out int x1, out int y1, out int x2, out int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetWindowProcessPathDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetWindowStateDelegate(long instance, long hwnd, int flag);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetForegroundWindowDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetWindowProcessIdDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetClientSizeDelegate(long instance, long hwnd, out int width, out int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetMousePointWindowDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetSpecialWindowDelegate(long instance, int flag);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetClientRectDelegate(long instance, long hwnd, out int x1, out int y1, out int x2, out int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetWindowTextDelegate(long instance, long hwnd, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetWindowSizeDelegate(long instance, long hwnd, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetClientSizeDelegate(long instance, long hwnd, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetWindowTransparentDelegate(long instance, long hwnd, int alpha);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowExDelegate(long instance, long parent, string class_name, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowByProcessDelegate(long instance, string process_name, string class_name, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MoveWindowDelegate(long instance, long hwnd, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetScaleFromWindowsDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double GetWindowDpiAwarenessScaleDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EnumProcessDelegate(long instance, string name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EnumWindowDelegate(long instance, long parent, string title, string className, int filter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EnumWindowByProcessDelegate(long instance, string process_name, string title, string class_name, int filter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EnumWindowByProcessIdDelegate(long instance, long pid, string title, string class_name, int filter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long EnumWindowSuperDelegate(long instance, string spec1, int flag1, int type1, string spec2, int flag2, int type2, int sort);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetPointWindowDelegate(long instance, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetProcessInfoDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ShowTaskBarIconDelegate(long instance, long hwnd, int show);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowByProcessIdDelegate(long instance, long process_id, string className, string title);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetWindowThreadIdDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long FindWindowSuperDelegate(long instance, string spec1, int flag1, int type1, string spec2, int flag2, int type2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ClientToScreenDelegate(long instance, long hwnd, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ScreenToClientDelegate(long instance, long hwnd, out int x, out int y);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetForegroundFocusDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetWindowDisplayDelegate(long instance, long hwnd, int affinity);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int IsDisplayDeadDelegate(long instance, int x1, int y1, int x2, int y2, int time);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetWindowsFpsDelegate(long instance, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TerminateProcessDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int TerminateProcessTreeDelegate(long instance, long pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetCommandLineDelegate(long instance, long hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CheckFontSmoothDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SetFontSmoothDelegate(long instance, int enable);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int EnableDebugPrivilegeDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int SystemStartDelegate(long instance, string applicationName, string commandLine);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateChildProcessDelegate(long instance, string applicationName, string commandLine, string currentDirectory, int showType, int parentProcessId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long GetProcessIconImageDelegate(long instance, long pid, int targetWidth, int targetHeight);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlCreateDocumentDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlParseDelegate(string str, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlParseFileDelegate(string filepath, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlToStringDelegate(long doc, int compact, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSaveToFileDelegate(long doc, string filepath, int compact, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlFreeDelegate(long doc);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetRootElementDelegate(long doc, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlCreateElementDelegate(long doc, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlInsertRootElementDelegate(long doc, long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlAppendChildDelegate(long parent, long child, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetFirstChildDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetNextSiblingDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlFindElementDelegate(long parent, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetElementNameDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetElementTextDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetElementTextDelegate(long element, string text, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlRemoveChildDelegate(long parent, long child, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlInsertBeforeDelegate(long parent, long newChild, long refChild, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlInsertAfterDelegate(long parent, long newChild, long refChild, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetParentDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetPreviousSiblingDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetLastChildDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlCloneElementDelegate(long doc, long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlHasChildrenDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetAttributeDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetAttributeDelegate(long element, string name, string value, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetAttributeIntDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetAttributeIntDelegate(long element, string name, int value, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate double XmlGetAttributeDoubleDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetAttributeDoubleDelegate(long element, string name, double value, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetAttributeBoolDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetAttributeBoolDelegate(long element, string name, int value, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetAttributeInt64Delegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetAttributeInt64Delegate(long element, string name, long value, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlHasAttributeDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlDeleteAttributeDelegate(long element, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetAttributeNamesDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetAttributeCountDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetCDATADelegate(long doc, long element, string content, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlAddCommentDelegate(long doc, long element, string comment, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlSetDeclarationDelegate(long doc, string version, string encoding, int standalone, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlQueryElementDelegate(long doc, string path, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetChildCountDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetChildCountByNameDelegate(long parent, string name, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetChildByIndexDelegate(long parent, int index, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetChildByNameAndIndexDelegate(long parent, string name, int index, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlFindElementByAttributeDelegate(long parent, string elementName, string attrName, string attrValue, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetElementDepthDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long XmlGetElementPathDelegate(long element, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlCompareElementsDelegate(long element1, long element2, int deep, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlMergeDocumentsDelegate(long targetDoc, long sourceDoc, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlValidateDelegate(long doc, out int err);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlGetObjectCountDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int XmlCleanupAllDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloLoadModelDelegate(long instance, string modelPath, string outputPath, string names_label, string password, int modelType, int inferenceType, int inferenceDevice);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloReleaseModelDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloLoadModelMemoryDelegate(long instance, long memoryAddr, int size, int modelType, int inferenceType, int inferenceDevice);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloInferDelegate(long instance, long handle, long imagePtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloIsModelValidDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloListModelsDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloGetModelInfoDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloSetModelConfigDelegate(long instance, long modelHandle, string configJson);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloGetModelConfigDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloWarmupDelegate(long instance, long modelHandle, int iterations);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, string classes, double confidence, double iou, int maxDetections);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectSimpleDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectFromPtrDelegate(long instance, long modelHandle, long imagePtr, string classes, double confidence, double iou, int maxDetections);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectFromFileDelegate(long instance, long modelHandle, string imagePath, string classes, double confidence, double iou, int maxDetections);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectFromBase64Delegate(long instance, long modelHandle, string base64Data, string classes, double confidence, double iou, int maxDetections);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloDetectBatchDelegate(long instance, long modelHandle, string imagesJson, string classes, double confidence, double iou, int maxDetections);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloClassifyDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, int topK);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloClassifyFromPtrDelegate(long instance, long modelHandle, long imagePtr, int topK);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloClassifyFromFileDelegate(long instance, long modelHandle, string imagePath, int topK);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloSegmentDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloSegmentFromPtrDelegate(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloPoseDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloPoseFromPtrDelegate(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloObbDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloObbFromPtrDelegate(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloKeyPointDelegate(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloKeyPointFromPtrDelegate(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloGetInferenceStatsDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloResetStatsDelegate(long instance, long modelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long YoloGetLastErrorDelegate(long instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int YoloClearErrorDelegate(long instance);

        #endregion

        #region 私有字段和单例管理

        private static DynamicNativeLibrary _library;
        private static readonly object _lockObject = new object();
        private static string _currentDllPath;

        #endregion

        #region DLL加载和初始化

        /// <summary>
        /// 初始化DLL加载器
        /// </summary>
        /// <param name="dllName">DLL文件名，如 "OLAPlug_x64.dll" 或 "OLAPlug_x64_Pro.dll"</param>
        /// <param name="dllDirectory">DLL所在目录，如果为null则使用当前目录下的"olaplug"文件夹</param>
        /// <returns>是否加载成功</returns>
        public static bool Initialize(string dllPath)
        {
            lock (_lockObject)
            {
                try
                {
                    // 确定DLL路径
                    if (!Path.IsPathRooted(dllPath))
                        dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllPath);

                    if (!File.Exists(dllPath))
                        throw new FileNotFoundException($"DLL文件不存在: {dllPath}");

                    // 如果已经加载了相同的DLL，直接返回成功
                    if (_library != null && _currentDllPath == dllPath)
                    {
                        return true;
                    }

                    // 释放旧的库
                    if (_library != null)
                    {
                        _library.Dispose();
                        _library = null;
                    }

                    // 创建新的库实例
                    _library = new DynamicNativeLibrary();
                    if (!_library.Load(dllPath))
                    {
                        _library = null;
                        throw new DllNotFoundException($"无法加载DLL: {dllPath}");
                    }

                    _currentDllPath = dllPath;

                    return true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"初始化DLL失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 检查DLL是否已初始化
        /// </summary>
        public static bool IsInitialized => _library != null;

        /// <summary>
        /// 获取当前加载的DLL路径
        /// </summary>
        public static string CurrentDllPath => _currentDllPath;

        /// <summary>
        /// 获取当前加载的DLL文件名（向后兼容，用于旧代码）
        /// </summary>
        public static string DLL => _currentDllPath != null ? Path.GetFileName(_currentDllPath) : null;

        #endregion

        #region 内部辅助方法

        /// <summary>
        /// 获取函数委托（内部方法）
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="functionName">函数名</param>
        /// <returns>函数委托</returns>
        public static T GetFunction<T>(string functionName) where T : Delegate
        {
            if (_library == null)
            {
                throw new InvalidOperationException("DLL未初始化，请先调用Initialize方法");
            }

            return _library.GetFunction<T>(functionName);
        }

        #endregion

    }

    #region 动态库加载器（内部类）

    /// <summary>
    /// 动态本地库加载器（单例模式）
    /// </summary>
    internal class DynamicNativeLibrary : IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private IntPtr _libraryHandle = IntPtr.Zero;
        private static ConcurrentDictionary<string, FuncMapInfo> funcMap = new ConcurrentDictionary<string, FuncMapInfo>();

        /// <summary>
        /// 加载DLL
        /// </summary>
        public bool Load(string libraryPath)
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                FreeLibrary(_libraryHandle);
                funcMap.Clear();
            }

            _libraryHandle = LoadLibrary(libraryPath);
            if (_libraryHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new DllNotFoundException($"无法加载DLL: {libraryPath}, 错误代码: {error}");
            }

            // 加载map
            LoadFuncMap();

            return _libraryHandle != IntPtr.Zero;
        }

        /// <summary>
        /// 加载函数映射，返回默认值
        /// </summary>
        private static void LoadFuncMap()
        {

        }

        /// <summary>
        /// 获取函数委托
        /// </summary>
        public T GetFunction<T>(string functionName) where T : Delegate
        {
            if (_libraryHandle == IntPtr.Zero)
                throw new InvalidOperationException("Library not loaded");

            var func = funcMap.GetValueOrDefault(functionName, new FuncMapInfo(functionName));

            // 检查缓存
            if (func.DelegateInstance!=null)
                return (T)func.DelegateInstance;

            // 获取函数地址
            IntPtr procAddress = IntPtr.Zero;
            if (func.RVA == 0)
            {
                procAddress = GetProcAddress(_libraryHandle, func.ExportedName);
            }
            else
            {
                procAddress = (nint)((long)_libraryHandle + func.RVA);
            }

            // 异常处理(地址获取失败)
            if (procAddress == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new EntryPointNotFoundException($"函数 '{functionName}' 在DLL中未找到, 错误代码: {error}");
            }

            // 创建委托，并缓存
            T delegateInstance = Marshal.GetDelegateForFunctionPointer<T>(procAddress);
            func.DelegateInstance = delegateInstance;
            funcMap[functionName] = func;

            return delegateInstance;
        }


        public void Dispose()
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                FreeLibrary(_libraryHandle);
                _libraryHandle = IntPtr.Zero;
                funcMap.Clear();
            }
            GC.SuppressFinalize(this);
        }

        ~DynamicNativeLibrary()
        {
            Dispose();
        }
    }

    #endregion
}
