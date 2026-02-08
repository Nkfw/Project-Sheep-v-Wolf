using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [SerializeField] WolfHealth wolfHealth;

    private void Start()
    {
        Debug.Log("GameOver Start, wolfHealth = " + wolfHealth);
        wolfHealth.OnWolfDied += HandleGameOver;
    }

    private void OnDestroy()
    {
        
        wolfHealth.OnWolfDied -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        Debug.Log("Game Over! The wolf has died.");
        SceneManager.LoadScene("MainMenu");
    }

}
