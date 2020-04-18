namespace Raze.Networking
{
    public interface INetSynched
    {
        bool IsDirty { get; set; }

        void FlagDirty()
        {
            Debug.Log("Thing");
        }
    }
}
