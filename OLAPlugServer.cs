using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text;
using static OLAPlug.OLAPlugDLLHelper;

namespace OLAPlug
{
    public class ColorModel
    {
        /// <summary>
        /// 颜色起始范围 颜色格式 RRGGBB 或者#RRGGBB
        /// </summary>
        public string StartColor { get; set; }

        /// <summary>
        /// 颜色结束范围 颜色格式 RRGGBB 或者#RRGGBB
        /// </summary>
        public string EndColor { get; set; }

        /// <summary>
        ///  0普通模式取合集,1反色模式取合集,2普通模式取交集,3反色模式取交集
        /// </summary>
        public int Type { get; set; }
    }

    public class PointColorModel
    {
        public Point Point { get; set; }
        public List<ColorModel> Colors { get; set; }
    }

    public class OcrResult
    {
        /// <summary>
        /// 识别结果
        /// </summary>
        public List<OcrModel> Regions { get; set; }

        /// <summary>
        /// 识别文字
        /// </summary>
        public string Text { get; set; }
    }

    public class OcrModel
    {
        /// <summary>
        /// 识别评分
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 识别文字
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 中心点
        /// </summary>
        public Point Center { get; set; }

        /// <summary>
        /// 矩形4个顶点
        /// </summary>
        public List<Point> Vertices { get; set; }

        /// <summary>
        /// 矩形角度. When the angle is 0, 90, 180, 270 etc., the rectangle becomes
        /// </summary>
        public double Angle { get; set; }
    }

    public class MatchResult
    {
        public bool MatchState { get; set; } = false;
        public double MatchVal { get; set; } = 0;
        public double Angle { get; set; } = 0;
        /// <summary>
        /// 多图识别时返回图片索引从0开始
        /// </summary>
        public int Index { get; set; } = 0;
        public Point MatchPoint { get; set; }
    }

    public class Point
    {
        public Point() { }
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Size
    {
        public Size() { }
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class OLAPlugServer
    {
        public long OLAObject;

        public string UserCode = "c38e200f116d4fa8bd0deb45ccb523ea";
        public string SoftCode = "701bc92ba84642c68845e7a06c10fd99";
        public string FeatureList = "OLA|OLAPlus";

        public OLAPlugServer()
        {
            OLAObject = CreateCOLAPlugInterFace();
        }

        public string PtrToStringUTF8(long ptr)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ptr);
            OLAPlugDLLHelper.FreeStringPtr(ptr);
            return str;
        }

        public string PtrToStringAuto(long ptr)
        {
            var str = Marshal.PtrToStringAuto((IntPtr)ptr);
            OLAPlugDLLHelper.FreeStringPtr(ptr);
            return str;
        }

        public string PtrToStringAnsi(long ptr)
        {
            var str = Marshal.PtrToStringAnsi((IntPtr)ptr);
            OLAPlugDLLHelper.FreeStringPtr(ptr);
            return str;
        }

        public string GetStringFromPtr(long ptr)
        {
            int size = OLAPlugDLLHelper.GetStringSize(ptr);
            StringBuilder lpString = new StringBuilder(size + 1);//+1 用于存储终止符 '\0'
            int outSize = OLAPlugDLLHelper.GetStringFromPtr(ptr, lpString, size + 1);
            return lpString.ToString();
        }

        /// <summary>
        /// 创建ola对象
        /// </summary>
        public long CreateCOLAPlugInterFace()
        {
            OLAObject = OLAPlugDLLHelper.CreateCOLAPlugInterFace();
            return OLAObject;
        }

        /// <summary>
        /// 释放OLA对象
        /// </summary>
        public void ReleaseObj()
        {
            OLAPlugDLLHelper.DestroyCOLAPlugInterFace(OLAObject);
        }

        public List<Dictionary<string, object>> Query(long db, string sql)
        {
            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            long stmt = ExecuteReader(db, sql);
            // 获取列名
            List<string> columnNames = new List<string>();
            for (int i = 0; i < GetColumnCount(stmt); i++)
            {
                columnNames.Add(GetColumnName(stmt, i));
            }
            //读取数据
            while (Read(stmt)==1)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                foreach (string columnName in columnNames)
                {
                    int iCol = GetColumnIndex(stmt, columnName);
                    switch (GetColumnType(stmt, iCol))
                    {
                        //SQLITE_INTEGER
                        case 1:
                            row[columnName] = GetInt64(stmt, iCol);
                            break;
                        //SQLITE_FLOAT
                        case 2:
                            row[columnName] = GetDouble(stmt, iCol);
                            break;
                        //SQLITE_TEXT
                        case 3:
                            row[columnName] = GetString(stmt, iCol);
                            break;
                        //SQLITE_BLOB
                        case 4:
                            row[columnName] = GetString(stmt, iCol);
                            break;
                        //SQLITE_NULL
                        case 5:
                            row[columnName] = null;
                            break;
                    }
                }
                data.Add(row);
            }
            ;
            //释放资源
            Finalize(stmt);
            return data;
        }

