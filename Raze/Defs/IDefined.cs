namespace Raze.Defs
{
    public interface IDefined<in D>where D : Def
    {
        void ApplyDef(D def);
        bool IsType(string defName);

        bool IsType(Def other)
        {
            return other != null && IsType(other.Name);
        }
    }
}
