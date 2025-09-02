using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Top Bar")]
    public TMP_Text wordText;
    public TMP_Text roundText;
    public TMP_Text scoreText;
    public TMP_Text mistakesText;
    public TMP_Text timerText;

    [Header("Panels")]
    public GameObject winPanel;
    public TMP_Text finalScoreWinText;
    public GameObject gameOverPanel;
    public TMP_Text finalScoreLoseText;

    public void SetWord(string word) => wordText.text = word;
    public void SetRound(int current, int total) => roundText.text = $"Ronda: {current}/{total}";
    public void SetScore(int score) => scoreText.text = $"Puntaje: {score}";
    public void SetMistakes(int mistakes, int max) => mistakesText.text = $"Fallos: {mistakes}/{max}";
    public void SetTimer(float seconds) => timerText.text = $"Tiempo: {seconds:0.00}s";

    public void ShowWin(int finalScore)
    {
        winPanel.SetActive(true);
        if (finalScoreWinText != null) finalScoreWinText.text = $"¡Ganaste!\nPuntaje: {finalScore}";
    }

    public void ShowGameOver(int finalScore)
    {
        gameOverPanel.SetActive(true);
        if (finalScoreLoseText != null) finalScoreLoseText.text = $"Game Over\nPuntaje: {finalScore}";
    }
}
