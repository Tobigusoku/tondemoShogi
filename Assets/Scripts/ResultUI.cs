using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    public void OnClickRestart()
    {
        SceneManager.LoadScene("Title");
    }
}
