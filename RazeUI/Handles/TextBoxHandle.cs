namespace RazeUI.Handles
{
    public class TextBoxHandle : IElementHandle
    {
        public bool IsMouseOver { get; set; }
        public bool IsSelected { get; set; }
        public bool AllowMultiLine { get; set; } = false;

        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                if (text == value)
                    return;

                text = value ?? "";
                CaretPosition = CaretPosition; // Seems stupid but it actually corrects the caret position.
                linesDirty = true;
            }
        }
        public string HintText { get; set; }
        public int MaxCharacters { get; set; }

        public bool IsCaretAtEnd
        {
            get
            {
                return CaretPosition == Text.Length;
            }
        }
        public bool IsCaretAtStart
        {
            get
            {
                return CaretPosition == 0;
            }
        }

        public int CaretPosition
        {
            get
            {
                return caretPos;
            }
            set
            {
                caretPos = GetCorrectedCaretPosition(value);
            }
        }


        private string[] lines;
        private bool linesDirty = true;
        private int caretPos;
        private string text = "";

        public virtual int GetCorrectedCaretPosition(int position, string textToUse = null)
        {
            if (textToUse == null)
                textToUse = this.Text;

            if (position < 0)
                return 0;

            if (position > textToUse.Length)
                return textToUse.Length;

            return position;
        }

        public string[] GetLines()
        {
            if (linesDirty || lines == null)
            {
                lines = Text.Split('\n');
                linesDirty = false;
            }

            return lines;
        }

        public int GetCaretLine(out int lineStartIndex)
        {
            int line = 0;
            int startIndex = 0;

            for (int i = 0; i < CaretPosition; i++)
            {
                char c = Text[i];
                if (c == '\n')
                {
                    line++;
                    startIndex = i + 1;
                }
            }

            lineStartIndex = startIndex;
            return line;
        }
    }
}
