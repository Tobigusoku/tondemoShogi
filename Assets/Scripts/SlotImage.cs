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
        Debug.Log($"‚¿‹î‚Ì{type.ToString()}‚ª‘I‘ğ‚³‚ê‚Ü‚µ‚½B");
        GameManager.Instance.OnCapturedPieceSelected(type, team);
    }
}
