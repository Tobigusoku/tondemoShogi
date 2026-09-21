using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    public void OnClickTitle()
    {
        SceneManager.LoadScene("Title");
    }

    public void OnSelectSilver()
    {
        PlayerPrefs.SetString("SpecialPiece", "Gin");
        Debug.Log("‹â‚ð‘I‘ð‚µ‚Ü‚µ‚½");
    }

    public void OnSelectAssassin()
    {
        PlayerPrefs.SetString("SpecialPiece", "Assassin");
        Debug.Log("ˆÃŽEŽÒ‚ð‘I‘ð‚µ‚Ü‚µ‚½");
    }

    public void OnSelectBomb()
    {
        PlayerPrefs.SetString("SpecialPiece", "Bomb");
        Debug.Log("”š’e‚ð‘I‘ð‚µ‚Ü‚µ‚½");
    }
}
