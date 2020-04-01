namespace GVS.Networking.Players
{
    /// <summary>
    /// A bot player is a player in the game that is controlled by AI.
    /// </summary>
    public class BotPlayer : Player
    {
        public override string ToString()
        {
            return $"[{ID}] Bot '{Name}'";
        }
    }
}
