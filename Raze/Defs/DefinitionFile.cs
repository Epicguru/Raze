namespace Raze.Defs
{
    public struct DefinitionFile
    {
        public string FilePath;
        public string Json;

        public DefinitionFile(string path, string json)
        {
            this.FilePath = path;
            this.Json = json;
        }
    }
}
