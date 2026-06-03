using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Assets.Sources.Config;
using Assets.Sources.Utils;
using NetData;
using Plugins.Unity;
using Plugins.Unity.Extension;
using SSJJBase.Singleton;
using SSJJBase.Utility;
using zlib;

namespace Plugins.Hacks.Functions
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class FakeScreenshotSender : ModuleBase
    {
        private MethodHookManager methodHookManager;
        private MethodInfo originalMethod;

        public override void Update()
        {
            if (MonoBehaviourSingleton<NetEaseCloudManager>.HasInstance && methodHookManager == null && originalMethod == null)
            {
                methodHookManager = new MethodHookManager();
                originalMethod = MonoBehaviourSingleton<NetEaseCloudManager>.Instance.GetMethodInfo("Send");
                MethodInfo hookMethod = typeof(FakeScreenshotSender).GetMethod("Send", BindingFlags.Static | BindingFlags.Public);
                methodHookManager.HookMethod(originalMethod, hookMethod);
            }
        }

        public override void OnDestroy()
        {
            if (methodHookManager != null && originalMethod != null)
            {
                methodHookManager.UnhookMethod(originalMethod);
            }
        }

        public static void Send(NetEaseCloudManager target, byte[] bytes, int methodId)
        {
            try
            {
                Thread.Sleep(1000);
                SendData(methodId);
            }
            catch
            {
            }
        }

        public static void SendData(int methodId)
        {
            try
            {
                byte[] blankScreenshot = new byte[4194304];
                RoomData roomData = Contexts.sharedInstance.battleRoom.roomData.Data;
                GameBootConfig gameBootConfig = TplManager.Instance.GameBootConfig;
                BinaryDataWriter binaryDataWriter = new BinaryDataWriter();
                string requestString = BuildRequestString(gameBootConfig, roomData);
                string md5Hash = Md5Utility.GetMD5HashFromFile(Encoding.Default.GetBytes(requestString + "adf35b91c956e63f7de79c5513f5823e"));
                WriteString(binaryDataWriter, requestString);
                WriteString(binaryDataWriter, md5Hash);
                binaryDataWriter.WriteByteArray(blankScreenshot, 0, blankScreenshot.Length);
                byte[] finalData = binaryDataWriter.GetBytes();
                MemoryStream memoryStream = new MemoryStream();
                ZOutputStream zOutputStream = new ZOutputStream(memoryStream, -1);
                zOutputStream.Write(finalData, 0, finalData.Length);
                zOutputStream.finish();
                zOutputStream.Close();
                Contexts.sharedInstance.battleServer.battleServer.Server.SendTcpData(methodId, memoryStream.GetBuffer());
            }
            catch
            {
            }
        }

        private static string BuildRequestString(GameBootConfig config, RoomData roomData)
        {
            return string.Concat(new object[]
            {
                "&platform=",
                config.Platform,
                "&serverId=",
                config.ServerId,
                "&uid=",
                config.UserId,
                "&charId=",
                config.CharId,
                "&ruleType=",
                1,
                "&gamePlugFlag=",
                1,
                "&raceType=",
                roomData.RaceType,
                "&sceneId=",
                roomData.SceneId
            });
        }

        public static void WriteString(BinaryDataWriter writer, string data)
        {
            writer.WriteShort((short)data.Length);
            writer.WriteUtf(data, 0);
        }
    }
}