using UnityEngine;
using UnityEngine.UI;

public class Lig4Manager : MonoBehaviour
{
    public GameObject pecaJogador1;  // Amarelo
    public GameObject pecaJogador2;  // Vermelho
    public Transform boardPanel;
    public Text textoStatus;

    public TcpServerLig4 tcpServer;  // Servidor usa isso
    public TcpClientLig4 tcpClient;  // Cliente usa isso

    private int[,] grade = new int[6, 7];
    private int jogadorAtual = 1;   // Quem tem o turno agora (1 ou 2)
    private bool jogoAtivo = true;

    private int jogadorLocal = 1;  // 1 = Amarelo (Servidor), 2 = Vermelho (Cliente)

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

        FazerJogadaLocal(coluna);
    }

    void FazerJogadaLocal(int coluna, bool enviarParaRede = true, int jogador = -1)
    {
        if (jogador == -1) jogador = jogadorAtual;

        for (int linha = 5; linha >= 0; linha--)
        {
            if (grade[linha, coluna] == 0)
            {
                grade[linha, coluna] = jogador;

                int slotIndex = linha * 7 + coluna;
                Transform slotTransform = boardPanel.GetChild(slotIndex);

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

                AtualizarTextoStatus();

                if (VerificarVitoria(linha, coluna, jogador))
                {
                    textoStatus.text = $"Jogador {ObterNomeJogador(jogador)} venceu!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    if (enviarParaRede)
                    {
                        EnviarMensagemRede($"VENCEU:{jogador}");
                    }
                    return;
                }

                if (VerificarEmpate())
                {
                    textoStatus.text = "Empate!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    if (enviarParaRede)
                    {
                        EnviarMensagemRede("EMPATE");
                    }
                    return;
                }

                if (enviarParaRede)
                {
                    EnviarJogadaRede(coluna);
                }

                jogadorAtual = 3 - jogadorAtual;
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

    void EnviarJogadaRede(int coluna)
    {
        if (jogadorLocal == 1)
        {
            tcpServer?.EnviarJogada(coluna);
        }
        else
        {
            tcpClient?.EnviarJogada(coluna);
        }
    }

    void EnviarMensagemRede(string mensagem)
    {
        if (jogadorLocal == 1)
        {
            tcpServer?.EnviarMensagem(mensagem);
        }
        else
        {
            tcpClient?.EnviarMensagem(mensagem);
        }
    }

    public void ProcessarMensagemRecebida(string mensagem)
    {
        if (mensagem.StartsWith("VENCEU:"))
        {
            string numJogadorStr = mensagem.Substring(7);
            if (int.TryParse(numJogadorStr, out int jogador))
            {
                textoStatus.text = $"Jogador {ObterNomeJogador(jogador)} venceu!";
                jogoAtivo = false;
                DesabilitarBotoes();
            }
        }
        else if (mensagem == "EMPATE")
        {
            textoStatus.text = "Empate!";
            jogoAtivo = false;
            DesabilitarBotoes();
        }
        else if (mensagem == "RESET")
        {
            ResetarTabuleiro();
        }
        else if (int.TryParse(mensagem, out int coluna))
        {
            FazerJogadaRemota(coluna);
        }
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
        grade = new int[6, 7];
        jogadorAtual = 1;
        jogoAtivo = true;

        foreach (Transform slot in boardPanel)
            foreach (Transform filho in slot)
                if (filho.CompareTag("Peca"))
                    Destroy(filho.gameObject);

        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        foreach (var btn in botoes)
            btn.interactable = true;

        AtualizarTextoStatus();
    }
    
    private string ObterNomeJogador(int jogador)
    {
        return jogador == 1 ? "Amarelo" : "Vermelho";
    }

    // Método para chamar via botão na UI para resetar e sincronizar
    public void BotaoResetar()
    {
        ResetarTabuleiro();
        EnviarMensagemRede("RESET");
    }
}