        public string GetConfigByKey(string configKey)
        {
            string configStr = PtrToStringUTF8(OLAPlugDLLHelper.GetConfig(OLAObject, configKey));
            if (string.IsNullOrEmpty(configKey))
            {
                return configStr;
            }
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configStr);
            if (configDict.TryGetValue(configKey, out var configValue))
            {
                return configValue.ToString();
            }
            return string.Empty;
        }

        public int DestroyCOLAPlugInterFace(){
            return OLAPlugDLLHelper.DestroyCOLAPlugInterFace(OLAObject);
        }

        public string Ver(){
            return PtrToStringUTF8(OLAPlugDLLHelper.Ver());
        }

        public int SetPath(string path){
            return OLAPlugDLLHelper.SetPath(OLAObject, path);
        }

        public string GetPath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetPath(OLAObject));
        }

        public string GetMachineCode(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMachineCode(OLAObject));
        }

        public string GetBasePath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetBasePath(OLAObject));
        }

        public int Reg(string userCode, string softCode, string featureList){
            return OLAPlugDLLHelper.Reg(userCode, softCode, featureList);
        }

        public int BindWindow(long hwnd, string display, string mouse, string keypad, int mode){
            return OLAPlugDLLHelper.BindWindow(OLAObject, hwnd, display, mouse, keypad, mode);
        }

        public int BindWindowEx(long hwnd, string display, string mouse, string keypad, string pubstr, int mode){
            return OLAPlugDLLHelper.BindWindowEx(OLAObject, hwnd, display, mouse, keypad, pubstr, mode);
        }

        public int UnBindWindow(){
            return OLAPlugDLLHelper.UnBindWindow(OLAObject);
        }

        public long GetBindWindow(){
            return OLAPlugDLLHelper.GetBindWindow(OLAObject);
        }

        public int ReleaseWindowsDll(long hwnd){
            return OLAPlugDLLHelper.ReleaseWindowsDll(OLAObject, hwnd);
        }

        public int FreeStringPtr(long ptr){
            return OLAPlugDLLHelper.FreeStringPtr(ptr);
        }

        public int FreeMemoryPtr(long ptr){
            return OLAPlugDLLHelper.FreeMemoryPtr(ptr);
        }

        public int GetStringSize(long ptr){
            return OLAPlugDLLHelper.GetStringSize(ptr);
        }

        public int Delay(int millisecond){
            return OLAPlugDLLHelper.Delay(millisecond);
        }

        public int Delays(int minMillisecond, int maxMillisecond){
            return OLAPlugDLLHelper.Delays(minMillisecond, maxMillisecond);
        }

        public int SetUAC(int enable){
            return OLAPlugDLLHelper.SetUAC(OLAObject, enable);
        }

        public int CheckUAC(){
            return OLAPlugDLLHelper.CheckUAC(OLAObject);
        }

        public int RunApp(string appPath, int mode){
            return OLAPlugDLLHelper.RunApp(OLAObject, appPath, mode);
        }

        public string ExecuteCmd(string cmd, string current_dir, int time_out){
            return PtrToStringUTF8(OLAPlugDLLHelper.ExecuteCmd(OLAObject, cmd, current_dir, time_out));
        }

        public string GetConfig(string configKey){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetConfig(OLAObject, configKey));
        }

        public int SetConfig(string configStr){
            return OLAPlugDLLHelper.SetConfig(OLAObject, configStr);
        }

        public int SetConfigByKey(string key, string value){
            return OLAPlugDLLHelper.SetConfigByKey(OLAObject, key, value);
        }

        public int SendDropFiles(long hwnd, string file_path){
            return OLAPlugDLLHelper.SendDropFiles(OLAObject, hwnd, file_path);
        }

        public int SetDefaultEncode(int inputEncoding, int outputEncoding){
            return OLAPlugDLLHelper.SetDefaultEncode(inputEncoding, outputEncoding);
        }

        public int GetRandomNumber(int min, int max){
            return OLAPlugDLLHelper.GetRandomNumber(OLAObject, min, max);
        }

        public double GetRandomDouble(double min, double max){
            return OLAPlugDLLHelper.GetRandomDouble(OLAObject, min, max);
        }

        public string ExcludePos(string json, int type, int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.ExcludePos(OLAObject, json, type, x1, y1, x2, y2));
        }

        public string FindNearestPos(string json, int type, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindNearestPos(OLAObject, json, type, x, y));
        }

        public string SortPosDistance(string json, int type, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.SortPosDistance(OLAObject, json, type, x, y));
        }

        public int GetDenseRect(long image, int width, int height, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetDenseRect(OLAObject, image, width, height, out x1, out y1, out x2, out y2);
        }

        public List<Point> PathPlanning(long image, int startX, int startY, int endX, int endY, double potentialRadius, double searchRadius){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.PathPlanning(OLAObject, image, startX, startY, endX, endY, potentialRadius, searchRadius));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public long CreateGraph(string json){
            return OLAPlugDLLHelper.CreateGraph(OLAObject, json);
        }

        public long GetGraph(long graphPtr){
            return OLAPlugDLLHelper.GetGraph(OLAObject, graphPtr);
        }

        public int AddEdge(long graphPtr, string from, string to, double weight, bool isDirected){
            return OLAPlugDLLHelper.AddEdge(OLAObject, graphPtr, from, to, weight, isDirected);
        }

        public string GetShortestPath(long graphPtr, string from, string to){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetShortestPath(OLAObject, graphPtr, from, to));
        }

        public double GetShortestDistance(long graphPtr, string from, string to){
            return OLAPlugDLLHelper.GetShortestDistance(OLAObject, graphPtr, from, to);
        }

        public int ClearGraph(long graphPtr){
            return OLAPlugDLLHelper.ClearGraph(OLAObject, graphPtr);
        }

        public int DeleteGraph(long graphPtr){
            return OLAPlugDLLHelper.DeleteGraph(OLAObject, graphPtr);
        }

        public int GetNodeCount(long graphPtr){
            return OLAPlugDLLHelper.GetNodeCount(OLAObject, graphPtr);
        }

        public int GetEdgeCount(long graphPtr){
            return OLAPlugDLLHelper.GetEdgeCount(OLAObject, graphPtr);
        }

        public string GetShortestPathToAllNodes(long graphPtr, string startNode){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetShortestPathToAllNodes(OLAObject, graphPtr, startNode));
        }

        public string GetMinimumSpanningTree(long graphPtr){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMinimumSpanningTree(OLAObject, graphPtr));
        }

        public string GetDirectedPathToAllNodes(long graphPtr, string startNode){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDirectedPathToAllNodes(OLAObject, graphPtr, startNode));
        }

        public string GetMinimumArborescence(long graphPtr, string root){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMinimumArborescence(OLAObject, graphPtr, root));
        }

        public long CreateGraphFromCoordinates(string json, bool connectAll, double maxDistance, bool useEuclideanDistance){
            return OLAPlugDLLHelper.CreateGraphFromCoordinates(OLAObject, json, connectAll, maxDistance, useEuclideanDistance);
        }

        public int AddCoordinateNode(long graphPtr, string name, double x, double y, bool connectToExisting, double maxDistance, bool useEuclideanDistance){
            return OLAPlugDLLHelper.AddCoordinateNode(OLAObject, graphPtr, name, x, y, connectToExisting, maxDistance, useEuclideanDistance);
        }

        public string GetNodeCoordinates(long graphPtr, string name){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetNodeCoordinates(OLAObject, graphPtr, name));
        }

        public int SetNodeConnection(long graphPtr, string from, string to, bool canConnect, double weight){
            return OLAPlugDLLHelper.SetNodeConnection(OLAObject, graphPtr, from, to, canConnect, weight);
        }

        public int GetNodeConnectionStatus(long graphPtr, string from, string to){
            return OLAPlugDLLHelper.GetNodeConnectionStatus(OLAObject, graphPtr, from, to);
        }

        public long AsmCall(long hwnd, string asmStr, int type, long baseAddr){
            return OLAPlugDLLHelper.AsmCall(OLAObject, hwnd, asmStr, type, baseAddr);
        }

        public string Assemble(string asmStr, long baseAddr, int arch, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.Assemble(OLAObject, asmStr, baseAddr, arch, mode));
        }

        public string Disassemble(string asmCode, long baseAddr, int arch, int mode, int showType){
            return PtrToStringUTF8(OLAPlugDLLHelper.Disassemble(OLAObject, asmCode, baseAddr, arch, mode, showType));
        }

        public int DrawGuiCleanup(){
            return OLAPlugDLLHelper.DrawGuiCleanup(OLAObject);
        }

        public int DrawGuiSetGuiActive(int active){
            return OLAPlugDLLHelper.DrawGuiSetGuiActive(OLAObject, active);
        }

        public int DrawGuiIsGuiActive(){
            return OLAPlugDLLHelper.DrawGuiIsGuiActive(OLAObject);
        }

        public int DrawGuiSetGuiClickThrough(int enabled){
            return OLAPlugDLLHelper.DrawGuiSetGuiClickThrough(OLAObject, enabled);
        }

        public int DrawGuiIsGuiClickThrough(){
            return OLAPlugDLLHelper.DrawGuiIsGuiClickThrough(OLAObject);
        }

        public long DrawGuiRectangle(int x, int y, int width, int height, int mode, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiRectangle(OLAObject, x, y, width, height, mode, lineThickness);
        }

        public long DrawGuiCircle(int x, int y, int radius, int mode, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiCircle(OLAObject, x, y, radius, mode, lineThickness);
        }

        public long DrawGuiLine(int x1, int y1, int x2, int y2, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiLine(OLAObject, x1, y1, x2, y2, lineThickness);
        }

        public long DrawGuiText(string text, int x, int y, string fontPath, int fontSize, int align){
            return OLAPlugDLLHelper.DrawGuiText(OLAObject, text, x, y, fontPath, fontSize, align);
        }

        public long DrawGuiImage(string imagePath, int x, int y){
            return OLAPlugDLLHelper.DrawGuiImage(OLAObject, imagePath, x, y);
        }

        public long DrawGuiImagePtr(long imagePtr, int x, int y){
            return OLAPlugDLLHelper.DrawGuiImagePtr(OLAObject, imagePtr, x, y);
        }

        public long DrawGuiWindow(string title, int x, int y, int width, int height, int style){
            return OLAPlugDLLHelper.DrawGuiWindow(OLAObject, title, x, y, width, height, style);
        }

        public long DrawGuiPanel(long parentHandle, int x, int y, int width, int height){
            return OLAPlugDLLHelper.DrawGuiPanel(OLAObject, parentHandle, x, y, width, height);
        }

        public long DrawGuiButton(long parentHandle, string text, int x, int y, int width, int height){
            return OLAPlugDLLHelper.DrawGuiButton(OLAObject, parentHandle, text, x, y, width, height);
        }

        public int DrawGuiSetPosition(long handle, int x, int y){
            return OLAPlugDLLHelper.DrawGuiSetPosition(OLAObject, handle, x, y);
        }

        public int DrawGuiSetSize(long handle, int width, int height){
            return OLAPlugDLLHelper.DrawGuiSetSize(OLAObject, handle, width, height);
        }

        public int DrawGuiSetColor(long handle, int r, int g, int b, int a){
            return OLAPlugDLLHelper.DrawGuiSetColor(OLAObject, handle, r, g, b, a);
        }

        public int DrawGuiSetAlpha(long handle, int alpha){
            return OLAPlugDLLHelper.DrawGuiSetAlpha(OLAObject, handle, alpha);
        }

        public int DrawGuiSetDrawMode(long handle, int mode){
            return OLAPlugDLLHelper.DrawGuiSetDrawMode(OLAObject, handle, mode);
        }

        public int DrawGuiSetLineThickness(long handle, double thickness){
            return OLAPlugDLLHelper.DrawGuiSetLineThickness(OLAObject, handle, thickness);
        }

        public int DrawGuiSetFont(long handle, string fontPath, int fontSize){
            return OLAPlugDLLHelper.DrawGuiSetFont(OLAObject, handle, fontPath, fontSize);
        }

        public int DrawGuiSetTextAlign(long handle, int align){
            return OLAPlugDLLHelper.DrawGuiSetTextAlign(OLAObject, handle, align);
        }

        public int DrawGuiSetText(long handle, string text){
            return OLAPlugDLLHelper.DrawGuiSetText(OLAObject, handle, text);
        }

        public int DrawGuiSetWindowTitle(long handle, string title){
            return OLAPlugDLLHelper.DrawGuiSetWindowTitle(OLAObject, handle, title);
        }

        public int DrawGuiSetWindowStyle(long handle, int style){
            return OLAPlugDLLHelper.DrawGuiSetWindowStyle(OLAObject, handle, style);
        }

        public int DrawGuiSetWindowTopMost(long handle, int topMost){
            return OLAPlugDLLHelper.DrawGuiSetWindowTopMost(OLAObject, handle, topMost);
        }

        public int DrawGuiSetWindowTransparency(long handle, int alpha){
            return OLAPlugDLLHelper.DrawGuiSetWindowTransparency(OLAObject, handle, alpha);
        }

        public int DrawGuiDeleteObject(long handle){
            return OLAPlugDLLHelper.DrawGuiDeleteObject(OLAObject, handle);
        }

        public int DrawGuiClearAll(){
            return OLAPlugDLLHelper.DrawGuiClearAll(OLAObject);
        }

        public int DrawGuiSetVisible(long handle, int visible){
            return OLAPlugDLLHelper.DrawGuiSetVisible(OLAObject, handle, visible);
        }

        public int DrawGuiSetZOrder(long handle, int zOrder){
            return OLAPlugDLLHelper.DrawGuiSetZOrder(OLAObject, handle, zOrder);
        }

        public int DrawGuiSetParent(long handle, long parentHandle){
            return OLAPlugDLLHelper.DrawGuiSetParent(OLAObject, handle, parentHandle);
        }

        public int DrawGuiSetButtonCallback(long handle, DrawGuiButtonCallback callback){
            return OLAPlugDLLHelper.DrawGuiSetButtonCallback(OLAObject, handle, callback);
        }

        public int DrawGuiSetMouseCallback(long handle, DrawGuiMouseCallback callback){
            return OLAPlugDLLHelper.DrawGuiSetMouseCallback(OLAObject, handle, callback);
        }

        public int DrawGuiGetDrawObjectType(long handle){
            return OLAPlugDLLHelper.DrawGuiGetDrawObjectType(OLAObject, handle);
        }

        public int DrawGuiGetPosition(long handle, out int x, out int y){
            return OLAPlugDLLHelper.DrawGuiGetPosition(OLAObject, handle, out x, out y);
        }

        public int DrawGuiGetSize(long handle, out int width, out int height){
            return OLAPlugDLLHelper.DrawGuiGetSize(OLAObject, handle, out width, out height);
        }

        public int DrawGuiIsPointInObject(long handle, int x, int y){
            return OLAPlugDLLHelper.DrawGuiIsPointInObject(OLAObject, handle, x, y);
        }

        public int SetMemoryMode(int mode){
            return OLAPlugDLLHelper.SetMemoryMode(OLAObject, mode);
        }

        public int ExportDriver(string driver_path, int type){
            return OLAPlugDLLHelper.ExportDriver(OLAObject, driver_path, type);
        }

        public int LoadDriver(string driver_name, string driver_path){
            return OLAPlugDLLHelper.LoadDriver(OLAObject, driver_name, driver_path);
        }

        public int UnloadDriver(string driver_name){
            return OLAPlugDLLHelper.UnloadDriver(OLAObject, driver_name);
        }

        public int DriverTest(){
            return OLAPlugDLLHelper.DriverTest(OLAObject);
        }

        public int LoadPdb(){
            return OLAPlugDLLHelper.LoadPdb(OLAObject);
        }

        public int HideProcess(long pid, int enable){
            return OLAPlugDLLHelper.HideProcess(OLAObject, pid, enable);
        }

        public int ProtectProcess(long pid, int enable){
            return OLAPlugDLLHelper.ProtectProcess(OLAObject, pid, enable);
        }

        public int AddProtectPID(long pid, long mode, long allow_pid){
            return OLAPlugDLLHelper.AddProtectPID(OLAObject, pid, mode, allow_pid);
        }

        public int RemoveProtectPID(long pid){
            return OLAPlugDLLHelper.RemoveProtectPID(OLAObject, pid);
        }

        public int AddAllowPID(long pid){
            return OLAPlugDLLHelper.AddAllowPID(OLAObject, pid);
        }

        public int RemoveAllowPID(long pid){
            return OLAPlugDLLHelper.RemoveAllowPID(OLAObject, pid);
        }

        public int InjectDll(long pid, string dll_path, int mode){
            return OLAPlugDLLHelper.InjectDll(OLAObject, pid, dll_path, mode);
        }

        public int FakeProcess(long pid, long fake_pid){
            return OLAPlugDLLHelper.FakeProcess(OLAObject, pid, fake_pid);
        }

        public int ProtectWindow(long hwnd, int flag){
            return OLAPlugDLLHelper.ProtectWindow(OLAObject, hwnd, flag);
        }

        public int KeOpenProcess(long pid, out long process_handle){
            return OLAPlugDLLHelper.KeOpenProcess(OLAObject, pid, out process_handle);
        }

        public int KeOpenThread(long thread_id, out long thread_handle){
            return OLAPlugDLLHelper.KeOpenThread(OLAObject, thread_id, out thread_handle);
        }

        public int StartSecurityGuard(){
            return OLAPlugDLLHelper.StartSecurityGuard(OLAObject);
        }

        public int GenerateRSAKey(string publicKeyPath, string privateKeyPath, int type, int keySize){
            return OLAPlugDLLHelper.GenerateRSAKey(OLAObject, publicKeyPath, privateKeyPath, type, keySize);
        }

        public string ConvertRSAPublicKey(string publicKey, int inputType, int outputType){
            return PtrToStringUTF8(OLAPlugDLLHelper.ConvertRSAPublicKey(OLAObject, publicKey, inputType, outputType));
        }

        public string ConvertRSAPrivateKey(string privateKey, int inputType, int outputType){
            return PtrToStringUTF8(OLAPlugDLLHelper.ConvertRSAPrivateKey(OLAObject, privateKey, inputType, outputType));
        }

        public string EncryptWithRsa(string message, string publicKey, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.EncryptWithRsa(OLAObject, message, publicKey, paddingType));
        }

        public string DecryptWithRsa(string cipher, string privateKey, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.DecryptWithRsa(OLAObject, cipher, privateKey, paddingType));
        }

        public string SignWithRsa(string message, string privateCer, int shaType, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SignWithRsa(OLAObject, message, privateCer, shaType, paddingType));
        }

        public int VerifySignWithRsa(string message, string signature, int shaType, int paddingType, string publicCer){
            return OLAPlugDLLHelper.VerifySignWithRsa(OLAObject, message, signature, shaType, paddingType, publicCer);
        }

        public string AESEncrypt(string source, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESEncrypt(OLAObject, source, key));
        }

        public string AESDecrypt(string source, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESDecrypt(OLAObject, source, key));
        }

        public string AESEncryptEx(string source, string key, string iv, int mode, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESEncryptEx(OLAObject, source, key, iv, mode, paddingType));
        }

        public string AESDecryptEx(string source, string key, string iv, int mode, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESDecryptEx(OLAObject, source, key, iv, mode, paddingType));
        }

        public string MD5Encrypt(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.MD5Encrypt(OLAObject, source));
        }

        public string SHAHash(string source, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SHAHash(OLAObject, source, shaType));
        }

        public string HMAC(string source, string key, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.HMAC(OLAObject, source, key, shaType));
        }

        public string GenerateRandomBytes(int length, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.GenerateRandomBytes(OLAObject, length, type));
        }

        public string GenerateGuid(int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.GenerateGuid(OLAObject, type));
        }

        public string Base64Encode(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.Base64Encode(OLAObject, source));
        }

        public string Base64Decode(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.Base64Decode(OLAObject, source));
        }

        public string PBKDF2(string password, string salt, int iterations, int keyLength, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.PBKDF2(OLAObject, password, salt, iterations, keyLength, shaType));
        }

        public string MD5File(string filePath){
            return PtrToStringUTF8(OLAPlugDLLHelper.MD5File(OLAObject, filePath));
        }

        public string SHAFile(string filePath, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SHAFile(OLAObject, filePath, shaType));
        }

        public int CreateFolder(string path){
            return OLAPlugDLLHelper.CreateFolder(OLAObject, path);
        }

        public int DeleteFolder(string path){
            return OLAPlugDLLHelper.DeleteFolder(OLAObject, path);
        }

        public string GetFolderList(string path, string baseDir){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFolderList(OLAObject, path, baseDir));
        }

        public int IsDirectory(string path){
            return OLAPlugDLLHelper.IsDirectory(OLAObject, path);
        }

        public int IsFile(string path){
            return OLAPlugDLLHelper.IsFile(OLAObject, path);
        }

        public int CreateFile(string path){
            return OLAPlugDLLHelper.CreateFile(OLAObject, path);
        }

        public int DeleteFile(string path){
            return OLAPlugDLLHelper.DeleteFile(OLAObject, path);
        }

        public int CopyFile(string src, string dst){
            return OLAPlugDLLHelper.CopyFile(OLAObject, src, dst);
        }

        public int MoveFile(string src, string dst){
            return OLAPlugDLLHelper.MoveFile(OLAObject, src, dst);
        }

        public int RenameFile(string src, string dst){
            return OLAPlugDLLHelper.RenameFile(OLAObject, src, dst);
        }

        public long GetFileSize(string path){
            return OLAPlugDLLHelper.GetFileSize(OLAObject, path);
        }

        public string GetFileList(string path, string baseDir){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFileList(OLAObject, path, baseDir));
        }

        public string GetFileName(string path, int withExtension){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFileName(OLAObject, path, withExtension));
        }

        public string ToAbsolutePath(string path){
            return PtrToStringUTF8(OLAPlugDLLHelper.ToAbsolutePath(OLAObject, path));
        }

        public string ToRelativePath(string path){
            return PtrToStringUTF8(OLAPlugDLLHelper.ToRelativePath(OLAObject, path));
        }

        public int FileOrDirectoryExists(string path){
            return OLAPlugDLLHelper.FileOrDirectoryExists(OLAObject, path);
        }

        public string ReadFileString(string filePath, int encoding){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadFileString(OLAObject, filePath, encoding));
        }

        public long ReadBytesFromFile(string filePath, int offset, long size){
            return OLAPlugDLLHelper.ReadBytesFromFile(OLAObject, filePath, offset, size);
        }

        public int StartHotkeyHook(){
            return OLAPlugDLLHelper.StartHotkeyHook(OLAObject);
        }

        public int StopHotkeyHook(){
            return OLAPlugDLLHelper.StopHotkeyHook(OLAObject);
        }

        public int RegisterHotkey(int keycode, int modifiers, HotkeyCallback callback){
            return OLAPlugDLLHelper.RegisterHotkey(OLAObject, keycode, modifiers, callback);
        }

        public int UnregisterHotkey(int keycode, int modifiers){
            return OLAPlugDLLHelper.UnregisterHotkey(OLAObject, keycode, modifiers);
        }

        public int RegisterMouseButton(int button, int type, MouseCallback callback){
            return OLAPlugDLLHelper.RegisterMouseButton(OLAObject, button, type, callback);
        }

        public int UnregisterMouseButton(int button, int type){
            return OLAPlugDLLHelper.UnregisterMouseButton(OLAObject, button, type);
        }

        public int RegisterMouseWheel(MouseWheelCallback callback){
            return OLAPlugDLLHelper.RegisterMouseWheel(OLAObject, callback);
        }

        public int UnregisterMouseWheel(){
            return OLAPlugDLLHelper.UnregisterMouseWheel(OLAObject);
        }

        public int RegisterMouseMove(MouseMoveCallback callback){
            return OLAPlugDLLHelper.RegisterMouseMove(OLAObject, callback);
        }

        public int UnregisterMouseMove(){
            return OLAPlugDLLHelper.UnregisterMouseMove(OLAObject);
        }

        public int RegisterMouseDrag(MouseDragCallback callback){
            return OLAPlugDLLHelper.RegisterMouseDrag(OLAObject, callback);
        }

        public int UnregisterMouseDrag(){
            return OLAPlugDLLHelper.UnregisterMouseDrag(OLAObject);
        }

        public long JsonCreateObject(){
            return OLAPlugDLLHelper.JsonCreateObject();
        }

        public long JsonCreateArray(){
            return OLAPlugDLLHelper.JsonCreateArray();
        }

        public long JsonParse(string str, out int err){
            return OLAPlugDLLHelper.JsonParse(str, out err);
        }

        public string JsonStringify(long obj, int indent, out int err){
            return PtrToStringUTF8(OLAPlugDLLHelper.JsonStringify(obj, indent, out err));
        }

        public int JsonFree(long obj){
            return OLAPlugDLLHelper.JsonFree(obj);
        }

        public long JsonGetValue(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetValue(obj, key, out err);
        }

        public long JsonGetArrayItem(long arr, int index, out int err){
            return OLAPlugDLLHelper.JsonGetArrayItem(arr, index, out err);
        }

        public string JsonGetString(long obj, string key, out int err){
            return PtrToStringUTF8(OLAPlugDLLHelper.JsonGetString(obj, key, out err));
        }

        public double JsonGetNumber(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetNumber(obj, key, out err);
        }

        public int JsonGetBool(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetBool(obj, key, out err);
        }

        public int JsonGetSize(long obj, out int err){
            return OLAPlugDLLHelper.JsonGetSize(obj, out err);
        }

        public int JsonSetValue(long obj, string key, long value){
            return OLAPlugDLLHelper.JsonSetValue(obj, key, value);
        }

        public int JsonArrayAppend(long arr, long value){
            return OLAPlugDLLHelper.JsonArrayAppend(arr, value);
        }

        public int JsonSetString(long obj, string key, string value){
            return OLAPlugDLLHelper.JsonSetString(obj, key, value);
        }

        public int JsonSetNumber(long obj, string key, double value){
            return OLAPlugDLLHelper.JsonSetNumber(obj, key, value);
        }

        public int JsonSetBool(long obj, string key, int value){
            return OLAPlugDLLHelper.JsonSetBool(obj, key, value);
        }

        public int JsonDeleteKey(long obj, string key){
            return OLAPlugDLLHelper.JsonDeleteKey(obj, key);
        }

        public int JsonClear(long obj){
            return OLAPlugDLLHelper.JsonClear(obj);
        }

        public int ParseMatchImageJson(string str, out int matchState, out int x, out int y, out double matchVal, out double angle, out int index){
            return OLAPlugDLLHelper.ParseMatchImageJson(str, out matchState, out x, out y, out matchVal, out angle, out index);
        }

        public int GetMatchImageAllCount(string str){
            return OLAPlugDLLHelper.GetMatchImageAllCount(str);
        }

        public int ParseMatchImageAllJson(string str, int parseIndex, out int matchState, out int x, out int y, out double matchVal, out double angle, out int index){
            return OLAPlugDLLHelper.ParseMatchImageAllJson(str, parseIndex, out matchState, out x, out y, out matchVal, out angle, out index);
        }

        public int GetResultCount(string resultStr){
            return OLAPlugDLLHelper.GetResultCount(resultStr);
        }

        public List<Point> GenerateMouseTrajectory(int startX, int startY, int endX, int endY){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.GenerateMouseTrajectory(OLAObject, startX, startY, endX, endY));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int KeyDown(int vk_code){
            return OLAPlugDLLHelper.KeyDown(OLAObject, vk_code);
        }

        public int KeyUp(int vk_code){
            return OLAPlugDLLHelper.KeyUp(OLAObject, vk_code);
        }

        public int KeyPress(int vk_code){
            return OLAPlugDLLHelper.KeyPress(OLAObject, vk_code);
        }

        public int LeftDown(){
            return OLAPlugDLLHelper.LeftDown(OLAObject);
        }

        public int LeftUp(){
            return OLAPlugDLLHelper.LeftUp(OLAObject);
        }

        public int MoveTo(int x, int y){
            return OLAPlugDLLHelper.MoveTo(OLAObject, x, y);
        }

        public int MoveToWithoutSimulator(int x, int y){
            return OLAPlugDLLHelper.MoveToWithoutSimulator(OLAObject, x, y);
        }

        public int RightClick(){
            return OLAPlugDLLHelper.RightClick(OLAObject);
        }

        public int RightDoubleClick(){
            return OLAPlugDLLHelper.RightDoubleClick(OLAObject);
        }

        public int RightDown(){
            return OLAPlugDLLHelper.RightDown(OLAObject);
        }

        public int RightUp(){
            return OLAPlugDLLHelper.RightUp(OLAObject);
        }

        public string GetCursorShape(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetCursorShape(OLAObject));
        }

        public long GetCursorImage(){
            return OLAPlugDLLHelper.GetCursorImage(OLAObject);
        }

        public int KeyPressStr(string keyStr, int delay){
            return OLAPlugDLLHelper.KeyPressStr(OLAObject, keyStr, delay);
        }

        public int SendString(long hwnd, string str){
            return OLAPlugDLLHelper.SendString(OLAObject, hwnd, str);
        }

        public int SendStringEx(long hwnd, long addr, int len, int type){
            return OLAPlugDLLHelper.SendStringEx(OLAObject, hwnd, addr, len, type);
        }

        public int KeyPressChar(string keyStr){
            return OLAPlugDLLHelper.KeyPressChar(OLAObject, keyStr);
        }

        public int KeyDownChar(string keyStr){
            return OLAPlugDLLHelper.KeyDownChar(OLAObject, keyStr);
        }

        public int KeyUpChar(string keyStr){
            return OLAPlugDLLHelper.KeyUpChar(OLAObject, keyStr);
        }

        public int MoveR(int rx, int ry){
            return OLAPlugDLLHelper.MoveR(OLAObject, rx, ry);
        }

        public int MiddleClick(){
            return OLAPlugDLLHelper.MiddleClick(OLAObject);
        }

        public string MoveToEx(int x, int y, int w, int h){
            return PtrToStringUTF8(OLAPlugDLLHelper.MoveToEx(OLAObject, x, y, w, h));
        }

        public int GetCursorPos(out int x, out int y){
            return OLAPlugDLLHelper.GetCursorPos(OLAObject, out x, out y);
        }

        public int MiddleUp(){
            return OLAPlugDLLHelper.MiddleUp(OLAObject);
        }

        public int MiddleDown(){
            return OLAPlugDLLHelper.MiddleDown(OLAObject);
        }

        public int MiddleDoubleClick(){
            return OLAPlugDLLHelper.MiddleDoubleClick(OLAObject);
        }

        public int LeftClick(){
            return OLAPlugDLLHelper.LeftClick(OLAObject);
        }

        public int LeftDoubleClick(){
            return OLAPlugDLLHelper.LeftDoubleClick(OLAObject);
        }

        public int WheelUp(){
            return OLAPlugDLLHelper.WheelUp(OLAObject);
        }

        public int WheelDown(){
            return OLAPlugDLLHelper.WheelDown(OLAObject);
        }

        public int WaitKey(int vk_code, int time_out){
            return OLAPlugDLLHelper.WaitKey(OLAObject, vk_code, time_out);
        }

        public int EnableMouseAccuracy(int enable){
            return OLAPlugDLLHelper.EnableMouseAccuracy(OLAObject, enable);
        }

        public string DoubleToData(double double_value){
            return PtrToStringUTF8(OLAPlugDLLHelper.DoubleToData(OLAObject, double_value));
        }

        public string FloatToData(float float_value){
            return PtrToStringUTF8(OLAPlugDLLHelper.FloatToData(OLAObject, float_value));
        }

        public string StringToData(string string_value, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.StringToData(OLAObject, string_value, type));
        }

        public int Int64ToInt32(long v){
            return OLAPlugDLLHelper.Int64ToInt32(OLAObject, v);
        }

        public long Int32ToInt64(int v){
            return OLAPlugDLLHelper.Int32ToInt64(OLAObject, v);
        }

        public string FindData(long hwnd, string addr_range, string data){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindData(OLAObject, hwnd, addr_range, data));
        }

        public string FindDataEx(long hwnd, string addr_range, string data, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDataEx(OLAObject, hwnd, addr_range, data, step, multi_thread, mode));
        }

        public string FindDouble(long hwnd, string addr_range, double double_value_min, double double_value_max){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDouble(OLAObject, hwnd, addr_range, double_value_min, double_value_max));
        }

        public string FindDoubleEx(long hwnd, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDoubleEx(OLAObject, hwnd, addr_range, double_value_min, double_value_max, step, multi_thread, mode));
        }

        public string FindFloat(long hwnd, string addr_range, float float_value_min, float float_value_max){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindFloat(OLAObject, hwnd, addr_range, float_value_min, float_value_max));
        }

        public string FindFloatEx(long hwnd, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindFloatEx(OLAObject, hwnd, addr_range, float_value_min, float_value_max, step, multi_thread, mode));
        }

        public string FindInt(long hwnd, string addr_range, long int_value_min, long int_value_max, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindInt(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type));
        }

        public string FindIntEx(long hwnd, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindIntEx(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type, step, multi_thread, mode));
        }

        public string FindString(long hwnd, string addr_range, string string_value, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindString(OLAObject, hwnd, addr_range, string_value, type));
        }

        public string FindStringEx(long hwnd, string addr_range, string string_value, int type, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindStringEx(OLAObject, hwnd, addr_range, string_value, type, step, multi_thread, mode));
        }

        public string ReadData(long hwnd, string addr, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadData(OLAObject, hwnd, addr, len));
        }

        public string ReadDataAddr(long hwnd, long addr, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadDataAddr(OLAObject, hwnd, addr, len));
        }

        public long ReadDataAddrToBin(long hwnd, long addr, int len){
            return OLAPlugDLLHelper.ReadDataAddrToBin(OLAObject, hwnd, addr, len);
        }

        public long ReadDataToBin(long hwnd, string addr, int len){
            return OLAPlugDLLHelper.ReadDataToBin(OLAObject, hwnd, addr, len);
        }

        public double ReadDouble(long hwnd, string addr){
            return OLAPlugDLLHelper.ReadDouble(OLAObject, hwnd, addr);
        }

        public double ReadDoubleAddr(long hwnd, long addr){
            return OLAPlugDLLHelper.ReadDoubleAddr(OLAObject, hwnd, addr);
        }

        public float ReadFloat(long hwnd, string addr){
            return OLAPlugDLLHelper.ReadFloat(OLAObject, hwnd, addr);
        }

        public float ReadFloatAddr(long hwnd, long addr){
            return OLAPlugDLLHelper.ReadFloatAddr(OLAObject, hwnd, addr);
        }

        public long ReadInt(long hwnd, string addr, int type){
            return OLAPlugDLLHelper.ReadInt(OLAObject, hwnd, addr, type);
        }

        public long ReadIntAddr(long hwnd, long addr, int type){
            return OLAPlugDLLHelper.ReadIntAddr(OLAObject, hwnd, addr, type);
        }

        public string ReadString(long hwnd, string addr, int type, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadString(OLAObject, hwnd, addr, type, len));
        }

        public string ReadStringAddr(long hwnd, long addr, int type, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadStringAddr(OLAObject, hwnd, addr, type, len));
        }

        public int WriteData(long hwnd, string addr, string data){
            return OLAPlugDLLHelper.WriteData(OLAObject, hwnd, addr, data);
        }

        public int WriteDataFromBin(long hwnd, string addr, long data, int len){
            return OLAPlugDLLHelper.WriteDataFromBin(OLAObject, hwnd, addr, data, len);
        }

        public int WriteDataAddr(long hwnd, long addr, string data){
            return OLAPlugDLLHelper.WriteDataAddr(OLAObject, hwnd, addr, data);
        }

        public int WriteDataAddrFromBin(long hwnd, long addr, long data, int len){
            return OLAPlugDLLHelper.WriteDataAddrFromBin(OLAObject, hwnd, addr, data, len);
        }

        public int WriteDouble(long hwnd, string addr, double double_value){
            return OLAPlugDLLHelper.WriteDouble(OLAObject, hwnd, addr, double_value);
        }

        public int WriteDoubleAddr(long hwnd, long addr, double double_value){
            return OLAPlugDLLHelper.WriteDoubleAddr(OLAObject, hwnd, addr, double_value);
        }

        public int WriteFloat(long hwnd, string addr, float float_value){
            return OLAPlugDLLHelper.WriteFloat(OLAObject, hwnd, addr, float_value);
        }

        public int WriteFloatAddr(long hwnd, long addr, float float_value){
            return OLAPlugDLLHelper.WriteFloatAddr(OLAObject, hwnd, addr, float_value);
        }

        public int WriteInt(long hwnd, string addr, int type, long value){
            return OLAPlugDLLHelper.WriteInt(OLAObject, hwnd, addr, type, value);
        }

        public int WriteIntAddr(long hwnd, long addr, int type, long value){
            return OLAPlugDLLHelper.WriteIntAddr(OLAObject, hwnd, addr, type, value);
        }

        public int WriteString(long hwnd, string addr, int type, string value){
            return OLAPlugDLLHelper.WriteString(OLAObject, hwnd, addr, type, value);
        }

        public int WriteStringAddr(long hwnd, long addr, int type, string value){
            return OLAPlugDLLHelper.WriteStringAddr(OLAObject, hwnd, addr, type, value);
        }

        public int SetMemoryHwndAsProcessId(int enable){
            return OLAPlugDLLHelper.SetMemoryHwndAsProcessId(OLAObject, enable);
        }

        public int FreeProcessMemory(long hwnd){
            return OLAPlugDLLHelper.FreeProcessMemory(OLAObject, hwnd);
        }

        public long GetModuleBaseAddr(long hwnd, string module_name){
            return OLAPlugDLLHelper.GetModuleBaseAddr(OLAObject, hwnd, module_name);
        }

        public int GetModuleSize(long hwnd, string module_name){
            return OLAPlugDLLHelper.GetModuleSize(OLAObject, hwnd, module_name);
        }

        public long GetRemoteApiAddress(long hwnd, long base_addr, string fun_name){
            return OLAPlugDLLHelper.GetRemoteApiAddress(OLAObject, hwnd, base_addr, fun_name);
        }

        public long VirtualAllocEx(long hwnd, long addr, int size, int type){
            return OLAPlugDLLHelper.VirtualAllocEx(OLAObject, hwnd, addr, size, type);
        }

        public int VirtualFreeEx(long hwnd, long addr){
            return OLAPlugDLLHelper.VirtualFreeEx(OLAObject, hwnd, addr);
        }

        public int VirtualProtectEx(long hwnd, long addr, int size, int type, int protect){
            return OLAPlugDLLHelper.VirtualProtectEx(OLAObject, hwnd, addr, size, type, protect);
        }

        public string VirtualQueryEx(long hwnd, long addr, long pmbi){
            return PtrToStringUTF8(OLAPlugDLLHelper.VirtualQueryEx(OLAObject, hwnd, addr, pmbi));
        }

        public long CreateRemoteThread(long hwnd, long lpStartAddress, long lpParameter, int dwCreationFlags, out long lpThreadId){
            return OLAPlugDLLHelper.CreateRemoteThread(OLAObject, hwnd, lpStartAddress, lpParameter, dwCreationFlags, out lpThreadId);
        }

        public int CloseHandle(long handle){
            return OLAPlugDLLHelper.CloseHandle(OLAObject, handle);
        }

        public string Ocr(int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.Ocr(OLAObject, x1, y1, x2, y2));
        }

        public string OcrFromPtr(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromPtr(OLAObject, ptr));
        }

        public string OcrFromBmpData(long ptr, int size){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromBmpData(OLAObject, ptr, size));
        }

        public OcrResult OcrDetails(int x1, int y1, int x2, int y2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrDetails(OLAObject, x1, y1, x2, y2));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public OcrResult OcrFromPtrDetails(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromPtrDetails(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public OcrResult OcrFromBmpDataDetails(long ptr, int size){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromBmpDataDetails(OLAObject, ptr, size));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public string OcrV5(int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrV5(OLAObject, x1, y1, x2, y2));
        }

        public OcrResult OcrV5Details(int x1, int y1, int x2, int y2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrV5Details(OLAObject, x1, y1, x2, y2));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public string OcrV5FromPtr(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrV5FromPtr(OLAObject, ptr));
        }

        public OcrResult OcrV5FromPtrDetails(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrV5FromPtrDetails(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public string GetOcrConfig(string configKey){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetOcrConfig(OLAObject, configKey));
        }

        public int SetOcrConfig(string configStr){
            return OLAPlugDLLHelper.SetOcrConfig(OLAObject, configStr);
        }

        public int SetOcrConfigByKey(string key, string value){
            return OLAPlugDLLHelper.SetOcrConfigByKey(OLAObject, key, value);
        }

        public string OcrFromDict(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDict(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
        }
        
        public string OcrFromDict(int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDict(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
        }

        public OcrResult OcrFromDictDetails(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictDetails(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }
        
        public OcrResult OcrFromDictDetails(int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictDetails(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public string OcrFromDictPtr(long ptr, List<ColorModel> colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
        }
        
        public string OcrFromDictPtr(long ptr, string colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtr(OLAObject, ptr, colorJson, dict_name, matchVal));
        }

        public OcrResult OcrFromDictPtrDetails(long ptr, List<ColorModel> colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtrDetails(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }
        
        public OcrResult OcrFromDictPtrDetails(long ptr, string colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtrDetails(OLAObject, ptr, colorJson, dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        public int FindStr(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal, out int outX, out int outY){
            return OLAPlugDLLHelper.FindStr(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal, out outX, out outY);
        }
        
        public int FindStr(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal, out int outX, out int outY){
            return OLAPlugDLLHelper.FindStr(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal, out outX, out outY);
        }

        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrDetail(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }
        
        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrDetail(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrAll(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }
        
        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrAll(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public MatchResult FindStrFromPtr(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtr(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }
        
        public MatchResult FindStrFromPtr(long source, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtr(OLAObject, source, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> FindStrFromPtrAll(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtrAll(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }
        
        public List<MatchResult> FindStrFromPtrAll(long source, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtrAll(OLAObject, source, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public int FastNumberOcrFromPtr(long source, string numbers, List<ColorModel> colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcrFromPtr(OLAObject, source, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
        }
        
        public int FastNumberOcrFromPtr(long source, string numbers, string colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcrFromPtr(OLAObject, source, numbers, colorJson, matchVal);
        }

        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, List<ColorModel> colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcr(OLAObject, x1, y1, x2, y2, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
        }
        
        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, string colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcr(OLAObject, x1, y1, x2, y2, numbers, colorJson, matchVal);
        }

        public int Capture(int x1, int y1, int x2, int y2, string file){
            return OLAPlugDLLHelper.Capture(OLAObject, x1, y1, x2, y2, file);
        }

        public int GetScreenDataBmp(int x1, int y1, int x2, int y2, out long data, out int dataLen){
            return OLAPlugDLLHelper.GetScreenDataBmp(OLAObject, x1, y1, x2, y2, out data, out dataLen);
        }

        public int GetScreenData(int x1, int y1, int x2, int y2, out long data, out int dataLen, out int stride){
            return OLAPlugDLLHelper.GetScreenData(OLAObject, x1, y1, x2, y2, out data, out dataLen, out stride);
        }

        public long GetScreenDataPtr(int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.GetScreenDataPtr(OLAObject, x1, y1, x2, y2);
        }

        public int CaptureGif(int x1, int y1, int x2, int y2, string file, int delay, int time){
            return OLAPlugDLLHelper.CaptureGif(OLAObject, x1, y1, x2, y2, file, delay, time);
        }

        public int GetImageData(long imgPtr, out long data, out int size, out int stride){
            return OLAPlugDLLHelper.GetImageData(OLAObject, imgPtr, out data, out size, out stride);
        }

        public MatchResult MatchImageFromPath(string source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPath(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchImageFromPathAll(string source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPathAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public MatchResult MatchImagePtrFromPath(long source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImagePtrFromPath(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchImagePtrFromPathAll(long source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImagePtrFromPathAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public string GetColor(int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColor(OLAObject, x, y));
        }

        public string GetColorPtr(long source, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColorPtr(OLAObject, source, x, y));
        }

        public long CopyImage(long sourcePtr){
            return OLAPlugDLLHelper.CopyImage(OLAObject, sourcePtr);
        }

        public int FreeImagePath(string path){
            return OLAPlugDLLHelper.FreeImagePath(OLAObject, path);
        }

        public int FreeImageAll(){
            return OLAPlugDLLHelper.FreeImageAll(OLAObject);
        }

        public long LoadImage(string path){
            return OLAPlugDLLHelper.LoadImage(OLAObject, path);
        }

        public long LoadImageFromBmpData(long data, int dataSize){
            return OLAPlugDLLHelper.LoadImageFromBmpData(OLAObject, data, dataSize);
        }

        public long LoadImageFromRGBData(int width, int height, long scan0, int stride){
            return OLAPlugDLLHelper.LoadImageFromRGBData(OLAObject, width, height, scan0, stride);
        }

        public int FreeImagePtr(long screenPtr){
            return OLAPlugDLLHelper.FreeImagePtr(OLAObject, screenPtr);
        }

        public MatchResult MatchWindowsFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPtr(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public MatchResult MatchImageFromPtr(long source, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPtr(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchImageFromPtrAll(long source, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPtrAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public List<MatchResult> MatchWindowsFromPtrAll(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPtrAll(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public MatchResult MatchWindowsFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPath(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchWindowsFromPathAll(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPathAll(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtr(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }
        
        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtr(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtrAll(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }
        
        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtrAll(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPath(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }
        
        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPath(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPathAll(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }
        
        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPathAll(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        public int ShowMatchWindow(int flag){
            return OLAPlugDLLHelper.ShowMatchWindow(OLAObject, flag);
        }

        public double CalculateSSIM(long image1, long image2){
            return OLAPlugDLLHelper.CalculateSSIM(OLAObject, image1, image2);
        }

        public double CalculateHistograms(long image1, long image2){
            return OLAPlugDLLHelper.CalculateHistograms(OLAObject, image1, image2);
        }

        public double CalculateMSE(long image1, long image2){
            return OLAPlugDLLHelper.CalculateMSE(OLAObject, image1, image2);
        }

        public int SaveImageFromPtr(long ptr, string path){
            return OLAPlugDLLHelper.SaveImageFromPtr(OLAObject, ptr, path);
        }

        public long ReSize(long ptr, int width, int height){
            return OLAPlugDLLHelper.ReSize(OLAObject, ptr, width, height);
        }

        public int FindColor(int x1, int y1, int x2, int y2, string color1, string color2, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColor(OLAObject, x1, y1, x2, y2, color1, color2, dir, out x, out y);
        }

        public List<Point> FindColorList(int x1, int y1, int x2, int y2, string color1, string color2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorList(OLAObject, x1, y1, x2, y2, color1, color2));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int FindColorEx(int x1, int y1, int x2, int y2, string colorJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorEx(OLAObject, x1, y1, x2, y2, colorJson, dir, out x, out y);
        }

        public List<Point> FindColorListEx(int x1, int y1, int x2, int y2, string colorJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorListEx(OLAObject, x1, y1, x2, y2, colorJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int FindMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColor(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), dir, out x, out y);
        }
        
        public int FindMultiColor(int x1, int y1, int x2, int y2, string colorJson, string pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColor(OLAObject, x1, y1, x2, y2, colorJson, pointJson, dir, out x, out y);
        }

        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorList(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson)));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, string colorJson, string pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorList(OLAObject, x1, y1, x2, y2, colorJson, pointJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int FindMultiColorFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColorFromPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), dir, out x, out y);
        }
        
        public int FindMultiColorFromPtr(long ptr, string colorJson, string pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColorFromPtr(OLAObject, ptr, colorJson, pointJson, dir, out x, out y);
        }

        public List<Point> FindMultiColorListFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorListFromPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson)));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindMultiColorListFromPtr(long ptr, string colorJson, string pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorListFromPtr(OLAObject, ptr, colorJson, pointJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int GetImageSize(long ptr, out int width, out int height){
            return OLAPlugDLLHelper.GetImageSize(OLAObject, ptr, out width, out height);
        }

        public int FindColorBlock(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlock(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
        }
        
        public int FindColorBlock(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlock(OLAObject, x1, y1, x2, y2, colorList, count, width, height, out x, out y);
        }

        public int FindColorBlockPtr(long ptr, List<ColorModel> colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
        }
        
        public int FindColorBlockPtr(long ptr, string colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtr(OLAObject, ptr, colorList, count, width, height, out x, out y);
        }

        public List<Point> FindColorBlockList(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockList(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindColorBlockList(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockList(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public List<Point> FindColorBlockListPtr(long ptr, List<ColorModel> colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindColorBlockListPtr(long ptr, string colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtr(OLAObject, ptr, colorList, count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int FindColorBlockEx(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockEx(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
        }
        
        public int FindColorBlockEx(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockEx(OLAObject, x1, y1, x2, y2, colorList, count, width, height, dir, out x, out y);
        }

        public int FindColorBlockPtrEx(long ptr, List<ColorModel> colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtrEx(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
        }
        
        public int FindColorBlockPtrEx(long ptr, string colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtrEx(OLAObject, ptr, colorList, count, width, height, dir, out x, out y);
        }

        public List<Point> FindColorBlockListEx(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListEx(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindColorBlockListEx(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListEx(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public List<Point> FindColorBlockListPtrEx(long ptr, List<ColorModel> colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtrEx(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }
        
        public List<Point> FindColorBlockListPtrEx(long ptr, string colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtrEx(OLAObject, ptr, colorList, count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public int GetColorNum(int x1, int y1, int x2, int y2, List<ColorModel> colorList){
            return OLAPlugDLLHelper.GetColorNum(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList));
        }
        
        public int GetColorNum(int x1, int y1, int x2, int y2, string colorList){
            return OLAPlugDLLHelper.GetColorNum(OLAObject, x1, y1, x2, y2, colorList);
        }

        public int GetColorNumPtr(long ptr, List<ColorModel> colorList){
            return OLAPlugDLLHelper.GetColorNumPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList));
        }
        
        public int GetColorNumPtr(long ptr, string colorList){
            return OLAPlugDLLHelper.GetColorNumPtr(OLAObject, ptr, colorList);
        }

        public long Cropped(long image, int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.Cropped(OLAObject, image, x1, y1, x2, y2);
        }

        public long GetThresholdImageFromMultiColorPtr(long ptr, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColorPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }
        
        public long GetThresholdImageFromMultiColorPtr(long ptr, string colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColorPtr(OLAObject, ptr, colorJson);
        }

        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColor(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson));
        }
        
        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, string colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColor(OLAObject, x1, y1, x2, y2, colorJson);
        }

        public int IsSameImage(long ptr, long ptr2){
            return OLAPlugDLLHelper.IsSameImage(OLAObject, ptr, ptr2);
        }

        public int ShowImage(long ptr){
            return OLAPlugDLLHelper.ShowImage(OLAObject, ptr);
        }

        public int ShowImageFromFile(string file){
            return OLAPlugDLLHelper.ShowImageFromFile(OLAObject, file);
        }

        public long SetColorsToNewColor(long ptr, List<ColorModel> colorJson, string color){
            return OLAPlugDLLHelper.SetColorsToNewColor(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), color);
        }
        
        public long SetColorsToNewColor(long ptr, string colorJson, string color){
            return OLAPlugDLLHelper.SetColorsToNewColor(OLAObject, ptr, colorJson, color);
        }

        public long RemoveOtherColors(long ptr, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.RemoveOtherColors(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }
        
        public long RemoveOtherColors(long ptr, string colorJson){
            return OLAPlugDLLHelper.RemoveOtherColors(OLAObject, ptr, colorJson);
        }

        public long DrawRectangle(long ptr, int x1, int y1, int x2, int y2, int thickness, string color){
            return OLAPlugDLLHelper.DrawRectangle(OLAObject, ptr, x1, y1, x2, y2, thickness, color);
        }

        public long DrawCircle(long ptr, int x, int y, int radius, int thickness, string color){
            return OLAPlugDLLHelper.DrawCircle(OLAObject, ptr, x, y, radius, thickness, color);
        }

        public long DrawFillPoly(long ptr, List<Point> pointJson, string color){
            return OLAPlugDLLHelper.DrawFillPoly(OLAObject, ptr, JsonConvert.SerializeObject(pointJson), color);
        }
        
        public long DrawFillPoly(long ptr, string pointJson, string color){
            return OLAPlugDLLHelper.DrawFillPoly(OLAObject, ptr, pointJson, color);
        }

        public string DecodeQRCode(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.DecodeQRCode(OLAObject, ptr));
        }

        public long CreateQRCode(string str, int pixelsPerModule){
            return OLAPlugDLLHelper.CreateQRCode(OLAObject, str, pixelsPerModule);
        }

        public long CreateQRCodeEx(string str, int pixelsPerModule, int version, int correction_level, int mode, int structure_number){
            return OLAPlugDLLHelper.CreateQRCodeEx(OLAObject, str, pixelsPerModule, version, correction_level, mode, structure_number);
        }

        public MatchResult MatchAnimationFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchAnimationFromPtr(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public MatchResult MatchAnimationFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchAnimationFromPath(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public long RemoveImageDiff(long image1, long image2){
            return OLAPlugDLLHelper.RemoveImageDiff(OLAObject, image1, image2);
        }

        public int GetImageBmpData(long imgPtr, out long data, out int size){
            return OLAPlugDLLHelper.GetImageBmpData(OLAObject, imgPtr, out data, out size);
        }

        public int GetImagePngData(long imgPtr, out long data, out int size){
            return OLAPlugDLLHelper.GetImagePngData(OLAObject, imgPtr, out data, out size);
        }

        public int FreeImageData(long screenPtr){
            return OLAPlugDLLHelper.FreeImageData(OLAObject, screenPtr);
        }

        public long ScalePixels(long ptr, int pixelsPerModule){
            return OLAPlugDLLHelper.ScalePixels(OLAObject, ptr, pixelsPerModule);
        }

        public long CreateImage(int width, int height, string color){
            return OLAPlugDLLHelper.CreateImage(OLAObject, width, height, color);
        }

        public int SetPixel(long image, int x, int y, string color){
            return OLAPlugDLLHelper.SetPixel(OLAObject, image, x, y, color);
        }

        public int SetPixelList(long image, List<Point> points, string color){
            return OLAPlugDLLHelper.SetPixelList(OLAObject, image, JsonConvert.SerializeObject(points), color);
        }
        
        public int SetPixelList(long image, string points, string color){
            return OLAPlugDLLHelper.SetPixelList(OLAObject, image, points, color);
        }

        public long ConcatImage(long image1, long image2, int gap, string color, int dir){
            return OLAPlugDLLHelper.ConcatImage(OLAObject, image1, image2, gap, color, dir);
        }

        public long CoverImage(long image1, long image2, int x, int y, double alpha){
            return OLAPlugDLLHelper.CoverImage(OLAObject, image1, image2, x, y, alpha);
        }

        public long RotateImage(long image, double angle){
            return OLAPlugDLLHelper.RotateImage(OLAObject, image, angle);
        }

        public string ImageToBase64(long image){
            return PtrToStringUTF8(OLAPlugDLLHelper.ImageToBase64(OLAObject, image));
        }

        public long Base64ToImage(string base64){
            return OLAPlugDLLHelper.Base64ToImage(OLAObject, base64);
        }

        public int Hex2ARGB(string hex, out int a, out int r, out int g, out int b){
            return OLAPlugDLLHelper.Hex2ARGB(OLAObject, hex, out a, out r, out g, out b);
        }

        public int Hex2RGB(string hex, out int r, out int g, out int b){
            return OLAPlugDLLHelper.Hex2RGB(OLAObject, hex, out r, out g, out b);
        }

        public string ARGB2Hex(int a, int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.ARGB2Hex(OLAObject, a, r, g, b));
        }

        public string RGB2Hex(int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.RGB2Hex(OLAObject, r, g, b));
        }

        public string Hex2HSV(string hex){
            return PtrToStringUTF8(OLAPlugDLLHelper.Hex2HSV(OLAObject, hex));
        }

        public string RGB2HSV(int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.RGB2HSV(OLAObject, r, g, b));
        }

        public int CmpColor(int x1, int y1, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColor(OLAObject, x1, y1, colorStart, colorEnd);
        }

        public int CmpColorPtr(long ptr, int x, int y, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColorPtr(OLAObject, ptr, x, y, colorStart, colorEnd);
        }

        public int CmpColorEx(int x1, int y1, string colorJson){
            return OLAPlugDLLHelper.CmpColorEx(OLAObject, x1, y1, colorJson);
        }

        public int CmpColorPtrEx(long ptr, int x, int y, string colorJson){
            return OLAPlugDLLHelper.CmpColorPtrEx(OLAObject, ptr, x, y, colorJson);
        }

        public int CmpColorHexEx(string hex, string colorJson){
            return OLAPlugDLLHelper.CmpColorHexEx(OLAObject, hex, colorJson);
        }

        public int CmpColorHex(string hex, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColorHex(OLAObject, hex, colorStart, colorEnd);
        }

        public long GetConnectedComponents(long ptr, List<Point> points, int tolerance){
            return OLAPlugDLLHelper.GetConnectedComponents(OLAObject, ptr, JsonConvert.SerializeObject(points), tolerance);
        }
        
        public long GetConnectedComponents(long ptr, string points, int tolerance){
            return OLAPlugDLLHelper.GetConnectedComponents(OLAObject, ptr, points, tolerance);
        }

        public double DetectPointerDirection(long ptr, int x, int y){
            return OLAPlugDLLHelper.DetectPointerDirection(OLAObject, ptr, x, y);
        }

        public double DetectPointerDirectionByFeatures(long ptr, long templatePtr, int x, int y, bool useTemplate){
            return OLAPlugDLLHelper.DetectPointerDirectionByFeatures(OLAObject, ptr, templatePtr, x, y, useTemplate);
        }

        public MatchResult FastMatch(long ptr, long templatePtr, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FastMatch(OLAObject, ptr, templatePtr, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        public long FastROI(long ptr){
            return OLAPlugDLLHelper.FastROI(OLAObject, ptr);
        }

        public int GetROIRegion(long ptr, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetROIRegion(OLAObject, ptr, out x1, out y1, out x2, out y2);
        }

        public List<Point> GetForegroundPoints(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.GetForegroundPoints(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        public long ConvertColor(long ptr, int type){
            return OLAPlugDLLHelper.ConvertColor(OLAObject, ptr, type);
        }

        public long Threshold(long ptr, double thresh, double maxVal, int type){
            return OLAPlugDLLHelper.Threshold(OLAObject, ptr, thresh, maxVal, type);
        }

        public long RemoveIslands(long ptr, int minArea){
            return OLAPlugDLLHelper.RemoveIslands(OLAObject, ptr, minArea);
        }

        public long MorphGradient(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphGradient(OLAObject, ptr, kernelSize);
        }

        public long MorphTophat(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphTophat(OLAObject, ptr, kernelSize);
        }

        public long MorphBlackhat(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphBlackhat(OLAObject, ptr, kernelSize);
        }

        public long Dilation(long ptr, int kernelSize){
            return OLAPlugDLLHelper.Dilation(OLAObject, ptr, kernelSize);
        }

        public long Erosion(long ptr, int kernelSize){
            return OLAPlugDLLHelper.Erosion(OLAObject, ptr, kernelSize);
        }

        public long GaussianBlur(long ptr, int kernelSize){
            return OLAPlugDLLHelper.GaussianBlur(OLAObject, ptr, kernelSize);
        }

        public long Sharpen(long ptr){
            return OLAPlugDLLHelper.Sharpen(OLAObject, ptr);
        }

        public long CannyEdge(long ptr, int kernelSize){
            return OLAPlugDLLHelper.CannyEdge(OLAObject, ptr, kernelSize);
        }

        public long Flip(long ptr, int flipCode){
            return OLAPlugDLLHelper.Flip(OLAObject, ptr, flipCode);
        }

        public long MorphOpen(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphOpen(OLAObject, ptr, kernelSize);
        }

        public long MorphClose(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphClose(OLAObject, ptr, kernelSize);
        }

        public long Skeletonize(long ptr){
            return OLAPlugDLLHelper.Skeletonize(OLAObject, ptr);
        }

        public long ImageStitchFromPath(string path, out long trajectory){
            return OLAPlugDLLHelper.ImageStitchFromPath(OLAObject, path, out trajectory);
        }

        public long ImageStitchCreate(){
            return OLAPlugDLLHelper.ImageStitchCreate(OLAObject);
        }

        public int ImageStitchAppend(long imageStitch, long image){
            return OLAPlugDLLHelper.ImageStitchAppend(OLAObject, imageStitch, image);
        }

        public long ImageStitchGetResult(long imageStitch, out long trajectory){
            return OLAPlugDLLHelper.ImageStitchGetResult(OLAObject, imageStitch, out trajectory);
        }

        public int ImageStitchFree(long imageStitch){
            return OLAPlugDLLHelper.ImageStitchFree(OLAObject, imageStitch);
        }

        public long CreateDatabase(string dbName, string password){
            return OLAPlugDLLHelper.CreateDatabase(OLAObject, dbName, password);
        }

        public long OpenDatabase(string dbName, string password){
            return OLAPlugDLLHelper.OpenDatabase(OLAObject, dbName, password);
        }

        public long OpenMemoryDatabase(long address, int size, string password){
            return OLAPlugDLLHelper.OpenMemoryDatabase(OLAObject, address, size, password);
        }

        public string GetDatabaseError(long db){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDatabaseError(OLAObject, db));
        }

        public int CloseDatabase(long db){
            return OLAPlugDLLHelper.CloseDatabase(OLAObject, db);
        }

        public string GetAllTableNames(long db){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetAllTableNames(OLAObject, db));
        }

        public string GetTableInfo(long db, string tableName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetTableInfo(OLAObject, db, tableName));
        }

        public string GetTableInfoDetail(long db, string tableName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetTableInfoDetail(OLAObject, db, tableName));
        }

        public int ExecuteSql(long db, string sql){
            return OLAPlugDLLHelper.ExecuteSql(OLAObject, db, sql);
        }

        public int ExecuteScalar(long db, string sql){
            return OLAPlugDLLHelper.ExecuteScalar(OLAObject, db, sql);
        }

        public long ExecuteReader(long db, string sql){
            return OLAPlugDLLHelper.ExecuteReader(OLAObject, db, sql);
        }

        public int Read(long stmt){
            return OLAPlugDLLHelper.Read(OLAObject, stmt);
        }

        public int GetDataCount(long stmt){
            return OLAPlugDLLHelper.GetDataCount(OLAObject, stmt);
        }

        public int GetColumnCount(long stmt){
            return OLAPlugDLLHelper.GetColumnCount(OLAObject, stmt);
        }

        public string GetColumnName(long stmt, int iCol){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColumnName(OLAObject, stmt, iCol));
        }

        public int GetColumnIndex(long stmt, string columnName){
            return OLAPlugDLLHelper.GetColumnIndex(OLAObject, stmt, columnName);
        }

        public int GetColumnType(long stmt, int iCol){
            return OLAPlugDLLHelper.GetColumnType(OLAObject, stmt, iCol);
        }

        public int Finalize(long stmt){
            return OLAPlugDLLHelper.Finalize(OLAObject, stmt);
        }

        public double GetDouble(long stmt, int iCol){
            return OLAPlugDLLHelper.GetDouble(OLAObject, stmt, iCol);
        }

        public int GetInt32(long stmt, int iCol){
            return OLAPlugDLLHelper.GetInt32(OLAObject, stmt, iCol);
        }

        public long GetInt64(long stmt, int iCol){
            return OLAPlugDLLHelper.GetInt64(OLAObject, stmt, iCol);
        }

        public string GetString(long stmt, int iCol){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetString(OLAObject, stmt, iCol));
        }

        public double GetDoubleByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetDoubleByColumnName(OLAObject, stmt, columnName);
        }

        public int GetInt32ByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetInt32ByColumnName(OLAObject, stmt, columnName);
        }

        public long GetInt64ByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetInt64ByColumnName(OLAObject, stmt, columnName);
        }

        public string GetStringByColumnName(long stmt, string columnName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetStringByColumnName(OLAObject, stmt, columnName));
        }

        public int InitOlaDatabase(long db){
            return OLAPlugDLLHelper.InitOlaDatabase(OLAObject, db);
        }

        public int InitOlaImageFromDir(long db, string dir, int cover){
            return OLAPlugDLLHelper.InitOlaImageFromDir(OLAObject, db, dir, cover);
        }

        public int RemoveOlaImageFromDir(long db, string dir){
            return OLAPlugDLLHelper.RemoveOlaImageFromDir(OLAObject, db, dir);
        }

        public int ExportOlaImageDir(long db, string dir, string exportDir){
            return OLAPlugDLLHelper.ExportOlaImageDir(OLAObject, db, dir, exportDir);
        }

        public int ImportOlaImage(long db, string dir, string fileName, int cover){
            return OLAPlugDLLHelper.ImportOlaImage(OLAObject, db, dir, fileName, cover);
        }

        public long GetOlaImage(long db, string dir, string fileName){
            return OLAPlugDLLHelper.GetOlaImage(OLAObject, db, dir, fileName);
        }

        public int RemoveOlaImage(long db, string dir, string fileName){
            return OLAPlugDLLHelper.RemoveOlaImage(OLAObject, db, dir, fileName);
        }

        public int SetDbConfig(long db, string key, string value){
            return OLAPlugDLLHelper.SetDbConfig(OLAObject, db, key, value);
        }

        public string GetDbConfig(long db, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDbConfig(OLAObject, db, key));
        }

        public int RemoveDbConfig(long db, string key){
            return OLAPlugDLLHelper.RemoveDbConfig(OLAObject, db, key);
        }

        public int SetDbConfigEx(string key, string value){
            return OLAPlugDLLHelper.SetDbConfigEx(OLAObject, key, value);
        }

        public string GetDbConfigEx(string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDbConfigEx(OLAObject, key));
        }

        public int RemoveDbConfigEx(string key){
            return OLAPlugDLLHelper.RemoveDbConfigEx(OLAObject, key);
        }

        public int InitDictFromDir(long db, string dict_name, string dict_path, int cover){
            return OLAPlugDLLHelper.InitDictFromDir(OLAObject, db, dict_name, dict_path, cover);
        }

        public int ImportDictWord(long db, string dict_name, string pic_file_name, int cover){
            return OLAPlugDLLHelper.ImportDictWord(OLAObject, db, dict_name, pic_file_name, cover);
        }

        public int ExportDict(long db, string dict_name, string export_dir){
            return OLAPlugDLLHelper.ExportDict(OLAObject, db, dict_name, export_dir);
        }

        public int RemoveDict(long db, string dict_name){
            return OLAPlugDLLHelper.RemoveDict(OLAObject, db, dict_name);
        }

        public int RemoveDictWord(long db, string dict_name, string word){
            return OLAPlugDLLHelper.RemoveDictWord(OLAObject, db, dict_name, word);
        }

        public long GetDictImage(long db, string dict_name, string word, int gap, int dir){
            return OLAPlugDLLHelper.GetDictImage(OLAObject, db, dict_name, word, gap, dir);
        }

        public long OpenVideo(string videoPath){
            return OLAPlugDLLHelper.OpenVideo(OLAObject, videoPath);
        }

        public long OpenCamera(int deviceIndex){
            return OLAPlugDLLHelper.OpenCamera(OLAObject, deviceIndex);
        }

        public int CloseVideo(long videoHandle){
            return OLAPlugDLLHelper.CloseVideo(OLAObject, videoHandle);
        }

        public int IsVideoOpened(long videoHandle){
            return OLAPlugDLLHelper.IsVideoOpened(OLAObject, videoHandle);
        }

        public string GetVideoInfo(long videoHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetVideoInfo(OLAObject, videoHandle));
        }

        public int GetVideoWidth(long videoHandle){
            return OLAPlugDLLHelper.GetVideoWidth(OLAObject, videoHandle);
        }

        public int GetVideoHeight(long videoHandle){
            return OLAPlugDLLHelper.GetVideoHeight(OLAObject, videoHandle);
        }

        public double GetVideoFPS(long videoHandle){
            return OLAPlugDLLHelper.GetVideoFPS(OLAObject, videoHandle);
        }

        public int GetVideoTotalFrames(long videoHandle){
            return OLAPlugDLLHelper.GetVideoTotalFrames(OLAObject, videoHandle);
        }

        public double GetVideoDuration(long videoHandle){
            return OLAPlugDLLHelper.GetVideoDuration(OLAObject, videoHandle);
        }

        public int GetCurrentFrameIndex(long videoHandle){
            return OLAPlugDLLHelper.GetCurrentFrameIndex(OLAObject, videoHandle);
        }

        public double GetCurrentTimestamp(long videoHandle){
            return OLAPlugDLLHelper.GetCurrentTimestamp(OLAObject, videoHandle);
        }

        public long ReadNextFrame(long videoHandle){
            return OLAPlugDLLHelper.ReadNextFrame(OLAObject, videoHandle);
        }

        public long ReadFrameAtIndex(long videoHandle, int frameIndex){
            return OLAPlugDLLHelper.ReadFrameAtIndex(OLAObject, videoHandle, frameIndex);
        }

        public long ReadFrameAtTime(long videoHandle, double timestamp){
            return OLAPlugDLLHelper.ReadFrameAtTime(OLAObject, videoHandle, timestamp);
        }

        public long ReadCurrentFrame(long videoHandle){
            return OLAPlugDLLHelper.ReadCurrentFrame(OLAObject, videoHandle);
        }

        public int SeekToFrame(long videoHandle, int frameIndex){
            return OLAPlugDLLHelper.SeekToFrame(OLAObject, videoHandle, frameIndex);
        }

        public int SeekToTime(long videoHandle, double timestamp){
            return OLAPlugDLLHelper.SeekToTime(OLAObject, videoHandle, timestamp);
        }

        public int SeekToBeginning(long videoHandle){
            return OLAPlugDLLHelper.SeekToBeginning(OLAObject, videoHandle);
        }

        public int SeekToEnd(long videoHandle){
            return OLAPlugDLLHelper.SeekToEnd(OLAObject, videoHandle);
        }

        public int ExtractFramesToFiles(long videoHandle, int startFrame, int endFrame, int step, string outputDir, string imageFormat, int jpegQuality){
            return OLAPlugDLLHelper.ExtractFramesToFiles(OLAObject, videoHandle, startFrame, endFrame, step, outputDir, imageFormat, jpegQuality);
        }

        public int ExtractFramesByInterval(long videoHandle, double intervalSeconds, string outputDir, string imageFormat){
            return OLAPlugDLLHelper.ExtractFramesByInterval(OLAObject, videoHandle, intervalSeconds, outputDir, imageFormat);
        }

        public int ExtractKeyFrames(long videoHandle, double threshold, int maxFrames, string outputDir, string imageFormat){
            return OLAPlugDLLHelper.ExtractKeyFrames(OLAObject, videoHandle, threshold, maxFrames, outputDir, imageFormat);
        }

        public int SaveCurrentFrame(long videoHandle, string outputPath, int quality){
            return OLAPlugDLLHelper.SaveCurrentFrame(OLAObject, videoHandle, outputPath, quality);
        }

        public int SaveFrameAtIndex(long videoHandle, int frameIndex, string outputPath, int quality){
            return OLAPlugDLLHelper.SaveFrameAtIndex(OLAObject, videoHandle, frameIndex, outputPath, quality);
        }

        public string FrameToBase64(long videoHandle, string format){
            return PtrToStringUTF8(OLAPlugDLLHelper.FrameToBase64(OLAObject, videoHandle, format));
        }

        public double CalculateFrameSimilarity(long frame1, long frame2){
            return OLAPlugDLLHelper.CalculateFrameSimilarity(OLAObject, frame1, frame2);
        }

        public string GetVideoInfoFromPath(string videoPath){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetVideoInfoFromPath(OLAObject, videoPath));
        }

        public int IsValidVideoFile(string videoPath){
            return OLAPlugDLLHelper.IsValidVideoFile(OLAObject, videoPath);
        }

        public long ExtractSingleFrame(string videoPath, int frameIndex){
            return OLAPlugDLLHelper.ExtractSingleFrame(OLAObject, videoPath, frameIndex);
        }

        public long ExtractThumbnail(string videoPath){
            return OLAPlugDLLHelper.ExtractThumbnail(OLAObject, videoPath);
        }

        public int ConvertVideo(string inputPath, string outputPath, string codec, double fps){
            return OLAPlugDLLHelper.ConvertVideo(OLAObject, inputPath, outputPath, codec, fps);
        }

        public int ResizeVideo(string inputPath, string outputPath, int width, int height){
            return OLAPlugDLLHelper.ResizeVideo(OLAObject, inputPath, outputPath, width, height);
        }

        public int TrimVideo(string inputPath, string outputPath, double startTime, double endTime){
            return OLAPlugDLLHelper.TrimVideo(OLAObject, inputPath, outputPath, startTime, endTime);
        }

        public int CreateVideoFromImages(string imageDir, string outputPath, double fps, string codec){
            return OLAPlugDLLHelper.CreateVideoFromImages(OLAObject, imageDir, outputPath, fps, codec);
        }

        public string DetectSceneChanges(string videoPath, double threshold){
            return PtrToStringUTF8(OLAPlugDLLHelper.DetectSceneChanges(OLAObject, videoPath, threshold));
        }

        public double CalculateAverageBrightness(string videoPath){
            return OLAPlugDLLHelper.CalculateAverageBrightness(OLAObject, videoPath);
        }

        public string DetectMotion(string videoPath, double threshold){
            return PtrToStringUTF8(OLAPlugDLLHelper.DetectMotion(OLAObject, videoPath, threshold));
        }

        public int SetWindowState(long hwnd, int state){
            return OLAPlugDLLHelper.SetWindowState(OLAObject, hwnd, state);
        }

        public long FindWindow(string class_name, string title){
            return OLAPlugDLLHelper.FindWindow(OLAObject, class_name, title);
        }

        public long GetClipboard(){
            return OLAPlugDLLHelper.GetClipboard(OLAObject);
        }

        public int SetClipboard(string text){
            return OLAPlugDLLHelper.SetClipboard(OLAObject, text);
        }

        public int SendPaste(long hwnd){
            return OLAPlugDLLHelper.SendPaste(OLAObject, hwnd);
        }

        public long GetWindow(long hwnd, int flag){
            return OLAPlugDLLHelper.GetWindow(OLAObject, hwnd, flag);
        }

        public string GetWindowTitle(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowTitle(OLAObject, hwnd));
        }

        public string GetWindowClass(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowClass(OLAObject, hwnd));
        }

        public int GetWindowRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetWindowRect(OLAObject, hwnd, out x1, out y1, out x2, out y2);
        }

        public string GetWindowProcessPath(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowProcessPath(OLAObject, hwnd));
        }

        public int GetWindowState(long hwnd, int flag){
            return OLAPlugDLLHelper.GetWindowState(OLAObject, hwnd, flag);
        }

        public long GetForegroundWindow(){
            return OLAPlugDLLHelper.GetForegroundWindow(OLAObject);
        }

        public int GetWindowProcessId(long hwnd){
            return OLAPlugDLLHelper.GetWindowProcessId(OLAObject, hwnd);
        }

        public int GetClientSize(long hwnd, out int width, out int height){
            return OLAPlugDLLHelper.GetClientSize(OLAObject, hwnd, out width, out height);
        }

        public long GetMousePointWindow(){
            return OLAPlugDLLHelper.GetMousePointWindow(OLAObject);
        }

        public long GetSpecialWindow(int flag){
            return OLAPlugDLLHelper.GetSpecialWindow(OLAObject, flag);
        }

        public int GetClientRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetClientRect(OLAObject, hwnd, out x1, out y1, out x2, out y2);
        }

        public int SetWindowText(long hwnd, string title){
            return OLAPlugDLLHelper.SetWindowText(OLAObject, hwnd, title);
        }

        public int SetWindowSize(long hwnd, int width, int height){
            return OLAPlugDLLHelper.SetWindowSize(OLAObject, hwnd, width, height);
        }

        public int SetClientSize(long hwnd, int width, int height){
            return OLAPlugDLLHelper.SetClientSize(OLAObject, hwnd, width, height);
        }

        public int SetWindowTransparent(long hwnd, int alpha){
            return OLAPlugDLLHelper.SetWindowTransparent(OLAObject, hwnd, alpha);
        }

        public long FindWindowEx(long parent, string class_name, string title){
            return OLAPlugDLLHelper.FindWindowEx(OLAObject, parent, class_name, title);
        }

        public long FindWindowByProcess(string process_name, string class_name, string title){
            return OLAPlugDLLHelper.FindWindowByProcess(OLAObject, process_name, class_name, title);
        }

        public int MoveWindow(long hwnd, int x, int y){
            return OLAPlugDLLHelper.MoveWindow(OLAObject, hwnd, x, y);
        }

        public double GetScaleFromWindows(long hwnd){
            return OLAPlugDLLHelper.GetScaleFromWindows(OLAObject, hwnd);
        }

        public double GetWindowDpiAwarenessScale(long hwnd){
            return OLAPlugDLLHelper.GetWindowDpiAwarenessScale(OLAObject, hwnd);
        }

        public string EnumProcess(string name){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumProcess(OLAObject, name));
        }

        public string EnumWindow(long parent, string title, string className, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindow(OLAObject, parent, title, className, filter));
        }

        public string EnumWindowByProcess(string process_name, string title, string class_name, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowByProcess(OLAObject, process_name, title, class_name, filter));
        }

        public string EnumWindowByProcessId(long pid, string title, string class_name, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowByProcessId(OLAObject, pid, title, class_name, filter));
        }

        public string EnumWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2, int sort){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowSuper(OLAObject, spec1, flag1, type1, spec2, flag2, type2, sort));
        }

        public long GetPointWindow(int x, int y){
            return OLAPlugDLLHelper.GetPointWindow(OLAObject, x, y);
        }

        public string GetProcessInfo(long pid){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetProcessInfo(OLAObject, pid));
        }

        public int ShowTaskBarIcon(long hwnd, int show){
            return OLAPlugDLLHelper.ShowTaskBarIcon(OLAObject, hwnd, show);
        }

        public long FindWindowByProcessId(long process_id, string className, string title){
            return OLAPlugDLLHelper.FindWindowByProcessId(OLAObject, process_id, className, title);
        }

        public long GetWindowThreadId(long hwnd){
            return OLAPlugDLLHelper.GetWindowThreadId(OLAObject, hwnd);
        }

        public long FindWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2){
            return OLAPlugDLLHelper.FindWindowSuper(OLAObject, spec1, flag1, type1, spec2, flag2, type2);
        }

        public int ClientToScreen(long hwnd, out int x, out int y){
            return OLAPlugDLLHelper.ClientToScreen(OLAObject, hwnd, out x, out y);
        }

        public int ScreenToClient(long hwnd, out int x, out int y){
            return OLAPlugDLLHelper.ScreenToClient(OLAObject, hwnd, out x, out y);
        }

        public long GetForegroundFocus(){
            return OLAPlugDLLHelper.GetForegroundFocus(OLAObject);
        }

        public int SetWindowDisplay(long hwnd, int affinity){
            return OLAPlugDLLHelper.SetWindowDisplay(OLAObject, hwnd, affinity);
        }

        public int IsDisplayDead(int x1, int y1, int x2, int y2, int time){
            return OLAPlugDLLHelper.IsDisplayDead(OLAObject, x1, y1, x2, y2, time);
        }

        public int GetWindowsFps(int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.GetWindowsFps(OLAObject, x1, y1, x2, y2);
        }

        public int TerminateProcess(long pid){
            return OLAPlugDLLHelper.TerminateProcess(OLAObject, pid);
        }

        public int TerminateProcessTree(long pid){
            return OLAPlugDLLHelper.TerminateProcessTree(OLAObject, pid);
        }

        public string GetCommandLine(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetCommandLine(OLAObject, hwnd));
        }

        public int CheckFontSmooth(){
            return OLAPlugDLLHelper.CheckFontSmooth(OLAObject);
        }

        public int SetFontSmooth(int enable){
            return OLAPlugDLLHelper.SetFontSmooth(OLAObject, enable);
        }

        public int EnableDebugPrivilege(){
            return OLAPlugDLLHelper.EnableDebugPrivilege(OLAObject);
        }

        public int SystemStart(string applicationName, string commandLine){
            return OLAPlugDLLHelper.SystemStart(OLAObject, applicationName, commandLine);
        }

        public int CreateChildProcess(string applicationName, string commandLine, string currentDirectory, int showType, int parentProcessId){
            return OLAPlugDLLHelper.CreateChildProcess(OLAObject, applicationName, commandLine, currentDirectory, showType, parentProcessId);
        }

        public string YoloV5(int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloV5(OLAObject, x1, y1, x2, y2));
        }

        public long YoloLoadModel(string modelPath, string password, int modelType, int inferenceType, int inferenceDevice){
            return OLAPlugDLLHelper.YoloLoadModel(OLAObject, modelPath, password, modelType, inferenceType, inferenceDevice);
        }

        public long YoloLoadModelMemory(long memoryAddr, int size, int modelType, int inferenceType, int inferenceDevice){
            return OLAPlugDLLHelper.YoloLoadModelMemory(OLAObject, memoryAddr, size, modelType, inferenceType, inferenceDevice);
        }

        public int YoloReleaseModel(long modelHandle){
            return OLAPlugDLLHelper.YoloReleaseModel(OLAObject, modelHandle);
        }

        public int YoloIsModelValid(long modelHandle){
            return OLAPlugDLLHelper.YoloIsModelValid(OLAObject, modelHandle);
        }

        public string YoloListModels(){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloListModels(OLAObject));
        }

        public string YoloGetModelInfo(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetModelInfo(OLAObject, modelHandle));
        }

        public int YoloSetModelConfig(long modelHandle, string configJson){
            return OLAPlugDLLHelper.YoloSetModelConfig(OLAObject, modelHandle, configJson);
        }

        public string YoloGetModelConfig(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetModelConfig(OLAObject, modelHandle));
        }

        public int YoloWarmup(long modelHandle, int iterations){
            return OLAPlugDLLHelper.YoloWarmup(OLAObject, modelHandle, iterations);
        }

        public string YoloDetect(long modelHandle, int x1, int y1, int x2, int y2, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetect(OLAObject, modelHandle, x1, y1, x2, y2, classes, confidence, iou, maxDetections));
        }

        public string YoloDetectSimple(long modelHandle, int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectSimple(OLAObject, modelHandle, x1, y1, x2, y2));
        }

        public string YoloDetectFromPtr(long modelHandle, long imagePtr, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromPtr(OLAObject, modelHandle, imagePtr, classes, confidence, iou, maxDetections));
        }

        public string YoloDetectFromFile(long modelHandle, string imagePath, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromFile(OLAObject, modelHandle, imagePath, classes, confidence, iou, maxDetections));
        }

        public string YoloDetectFromBase64(long modelHandle, string base64Data, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromBase64(OLAObject, modelHandle, base64Data, classes, confidence, iou, maxDetections));
        }

        public string YoloDetectBatch(long modelHandle, string imagesJson, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectBatch(OLAObject, modelHandle, imagesJson, classes, confidence, iou, maxDetections));
        }

        public string YoloClassify(long modelHandle, int x1, int y1, int x2, int y2, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassify(OLAObject, modelHandle, x1, y1, x2, y2, topK));
        }

        public string YoloClassifyFromPtr(long modelHandle, long imagePtr, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassifyFromPtr(OLAObject, modelHandle, imagePtr, topK));
        }

        public string YoloClassifyFromFile(long modelHandle, string imagePath, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassifyFromFile(OLAObject, modelHandle, imagePath, topK));
        }

        public string YoloSegment(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloSegment(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        public string YoloSegmentFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloSegmentFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        public string YoloPose(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloPose(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        public string YoloPoseFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloPoseFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        public string YoloObb(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloObb(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        public string YoloObbFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloObbFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        public string YoloKeyPoint(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloKeyPoint(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        public string YoloKeyPointFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloKeyPointFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        public string YoloGetInferenceStats(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetInferenceStats(OLAObject, modelHandle));
        }

        public int YoloResetStats(long modelHandle){
            return OLAPlugDLLHelper.YoloResetStats(OLAObject, modelHandle);
        }

        public string YoloGetLastError(){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetLastError(OLAObject));
        }

        public int YoloClearError(){
            return OLAPlugDLLHelper.YoloClearError(OLAObject);
        }


    }
}
