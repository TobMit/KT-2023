using System.Net.Sockets;
using System.Text;

namespace MauiApp1;

public partial class MainPage : ContentPage
{

    TcpClient client;
    NetworkStream stream;
    
    public MainPage()
    {
        InitializeComponent();
        // Nastavenie udalostí
        // SendButton.Clicked += OnSendButtonClicked;

        // Pripojenie k serveru
        client = new TcpClient("127.0.0.1", 12345);
        stream = client.GetStream();

        // Spustiť vlákno na čítanie zo servera
        System.Threading.Thread clientThread = new System.Threading.Thread(ReceiveMessages);
        clientThread.Start();
    }
    private void OnSendButtonClicked(object sender, EventArgs e)
    {
        string message = MessageEntry.Text;

        // Odošli správu na server
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);

        // Vyčistiť vstup po odoslaní
        MessageEntry.Text = "";
    }

    private void ReceiveMessages()
    {
        byte[] data = new byte[256];
        StringBuilder builder = new StringBuilder();

        while (true)
        {
            int bytes = stream.Read(data, 0, data.Length);
            if (bytes == 0)
                break;

            string message = Encoding.UTF8.GetString(data, 0, bytes);
            builder.Append(message);

            // Aktualizuj UI so správou od servera
            Device.BeginInvokeOnMainThread(() =>
            {
                ChatLabel.Text = builder.ToString();
            });
        }

        client.Close();
    }
}