using GVS.Networking.Players;
using GVS.World;
using Lidgren.Network;
using System;
using System.Collections.Generic;

namespace GVS.Networking
{
    public class GameServer : NetPeerBase
    {
        public NetPeerStatus Status
        {
            get
            {
                return server.Status;
            }
        }
        /// <summary>
        /// The password that clients need to give before connecting.
        /// If null, no password is required. This can be changed once the server is
        /// running, or beforehand.
        /// </summary>
        public string Password { get; set; } = null;
        /// <summary>
        /// The number of current uploads that are happening simultaneously.
        /// To limit the number of uploads allowed at once (each upload makes the game and network lag),
        /// you can set <see cref="MaxConcurrentUploads"/>.
        /// </summary>
        public int ActiveWorldUploadCount
        {
            get
            {
                return worldUploads.Count;
            }
        }
        /// <summary>
        /// The maximum number of concurrent world uploads that can happen at once.
        /// This is only really important for large maps that will have more than a few people trying
        /// to connect at once.
        /// Default value is 2.
        /// </summary>
        public int MaxConcurrentUploads { get; set; } = 2;
        /// <summary>
        /// The maximum amount of world chunks that can be sent each frame that an active upload is happening.
        /// So, the total number of chunks that could be uploaded any given frame is <see cref="MaxChunksToUploadPerFrame"/> * <see cref="MaxConcurrentUploads"/>.
        /// Making this value smaller will put less strain on the server and it's network, but will give longer world download times
        /// to clients. For small maps with a small number of players, this will have basically no effect. Only tweak for larger maps
        /// with players entering and leaving more frequently.
        ///
        /// Default value is 8.
        /// </summary>
        public int MaxChunksToUploadPerFrame { get; set; } = 4;
        /// <summary>
        /// The maximum number of players (humans and bots) that are allowed in the current match.
        /// </summary>
        public int MaxPlayers { get; }
        /// <summary>
        /// The connection to the host client, if they exist. This may be null if there is no host.
        /// This is useful when used with <see cref="SendMessageToAll(NetOutgoingMessage, NetDeliveryMethod, NetConnection, int)"/>
        /// because it allows for messages to be sent to all except for the host, and if this value is null then the method
        /// will send to all.
        /// </summary>
        public NetConnection HostConnection { get; private set; }
        /// <summary>
        /// The host key that the connecting client uses to tell the server that it is the host.
        /// It is randomly generated, so if a 'hacker' tried to impersonate the host then there is
        /// a 1 in 18446744073709551614 chance that they would guess correctly.
        /// </summary>
        internal ulong HostKey = 0;

        private List<ActiveWorldUpload> worldUploads;
        private List<Player> connectedPlayers;
        private Dictionary<uint, Player> idToPlayer;
        private Dictionary<long, HumanPlayer> remoteIDToPlayer;
        private NetServer server;
        private uint nextID;

