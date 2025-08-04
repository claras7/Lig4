using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServerLig4 : MonoBehaviour
{
    public int porta = 7777;
    private TcpListener listener;
    private TcpClient clienteConectado;
    private NetworkStream stream;
    private Thread threadEscuta;

    public Lig4Manager lig4Manager; // Arraste no Inspector

    void Start()
    {
        threadEscuta = new Thread(OuvirCliente);
        threadEscuta.Start();
    }

    void OuvirCliente()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, porta);
            listener.Start();
            Debug.Log("Servidor aguardando conexão...");

            clienteConectado = listener.AcceptTcpClient();
            Debug.Log("Cliente conectado!");

            stream = clienteConectado.GetStream();

            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesLidos = stream.Read(buffer, 0, buffer.Length);
                if (bytesLidos > 0)
                {
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                    Debug.Log("Recebido do cliente: " + mensagem);

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        lig4Manager.ProcessarMensagemRecebida(mensagem);
                    });
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.Log("Erro servidor: " + ex.Message);
        }
    }

    public void EnviarJogada(int coluna)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] dados = Encoding.UTF8.GetBytes(coluna.ToString());
            stream.Write(dados, 0, dados.Length);
            stream.Flush();
            Debug.Log("Enviado para cliente: " + coluna);
        }
    }

    public void EnviarMensagem(string mensagem)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            stream.Write(dados, 0, dados.Length);
            stream.Flush();
            Debug.Log("Enviado para cliente: " + mensagem);
        }
    }

    private void OnApplicationQuit()
    {
        stream?.Close();
        clienteConectado?.Close();
        listener?.Stop();
        threadEscuta?.Abort();
    }
}




