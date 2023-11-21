using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;

class Server
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static List<string> chatHistory = new List<string>();
    private static HashSet<TcpClient> clientsWithHistory = new HashSet<TcpClient>();
    private static object lockObj = new object();

    private static int clientId = 0;

    static void Main()
    {
        LoadChatHistory(); // Načítaj históriu zo súboru

        TcpListener server = null;
        try
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 12345;

            server = new TcpListener(ipAddress, port);
            server.Start();

            Console.WriteLine($"Server spustený na {ipAddress}:{port}");

            while (true)
            {
                Console.WriteLine("Čakám na pripojenie klienta...");
                TcpClient client = server.AcceptTcpClient();

                lock (lockObj)
                {
                    clients.Add(client);
                }

                Console.WriteLine("Klient pripojený!");

                // Odošli históriu klientovi len ak ešte neobdržal históriu
                lock (lockObj)
                {
                    if (!clientsWithHistory.Contains(client))
                    {
                        foreach (string message in chatHistory)
                        {
                            NetworkStream newClientStream = client.GetStream();
                            byte[] responseData = Encoding.UTF8.GetBytes(message);
                            newClientStream.Write(responseData, 0, responseData.Length);
                        }

                        clientsWithHistory.Add(client);
                    }
                }
                clientId++;
                Thread clientThread = new Thread(() => HandleClient(client, clientId));
                clientThread.Start();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Chyba: {e}");
        }
        finally
        {
            server?.Stop();
        }
    }

    static void HandleClient(TcpClient client, int pClientId)
    {
        NetworkStream stream = client.GetStream();
        byte[] data = new byte[256];

        string clientName = $"TobiasMitala client - {pClientId}";

        while (true)
        {
            int bytes = stream.Read(data, 0, data.Length);
            if (bytes == 0)
                break;

            string message = Encoding.UTF8.GetString(data, 0, bytes);
            Console.WriteLine($"Prijatá správa od {clientName} ({DateTime.Now}): {message}");

            // Odošli správu všetkým klientom
            lock (lockObj)
            {
                string formattedMessage = $"{clientName} povedal ({DateTime.Now}): {message} \n";
                chatHistory.Add(formattedMessage);
                ArchiveMessage(formattedMessage); // Archivuj správu do súboru

                foreach (TcpClient otherClient in clients)
                {
                    NetworkStream otherStream = otherClient.GetStream();
                    byte[] responseData = Encoding.UTF8.GetBytes(formattedMessage);
                    otherStream.Write(responseData, 0, responseData.Length);
                }
            }
        }

        // Zmaz klienta zo zoznamu po odpojení
        lock (lockObj)
        {
            clients.Remove(client);
        }

        client.Close();
    }

    static void ArchiveMessage(string message)
    {
        string filePath = "chat_history.txt";
        try
        {
            File.AppendAllText(filePath, message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Chyba pri archivácii správy: {e}");
        }
    }

    static void LoadChatHistory()
    {
        string filePath = "chat_history.txt";
        try
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    chatHistory.Add($"{line}\n");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Chyba pri načítavaní histórie: {e}");
        }
    }
}