        public GameServer(int port, int maxPlayers) : base(MakeConfig(port, maxPlayers))
        {
            server = new NetServer(base.Config);
            base.peer = server;
            base.tag = "Server";

            this.MaxPlayers = maxPlayers;

            this.connectedPlayers = new List<Player>();
            this.idToPlayer = new Dictionary<uint, Player>();
            this.remoteIDToPlayer = new Dictionary<long, HumanPlayer>();
            this.worldUploads = new List<ActiveWorldUpload>();

            base.SetHandler(NetMessageType.Req_BasicServerInfo, (id, msg) =>
            {
                // Note: this message never arrives from the client if they are a host, as they already know everything about the world.

                // Send a little info about the server and the map...
                NetOutgoingMessage first = CreateBaseMapInfo();
                SendMessage(msg.SenderConnection, first, NetDeliveryMethod.ReliableOrdered);

                // Send player list.
                SendAllPlayerData(msg.SenderConnection);

                // The client should send back a response asking for full map info. (see below for response)
            });

            base.SetHandler(NetMessageType.Req_WorldChunks, (id, msg) =>
            {
                // Client has request for map/entity data to be sent.
                // TODO check that the client hasn't already requested this before.

                // Send map chunks.

                // Add a new world upload. TODO also upload entities.
                var upload = new ActiveWorldUpload(msg.SenderConnection, Main.Map.GetNumberOfNetChunks(), MaxChunksToUploadPerFrame);
                worldUploads.Add(upload);

                Trace($"Now starting to send {upload.TotalChunksToSend} chunks of map data...");
            });

            // Add status change listener.
            base.OnStatusChange += (conn, status, msg) =>
            {
                HumanPlayer player = GetPlayer(conn);
                switch (status)
                {
                    case NetConnectionStatus.Connected:
                        // At this point, the HumanPlayer object has already been set up because it's connection was approved.
                        // Nothing to do here for now.

                        if (player == null)
                        {
                            Error($"Remote client has connected, but a player object is not associated with them... Did setup fail?");
                            break;
                        }

                        Log($"Client has connected: {player.Name} {(player.IsHost ? "(host)" : "")}");
                        Net.BroadcastConnect(player);

                        // Notify all clients that a new player has joined (apart from the host who already knows)
                        var newConnectMsg = CreateMessage(NetMessageType.Data_PlayerData, 32);
                        newConnectMsg.Write((byte)1);
                        newConnectMsg.Write(false); // IsBot
                        player.WriteToMessage(newConnectMsg); // Other data such as IsHost, name etc.
                        SendMessageToAll(newConnectMsg, NetDeliveryMethod.ReliableSequenced, HostConnection);

                        break;

                    case NetConnectionStatus.Disconnected:
                        // Player has left the game, bye!
                        // Remove the player associated with them.
                        if(player == null)
                        {
                            Error($"Remote client has disconnected, but a player object is not associated with them...");
                            break;
                        }

                        string text = msg.PeekString();

                        Log($"Client '{player.Name}' has disconnected: {text}");
                        Net.BroadcastDisconnect(player);

                        // Notify all clients that a player has left (apart from the host who already knows)
                        var disconnectMsg = CreateMessage(NetMessageType.Data_PlayerData, 32);
                        disconnectMsg.Write((byte)2);
                        disconnectMsg.Write(player.ID);
                        SendMessageToAll(disconnectMsg, NetDeliveryMethod.ReliableSequenced, HostConnection);

                        RemovePlayer(player);
                        break;
                }
            };

            // Add handlers.
            base.SetBaseHandler(NetIncomingMessageType.ConnectionApproval, (msg) =>
            {
                // TODO add check for number of players currently in the game.

                ulong hostKey = msg.ReadUInt64();
                bool isHost = false;
                if(hostKey != 0)
                {
                    // This means that the client is claiming to be the local host.
                    // If so, the host key should be the same as the one on this object,
                    // which is generated when the server Start() method is called.
                    // Otherwise, there is either a serious bug or a hacking attempt.
                    if(hostKey != this.HostKey)
                    {
                        Warn($"Connecting client from {msg.SenderConnection} has claimed they are the host with host key {hostKey}, but this in the incorrect host key. Bug or impersonator?");
                    }
                    else
                    {
                        // Correct host key, they are the real host (or 1 in 18.4 quintillion chance).
                        isHost = true;
                    }
                }

                // Password (or empty string).
                string password = msg.ReadString();
                if(this.Password != null && password.Trim() != this.Password.Trim())
                {
                    msg.SenderConnection.Deny("Incorrect password");
                    return;
                }

                // Player name.
                string name = msg.ReadString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    msg.SenderConnection.Deny("Invalid name");
                    return;
                }

                // Create player object for them.
                HumanPlayer p = new HumanPlayer();
                p.IsHost = isHost;
                p.Name = name;
                p.ConnectionToClient = msg.SenderConnection;

                // Add player to the game. (gives them an Id and stuff)
                AddPlayer(p);

                if (isHost)
                {
                    // Flag them as the host connection.
                    HostConnection = msg.SenderConnection;
                }

                // Accept the connection, everything looks good!
                msg.SenderConnection.Approve();
            });
        }

