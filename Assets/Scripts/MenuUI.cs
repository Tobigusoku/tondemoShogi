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
        Debug.Log("銀を選択しました");
    }

    public void OnSelectAssassin()
    {
        PlayerPrefs.SetString("SpecialPiece", "Assassin");
        Debug.Log("暗殺者を選択しました");
    }

    public void OnSelectBomb()
    {
        PlayerPrefs.SetString("SpecialPiece", "Bomb");
        Debug.Log("爆弾を選択しました");
    }
}
