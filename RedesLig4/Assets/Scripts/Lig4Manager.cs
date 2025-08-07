using UnityEngine;
using UnityEngine.UI;

public class Lig4Manager : MonoBehaviour
{
    public GameObject pecaJogador1;  // Amarelo
    public GameObject pecaJogador2;  // Vermelho
    public Transform boardPanel;
    public Text textoStatus;

    public TcpServerLig4 tcpServer;  // Para servidor (opcional)
    public TcpClientLig4 tcpClient;  // Para cliente (opcional)

    private int[,] grade = new int[6, 7];
    private int jogadorAtual = 1;   // Quem tem o turno agora (1 ou 2)
    private bool jogoAtivo = true;

    // 1 = Amarelo (Servidor), 2 = Vermelho (Cliente)
    private int jogadorLocal = 1;

    void Start()
    {
        jogadorLocal = (PlayerInfo.CorDoJogador == "AMARELO") ? 1 : 2;
        jogadorAtual = 1; // Amarelo sempre começa

        ResetarTabuleiro();
        ConfigurarBotoes();
        AtualizarTextoStatus();
    }

    void ConfigurarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < botoes.Length; i++)
        {
            int index = i;
            botoes[i].onClick.RemoveAllListeners();
            botoes[i].onClick.AddListener(() => OnSlotClick(index));
            botoes[i].interactable = true;
        }
    }

    void OnSlotClick(int index)
    {
        if (!jogoAtivo) return;

        int coluna = index % 7;

        // Só permite jogar se for o turno do jogador local
        if (jogadorAtual != jogadorLocal)
        {
            Debug.Log("Não é seu turno.");
            return;
        }

        if (grade[0, coluna] != 0)
        {
            Debug.Log("Coluna cheia.");
            return;
        }

        // Executa a jogada local e envia para o adversário
        FazerJogadaLocal(coluna);
    }

    // Faz a jogada no tabuleiro
    // enviarParaRede = true quando é jogada iniciada localmente
    public void FazerJogadaLocal(int coluna, bool enviarParaRede = true, int jogador = -1)
    {
        if (!jogoAtivo) return;

        if (jogador == -1) jogador = jogadorAtual; // Se não passar, usa o turno atual

        for (int linha = 5; linha >= 0; linha--)
        {
            if (grade[linha, coluna] == 0)
            {
                grade[linha, coluna] = jogador;

                int slotIndex = linha * 7 + coluna;
                Transform slotTransform = boardPanel.GetChild(slotIndex);

                // Instancia a peça correta conforme o jogador que fez a jogada
                GameObject peca = Instantiate(
                    jogador == 1 ? pecaJogador1 : pecaJogador2,
                    slotTransform
                );
                peca.transform.localPosition = Vector3.zero;
                peca.transform.localRotation = Quaternion.identity;
                peca.transform.localScale = Vector3.one;
                peca.tag = "Peca";

                Button btn = slotTransform.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = false;

                // Verifica se ganhou
                if (VerificarVitoria(linha, coluna, jogador))
                {
                    if (enviarParaRede)
                    {
                        EnviarJogada(coluna);
                        if (jogadorLocal == 1 && tcpServer != null) tcpServer.EnviarVitoria(jogador);
                        if (jogadorLocal == 2 && tcpClient != null) tcpClient.EnviarVitoria(jogador);
                    }

                    textoStatus.text = $"Jogador {ObterNomeJogador(jogador)} venceu!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    Debug.Log($"VITÓRIA detectada localmente: jogador {jogador}");
                    return;
                }

                // Verifica empate
                if (VerificarEmpate())
                {
                    textoStatus.text = "Empate!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    if (enviarParaRede) EnviarJogada(coluna); 
                    return;
                }

                if (enviarParaRede)
                {
                    EnviarJogada(coluna);
                }

                jogadorAtual = 3 - jogador;

                AtualizarTextoStatus();

                return;
            }
        }

        Debug.Log("Coluna cheia.");
    }

    public void FazerJogadaRemota(int coluna)
    {
        if (!jogoAtivo) return;

        int jogadorRemoto = jogadorLocal == 1 ? 2 : 1;
        Debug.Log($"FazerJogadaRemota chamada! coluna = {coluna} jogadorRemoto = {jogadorRemoto}");

        FazerJogadaLocal(coluna, false, jogadorRemoto);
    }

    void EnviarJogada(int coluna)
    {
        if (jogadorLocal == 1)
        {
            if (tcpServer != null)
            {
                tcpServer.EnviarJogada(coluna);
                Debug.Log($"Lig4Manager: enviado (server) coluna {coluna}");
            }
            else
            {
                Debug.LogWarning("tcpServer == null, não enviou jogada.");
            }
        }
        else
        {
            if (tcpClient != null)
            {
                tcpClient.EnviarJogada(coluna);
                Debug.Log($"Lig4Manager: enviado (client) coluna {coluna}");
            }
            else
            {
                Debug.LogWarning("tcpClient == null, não enviou jogada.");
            }
        }
    }

    // *** NOVO MÉTODO ***
    public void ReiniciarJogo()
    {
        ResetarTabuleiro();

        if (jogadorLocal == 1 && tcpServer != null)
        {
            tcpServer.EnviarMensagem("RESET\n");
        }
        else if (jogadorLocal == 2 && tcpClient != null)
        {
            tcpClient.EnviarMensagem("RESET\n");
        }
    }

    public void ProcessarMensagemRecebida(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;

        string[] tokens = mensagem.Split(new char[] { '\n', '\r', ';', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            string s = t.Trim();
            if (string.IsNullOrEmpty(s)) continue;

            if (s == "RESET")
            {
                Debug.Log("Recebido comando RESET, reiniciando jogo.");
                ResetarTabuleiro();
                continue;
            }

            if (s.StartsWith("WIN|"))
            {
                string[] parts = s.Split('|');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int vencedor))
                {
                    Debug.Log($"ProcessarMensagemRecebida: WIN recebido -> jogador {vencedor}");
                    AplicarVitoriaRemota(vencedor);
                    continue;
                }
                else
                {
                    Debug.LogWarning("ProcessarMensagemRecebida: tag WIN mal formada: " + s);
                    continue;
                }
            }

            if (int.TryParse(s, out int coluna))
            {
                if (coluna >= 0 && coluna < 7)
                {
                    FazerJogadaRemota(coluna);
                }
                else
                {
                    Debug.LogWarning("Coluna recebida fora do intervalo: " + coluna);
                }
            }
            else
            {
                Debug.LogWarning("Mensagem de rede inválida (não é número nem WIN): " + s);
            }
        }
    }

    void AplicarVitoriaRemota(int vencedor)
    {
        textoStatus.text = $"Jogador {ObterNomeJogador(vencedor)} venceu!";
        jogoAtivo = false;
        DesabilitarBotoes();
        Debug.Log($"AplicarVitoriaRemota: jogador {vencedor} venceu (recebido pela rede)");
    }

    void AtualizarTextoStatus()
    {
        if (!jogoAtivo) return;

        textoStatus.text = $"Vez do jogador {ObterNomeJogador(jogadorAtual)}";
    }

    void DesabilitarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        foreach (var btn in botoes)
            btn.interactable = false;
    }

    bool VerificarEmpate()
    {
        for (int linha = 0; linha < 6; linha++)
            for (int col = 0; col < 7; col++)
                if (grade[linha, col] == 0)
                    return false;
        return true;
    }

    bool VerificarVitoria(int linha, int coluna, int jogador)
    {
        int[][] direcoes = new int[][]
        {
            new int[] {0, 1},
            new int[] {1, 0},
            new int[] {1, 1},
            new int[] {1, -1}
        };

        foreach (var dir in direcoes)
        {
            int count = 1;
            count += ContarNaDirecao(linha, coluna, dir[0], dir[1], jogador);
            count += ContarNaDirecao(linha, coluna, -dir[0], -dir[1], jogador);
            if (count >= 4) return true;
        }
        return false;
    }

    int ContarNaDirecao(int linha, int coluna, int dLinha, int dColuna, int jogador)
    {
        int cont = 0;
        int l = linha + dLinha;
        int c = coluna + dColuna;

        while (l >= 0 && l < 6 && c >= 0 && c < 7 && grade[l, c] == jogador)
        {
            cont++;
            l += dLinha;
            c += dColuna;
        }
        return cont;
    }

    public void ResetarTabuleiro()
    {
        for (int linha = 0; linha < 6; linha++)
        {
            for (int col = 0; col < 7; col++)
            {
                grade[linha, col] = 0;
            }
        }

        foreach (Transform slot in boardPanel)
        {
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
                btn.interactable = true;
        }

        jogoAtivo = true;
        jogadorAtual = 1;
        AtualizarTextoStatus();
    }

    string ObterNomeJogador(int jogador)
    {
        return jogador == 1 ? "Amarelo" : "Vermelho";
    }
}
