using Lidgren.Network;

namespace GVS.Networking.Players
{
    /// <summary>
    /// A player is a human or a bot that is playing the game.
    /// </summary>
    public abstract class Player
    {
        public static Player Create(NetBuffer msg)
        {
            bool isBot = msg.ReadBoolean();

            Player p = isBot ? (Player)new BotPlayer() : (Player)new HumanPlayer();
            p.ReadFromMessage(msg);

            return p;
        }

        /// <summary>
        /// The name of this player or bot. May not be unique (there could be multiple players with the same name).
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Is this an instance of <see cref="HumanPlayer"/>?
        /// </summary>
        public bool IsHuman
        {
            get
            {
                return this is HumanPlayer;
            }
        }
        /// <summary>
        /// Is this an instance of <see cref="BotPlayer"/>?
        /// </summary>
        public bool IsBot
        {
            get
            {
                return this is BotPlayer;
            }
        }
        /// <summary>
        /// The unique ID of this player. Guaranteed to be unique per-session, but not constant between sessions.
        /// This means that it should be used to distinguish between players that might have the same <see cref="Name"/>
        /// , but should not be used to save data associated with a player because that player's unique ID might change
        /// the next time they join the game.
        /// </summary>
        public uint ID { get; internal set; }

        protected internal Player()
        {

        }

        /// <summary>
        /// Writes basic info the a message.
        /// </summary>
        internal virtual void WriteToMessage(NetBuffer msg)
        {
            msg.Write(ID);
            msg.Write(Name);
        }

        /// <summary>
        /// Reads basic info from a message.
        /// </summary>
        /// <param name="msg"></param>
        internal virtual void ReadFromMessage(NetBuffer msg)
        {
            ID = msg.ReadUInt32();
            Name = msg.ReadString();
        }
    }
}
