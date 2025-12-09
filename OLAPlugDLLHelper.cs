using System.Runtime.InteropServices;
using System.Text;

namespace OLAPlug
{
    public static class OLAPlugDLLHelper
    {
        public const string DLL = "OLAPlug-1205_x64.dll"; //支持修改DLL名称为任意值,只要跟文件对应好就行 比如abc.cdf。
        //public const string DLL = "OLAPlug_x86.dll"; //支持修改DLL名称为任意值,只要跟文件对应好就行 比如abc.cdf。

        /// <summary>
        /// 键盘按键回调
        /// </summary>
        /// <param name="keycode"></param>
        /// <param name="modifiers"></param>
        public delegate void HotkeyCallback(int keycode, int modifiers);

        /// <summary>
        /// 鼠标按键回调
        /// </summary>
        /// <param name="button"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="clicks"></param>
        public delegate void MouseCallback(int button, int x, int y, int clicks);

        /// <summary>
        /// 鼠标滚轮回调
        /// </summary>
        /// <param name="x">鼠标滚轮的X坐标</param>
        /// <param name="y">鼠标滚轮的Y坐标</param>
        /// <param name="amount">滚轮的滚动量</param>
        /// <param name="rotation">滚轮的旋转方向</param>
        public delegate void MouseWheelCallback(int x, int y, int amount, int rotation);

        /// <summary>
        /// 鼠标移动回调
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public delegate void MouseMoveCallback(int x, int y);

        /// <summary>
        /// 鼠标拖拽回调
        /// </summary>
        /// <param name="button"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public delegate void MouseDragCallback(int x, int y);

        /// <summary>
        /// 绘图按钮回调
        /// </summary>
        /// <param name="buttonHandle"></param>
        public delegate void DrawGuiButtonCallback(long buttonHandle);

        /// <summary>
        /// 绘图鼠标回调
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="event"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public delegate void DrawGuiMouseCallback(long handle, int _event, int x, int y);

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

        [DllImport(DLL)]
        public static extern long CreateCOLAPlugInterFace();

        [DllImport(DLL)]
        public static extern int DestroyCOLAPlugInterFace(long instance);

        [DllImport(DLL)]
        public static extern long Ver();

        [DllImport(DLL)]
        public static extern int SetPath(long instance, string path);

        [DllImport(DLL)]
        public static extern long GetPath(long instance);

        [DllImport(DLL)]
        public static extern long GetMachineCode(long instance);

        [DllImport(DLL)]
        public static extern long GetBasePath(long instance);

        [DllImport(DLL)]
        public static extern int Reg(string userCode, string softCode, string featureList);

        [DllImport(DLL)]
        public static extern int BindWindow(long instance, long hwnd, string display, string mouse, string keypad, int mode);

        [DllImport(DLL)]
        public static extern int BindWindowEx(long instance, long hwnd, string display, string mouse, string keypad, string pubstr, int mode);

        [DllImport(DLL)]
        public static extern int UnBindWindow(long instance);

        [DllImport(DLL)]
        public static extern long GetBindWindow(long instance);

