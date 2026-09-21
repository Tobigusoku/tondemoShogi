using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public void OnStartButtonPressed()
    {
        SceneManager.LoadScene("Game");
    }

    public void OnMenuButtonPressed()
    {
        SceneManager.LoadScene("Menu");
    }
}
