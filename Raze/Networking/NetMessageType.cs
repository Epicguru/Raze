namespace GVS.Networking
{
    public static class NetMessageType
    {
        public const byte Req_BasicServerInfo = 0;
        public const byte Data_BasicServerInfo = 1;
        public const byte Req_WorldChunks = 2;
        public const byte Data_WorldChunk = 3;
        public const byte Data_PlayerData = 4;
    }
}
