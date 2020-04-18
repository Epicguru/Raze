using Lidgren.Network;

namespace Raze.Networking.Players
{
    /// <summary>
    /// A human player is a player, who is human. Duh. As opposed to a bot.
    /// See <see cref="Player"/>. Most of the data in this class is only valid on the server.
    /// </summary>
    public class HumanPlayer : Player
    {
        /// <summary>
        /// Gets the network connection to the remote client. Will be null if not on server, or if the client has disconnected.
        /// This has lots of useful information such as client IP address, RTT and other.
        /// </summary>
        public NetConnection ConnectionToClient { get; internal set; }
        /// <summary>
        /// Gets a unique identifier for this player's network connection.
        /// Only valid on server (same as <see cref="ConnectionToClient"/>).
        /// This is NOT the same as <see cref="Player.ID"/>, because only human players have client connections.
        /// This value can be used to find out which Player a message has come from, by using <see cref="GameServer.GetPlayer(long)"/>.
        /// </summary>
        public long RemoteUniqueIdentifier
        {
            get
            {
                return ConnectionToClient?.RemoteUniqueIdentifier ?? 0;
            }
        }
        /// <summary>
        /// If true, then this is the player that is running the server on their PC.
        /// In any given game, there may be no host client, or there may be one, but there will never be more than one.
        /// </summary>
        public bool IsHost { get; internal set; }

        internal override void WriteToMessage(NetBuffer msg)
        {
            base.WriteToMessage(msg);

            msg.Write(IsHost);
        }

        internal override void ReadFromMessage(NetBuffer msg)
        {
            base.ReadFromMessage(msg);

            IsHost = msg.ReadBoolean();
        }

        public override string ToString()
        {
            return $"[{ID}] Human '{Name}' (from {ConnectionToClient?.RemoteEndPoint.ToString() ?? "---"}, {RemoteUniqueIdentifier})";
        }
    }
}
