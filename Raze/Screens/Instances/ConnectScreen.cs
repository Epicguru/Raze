using GVS.Networking;
using Lidgren.Network;

namespace GVS.Screens.Instances
{
    public class ConnectScreen : GameScreen
    {
        public ConnectScreen() : base("Connect Screen")
        {
        }

        public override void Load()
        {

        }

        public void OnConnectClicked()
        {
            string ip = "localhost";
            string port = "7777";
            string password = "";

            //MessageBox.DefaultMsgBoxSize = new Vector2(300, 300);
            if (string.IsNullOrWhiteSpace(ip))
            {
                //MessageBox.ShowMsgBox("Error", "Please input an IP address to connect to.", "Ok");
                return;
            }
            if (string.IsNullOrWhiteSpace(port))
            {
                //MessageBox.ShowMsgBox("Error", "Please input a port number to connect on.", "Ok");
                return;
            }

            int portNum = int.Parse(port); // Should always work because there is a validator on the input.

            Debug.Log($"Starting connect: {ip}, {portNum}");
            bool worked = Net.Client.Connect(ip, portNum, out string error, password);
            if (!worked)
            {
                //MessageBox.ShowMsgBox("Failed to connect", $"Connecting failed:\n{error}");
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
            // URGTODO reimplement
        }

        private void SetConnectionStatus(string txt)
        {
            // URGTODO reimplement
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
            // URGTODO re-implement
            //MessageBox.ShowMsgBox("Failed to connect", $"Connection was rejected:\n{reason}");
        }

        public override void Update()
        {
            Net.Client.Update();
        }
    }
}
