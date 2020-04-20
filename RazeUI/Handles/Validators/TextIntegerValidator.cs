namespace RazeUI.Handles.Validators
{
    public class TextIntegerValidator : ITextValidator
    {
        public bool AllowBlank { get; set; } = true;

        public bool IsCharacterValid(char c)
        {
            return char.IsDigit(c);
        }

        public bool IsStringValid(ref string text)
        {
            if (!AllowBlank && string.IsNullOrWhiteSpace(text))
                text = "0";

            bool converted = int.TryParse(text, out int f);
            return converted || (AllowBlank && string.IsNullOrWhiteSpace(text));
        }
    }
}
