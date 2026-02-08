using UnityEngine;
using System;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int currentScore = 0;  
    public TextMeshProUGUI scoreText;
    public event Action<int, int> OnScoreChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ScoreManager instance set.");
        }
    }

    private void Start()
    {
        scoreText.text = "Score: " + currentScore.ToString();
    }

    public void AddScore(int points)
    {
        int oldScore = currentScore;
        currentScore += points;
        scoreText.text = "Score: " + currentScore.ToString();
        OnScoreChanged?.Invoke(oldScore, currentScore);
    }
}