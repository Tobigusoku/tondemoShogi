using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;


public class SlotController : MonoBehaviour
{
    [Header("スロット情報")]
    public Type type;
    public Team team;

    [Header("参照設定")]
    [SerializeField] private GameObject slotContainer;
    [SerializeField] private TextMeshPro slotCountText;

    public Transform highlightTarget;

    private int count;
    public int CurrentCount { get; private set; }

    void Start()
    {
        if (CurrentCount == 0)
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateCount(int newCount)
    {
        CurrentCount = newCount;
        slotCountText.text = CurrentCount.ToString();

        // 持ち駒が 0 以下なら非表示、1 以上なら表示
        gameObject.SetActive(CurrentCount > 0);
    }
}
