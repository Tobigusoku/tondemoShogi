using UnityEngine;

public class SlotImage : MonoBehaviour
{
    private Type type;
    private Team team;

    void Start()
    {
        var slot = GetComponentInParent<SlotController>();
        type = slot.type;
        team = slot.team;
    }

    void OnMouseDown()
    {
        Debug.Log($"持ち駒の{type.ToString()}が選択されました。");
        GameManager.Instance.OnCapturedPieceSelected(type, team);
    }
}
