namespace RazeUI.Handles.Validators
{
    public class TextIPAddressValidator : ITextValidator
    {
        public bool NumericalOnly { get; set; } = false;

        public bool IsCharacterValid(char c)
        {
            if (NumericalOnly)
                return c == '.' || char.IsDigit(c);
            else
                return true;
        }

        public bool IsStringValid(ref string text)
        {
            return true;
        }
    }
}
