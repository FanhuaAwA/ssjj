using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Sources.Chat;
using Assets.Sources.Framework;
using Assets.Sources.Framework.System;
using Assets.Sources.Modules.Ui.Chat;
using Entitas;
using NetData;
using Plugins.Unity;
using Plugins.Unity.Extension;
using SSJJNetworking.BattleServer;

namespace Plugins.Hacks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class ChatModule : ModuleBase
    {
        public static void SendServerMessage(string messageContent)
        {
            IPlaybackSystem chatJobSystem = ChatModule.GetChatJobSystem();
            ChatInputData chatInputData = new ChatInputData
            {
                SenderInputContent = messageContent,
                SenderType = ChatModule.ChatTypes[1],
                ReceiverName = string.Empty,
                ReceiverCid = string.Empty
            };
            chatJobSystem?.InvokeMethodSafely("SendChatInfo", new object[] { chatInputData });
        }

        public static void SendLocalMessage(ChatModule.MessageType type, string senderName, string messageContent)
        {
            ChatJobSystem chatJobSystem = ChatModule.GetChatJobSystem();
            if (chatJobSystem != null)
            {
                ChatHistroyData chatHistroyData = default;
                chatHistroyData.MsgType = type.ToString(); chatHistroyData.ReceiverName = string.Empty; chatHistroyData.ReceiverCid = string.Empty; chatHistroyData.SenderName = senderName; chatHistroyData.SenderBody = messageContent; chatHistroyData.AlphaData.RemainTime = 6000; chatHistroyData.AlphaData.AlphaRemainTime = 100;
                chatJobSystem.InvokeMethodSafely("OnRecvChatInfo", new object[] { chatHistroyData });
            }
        }

        private static ChatJobSystem GetChatJobSystem()
        {
            GameModuleFeature instance = GameModuleFeature.Instance;
            if (instance == null)
            {
                return null;
            }
            PlaybackSystem fieldValue = instance.GetFieldValue<PlaybackSystem>("_playbackSystem");
            if (fieldValue == null)
            {
                return null;
            }
            List<IPlaybackSystem> fieldValue2 = fieldValue.GetFieldValue<List<IPlaybackSystem>>("_systems");
            object obj = fieldValue2 == null ? null : (object)fieldValue2.FirstOrDefault((IPlaybackSystem system) => system.GetType() == typeof(ChatJobSystem));
            return obj as ChatJobSystem;
        }

        public override void Update()
        {
            if (!Menu.Report)
            {
                return;
            }
            IBattleServer server = Contexts.sharedInstance.battleServer.battleServer.Server;
            ReportRequest reportRequest = new ReportRequest();
            foreach (IEntity entity in Contexts.sharedInstance.player.GetEntities())
            {
                if (entity is PlayerEntity playerEntity && !playerEntity.isMyPlayer)
                {
                    reportRequest.CidList.Add(playerEntity.basicInfo.Cid);
                    reportRequest.Reason = 0; server.SendTcpMessage(20, reportRequest); reportRequest.Reason = 4; server.SendTcpMessage(20, reportRequest);
                }
            }
        }

        private static readonly string[] ChatTypes = new string[] { "battle_team", "battle_all", "team", "personal", "big_horn2" };

        public enum MessageType
        {
            BattleAll,
            BattleObserverAll,
            BattleTeam,
            Team,
            Personal,
            System,
            Prompt,
            TacticsSound,
            BigHorn2,
            BigHorn3,
            LiveBarrage,
            LiveGift
        }
    }
}