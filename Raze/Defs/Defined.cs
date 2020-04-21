namespace Raze.Defs
{
    public abstract class Defined<T>where T : Def
    {
        public T Def { get; protected set; }

        protected Defined()
        {

        }

        protected Defined(T def)
        {
            Def = def;
        }

        public abstract void ApplyDef(T def);

        public virtual bool IsType(string defName, bool inherit = false)
        {
            if (defName == null)
                return false;

            if (defName == Def.Name)
                return true;

            return IsType(Main.DefDatabase.Get(defName), inherit);
        }

        public virtual bool IsType(Def other, bool inherit = false)
        {
            if (other == null)
                return false;

            bool isExact = other.Name == Def.Name;
            if (!inherit)
                return isExact;

            if (isExact)
                return true;

            return Def.IsChildOf(other.Name);
        }
    }
}
