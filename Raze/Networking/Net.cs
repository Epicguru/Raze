using System;
using Raze.Networking.Players;

namespace Raze.Networking
{
    public static class Net
    {
        public const string APP_ID = "GVS_Game";

        public static event Action<Player> OnHumanPlayerConnect;
        public static event Action<Player> OnHumanPlayerDisconnect;

        public static GameClient Client { get; internal set; }
        public static GameServer Server { get; internal set; }

        public static bool IsClient
        {
            get
            {
                return Client != null && Client.ConnectionStatus == Lidgren.Network.NetConnectionStatus.Connected;
            }
        }
        public static bool IsServer
        {
            get
            {
                return Server != null && Server.Status == Lidgren.Network.NetPeerStatus.Running;
            }
        }
        public static bool IsHost
        {
            get
            {
                return IsClient && IsServer;
            }
        }

        internal static void BroadcastConnect(Player p)
        {
            OnHumanPlayerConnect?.Invoke(p);
        }

        internal static void BroadcastDisconnect(Player p)
        {
            OnHumanPlayerDisconnect?.Invoke(p);
        }
    }
}
