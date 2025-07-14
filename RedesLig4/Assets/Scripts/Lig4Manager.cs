using UnityEngine;
using UnityEngine.UI;

public class Lig4Manager : MonoBehaviour
{
    public GameObject pecaJogador1;     // Prefab da peça vermelha
    public GameObject pecaJogador2;     // Prefab da peça amarela
    public Transform boardPanel;        // Painel com os 42 slots
    public Text textoStatus;            // Texto na tela que mostra o turno ou resultado

    private int[,] grade = new int[6, 7];  // Matriz do tabuleiro
    private int jogadorAtual = 1;
    private bool jogoAtivo = true;

    void Start()
    {
        ResetarTabuleiro();  // Começa o jogo limpo
    }

    void ConfigurarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < botoes.Length; i++)
        {
            int index = i; // Importante capturar valor local
            botoes[i].onClick.RemoveAllListeners();
            botoes[i].onClick.AddListener(() => OnSlotClick(index));
            botoes[i].interactable = true;
        }
    }

    void OnSlotClick(int index)
    {
        if (!jogoAtivo) return;

        int coluna = index % 7;

        for (int linha = 5; linha >= 0; linha--)
        {
            if (grade[linha, coluna] == 0)
            {
                grade[linha, coluna] = jogadorAtual;

                int slotIndex = linha * 7 + coluna;
                Transform slotTransform = boardPanel.GetChild(slotIndex);

                GameObject peca = Instantiate(
                    jogadorAtual == 1 ? pecaJogador1 : pecaJogador2,
                    slotTransform
                );

                peca.transform.localPosition = Vector3.zero;
                peca.transform.localRotation = Quaternion.identity;
                peca.transform.localScale = Vector3.one;

                Button btn = slotTransform.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = false;

                if (VerificarVitoria(linha, coluna, jogadorAtual))
                {
                    textoStatus.text = $"Jogador {ObterNomeJogador(jogadorAtual)} venceu!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                if (VerificarEmpate())
                {
                    textoStatus.text = "Empate!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                jogadorAtual = 3 - jogadorAtual; // troca jogador
                AtualizarTextoStatus();
                return;
            }
        }

        Debug.Log("Coluna cheia!");
    }

    void AtualizarTextoStatus()
    {
        textoStatus.text = $"Vez do Jogador {ObterNomeJogador(jogadorAtual)}";
    }

    void DesabilitarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        foreach (var btn in botoes)
        {
            btn.interactable = false;
        }
    }

    bool VerificarEmpate()
    {
        for (int linha = 0; linha < 6; linha++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (grade[linha, col] == 0)
                    return false;
            }
        }
        return true;
    }

    bool VerificarVitoria(int linha, int coluna, int jogador)
    {
        int[][] direcoes = new int[][]
        {
            new int[] {0, 1},   // Horizontal
            new int[] {1, 0},   // Vertical
            new int[] {1, 1},   // Diagonal principal
            new int[] {1, -1}   // Diagonal inversa
        };

        foreach (var dir in direcoes)
        {
            int contagem = 1;
            contagem += ContarNaDirecao(linha, coluna, dir[0], dir[1], jogador);
            contagem += ContarNaDirecao(linha, coluna, -dir[0], -dir[1], jogador);

            if (contagem >= 4)
                return true;
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
        Debug.Log("Reiniciando o jogo...");

        grade = new int[6, 7];
        jogadorAtual = 1;
        jogoAtivo = true;

        // Apaga as peças (tag "Peca")
        foreach (Transform slot in boardPanel)
        {
            foreach (Transform filho in slot)
            {
                if (filho.CompareTag("Peca"))
                {
                    Destroy(filho.gameObject);
                }
            }
        }

        // Reativa botões
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        foreach (var btn in botoes)
        {
            btn.interactable = true;
        }

        AtualizarTextoStatus();
        ConfigurarBotoes();
    }

    public void FazerJogadaRemota(int coluna)
    {
        if (!jogoAtivo) return;

        for (int linha = 5; linha >= 0; linha--)
        {
            if (grade[linha, coluna] == 0)
            {
                grade[linha, coluna] = jogadorAtual;

                int slotIndex = linha * 7 + coluna;
                Transform slotTransform = boardPanel.GetChild(slotIndex);

                GameObject peca = Instantiate(
                    jogadorAtual == 1 ? pecaJogador1 : pecaJogador2,
                    slotTransform
                );

                peca.transform.localPosition = Vector3.zero;
                peca.transform.localRotation = Quaternion.identity;
                peca.transform.localScale = Vector3.one;

                Button btn = slotTransform.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = false;

                if (VerificarVitoria(linha, coluna, jogadorAtual))
                {
                    textoStatus.text = $"Jogador {ObterNomeJogador(jogadorAtual)} venceu!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                if (VerificarEmpate())
                {
                    textoStatus.text = "Empate!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                jogadorAtual = 3 - jogadorAtual;
                AtualizarTextoStatus();
                return;
            }
        }

        Debug.Log("Coluna cheia (remoto).");
    }

    private string ObterNomeJogador(int jogador)
    {
        return jogador == 1 ? "Vermelho" : "Amarelo";
    }
}