        [DllImport(DLL)]
        public static extern int ReleaseWindowsDll(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern int FreeStringPtr(long ptr);

        [DllImport(DLL)]
        public static extern int FreeMemoryPtr(long ptr);

        [DllImport(DLL)]
        public static extern int GetStringSize(long ptr);

        [DllImport(DLL)]
        public static extern int GetStringFromPtr(long ptr, StringBuilder lpString, int size);

        [DllImport(DLL)]
        public static extern int Delay(int millisecond);

        [DllImport(DLL)]
        public static extern int Delays(int minMillisecond, int maxMillisecond);

        [DllImport(DLL)]
        public static extern int SetUAC(long instance, int enable);

        [DllImport(DLL)]
        public static extern int CheckUAC(long instance);

        [DllImport(DLL)]
        public static extern int RunApp(long instance, string appPath, int mode);

        [DllImport(DLL)]
        public static extern long ExecuteCmd(long instance, string cmd, string current_dir, int time_out);

        [DllImport(DLL)]
        public static extern long GetConfig(long instance, string configKey);

        [DllImport(DLL)]
        public static extern int SetConfig(long instance, string configStr);

        [DllImport(DLL)]
        public static extern int SetConfigByKey(long instance, string key, string value);

        [DllImport(DLL)]
        public static extern int SendDropFiles(long instance, long hwnd, string file_path);

        [DllImport(DLL)]
        public static extern int SetDefaultEncode(int inputEncoding, int outputEncoding);

        [DllImport(DLL)]
        public static extern int GetRandomNumber(long instance, int min, int max);

        [DllImport(DLL)]
        public static extern double GetRandomDouble(long instance, double min, double max);

        [DllImport(DLL)]
        public static extern long ExcludePos(long instance, string json, int type, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long FindNearestPos(long instance, string json, int type, int x, int y);

        [DllImport(DLL)]
        public static extern long SortPosDistance(long instance, string json, int type, int x, int y);

        [DllImport(DLL)]
        public static extern int GetDenseRect(long instance, long image, int width, int height, out int x1, out int y1, out int x2, out int y2);

        [DllImport(DLL)]
        public static extern long PathPlanning(long instance, long image, int startX, int startY, int endX, int endY, double potentialRadius, double searchRadius);

        [DllImport(DLL)]
        public static extern long CreateGraph(long instance, string json);

        [DllImport(DLL)]
        public static extern long GetGraph(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern int AddEdge(long instance, long graphPtr, string from, string to, double weight, bool isDirected);

        [DllImport(DLL)]
        public static extern long GetShortestPath(long instance, long graphPtr, string from, string to);

        [DllImport(DLL)]
        public static extern double GetShortestDistance(long instance, long graphPtr, string from, string to);

        [DllImport(DLL)]
        public static extern int ClearGraph(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern int DeleteGraph(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern int GetNodeCount(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern int GetEdgeCount(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern long GetShortestPathToAllNodes(long instance, long graphPtr, string startNode);

        [DllImport(DLL)]
        public static extern long GetMinimumSpanningTree(long instance, long graphPtr);

        [DllImport(DLL)]
        public static extern long GetDirectedPathToAllNodes(long instance, long graphPtr, string startNode);

        [DllImport(DLL)]
        public static extern long GetMinimumArborescence(long instance, long graphPtr, string root);

        [DllImport(DLL)]
        public static extern long CreateGraphFromCoordinates(long instance, string json, bool connectAll, double maxDistance, bool useEuclideanDistance);

        [DllImport(DLL)]
        public static extern int AddCoordinateNode(long instance, long graphPtr, string name, double x, double y, bool connectToExisting, double maxDistance, bool useEuclideanDistance);

        [DllImport(DLL)]
        public static extern long GetNodeCoordinates(long instance, long graphPtr, string name);

        [DllImport(DLL)]
        public static extern int SetNodeConnection(long instance, long graphPtr, string from, string to, bool canConnect, double weight);

        [DllImport(DLL)]
        public static extern int GetNodeConnectionStatus(long instance, long graphPtr, string from, string to);

        [DllImport(DLL)]
        public static extern long AsmCall(long instance, long hwnd, string asmStr, int type, long baseAddr);

        [DllImport(DLL)]
        public static extern long Assemble(long instance, string asmStr, long baseAddr, int arch, int mode);

        [DllImport(DLL)]
        public static extern long Disassemble(long instance, string asmCode, long baseAddr, int arch, int mode, int showType);

        [DllImport(DLL)]
        public static extern int DrawGuiCleanup(long instance);

        [DllImport(DLL)]
        public static extern int DrawGuiSetGuiActive(long instance, int active);

        [DllImport(DLL)]
        public static extern int DrawGuiIsGuiActive(long instance);

        [DllImport(DLL)]
        public static extern int DrawGuiSetGuiClickThrough(long instance, int enabled);

        [DllImport(DLL)]
        public static extern int DrawGuiIsGuiClickThrough(long instance);

        [DllImport(DLL)]
        public static extern long DrawGuiRectangle(long instance, int x, int y, int width, int height, int mode, double lineThickness);

        [DllImport(DLL)]
        public static extern long DrawGuiCircle(long instance, int x, int y, int radius, int mode, double lineThickness);

        [DllImport(DLL)]
        public static extern long DrawGuiLine(long instance, int x1, int y1, int x2, int y2, double lineThickness);

        [DllImport(DLL)]
        public static extern long DrawGuiText(long instance, string text, int x, int y, string fontPath, int fontSize, int align);

        [DllImport(DLL)]
        public static extern long DrawGuiImage(long instance, string imagePath, int x, int y);

        [DllImport(DLL)]
        public static extern long DrawGuiImagePtr(long instance, long imagePtr, int x, int y);

        [DllImport(DLL)]
        public static extern long DrawGuiWindow(long instance, string title, int x, int y, int width, int height, int style);

        [DllImport(DLL)]
        public static extern long DrawGuiPanel(long instance, long parentHandle, int x, int y, int width, int height);

        [DllImport(DLL)]
        public static extern long DrawGuiButton(long instance, long parentHandle, string text, int x, int y, int width, int height);

        [DllImport(DLL)]
        public static extern int DrawGuiSetPosition(long instance, long handle, int x, int y);

        [DllImport(DLL)]
        public static extern int DrawGuiSetSize(long instance, long handle, int width, int height);

        [DllImport(DLL)]
        public static extern int DrawGuiSetColor(long instance, long handle, int r, int g, int b, int a);

        [DllImport(DLL)]
        public static extern int DrawGuiSetAlpha(long instance, long handle, int alpha);

        [DllImport(DLL)]
        public static extern int DrawGuiSetDrawMode(long instance, long handle, int mode);

        [DllImport(DLL)]
        public static extern int DrawGuiSetLineThickness(long instance, long handle, double thickness);

        [DllImport(DLL)]
        public static extern int DrawGuiSetFont(long instance, long handle, string fontPath, int fontSize);

        [DllImport(DLL)]
        public static extern int DrawGuiSetTextAlign(long instance, long handle, int align);

        [DllImport(DLL)]
        public static extern int DrawGuiSetText(long instance, long handle, string text);

        [DllImport(DLL)]
        public static extern int DrawGuiSetWindowTitle(long instance, long handle, string title);

        [DllImport(DLL)]
        public static extern int DrawGuiSetWindowStyle(long instance, long handle, int style);

        [DllImport(DLL)]
        public static extern int DrawGuiSetWindowTopMost(long instance, long handle, int topMost);

        [DllImport(DLL)]
        public static extern int DrawGuiSetWindowTransparency(long instance, long handle, int alpha);

        [DllImport(DLL)]
        public static extern int DrawGuiDeleteObject(long instance, long handle);

        [DllImport(DLL)]
        public static extern int DrawGuiClearAll(long instance);

        [DllImport(DLL)]
        public static extern int DrawGuiSetVisible(long instance, long handle, int visible);

        [DllImport(DLL)]
        public static extern int DrawGuiSetZOrder(long instance, long handle, int zOrder);

        [DllImport(DLL)]
        public static extern int DrawGuiSetParent(long instance, long handle, long parentHandle);

        [DllImport(DLL)]
        public static extern int DrawGuiSetButtonCallback(long instance, long handle, DrawGuiButtonCallback callback);

        [DllImport(DLL)]
        public static extern int DrawGuiSetMouseCallback(long instance, long handle, DrawGuiMouseCallback callback);

        [DllImport(DLL)]
        public static extern int DrawGuiGetDrawObjectType(long instance, long handle);

        [DllImport(DLL)]
        public static extern int DrawGuiGetPosition(long instance, long handle, out int x, out int y);

        [DllImport(DLL)]
        public static extern int DrawGuiGetSize(long instance, long handle, out int width, out int height);

        [DllImport(DLL)]
        public static extern int DrawGuiIsPointInObject(long instance, long handle, int x, int y);

        [DllImport(DLL)]
        public static extern int SetMemoryMode(long instance, int mode);

        [DllImport(DLL)]
        public static extern int ExportDriver(long instance, string driver_path, int type);

        [DllImport(DLL)]
        public static extern int LoadDriver(long instance, string driver_name, string driver_path);

        [DllImport(DLL)]
        public static extern int UnloadDriver(long instance, string driver_name);

        [DllImport(DLL)]
        public static extern int DriverTest(long instance);

        [DllImport(DLL)]
        public static extern int LoadPdb(long instance);

        [DllImport(DLL)]
        public static extern int HideProcess(long instance, long pid, int enable);

        [DllImport(DLL)]
        public static extern int ProtectProcess(long instance, long pid, int enable);

        [DllImport(DLL)]
        public static extern int AddProtectPID(long instance, long pid, long mode, long allow_pid);

        [DllImport(DLL)]
        public static extern int RemoveProtectPID(long instance, long pid);

        [DllImport(DLL)]
        public static extern int AddAllowPID(long instance, long pid);

        [DllImport(DLL)]
        public static extern int RemoveAllowPID(long instance, long pid);

        [DllImport(DLL)]
        public static extern int InjectDll(long instance, long pid, string dll_path, int mode);

        [DllImport(DLL)]
        public static extern int FakeProcess(long instance, long pid, long fake_pid);

        [DllImport(DLL)]
        public static extern int ProtectWindow(long instance, long hwnd, int flag);

        [DllImport(DLL)]
        public static extern int KeOpenProcess(long instance, long pid, out long process_handle);

        [DllImport(DLL)]
        public static extern int KeOpenThread(long instance, long thread_id, out long thread_handle);

        [DllImport(DLL)]
        public static extern int StartSecurityGuard(long instance);

        [DllImport(DLL)]
        public static extern int GenerateRSAKey(long instance, string publicKeyPath, string privateKeyPath, int type, int keySize);

        [DllImport(DLL)]
        public static extern long ConvertRSAPublicKey(long instance, string publicKey, int inputType, int outputType);

        [DllImport(DLL)]
        public static extern long ConvertRSAPrivateKey(long instance, string privateKey, int inputType, int outputType);

        [DllImport(DLL)]
        public static extern long EncryptWithRsa(long instance, string message, string publicKey, int paddingType);

        [DllImport(DLL)]
        public static extern long DecryptWithRsa(long instance, string cipher, string privateKey, int paddingType);

        [DllImport(DLL)]
        public static extern long SignWithRsa(long instance, string message, string privateCer, int shaType, int paddingType);

        [DllImport(DLL)]
        public static extern int VerifySignWithRsa(long instance, string message, string signature, int shaType, int paddingType, string publicCer);

        [DllImport(DLL)]
        public static extern long AESEncrypt(long instance, string source, string key);

        [DllImport(DLL)]
        public static extern long AESDecrypt(long instance, string source, string key);

        [DllImport(DLL)]
        public static extern long AESEncryptEx(long instance, string source, string key, string iv, int mode, int paddingType);

        [DllImport(DLL)]
        public static extern long AESDecryptEx(long instance, string source, string key, string iv, int mode, int paddingType);

        [DllImport(DLL)]
        public static extern long MD5Encrypt(long instance, string source);

        [DllImport(DLL)]
        public static extern long SHAHash(long instance, string source, int shaType);

        [DllImport(DLL)]
        public static extern long HMAC(long instance, string source, string key, int shaType);

        [DllImport(DLL)]
        public static extern long GenerateRandomBytes(long instance, int length, int type);

        [DllImport(DLL)]
        public static extern long GenerateGuid(long instance, int type);

        [DllImport(DLL)]
        public static extern long Base64Encode(long instance, string source);

        [DllImport(DLL)]
        public static extern long Base64Decode(long instance, string source);

        [DllImport(DLL)]
        public static extern long PBKDF2(long instance, string password, string salt, int iterations, int keyLength, int shaType);

        [DllImport(DLL)]
        public static extern long MD5File(long instance, string filePath);

        [DllImport(DLL)]
        public static extern long SHAFile(long instance, string filePath, int shaType);

        [DllImport(DLL)]
        public static extern int CreateFolder(long instance, string path);

        [DllImport(DLL)]
        public static extern int DeleteFolder(long instance, string path);

        [DllImport(DLL)]
        public static extern long GetFolderList(long instance, string path, string baseDir);

        [DllImport(DLL)]
        public static extern int IsDirectory(long instance, string path);

        [DllImport(DLL)]
        public static extern int IsFile(long instance, string path);

        [DllImport(DLL)]
        public static extern int CreateFile(long instance, string path);

        [DllImport(DLL)]
        public static extern int DeleteFile(long instance, string path);

        [DllImport(DLL)]
        public static extern int CopyFile(long instance, string src, string dst);

        [DllImport(DLL)]
        public static extern int MoveFile(long instance, string src, string dst);

        [DllImport(DLL)]
        public static extern int RenameFile(long instance, string src, string dst);

        [DllImport(DLL)]
        public static extern long GetFileSize(long instance, string path);

        [DllImport(DLL)]
        public static extern long GetFileList(long instance, string path, string baseDir);

        [DllImport(DLL)]
        public static extern long GetFileName(long instance, string path, int withExtension);

        [DllImport(DLL)]
        public static extern long ToAbsolutePath(long instance, string path);

        [DllImport(DLL)]
        public static extern long ToRelativePath(long instance, string path);

        [DllImport(DLL)]
        public static extern int FileOrDirectoryExists(long instance, string path);

        [DllImport(DLL)]
        public static extern long ReadFileString(long instance, string filePath, int encoding);

        [DllImport(DLL)]
        public static extern long ReadBytesFromFile(long instance, string filePath, int offset, long size);

        [DllImport(DLL)]
        public static extern int StartHotkeyHook(long instance);

        [DllImport(DLL)]
        public static extern int StopHotkeyHook(long instance);

        [DllImport(DLL)]
        public static extern int RegisterHotkey(long instance, int keycode, int modifiers, HotkeyCallback callback);

        [DllImport(DLL)]
        public static extern int UnregisterHotkey(long instance, int keycode, int modifiers);

        [DllImport(DLL)]
        public static extern int RegisterMouseButton(long instance, int button, int type, MouseCallback callback);

        [DllImport(DLL)]
        public static extern int UnregisterMouseButton(long instance, int button, int type);

        [DllImport(DLL)]
        public static extern int RegisterMouseWheel(long instance, MouseWheelCallback callback);

        [DllImport(DLL)]
        public static extern int UnregisterMouseWheel(long instance);

        [DllImport(DLL)]
        public static extern int RegisterMouseMove(long instance, MouseMoveCallback callback);

        [DllImport(DLL)]
        public static extern int UnregisterMouseMove(long instance);

        [DllImport(DLL)]
        public static extern int RegisterMouseDrag(long instance, MouseDragCallback callback);

        [DllImport(DLL)]
        public static extern int UnregisterMouseDrag(long instance);

        [DllImport(DLL)]
        public static extern long JsonCreateObject();

        [DllImport(DLL)]
        public static extern long JsonCreateArray();

        [DllImport(DLL)]
        public static extern long JsonParse(string str, out int err);

        [DllImport(DLL)]
        public static extern long JsonStringify(long obj, int indent, out int err);

        [DllImport(DLL)]
        public static extern int JsonFree(long obj);

        [DllImport(DLL)]
        public static extern long JsonGetValue(long obj, string key, out int err);

        [DllImport(DLL)]
        public static extern long JsonGetArrayItem(long arr, int index, out int err);

        [DllImport(DLL)]
        public static extern long JsonGetString(long obj, string key, out int err);

        [DllImport(DLL)]
        public static extern double JsonGetNumber(long obj, string key, out int err);

        [DllImport(DLL)]
        public static extern int JsonGetBool(long obj, string key, out int err);

        [DllImport(DLL)]
        public static extern int JsonGetSize(long obj, out int err);

        [DllImport(DLL)]
        public static extern int JsonSetValue(long obj, string key, long value);

        [DllImport(DLL)]
        public static extern int JsonArrayAppend(long arr, long value);

        [DllImport(DLL)]
        public static extern int JsonSetString(long obj, string key, string value);

        [DllImport(DLL)]
        public static extern int JsonSetNumber(long obj, string key, double value);

        [DllImport(DLL)]
        public static extern int JsonSetBool(long obj, string key, int value);

        [DllImport(DLL)]
        public static extern int JsonDeleteKey(long obj, string key);

        [DllImport(DLL)]
        public static extern int JsonClear(long obj);

        [DllImport(DLL)]
        public static extern int ParseMatchImageJson(string str, out int matchState, out int x, out int y, out double matchVal, out double angle, out int index);

        [DllImport(DLL)]
        public static extern int GetMatchImageAllCount(string str);

        [DllImport(DLL)]
        public static extern int ParseMatchImageAllJson(string str, int parseIndex, out int matchState, out int x, out int y, out double matchVal, out double angle, out int index);

        [DllImport(DLL)]
        public static extern int GetResultCount(string resultStr);

        [DllImport(DLL)]
        public static extern long GenerateMouseTrajectory(long instance, int startX, int startY, int endX, int endY);

        [DllImport(DLL)]
        public static extern int KeyDown(long instance, int vk_code);

        [DllImport(DLL)]
        public static extern int KeyUp(long instance, int vk_code);

        [DllImport(DLL)]
        public static extern int KeyPress(long instance, int vk_code);

        [DllImport(DLL)]
        public static extern int LeftDown(long instance);

        [DllImport(DLL)]
        public static extern int LeftUp(long instance);

        [DllImport(DLL)]
        public static extern int MoveTo(long instance, int x, int y);

        [DllImport(DLL)]
        public static extern int MoveToWithoutSimulator(long instance, int x, int y);

        [DllImport(DLL)]
        public static extern int RightClick(long instance);

        [DllImport(DLL)]
        public static extern int RightDoubleClick(long instance);

        [DllImport(DLL)]
        public static extern int RightDown(long instance);

        [DllImport(DLL)]
        public static extern int RightUp(long instance);

        [DllImport(DLL)]
        public static extern long GetCursorShape(long instance);

        [DllImport(DLL)]
        public static extern long GetCursorImage(long instance);

        [DllImport(DLL)]
        public static extern int KeyPressStr(long instance, string keyStr, int delay);

        [DllImport(DLL)]
        public static extern int SendString(long instance, long hwnd, string str);

        [DllImport(DLL)]
        public static extern int SendStringEx(long instance, long hwnd, long addr, int len, int type);

        [DllImport(DLL)]
        public static extern int KeyPressChar(long instance, string keyStr);

        [DllImport(DLL)]
        public static extern int KeyDownChar(long instance, string keyStr);

        [DllImport(DLL)]
        public static extern int KeyUpChar(long instance, string keyStr);

        [DllImport(DLL)]
        public static extern int MoveR(long instance, int rx, int ry);

        [DllImport(DLL)]
        public static extern int MiddleClick(long instance);

        [DllImport(DLL)]
        public static extern long MoveToEx(long instance, int x, int y, int w, int h);

        [DllImport(DLL)]
        public static extern int GetCursorPos(long instance, out int x, out int y);

        [DllImport(DLL)]
        public static extern int MiddleUp(long instance);

        [DllImport(DLL)]
        public static extern int MiddleDown(long instance);

        [DllImport(DLL)]
        public static extern int MiddleDoubleClick(long instance);

        [DllImport(DLL)]
        public static extern int LeftClick(long instance);

        [DllImport(DLL)]
        public static extern int LeftDoubleClick(long instance);

        [DllImport(DLL)]
        public static extern int WheelUp(long instance);

        [DllImport(DLL)]
        public static extern int WheelDown(long instance);

        [DllImport(DLL)]
        public static extern int WaitKey(long instance, int vk_code, int time_out);

        [DllImport(DLL)]
        public static extern int EnableMouseAccuracy(long instance, int enable);

        [DllImport(DLL)]
        public static extern long DoubleToData(long instance, double double_value);

        [DllImport(DLL)]
        public static extern long FloatToData(long instance, float float_value);

        [DllImport(DLL)]
        public static extern long StringToData(long instance, string string_value, int type);

        [DllImport(DLL)]
        public static extern int Int64ToInt32(long instance, long v);

        [DllImport(DLL)]
        public static extern long Int32ToInt64(long instance, int v);

        [DllImport(DLL)]
        public static extern long FindData(long instance, long hwnd, string addr_range, string data);

        [DllImport(DLL)]
        public static extern long FindDataEx(long instance, long hwnd, string addr_range, string data, int step, int multi_thread, int mode);

        [DllImport(DLL)]
        public static extern long FindDouble(long instance, long hwnd, string addr_range, double double_value_min, double double_value_max);

        [DllImport(DLL)]
        public static extern long FindDoubleEx(long instance, long hwnd, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode);

        [DllImport(DLL)]
        public static extern long FindFloat(long instance, long hwnd, string addr_range, float float_value_min, float float_value_max);

        [DllImport(DLL)]
        public static extern long FindFloatEx(long instance, long hwnd, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode);

        [DllImport(DLL)]
        public static extern long FindInt(long instance, long hwnd, string addr_range, long int_value_min, long int_value_max, int type);

        [DllImport(DLL)]
        public static extern long FindIntEx(long instance, long hwnd, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode);

        [DllImport(DLL)]
        public static extern long FindString(long instance, long hwnd, string addr_range, string string_value, int type);

        [DllImport(DLL)]
        public static extern long FindStringEx(long instance, long hwnd, string addr_range, string string_value, int type, int step, int multi_thread, int mode);

        [DllImport(DLL)]
        public static extern long ReadData(long instance, long hwnd, string addr, int len);

        [DllImport(DLL)]
        public static extern long ReadDataAddr(long instance, long hwnd, long addr, int len);

        [DllImport(DLL)]
        public static extern long ReadDataAddrToBin(long instance, long hwnd, long addr, int len);

        [DllImport(DLL)]
        public static extern long ReadDataToBin(long instance, long hwnd, string addr, int len);

        [DllImport(DLL)]
        public static extern double ReadDouble(long instance, long hwnd, string addr);

        [DllImport(DLL)]
        public static extern double ReadDoubleAddr(long instance, long hwnd, long addr);

        [DllImport(DLL)]
        public static extern float ReadFloat(long instance, long hwnd, string addr);

        [DllImport(DLL)]
        public static extern float ReadFloatAddr(long instance, long hwnd, long addr);

        [DllImport(DLL)]
        public static extern long ReadInt(long instance, long hwnd, string addr, int type);

        [DllImport(DLL)]
        public static extern long ReadIntAddr(long instance, long hwnd, long addr, int type);

        [DllImport(DLL)]
        public static extern long ReadString(long instance, long hwnd, string addr, int type, int len);

        [DllImport(DLL)]
        public static extern long ReadStringAddr(long instance, long hwnd, long addr, int type, int len);

        [DllImport(DLL)]
        public static extern int WriteData(long instance, long hwnd, string addr, string data);

        [DllImport(DLL)]
        public static extern int WriteDataFromBin(long instance, long hwnd, string addr, long data, int len);

        [DllImport(DLL)]
        public static extern int WriteDataAddr(long instance, long hwnd, long addr, string data);

        [DllImport(DLL)]
        public static extern int WriteDataAddrFromBin(long instance, long hwnd, long addr, long data, int len);

        [DllImport(DLL)]
        public static extern int WriteDouble(long instance, long hwnd, string addr, double double_value);

        [DllImport(DLL)]
        public static extern int WriteDoubleAddr(long instance, long hwnd, long addr, double double_value);

        [DllImport(DLL)]
        public static extern int WriteFloat(long instance, long hwnd, string addr, float float_value);

        [DllImport(DLL)]
        public static extern int WriteFloatAddr(long instance, long hwnd, long addr, float float_value);

        [DllImport(DLL)]
        public static extern int WriteInt(long instance, long hwnd, string addr, int type, long value);

        [DllImport(DLL)]
        public static extern int WriteIntAddr(long instance, long hwnd, long addr, int type, long value);

        [DllImport(DLL)]
        public static extern int WriteString(long instance, long hwnd, string addr, int type, string value);

        [DllImport(DLL)]
        public static extern int WriteStringAddr(long instance, long hwnd, long addr, int type, string value);

        [DllImport(DLL)]
        public static extern int SetMemoryHwndAsProcessId(long instance, int enable);

        [DllImport(DLL)]
        public static extern int FreeProcessMemory(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern long GetModuleBaseAddr(long instance, long hwnd, string module_name);

        [DllImport(DLL)]
        public static extern int GetModuleSize(long instance, long hwnd, string module_name);

        [DllImport(DLL)]
        public static extern long GetRemoteApiAddress(long instance, long hwnd, long base_addr, string fun_name);

        [DllImport(DLL)]
        public static extern long VirtualAllocEx(long instance, long hwnd, long addr, int size, int type);

        [DllImport(DLL)]
        public static extern int VirtualFreeEx(long instance, long hwnd, long addr);

        [DllImport(DLL)]
        public static extern int VirtualProtectEx(long instance, long hwnd, long addr, int size, int type, int protect);

        [DllImport(DLL)]
        public static extern long VirtualQueryEx(long instance, long hwnd, long addr, long pmbi);

        [DllImport(DLL)]
        public static extern long CreateRemoteThread(long instance, long hwnd, long lpStartAddress, long lpParameter, int dwCreationFlags, out long lpThreadId);

        [DllImport(DLL)]
        public static extern int CloseHandle(long instance, long handle);

        [DllImport(DLL)]
        public static extern long Ocr(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long OcrFromPtr(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long OcrFromBmpData(long instance, long ptr, int size);

        [DllImport(DLL)]
        public static extern long OcrDetails(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long OcrFromPtrDetails(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long OcrFromBmpDataDetails(long instance, long ptr, int size);

        [DllImport(DLL)]
        public static extern long OcrV5(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long OcrV5Details(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long OcrV5FromPtr(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long OcrV5FromPtrDetails(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long GetOcrConfig(long instance, string configKey);

        [DllImport(DLL)]
        public static extern int SetOcrConfig(long instance, string configStr);

        [DllImport(DLL)]
        public static extern int SetOcrConfigByKey(long instance, string key, string value);

        [DllImport(DLL)]
        public static extern long OcrFromDict(long instance, int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal);

        [DllImport(DLL)]
        public static extern long OcrFromDictDetails(long instance, int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal);

        [DllImport(DLL)]
        public static extern long OcrFromDictPtr(long instance, long ptr, string colorJson, string dict_name, double matchVal);

        [DllImport(DLL)]
        public static extern long OcrFromDictPtrDetails(long instance, long ptr, string colorJson, string dict_name, double matchVal);

        [DllImport(DLL)]
        public static extern int FindStr(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal, out int outX, out int outY);

        [DllImport(DLL)]
        public static extern long FindStrDetail(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal);

        [DllImport(DLL)]
        public static extern long FindStrAll(long instance, int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal);

        [DllImport(DLL)]
        public static extern long FindStrFromPtr(long instance, long source, string str, string colorJson, string dict, double matchVal);

        [DllImport(DLL)]
        public static extern long FindStrFromPtrAll(long instance, long source, string str, string colorJson, string dict, double matchVal);

        [DllImport(DLL)]
        public static extern int FastNumberOcrFromPtr(long instance, long source, string numbers, string colorJson, double matchVal);

        [DllImport(DLL)]
        public static extern int FastNumberOcr(long instance, int x1, int y1, int x2, int y2, string numbers, string colorJson, double matchVal);

        [DllImport(DLL)]
        public static extern int Capture(long instance, int x1, int y1, int x2, int y2, string file);

        [DllImport(DLL)]
        public static extern int GetScreenDataBmp(long instance, int x1, int y1, int x2, int y2, out long data, out int dataLen);

        [DllImport(DLL)]
        public static extern int GetScreenData(long instance, int x1, int y1, int x2, int y2, out long data, out int dataLen, out int stride);

        [DllImport(DLL)]
        public static extern long GetScreenDataPtr(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern int CaptureGif(long instance, int x1, int y1, int x2, int y2, string file, int delay, int time);

        [DllImport(DLL)]
        public static extern int GetImageData(long instance, long imgPtr, out long data, out int size, out int stride);

        [DllImport(DLL)]
        public static extern long MatchImageFromPath(long instance, string source, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchImageFromPathAll(long instance, string source, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchImagePtrFromPath(long instance, long source, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchImagePtrFromPathAll(long instance, long source, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long GetColor(long instance, int x, int y);

        [DllImport(DLL)]
        public static extern long GetColorPtr(long instance, long source, int x, int y);

        [DllImport(DLL)]
        public static extern long CopyImage(long instance, long sourcePtr);

        [DllImport(DLL)]
        public static extern int FreeImagePath(long instance, string path);

        [DllImport(DLL)]
        public static extern int FreeImageAll(long instance);

        [DllImport(DLL)]
        public static extern long LoadImage(long instance, string path);

        [DllImport(DLL)]
        public static extern long LoadImageFromBmpData(long instance, long data, int dataSize);

        [DllImport(DLL)]
        public static extern long LoadImageFromRGBData(long instance, int width, int height, long scan0, int stride);

        [DllImport(DLL)]
        public static extern int FreeImagePtr(long instance, long screenPtr);

        [DllImport(DLL)]
        public static extern long MatchWindowsFromPtr(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchImageFromPtr(long instance, long source, long templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchImageFromPtrAll(long instance, long source, long templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsFromPtrAll(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsFromPath(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsFromPathAll(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsThresholdFromPtr(long instance, int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsThresholdFromPtrAll(long instance, int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsThresholdFromPath(long instance, int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale);

        [DllImport(DLL)]
        public static extern long MatchWindowsThresholdFromPathAll(long instance, int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale);

        [DllImport(DLL)]
        public static extern int ShowMatchWindow(long instance, int flag);

        [DllImport(DLL)]
        public static extern double CalculateSSIM(long instance, long image1, long image2);

        [DllImport(DLL)]
        public static extern double CalculateHistograms(long instance, long image1, long image2);

        [DllImport(DLL)]
        public static extern double CalculateMSE(long instance, long image1, long image2);

        [DllImport(DLL)]
        public static extern int SaveImageFromPtr(long instance, long ptr, string path);

        [DllImport(DLL)]
        public static extern long ReSize(long instance, long ptr, int width, int height);

        [DllImport(DLL)]
        public static extern int FindColor(long instance, int x1, int y1, int x2, int y2, string color1, string color2, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindColorList(long instance, int x1, int y1, int x2, int y2, string color1, string color2);

        [DllImport(DLL)]
        public static extern int FindColorEx(long instance, int x1, int y1, int x2, int y2, string colorJson, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindColorListEx(long instance, int x1, int y1, int x2, int y2, string colorJson);

        [DllImport(DLL)]
        public static extern int FindMultiColor(long instance, int x1, int y1, int x2, int y2, string colorJson, string pointJson, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindMultiColorList(long instance, int x1, int y1, int x2, int y2, string colorJson, string pointJson);

        [DllImport(DLL)]
        public static extern int FindMultiColorFromPtr(long instance, long ptr, string colorJson, string pointJson, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindMultiColorListFromPtr(long instance, long ptr, string colorJson, string pointJson);

        [DllImport(DLL)]
        public static extern int GetImageSize(long instance, long ptr, out int width, out int height);

        [DllImport(DLL)]
        public static extern int FindColorBlock(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, out int x, out int y);

        [DllImport(DLL)]
        public static extern int FindColorBlockPtr(long instance, long ptr, string colorList, int count, int width, int height, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindColorBlockList(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type);

        [DllImport(DLL)]
        public static extern long FindColorBlockListPtr(long instance, long ptr, string colorList, int count, int width, int height, int type);

        [DllImport(DLL)]
        public static extern int FindColorBlockEx(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern int FindColorBlockPtrEx(long instance, long ptr, string colorList, int count, int width, int height, int dir, out int x, out int y);

        [DllImport(DLL)]
        public static extern long FindColorBlockListEx(long instance, int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type, int dir);

        [DllImport(DLL)]
        public static extern long FindColorBlockListPtrEx(long instance, long ptr, string colorList, int count, int width, int height, int type, int dir);

        [DllImport(DLL)]
        public static extern int GetColorNum(long instance, int x1, int y1, int x2, int y2, string colorList);

        [DllImport(DLL)]
        public static extern int GetColorNumPtr(long instance, long ptr, string colorList);

        [DllImport(DLL)]
        public static extern long Cropped(long instance, long image, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long GetThresholdImageFromMultiColorPtr(long instance, long ptr, string colorJson);

        [DllImport(DLL)]
        public static extern long GetThresholdImageFromMultiColor(long instance, int x1, int y1, int x2, int y2, string colorJson);

        [DllImport(DLL)]
        public static extern int IsSameImage(long instance, long ptr, long ptr2);

        [DllImport(DLL)]
        public static extern int ShowImage(long instance, long ptr);

        [DllImport(DLL)]
        public static extern int ShowImageFromFile(long instance, string file);

        [DllImport(DLL)]
        public static extern long SetColorsToNewColor(long instance, long ptr, string colorJson, string color);

        [DllImport(DLL)]
        public static extern long RemoveOtherColors(long instance, long ptr, string colorJson);

        [DllImport(DLL)]
        public static extern long DrawRectangle(long instance, long ptr, int x1, int y1, int x2, int y2, int thickness, string color);

        [DllImport(DLL)]
        public static extern long DrawCircle(long instance, long ptr, int x, int y, int radius, int thickness, string color);

        [DllImport(DLL)]
        public static extern long DrawFillPoly(long instance, long ptr, string pointJson, string color);

        [DllImport(DLL)]
        public static extern long DecodeQRCode(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long CreateQRCode(long instance, string str, int pixelsPerModule);

        [DllImport(DLL)]
        public static extern long CreateQRCodeEx(long instance, string str, int pixelsPerModule, int version, int correction_level, int mode, int structure_number);

        [DllImport(DLL)]
        public static extern long MatchAnimationFromPtr(long instance, int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount);

        [DllImport(DLL)]
        public static extern long MatchAnimationFromPath(long instance, int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount);

        [DllImport(DLL)]
        public static extern long RemoveImageDiff(long instance, long image1, long image2);

        [DllImport(DLL)]
        public static extern int GetImageBmpData(long instance, long imgPtr, out long data, out int size);

        [DllImport(DLL)]
        public static extern int GetImagePngData(long instance, long imgPtr, out long data, out int size);

        [DllImport(DLL)]
        public static extern int FreeImageData(long instance, long screenPtr);

        [DllImport(DLL)]
        public static extern long ScalePixels(long instance, long ptr, int pixelsPerModule);

        [DllImport(DLL)]
        public static extern long CreateImage(long instance, int width, int height, string color);

        [DllImport(DLL)]
        public static extern int SetPixel(long instance, long image, int x, int y, string color);

        [DllImport(DLL)]
        public static extern int SetPixelList(long instance, long image, string points, string color);

        [DllImport(DLL)]
        public static extern long ConcatImage(long instance, long image1, long image2, int gap, string color, int dir);

        [DllImport(DLL)]
        public static extern long CoverImage(long instance, long image1, long image2, int x, int y, double alpha);

        [DllImport(DLL)]
        public static extern long RotateImage(long instance, long image, double angle);

        [DllImport(DLL)]
        public static extern long ImageToBase64(long instance, long image);

        [DllImport(DLL)]
        public static extern long Base64ToImage(long instance, string base64);

        [DllImport(DLL)]
        public static extern int Hex2ARGB(long instance, string hex, out int a, out int r, out int g, out int b);

        [DllImport(DLL)]
        public static extern int Hex2RGB(long instance, string hex, out int r, out int g, out int b);

        [DllImport(DLL)]
        public static extern long ARGB2Hex(long instance, int a, int r, int g, int b);

        [DllImport(DLL)]
        public static extern long RGB2Hex(long instance, int r, int g, int b);

        [DllImport(DLL)]
        public static extern long Hex2HSV(long instance, string hex);

        [DllImport(DLL)]
        public static extern long RGB2HSV(long instance, int r, int g, int b);

        [DllImport(DLL)]
        public static extern int CmpColor(long instance, int x1, int y1, string colorStart, string colorEnd);

        [DllImport(DLL)]
        public static extern int CmpColorPtr(long instance, long ptr, int x, int y, string colorStart, string colorEnd);

        [DllImport(DLL)]
        public static extern int CmpColorEx(long instance, int x1, int y1, string colorJson);

        [DllImport(DLL)]
        public static extern int CmpColorPtrEx(long instance, long ptr, int x, int y, string colorJson);

        [DllImport(DLL)]
        public static extern int CmpColorHexEx(long instance, string hex, string colorJson);

        [DllImport(DLL)]
        public static extern int CmpColorHex(long instance, string hex, string colorStart, string colorEnd);

        [DllImport(DLL)]
        public static extern long GetConnectedComponents(long instance, long ptr, string points, int tolerance);

        [DllImport(DLL)]
        public static extern double DetectPointerDirection(long instance, long ptr, int x, int y);

        [DllImport(DLL)]
        public static extern double DetectPointerDirectionByFeatures(long instance, long ptr, long templatePtr, int x, int y, bool useTemplate);

        [DllImport(DLL)]
        public static extern long FastMatch(long instance, long ptr, long templatePtr, double matchVal, int type, double angle, double scale);

        [DllImport(DLL)]
        public static extern long FastROI(long instance, long ptr);

        [DllImport(DLL)]
        public static extern int GetROIRegion(long instance, long ptr, out int x1, out int y1, out int x2, out int y2);

        [DllImport(DLL)]
        public static extern long GetForegroundPoints(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long ConvertColor(long instance, long ptr, int type);

        [DllImport(DLL)]
        public static extern long Threshold(long instance, long ptr, double thresh, double maxVal, int type);

        [DllImport(DLL)]
        public static extern long RemoveIslands(long instance, long ptr, int minArea);

        [DllImport(DLL)]
        public static extern long MorphGradient(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long MorphTophat(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long MorphBlackhat(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long Dilation(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long Erosion(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long GaussianBlur(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long Sharpen(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long CannyEdge(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long Flip(long instance, long ptr, int flipCode);

        [DllImport(DLL)]
        public static extern long MorphOpen(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long MorphClose(long instance, long ptr, int kernelSize);

        [DllImport(DLL)]
        public static extern long Skeletonize(long instance, long ptr);

        [DllImport(DLL)]
        public static extern long ImageStitchFromPath(long instance, string path, out long trajectory);

        [DllImport(DLL)]
        public static extern long ImageStitchCreate(long instance);

        [DllImport(DLL)]
        public static extern int ImageStitchAppend(long instance, long imageStitch, long image);

        [DllImport(DLL)]
        public static extern long ImageStitchGetResult(long instance, long imageStitch, out long trajectory);

        [DllImport(DLL)]
        public static extern int ImageStitchFree(long instance, long imageStitch);

        [DllImport(DLL)]
        public static extern long CreateDatabase(long instance, string dbName, string password);

        [DllImport(DLL)]
        public static extern long OpenDatabase(long instance, string dbName, string password);

        [DllImport(DLL)]
        public static extern long OpenMemoryDatabase(long instance, long address, int size, string password);

        [DllImport(DLL)]
        public static extern long GetDatabaseError(long instance, long db);

        [DllImport(DLL)]
        public static extern int CloseDatabase(long instance, long db);

        [DllImport(DLL)]
        public static extern long GetAllTableNames(long instance, long db);

        [DllImport(DLL)]
        public static extern long GetTableInfo(long instance, long db, string tableName);

        [DllImport(DLL)]
        public static extern long GetTableInfoDetail(long instance, long db, string tableName);

        [DllImport(DLL)]
        public static extern int ExecuteSql(long instance, long db, string sql);

        [DllImport(DLL)]
        public static extern int ExecuteScalar(long instance, long db, string sql);

        [DllImport(DLL)]
        public static extern long ExecuteReader(long instance, long db, string sql);

        [DllImport(DLL)]
        public static extern int Read(long instance, long stmt);

        [DllImport(DLL)]
        public static extern int GetDataCount(long instance, long stmt);

        [DllImport(DLL)]
        public static extern int GetColumnCount(long instance, long stmt);

        [DllImport(DLL)]
        public static extern long GetColumnName(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern int GetColumnIndex(long instance, long stmt, string columnName);

        [DllImport(DLL)]
        public static extern int GetColumnType(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern int Finalize(long instance, long stmt);

        [DllImport(DLL)]
        public static extern double GetDouble(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern int GetInt32(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern long GetInt64(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern long GetString(long instance, long stmt, int iCol);

        [DllImport(DLL)]
        public static extern double GetDoubleByColumnName(long instance, long stmt, string columnName);

        [DllImport(DLL)]
        public static extern int GetInt32ByColumnName(long instance, long stmt, string columnName);

        [DllImport(DLL)]
        public static extern long GetInt64ByColumnName(long instance, long stmt, string columnName);

        [DllImport(DLL)]
        public static extern long GetStringByColumnName(long instance, long stmt, string columnName);

        [DllImport(DLL)]
        public static extern int InitOlaDatabase(long instance, long db);

        [DllImport(DLL)]
        public static extern int InitOlaImageFromDir(long instance, long db, string dir, int cover);

        [DllImport(DLL)]
        public static extern int RemoveOlaImageFromDir(long instance, long db, string dir);

        [DllImport(DLL)]
        public static extern int ExportOlaImageDir(long instance, long db, string dir, string exportDir);

        [DllImport(DLL)]
        public static extern int ImportOlaImage(long instance, long db, string dir, string fileName, int cover);

        [DllImport(DLL)]
        public static extern long GetOlaImage(long instance, long db, string dir, string fileName);

        [DllImport(DLL)]
        public static extern int RemoveOlaImage(long instance, long db, string dir, string fileName);

        [DllImport(DLL)]
        public static extern int SetDbConfig(long instance, long db, string key, string value);

        [DllImport(DLL)]
        public static extern long GetDbConfig(long instance, long db, string key);

        [DllImport(DLL)]
        public static extern int RemoveDbConfig(long instance, long db, string key);

        [DllImport(DLL)]
        public static extern int SetDbConfigEx(long instance, string key, string value);

        [DllImport(DLL)]
        public static extern long GetDbConfigEx(long instance, string key);

        [DllImport(DLL)]
        public static extern int RemoveDbConfigEx(long instance, string key);

        [DllImport(DLL)]
        public static extern int InitDictFromDir(long instance, long db, string dict_name, string dict_path, int cover);

        [DllImport(DLL)]
        public static extern int ImportDictWord(long instance, long db, string dict_name, string pic_file_name, int cover);

        [DllImport(DLL)]
        public static extern int ExportDict(long instance, long db, string dict_name, string export_dir);

        [DllImport(DLL)]
        public static extern int RemoveDict(long instance, long db, string dict_name);

        [DllImport(DLL)]
        public static extern int RemoveDictWord(long instance, long db, string dict_name, string word);

        [DllImport(DLL)]
        public static extern long GetDictImage(long instance, long db, string dict_name, string word, int gap, int dir);

        [DllImport(DLL)]
        public static extern long OpenVideo(long instance, string videoPath);

        [DllImport(DLL)]
        public static extern long OpenCamera(long instance, int deviceIndex);

        [DllImport(DLL)]
        public static extern int CloseVideo(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int IsVideoOpened(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern long GetVideoInfo(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int GetVideoWidth(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int GetVideoHeight(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern double GetVideoFPS(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int GetVideoTotalFrames(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern double GetVideoDuration(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int GetCurrentFrameIndex(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern double GetCurrentTimestamp(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern long ReadNextFrame(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern long ReadFrameAtIndex(long instance, long videoHandle, int frameIndex);

        [DllImport(DLL)]
        public static extern long ReadFrameAtTime(long instance, long videoHandle, double timestamp);

        [DllImport(DLL)]
        public static extern long ReadCurrentFrame(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int SeekToFrame(long instance, long videoHandle, int frameIndex);

        [DllImport(DLL)]
        public static extern int SeekToTime(long instance, long videoHandle, double timestamp);

        [DllImport(DLL)]
        public static extern int SeekToBeginning(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int SeekToEnd(long instance, long videoHandle);

        [DllImport(DLL)]
        public static extern int ExtractFramesToFiles(long instance, long videoHandle, int startFrame, int endFrame, int step, string outputDir, string imageFormat, int jpegQuality);

        [DllImport(DLL)]
        public static extern int ExtractFramesByInterval(long instance, long videoHandle, double intervalSeconds, string outputDir, string imageFormat);

        [DllImport(DLL)]
        public static extern int ExtractKeyFrames(long instance, long videoHandle, double threshold, int maxFrames, string outputDir, string imageFormat);

        [DllImport(DLL)]
        public static extern int SaveCurrentFrame(long instance, long videoHandle, string outputPath, int quality);

        [DllImport(DLL)]
        public static extern int SaveFrameAtIndex(long instance, long videoHandle, int frameIndex, string outputPath, int quality);

        [DllImport(DLL)]
        public static extern long FrameToBase64(long instance, long videoHandle, string format);

        [DllImport(DLL)]
        public static extern double CalculateFrameSimilarity(long instance, long frame1, long frame2);

        [DllImport(DLL)]
        public static extern long GetVideoInfoFromPath(long instance, string videoPath);

        [DllImport(DLL)]
        public static extern int IsValidVideoFile(long instance, string videoPath);

        [DllImport(DLL)]
        public static extern long ExtractSingleFrame(long instance, string videoPath, int frameIndex);

        [DllImport(DLL)]
        public static extern long ExtractThumbnail(long instance, string videoPath);

        [DllImport(DLL)]
        public static extern int ConvertVideo(long instance, string inputPath, string outputPath, string codec, double fps);

        [DllImport(DLL)]
        public static extern int ResizeVideo(long instance, string inputPath, string outputPath, int width, int height);

        [DllImport(DLL)]
        public static extern int TrimVideo(long instance, string inputPath, string outputPath, double startTime, double endTime);

        [DllImport(DLL)]
        public static extern int CreateVideoFromImages(long instance, string imageDir, string outputPath, double fps, string codec);

        [DllImport(DLL)]
        public static extern long DetectSceneChanges(long instance, string videoPath, double threshold);

        [DllImport(DLL)]
        public static extern double CalculateAverageBrightness(long instance, string videoPath);

        [DllImport(DLL)]
        public static extern long DetectMotion(long instance, string videoPath, double threshold);

        [DllImport(DLL)]
        public static extern int SetWindowState(long instance, long hwnd, int state);

        [DllImport(DLL)]
        public static extern long FindWindow(long instance, string class_name, string title);

        [DllImport(DLL)]
        public static extern long GetClipboard(long instance);

        [DllImport(DLL)]
        public static extern int SetClipboard(long instance, string text);

        [DllImport(DLL)]
        public static extern int SendPaste(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern long GetWindow(long instance, long hwnd, int flag);

        [DllImport(DLL)]
        public static extern long GetWindowTitle(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern long GetWindowClass(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern int GetWindowRect(long instance, long hwnd, out int x1, out int y1, out int x2, out int y2);

        [DllImport(DLL)]
        public static extern long GetWindowProcessPath(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern int GetWindowState(long instance, long hwnd, int flag);

        [DllImport(DLL)]
        public static extern long GetForegroundWindow(long instance);

        [DllImport(DLL)]
        public static extern int GetWindowProcessId(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern int GetClientSize(long instance, long hwnd, out int width, out int height);

        [DllImport(DLL)]
        public static extern long GetMousePointWindow(long instance);

        [DllImport(DLL)]
        public static extern long GetSpecialWindow(long instance, int flag);

        [DllImport(DLL)]
        public static extern int GetClientRect(long instance, long hwnd, out int x1, out int y1, out int x2, out int y2);

        [DllImport(DLL)]
        public static extern int SetWindowText(long instance, long hwnd, string title);

        [DllImport(DLL)]
        public static extern int SetWindowSize(long instance, long hwnd, int width, int height);

        [DllImport(DLL)]
        public static extern int SetClientSize(long instance, long hwnd, int width, int height);

        [DllImport(DLL)]
        public static extern int SetWindowTransparent(long instance, long hwnd, int alpha);

        [DllImport(DLL)]
        public static extern long FindWindowEx(long instance, long parent, string class_name, string title);

        [DllImport(DLL)]
        public static extern long FindWindowByProcess(long instance, string process_name, string class_name, string title);

        [DllImport(DLL)]
        public static extern int MoveWindow(long instance, long hwnd, int x, int y);

        [DllImport(DLL)]
        public static extern double GetScaleFromWindows(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern double GetWindowDpiAwarenessScale(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern long EnumProcess(long instance, string name);

        [DllImport(DLL)]
        public static extern long EnumWindow(long instance, long parent, string title, string className, int filter);

        [DllImport(DLL)]
        public static extern long EnumWindowByProcess(long instance, string process_name, string title, string class_name, int filter);

        [DllImport(DLL)]
        public static extern long EnumWindowByProcessId(long instance, long pid, string title, string class_name, int filter);

        [DllImport(DLL)]
        public static extern long EnumWindowSuper(long instance, string spec1, int flag1, int type1, string spec2, int flag2, int type2, int sort);

        [DllImport(DLL)]
        public static extern long GetPointWindow(long instance, int x, int y);

        [DllImport(DLL)]
        public static extern long GetProcessInfo(long instance, long pid);

        [DllImport(DLL)]
        public static extern int ShowTaskBarIcon(long instance, long hwnd, int show);

        [DllImport(DLL)]
        public static extern long FindWindowByProcessId(long instance, long process_id, string className, string title);

        [DllImport(DLL)]
        public static extern long GetWindowThreadId(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern long FindWindowSuper(long instance, string spec1, int flag1, int type1, string spec2, int flag2, int type2);

        [DllImport(DLL)]
        public static extern int ClientToScreen(long instance, long hwnd, out int x, out int y);

        [DllImport(DLL)]
        public static extern int ScreenToClient(long instance, long hwnd, out int x, out int y);

        [DllImport(DLL)]
        public static extern long GetForegroundFocus(long instance);

        [DllImport(DLL)]
        public static extern int SetWindowDisplay(long instance, long hwnd, int affinity);

        [DllImport(DLL)]
        public static extern int IsDisplayDead(long instance, int x1, int y1, int x2, int y2, int time);

        [DllImport(DLL)]
        public static extern int GetWindowsFps(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern int TerminateProcess(long instance, long pid);

        [DllImport(DLL)]
        public static extern int TerminateProcessTree(long instance, long pid);

        [DllImport(DLL)]
        public static extern long GetCommandLine(long instance, long hwnd);

        [DllImport(DLL)]
        public static extern int CheckFontSmooth(long instance);

        [DllImport(DLL)]
        public static extern int SetFontSmooth(long instance, int enable);

        [DllImport(DLL)]
        public static extern int EnableDebugPrivilege(long instance);

        [DllImport(DLL)]
        public static extern int SystemStart(long instance, string applicationName, string commandLine);

        [DllImport(DLL)]
        public static extern int CreateChildProcess(long instance, string applicationName, string commandLine, string currentDirectory, int showType, int parentProcessId);

        [DllImport(DLL)]
        public static extern long YoloV5(long instance, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long YoloLoadModel(long instance, string modelPath, string password, int modelType, int inferenceType, int inferenceDevice);

        [DllImport(DLL)]
        public static extern long YoloLoadModelMemory(long instance, long memoryAddr, int size, int modelType, int inferenceType, int inferenceDevice);

        [DllImport(DLL)]
        public static extern int YoloReleaseModel(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern int YoloIsModelValid(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern long YoloListModels(long instance);

        [DllImport(DLL)]
        public static extern long YoloGetModelInfo(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern int YoloSetModelConfig(long instance, long modelHandle, string configJson);

        [DllImport(DLL)]
        public static extern long YoloGetModelConfig(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern int YoloWarmup(long instance, long modelHandle, int iterations);

        [DllImport(DLL)]
        public static extern long YoloDetect(long instance, long modelHandle, int x1, int y1, int x2, int y2, string classes, double confidence, double iou, int maxDetections);

        [DllImport(DLL)]
        public static extern long YoloDetectSimple(long instance, long modelHandle, int x1, int y1, int x2, int y2);

        [DllImport(DLL)]
        public static extern long YoloDetectFromPtr(long instance, long modelHandle, long imagePtr, string classes, double confidence, double iou, int maxDetections);

        [DllImport(DLL)]
        public static extern long YoloDetectFromFile(long instance, long modelHandle, string imagePath, string classes, double confidence, double iou, int maxDetections);

        [DllImport(DLL)]
        public static extern long YoloDetectFromBase64(long instance, long modelHandle, string base64Data, string classes, double confidence, double iou, int maxDetections);

        [DllImport(DLL)]
        public static extern long YoloDetectBatch(long instance, long modelHandle, string imagesJson, string classes, double confidence, double iou, int maxDetections);

        [DllImport(DLL)]
        public static extern long YoloClassify(long instance, long modelHandle, int x1, int y1, int x2, int y2, int topK);

        [DllImport(DLL)]
        public static extern long YoloClassifyFromPtr(long instance, long modelHandle, long imagePtr, int topK);

        [DllImport(DLL)]
        public static extern long YoloClassifyFromFile(long instance, long modelHandle, string imagePath, int topK);

        [DllImport(DLL)]
        public static extern long YoloSegment(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloSegmentFromPtr(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloPose(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloPoseFromPtr(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloObb(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloObbFromPtr(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloKeyPoint(long instance, long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloKeyPointFromPtr(long instance, long modelHandle, long imagePtr, double confidence, double iou);

        [DllImport(DLL)]
        public static extern long YoloGetInferenceStats(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern int YoloResetStats(long instance, long modelHandle);

        [DllImport(DLL)]
        public static extern long YoloGetLastError(long instance);

        [DllImport(DLL)]
        public static extern int YoloClearError(long instance);

    }
}
