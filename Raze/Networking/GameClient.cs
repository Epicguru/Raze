using System;
using System.Collections.Generic;
using Lidgren.Network;
using Raze.Networking.Players;

namespace Raze.Networking
{
    public class GameClient : NetPeerBase
    {
        public NetConnectionStatus ConnectionStatus
        {
            get
            {
                return client.ConnectionStatus;
            }
        }
        public event Action OnConnected;
        public event Action<string> OnDisconnected;

        private List<Player> playersInGame = new List<Player>();
        private Dictionary<uint, Player> idToPlayer = new Dictionary<uint, Player>();
        private NetClient client;

        public GameClient() : base(MakeConfig())
        {
            client = new NetClient(base.Config);
            base.peer = client;
            base.tag = "Client";
            base.OnStatusChange += (conn, status, msg) =>
            {
                switch (status)
                {
                    case NetConnectionStatus.Disconnected:

                        // Should have a string to read, reason for disconnect.
                        string text = msg.PeekString();
                        Log($"Disconnected. Reason: {text}");

                        OnDisconnected?.Invoke(text);
                        break;

                    case NetConnectionStatus.Connected:
                        Log("Connected.");
                        OnConnected?.Invoke();
                        break;
                }
            };

            base.SetHandler(NetMessageType.Data_PlayerData, (id, msg) =>
            {
                // Note: this type of message should never arrive if we are the host. Check just in case.
                if (Net.IsHost)
                {
                    Error("Did not expected a message of Data_PlayerData since we are the host. Bug???");
                    return;
                }

                byte type = msg.ReadByte();
                switch (type)
                {
                    case 0:
                        // All player list.
                        if(playersInGame.Count > 0)
                        {
                            // This should not happen, because it should only be send once when we join a match.
                            // Note: error has been disabled because the server sent a connect message before all players.
                            //Error("Received all player data message from server, but we already have players is list! Duplicate message?");
                            playersInGame.Clear();
                            idToPlayer.Clear();
                        }

                        // Read all players.
                        int count = msg.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            Player p = Player.Create(msg);
                            AddPlayer(p, true); // Add and send message.
                        }

                        Log($"Read all players: {playersInGame.Count}");

                        break;

                    case 1:
                        // Player connected.
                        Player p2 = Player.Create(msg);
                        AddPlayer(p2, true); // Adds and also broadcasts message.

                        break;

                    case 2:
                        // Player disconnected.
                        uint removedPlayerID = msg.ReadUInt32();
                        if (idToPlayer.ContainsKey(removedPlayerID))
                        {
                            Player p3 = idToPlayer[removedPlayerID];
                            RemovePlayer(p3, true); // Removes and also broadcasts message.
                        }
                        break;
                }
            });
        }

        public void SendMessage(NetOutgoingMessage msg, NetDeliveryMethod method)
        {
            client.SendMessage(msg, method);
        }

        private static NetPeerConfiguration MakeConfig()
        {
            var config = new NetPeerConfiguration(Net.APP_ID);

            return config;
        }

        public bool Connect(string ip, int port, out string errorMsg, string password = null)
        {
            if(ConnectionStatus != NetConnectionStatus.Disconnected)
            {
                Error($"Cannot connect now, wrong state: {ConnectionStatus}, expected Disconnected.");
                errorMsg = "Already connected or connecting.";
                return false;
            }

            // Reset values.
            playersInGame.Clear();
            idToPlayer.Clear();

            bool asHost = Net.Server != null && Net.Server.Status != NetPeerStatus.NotRunning;

            Debug.Trace($"Starting client connect to {ip} on port {port} {(asHost ? "as host" : "as remote client")}");

            if(client.Status == NetPeerStatus.NotRunning)
                client.Start();

            try
            {
                client.Connect(ip, port, CreateHailMessage(asHost, password));
                errorMsg = null;
                return true;
            }
            catch(Exception e)
            {
                errorMsg = e.Message;
                return false;
            }
        }

        private NetOutgoingMessage CreateHailMessage(bool asHost, string password = null)
        {
            NetOutgoingMessage msg = client.CreateMessage(64);
            /*
             * 0. Host key (or 0 if not host)
             * 1. Server password (or empty string)
             * 2. Player name.
             */
            msg.Write(asHost ? Net.Server.HostKey : (ulong)0);
            msg.Write(password == null ? string.Empty : password.Trim());
            msg.Write($"James #{Rand.Range(0, 1000)}");

            return msg;
        }

        private void AddPlayer(Player p, bool sendMessage)
        {
            if (p == null)
                return;

            playersInGame.Add(p);
            idToPlayer.Add(p.ID, p);

            if (sendMessage)
                Net.BroadcastConnect(p);
        }

        private void RemovePlayer(Player p, bool sendMessage)
        {
            if (p == null)
                return;

            playersInGame.Remove(p);
            idToPlayer.Remove(p.ID);

            if (sendMessage)
                Net.BroadcastDisconnect(p);
        }

        public void Update()
        {
            base.ProcessMessages(out int _);
        }

        public void Disconnect()
        {
            if (ConnectionStatus == NetConnectionStatus.Disconnected || ConnectionStatus == NetConnectionStatus.Disconnecting)
            {
                Warn("Already disconnected or disconnecting!");
                return;
            }

            client.Disconnect("Bye");
        }

        public override void Dispose()
        {
            if (ConnectionStatus != NetConnectionStatus.Disconnected)
                Disconnect();

            idToPlayer?.Clear();
            idToPlayer = null;

            playersInGame?.Clear();
            playersInGame = null;

            client = null;
            base.Dispose();
        }
    }
}
