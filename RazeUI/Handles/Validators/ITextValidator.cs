namespace RazeUI.Handles.Validators
{
    public interface ITextValidator
    {
        bool IsCharacterValid(char c);
        bool IsStringValid(ref string text);
    }
}
