using UnityEngine;
using System.Collections.Generic;

public class MoveHighlighter : MonoBehaviour
{
    public GameObject highlightPrefab;
    private List<GameObject> currentHighlights = new List<GameObject>();

    /// <summary>
    /// ワールド座標リストでハイライト表示
    /// </summary>
    public void ShowHighlights(List<Vector3> worldPositions)
    {
        ClearHighlights();
        if (highlightPrefab == null)
        {
            Debug.LogWarning("MoveHighlighter: highlightPrefab が設定されていません");
            return;
        }
        foreach (var pos in worldPositions)
        {
            var go = Instantiate(highlightPrefab, pos, Quaternion.identity);
            currentHighlights.Add(go);
        }
    }

    public void ClearHighlights()
    {
        foreach (var go in currentHighlights)
            Destroy(go);
        currentHighlights.Clear();
    }
}

