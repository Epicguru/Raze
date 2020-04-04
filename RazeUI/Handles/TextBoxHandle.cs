namespace RazeUI.Handles
{
    public class TextBoxHandle : IElementHandle
    {
        public bool IsMouseOver { get; set; }
        public bool IsSelected { get; set; }

        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                this.text = value ?? "";
            }
        }
        public string HintText { get; set; }
        public int MaxCharacters { get; set; }

        private string text;
    }
}
