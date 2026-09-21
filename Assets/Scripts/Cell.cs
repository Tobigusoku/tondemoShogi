using UnityEngine;

public class Cell : MonoBehaviour
{
    public int x;
    public int y;
    public GameObject currentPiece;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        currentPiece = null;
    }

    void OnMouseDown()
    {
        Debug.Log($"Cell clicked: ({x}, {y})");
        GameManager.Instance.OnCellClicked(x, y);
    }
}
