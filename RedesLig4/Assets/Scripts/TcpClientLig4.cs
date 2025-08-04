using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientLig4 : MonoBehaviour
{
    public string ipServidor = "127.0.0.1"; // IP do servidor
    public int porta = 7777;
    private TcpClient cliente;
    private NetworkStream stream;
    private Thread threadReceber;

    public Lig4Manager lig4Manager; // Arraste no Inspector

    void Start()
    {
        threadReceber = new Thread(ConectarServidor);
        threadReceber.Start();
    }

    void ConectarServidor()
    {
        try
        {
            cliente = new TcpClient();
            cliente.Connect(ipServidor, porta);
            stream = cliente.GetStream();
            Debug.Log("Conectado ao servidor!");

            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesLidos = stream.Read(buffer, 0, buffer.Length);
                if (bytesLidos > 0)
                {
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                    Debug.Log("Recebido do servidor: " + mensagem);

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        lig4Manager.ProcessarMensagemRecebida(mensagem);
                    });
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.Log("Erro cliente: " + ex.Message);
        }
    }

    public void EnviarJogada(int coluna)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] dados = Encoding.UTF8.GetBytes(coluna.ToString());
            stream.Write(dados, 0, dados.Length);
            stream.Flush();
            Debug.Log("Enviado para servidor: " + coluna);
        }
    }

    public void EnviarMensagem(string mensagem)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            stream.Write(dados, 0, dados.Length);
            stream.Flush();
            Debug.Log("Enviado para servidor: " + mensagem);
        }
    }

    private void OnApplicationQuit()
    {
        stream?.Close();
        cliente?.Close();
        threadReceber?.Abort();
    }
}



