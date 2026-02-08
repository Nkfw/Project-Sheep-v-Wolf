using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("BaseScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
