using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Networking;
using RazeUI;
using RazeUI.Handles;
using RazeUI.Handles.Validators;
using RazeUI.Windows;

namespace Raze.Screens.Instances
{
    public class ConnectScreen : GameScreen
    {
        private TextBoxHandle ipInput = new TextBoxHandle(){HintText = "Type Ip address...", MaxCharacters = 16, Text = "localhost", Validator = new TextIPAddressValidator()};
        private TextBoxHandle portInput = new TextBoxHandle() { HintText = "Type port number...", MaxCharacters = 9, Text = "7777", Validator = new TextIntegerValidator() };
        private TextBoxHandle passwordInput = new TextBoxHandle() { HintText = "Password (leave blank for no password)" };
        private string connectionStatus = "";
        private bool isConnecting;

        public ConnectScreen() : base("Connect Screen")
        {
        }

        public override void DrawUI(SpriteBatch _, LayoutUserInterface ui)
        {
            if (isConnecting)
                DrawConnectingUI(ui);
            else
                DrawInputUI(ui);
        }

        private void DrawInputUI(LayoutUserInterface ui)
        {
            int sw = ui.IMGUI.ScreenProvider.GetWidth();
            int sh = ui.IMGUI.ScreenProvider.GetHeight();
            int w = 350;
            int h = 350;

            var sb = new Rectangle((sw - w) / 2, (sh - h) / 2, w, h);

            ui.PanelContext(sb, PanelType.Default);
            var panel = ui.GetContextInnerBounds();

            ui.TextBox(ipInput, new Point(panel.Width, 40));
            ui.TextBox(portInput, new Point(panel.Width, 40));
            ui.TextBox(passwordInput, new Point(panel.Width, 40));
            ui.FlatSeparator();
            if (ui.Button("Connect"))
            {
                OnConnectClicked();
            }
            ui.ActiveTint = Color.Red;
            if (ui.IMGUI.Button(new Rectangle(panel.X, panel.Bottom - 32, panel.Width, 32), "Cancel"))
            {
                OnExitClicked();
            }
            ui.ActiveTint = Color.White;

            ui.EndContext();
        }

        private void DrawConnectingUI(LayoutUserInterface ui)
        {
            int sw = ui.IMGUI.ScreenProvider.GetWidth();
            int sh = ui.IMGUI.ScreenProvider.GetHeight();
            int w = 450;
            int h = 250;

            var sb = new Rectangle((sw - w) / 2, (sh - h) / 2, w, h);

            ui.PanelContext(sb);

            ui.Anchor = Anchor.Centered;
            ui.Paragraph(connectionStatus, TextAlignment.Centered, Color.White);
            ui.Anchor = Anchor.Vertical;
            ui.FlatSeparator();
            if (ui.Button("Cancel"))
            {
                OnCancelConnectClicked();
            }

            ui.EndContext();
        }

        public void OnConnectClicked()
        {
            string ip = ipInput.Text;
            string port = portInput.Text;
            string password = passwordInput.Text;

            if (string.IsNullOrWhiteSpace(ip))
            {
                new MessageBox("Error", "Please input an IP address to connect to.").Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(port))
            {
                new MessageBox("Error", "Please input a port number to connect on.").Show();

                return;
            }

            int portNum = int.Parse(port); // Should always work because there is a validator on the input.

            Debug.Log($"Starting connect: {ip}, {portNum}");
            bool worked = Net.Client.Connect(ip, portNum, out string error, password);
            if (!worked)
            {
                new MessageBox("Failed to connect", $"Connecting failed:\n{error}").Show();

                return;
            }

            ToggleConnectPanel(true);
        }

        public void OnCancelConnectClicked()
        {
            Net.Client.Disconnect();
            ToggleConnectPanel(false);
        }

        public void OnExitClicked()
        {
            if (!Manager.IsTransitioning)
                Manager.ChangeScreen<MainMenuScreen>();
        }

        private void ToggleConnectPanel(bool visible)
        {
            isConnecting = visible;
        }

        private void SetConnectionStatus(string txt)
        {
            connectionStatus = txt;
        }

        public override void UponShow()
        {
            Debug.Assert(Net.Client == null, "Expected client to be null!");
            Net.Client = new GameClient();
            Net.Client.OnStatusChange += ClientConnStatusChange;
            Net.Client.OnConnected += OnClientConnect;
            Net.Client.OnDisconnected += OnClientDisconnected;

            ToggleConnectPanel(false);
        }

        public override void UponHide()
        {
            if(Net.Client.ConnectionStatus == NetConnectionStatus.Disconnected)
            {
                Net.Client.Dispose();
                Net.Client = null;
            }
            else
            {
                Net.Client.OnStatusChange -= ClientConnStatusChange;
                Net.Client.OnConnected -= OnClientConnect;
                Net.Client.OnDisconnected -= OnClientDisconnected;
            }
        }

        private void ClientConnStatusChange(NetConnection conn, NetConnectionStatus status, NetIncomingMessage msg)
        {
            SetConnectionStatus($"Connecting: {status}");
        }

        private void OnClientConnect()
        {
            // Cool, connected. Move to game screen.
            var screen = Manager.GetScreen<PlayScreen>();
            screen.HostMode = false;
            Manager.ChangeScreen<PlayScreen>();
        }

        private void OnClientDisconnected(string reason)
        {
            ToggleConnectPanel(false);
            new MessageBox("Failed to connect", $"Connection was rejected:\n{reason}").Show();

        }

        public override void Update()
        {
            Net.Client.Update();
        }
    }
}