        private NetOutgoingMessage CreateBaseMapInfo()
        {
            var msg = CreateMessage(NetMessageType.Data_BasicServerInfo);

            // Write size of world.
            int width = Main.Map.Width;
            int depth = Main.Map.Depth;
            int height = Main.Map.Height;
            msg.Write(new Point3D(width, depth, height));

            // Write number of world chunks to send.
            msg.Write(Main.Map.GetNumberOfNetChunks());

            return msg;
        }

        private void SendAllPlayerData(NetConnection toSendTo)
        {
            if (toSendTo == null)
                return;

            int playerCount = connectedPlayers.Count;
            NetOutgoingMessage msg = CreateMessage(NetMessageType.Data_PlayerData, playerCount * 22);

            /*
             * 0 for all players.
             * 1 for player connect.
             * 2 for player disconnect.
             */
            msg.Write((byte)0);
            msg.Write(playerCount);
            foreach (var player in connectedPlayers)
            {
                msg.Write(player.IsBot);
                player.WriteToMessage(msg);
            }

            SendMessage(toSendTo, msg, NetDeliveryMethod.ReliableUnordered);
        }

        private static NetPeerConfiguration MakeConfig(int port, int maxPlayers)
        {
            var config = new NetPeerConfiguration(Net.APP_ID);
            config.Port = port;
            config.MaximumConnections = maxPlayers;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            return config;
        }

        private void AddPlayer(Player p)
        {
            if (p == null)
                return;

            // Set unique id.
            p.ID = nextID;
            nextID++;

            connectedPlayers.Add(p);
            idToPlayer.Add(p.ID, p);
            if(p is HumanPlayer human)
            {
                remoteIDToPlayer.Add(human.RemoteUniqueIdentifier, human);
            }

            // Log($"Player has joined: {p}");
        }

        private void RemovePlayer(Player p)
        {
            if (p == null)
                return;

            connectedPlayers.Remove(p);
            idToPlayer.Remove(p.ID);
            if(p is HumanPlayer human)
            {
                remoteIDToPlayer.Remove(human.RemoteUniqueIdentifier);
            }

            // Log($"Player has left: {p}");
        }

        public Player GetPlayer(uint id)
        {
            if (idToPlayer.ContainsKey(id))
                return idToPlayer[id];
            else
                return null;
        }

        public HumanPlayer GetPlayer(NetIncomingMessage msg)
        {
            if (msg == null)
                return null;
            return GetPlayer(msg.SenderConnection);
        }

        public HumanPlayer GetPlayer(NetConnection connection)
        {
            if (connection == null)
                return null;
            long id = connection.RemoteUniqueIdentifier;
            return GetPlayer(id);
        }

        public HumanPlayer GetPlayer(long remoteID)
        {
            if (remoteIDToPlayer.ContainsKey(remoteID))
                return remoteIDToPlayer[remoteID];
            else
                return null;
        }

        public void Start()
        {
            if(Status != NetPeerStatus.NotRunning)
            {
                Error($"Server cannot be started, expected NotRunning state, got {Status}.");
                return;
            }

            // Reset some values.
            nextID = 0;
            connectedPlayers.Clear();
            idToPlayer.Clear();
            remoteIDToPlayer.Clear();

            // Generate random host key.
            Random r = new Random();
            byte[] bytes = new byte[8];
            r.NextBytes(bytes);
            HostKey = BitConverter.ToUInt64(bytes, 0);
            
            Trace($"Starting server on port {Config.Port}...");
            server.Start();
        }

        public void Update()
        {
            bool shouldProcess = base.ProcessMessages(out int _);

            if (shouldProcess)
            {
                UpdateUploads();
            }
        }

        private void UpdateUploads()
        {
            int processed = 0;
            for (int i = 0; i < worldUploads.Count; i++)
            {
                var upload = worldUploads[i];

                upload.Tick(this);
                processed++;

                if (upload.IsDone)
                {
                    worldUploads.RemoveAt(i);
                    i--;
                }

                if (processed >= MaxConcurrentUploads)
                    break;
            }
        }

