using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace GVS.Networking
{
    public abstract class NetPeerBase : IDisposable
    {
        public static bool LogStatusChange = false;

        /// <summary>
        /// The configuration that was used to set up this end point.
        /// Changes values on this will do nothing, it is intended for read-only purposes.
        /// </summary>
        public NetPeerConfiguration Config { get; private set; }

        public event Action<NetConnection, NetConnectionStatus, NetIncomingMessage> OnStatusChange;

        protected NetPeer peer;
        protected string tag;

        private Action<byte, NetIncomingMessage>[] handlers;
        private Dictionary<NetIncomingMessageType, Action<NetIncomingMessage>> baseHandlers;

        protected NetPeerBase(NetPeerConfiguration config)
        {
            this.Config = config;
            this.baseHandlers = new Dictionary<NetIncomingMessageType, Action<NetIncomingMessage>>();
            handlers = new Action<byte, NetIncomingMessage>[byte.MaxValue + 1];

            // Add some default handlers
            SetBaseHandler(NetIncomingMessageType.Data, (msg) =>
            {
                byte id = msg.PeekByte(); // Peek ID bye.
                var handler = handlers[id]; // Find handler. May be null.
                if(handler == null)
                {
                    Warn($"Received data message of ID {id}, but not handler exists to process it!");
                }
                else
                {
                    msg.ReadByte(); // Consume the ID byte.
                    handler.Invoke(id, msg);
                }
            });
            SetBaseHandler(NetIncomingMessageType.DebugMessage, (msg) =>
            {
                Log(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.WarningMessage, (msg) =>
            {
                Warn(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.ErrorMessage, (msg) =>
            {
                Error(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.StatusChanged, (msg) =>
            {
                var status = (NetConnectionStatus) msg.ReadByte();
                if(LogStatusChange)
                    Trace($"Status: {msg.SenderEndPoint}, {status}");
                OnStatusChange?.Invoke(msg.SenderConnection, status, msg);
            });
        }

        public void SetHandler(byte id, Action<byte, NetIncomingMessage> action)
        {
            if (action == null)
            {
                Debug.Error($"Cannot set null handler for id {id}.");
                return;
            }

            var current = handlers[id];
            if(current != null)
            {
                Debug.Error($"There is already a handler for id {id}! Replacing is currently not allowed. (may be in future API)");
                return;
            }

            handlers[id] = action;
        }

        public void SetBaseHandler(NetIncomingMessageType type, Action<NetIncomingMessage> action)
        {
            if (!baseHandlers.ContainsKey(type))
                baseHandlers.Add(type, action);
            else
                baseHandlers[type] = action;
        }

        public void RecycleMessage(NetIncomingMessage msg)
        {
            if (msg != null && peer != null)
                peer.Recycle(msg);
        }

        public NetOutgoingMessage CreateMessage(byte id, int initialCapacity = 0)
        {
            NetOutgoingMessage msg = initialCapacity == 0 ? peer.CreateMessage() : peer.CreateMessage(initialCapacity);
            msg.Write(id);
            return msg;
        }

        protected bool ProcessMessages(out int messageCount)
        {
            messageCount = 0;
            if (peer == null)
                return false;
            if (peer.Status == NetPeerStatus.NotRunning)
                return false;

            NetIncomingMessage msg;
            while ((msg = peer.ReadMessage()) != null)
            {
                NetIncomingMessageType type = msg.MessageType;

                // This is not the fastest way to do this...
                if (baseHandlers.ContainsKey(type))
                {
                    messageCount++;
                    baseHandlers[type].Invoke(msg);
                }

                peer.Recycle(msg);
            }

            return true;
        }

        protected void Trace(string msg)
        {
            Debug.Trace($"[{tag}]: {msg}");
        }

        protected void Log(string msg)
        {
            Debug.Log($"[{tag}]: {msg}");
        }

        protected void Warn(string msg)
        {
            Debug.Warn($"[{tag}]: {msg}");
        }

        protected void Error(string msg)
        {
            Debug.Error($"[{tag}]: {msg}");
        }

        public virtual void Dispose()
        {
            Config = null;
            if(baseHandlers != null)
            {
                baseHandlers.Clear();
                baseHandlers = null;
            }
            handlers = null;
        }
    }
}
