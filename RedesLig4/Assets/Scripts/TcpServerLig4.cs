using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServerLig4 : MonoBehaviour
{
    TcpListener server;
    Thread serverThread;

    int[,] tabuleiro = new int[6, 7];
    int turno = 1;

    void Start()
    {
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Debug.Log("Servidor ouvindo na porta 8080...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int len = stream.Read(buffer, 0, buffer.Length);
            string mensagem = Encoding.UTF8.GetString(buffer, 0, len).Trim();

            Debug.Log($"[Servidor] Mensagem recebida: {mensagem}");

            string resposta = ProcessarMensagem(mensagem);

            byte[] respostaBytes = Encoding.UTF8.GetBytes(resposta);
            stream.Write(respostaBytes, 0, respostaBytes.Length);

            stream.Close();
            client.Close();
        }
    }

    string ProcessarMensagem(string msg)
    {
        if (!msg.StartsWith("JOGADA:")) return "ERRO:Mensagem inválida";

        string[] partes = msg.Split(':');
        if (!int.TryParse(partes[1], out int coluna)) return "ERRO:Coluna inválida";

        if (coluna < 0 || coluna > 6) return "ERRO:Coluna fora do limite";

        for (int linha = 5; linha >= 0; linha--)
        {
            if (tabuleiro[linha, coluna] == 0)
            {
                tabuleiro[linha, coluna] = turno;

                // Atualiza visual no Unity
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    FindFirstObjectByType<Lig4Manager>().FazerJogadaRemota(coluna);

                });

                string resposta = $"OK:Jogada em {coluna}";
                turno = turno == 1 ? 2 : 1;
                return resposta;
            }
        }

        return "ERRO:Coluna cheia";
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