        /// <summary>
        /// Sends a network message to a particular client connection using a particular delivery method.
        /// None of the parameters may be null, and the connection must be valid (connected to this server).
        /// </summary>
        /// <param name="conn">The client connection to send to, may not be null and must be connected to this server.</param>
        /// <param name="msg">The message to send. Should contain at least 1 byte corresponding to it's type. See <see cref="NetPeerBase.CreateMessage(byte, int)"/> and <see cref="NetMessageType"/> for more info.</param>
        /// <param name="delivery">The delivery method to use. Most are self explanatory, see Lidgren documentation for more info.</param>
        /// <param name="channel">The network channel to send the data on. See Lidgren documentation for more info. If unsure, leave as default value.</param>
        public void SendMessage(NetConnection conn, NetOutgoingMessage msg, NetDeliveryMethod delivery, int channel = NetChannel.Default)
        {
            server.SendMessage(msg, conn, delivery, channel);
        }

        /// <summary>
        /// Sends a network message to all connected clients, with an optional excluded client.
        /// The message may not be null but <paramref name="except"/> may be null to send to all.
        /// See <see cref="HostConnection"/>.
        /// </summary>
        /// <param name="msg">The message to send. Should contain at least 1 byte corresponding to it's type. See <see cref="NetPeerBase.CreateMessage(byte, int)"/> and <see cref="NetMessageType"/> for more info.</param>
        /// <param name="delivery">The delivery method to use. Most are self explanatory, see Lidgren documentation for more info.</param>
        /// <param name="except">The (optional) client to exclude. All clients apart from the excluded client will receive the message. Often used with <see cref="HostConnection"/>.</param>
        /// <param name="channel">The network channel to send the data on. See Lidgren documentation for more info. If unsure, leave as default value.</param>
        public void SendMessageToAll(NetOutgoingMessage msg, NetDeliveryMethod delivery, NetConnection except = null, int channel = NetChannel.Default)
        {
            server.SendToAll(msg, except, delivery, 0);
        }

        public void Shutdown(string message)
        {
            if (Status != NetPeerStatus.Running)
            {
                Error($"Server cannot be shutdown, expected state Running, got {Status}.");
                return;
            }

            Trace($"Shutting down server: {message}");
            server.Shutdown(message);

            connectedPlayers.Clear();
            idToPlayer.Clear();
            remoteIDToPlayer.Clear();
        }

        public override void Dispose()
        {
            if (!(Status == NetPeerStatus.ShutdownRequested || Status == NetPeerStatus.NotRunning))
                Shutdown("Server has closed (dsp)");
            server = null;

            if(idToPlayer != null)
            {
                idToPlayer.Clear();
                idToPlayer = null;
            }
            if(connectedPlayers != null)
            {
                connectedPlayers.Clear();
                connectedPlayers = null;
            }
            if(remoteIDToPlayer != null)
            {
                remoteIDToPlayer.Clear();
                remoteIDToPlayer = null;
            }
            if(worldUploads != null)
            {
                worldUploads.Clear();
                worldUploads = null;
            }

            base.Dispose();
        }

        private class ActiveWorldUpload
        {
            public bool IsDone
            {
                get
                {
                    return sentIndex == TotalChunksToSend;
                }
            }

            public readonly int TotalChunksToSend;

            private readonly int maxChunksPerFrame;
            private readonly NetConnection targetClient;
            private int sentIndex;

            public ActiveWorldUpload(NetConnection client, int toSendCount, int maxChunksPerFrame)
            {
                this.targetClient = client;
                this.maxChunksPerFrame = maxChunksPerFrame;
                this.TotalChunksToSend = toSendCount;
            }

            public void Tick(GameServer server)
            {
                for (int i = 0; i < maxChunksPerFrame; i++)
                {
                    if (sentIndex == TotalChunksToSend)
                        break;

                    var msg = Main.Map.NetSerializeAllTiles(server, sentIndex);
                    server.SendMessage(targetClient, msg, NetDeliveryMethod.ReliableUnordered);
                    sentIndex++;
                }
            }
        }
    }
}
