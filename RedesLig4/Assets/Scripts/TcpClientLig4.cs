using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientLig4 : MonoBehaviour
{
    public Lig4Manager lig4Manager;
    public string ipServidor = "127.0.0.1";
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    void Start()
    {
        receiveThread = new Thread(ConnectAndReceive);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ConnectAndReceive()
    {
        try
        {
            client = new TcpClient(ipServidor, 7777);
            Debug.Log("Conectado ao servidor!");
            stream = client.GetStream();

            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytes = stream.Read(buffer, 0, buffer.Length);
                string mensagem = Encoding.ASCII.GetString(buffer, 0, bytes);
                Debug.Log("Recebido do servidor: " + mensagem);

                if (int.TryParse(mensagem, out int coluna))
                {
                    UnityMainThreadDispatcher.Enqueue(() => lig4Manager.FazerJogadaRemota(coluna));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Cliente erro: " + ex.Message);
        }
    }

    public void EnviarJogada(int coluna)
    {
        if (client != null && client.Connected)
        {
            byte[] data = Encoding.ASCII.GetBytes(coluna.ToString());
            stream.Write(data, 0, data.Length);
        }
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }
}
