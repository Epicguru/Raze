using Microsoft.Xna.Framework;

namespace RazeUI.Windows
{
    public class MessageBox : Window
    {
        public string Text { get; set; }

        public MessageBox(string title, string text)
        {
            base.Title = title;
            base.Size = new Point(550, 550);
            this.Text = text ?? "No more information.";
        }

        public void Show()
        {
            if(!IsOpen)
                LayoutUserInterface.Instance?.AddWindow(this);
        }

        public override void Draw(LayoutUserInterface ui)
        {
            ui.Paragraph(Text, Color.White);
        }
    }
}
