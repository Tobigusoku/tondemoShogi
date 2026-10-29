using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public CellGenerator cellGenerator;
    public GameObject resultPanel;

    // 持ち駒スロット管理
    private Dictionary<(Type pieceType, Team team), SlotController> slotMap;
    public bool isGameOver { get; private set; } = false;

    // 選択状態
    private GameObject selectedPieceObj = null;
    private PieceData selectedPieceData = null;
    private int selectedX = -1, selectedY = -1;
    private bool isCapturedPieceSelected = false;

    // 手番管理
    private Team currentTurn = Team.Black;
    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.HumanInput;
    public TextMeshProUGUI turnText;

    // ハイライト管理
    public MoveHighlighter moveHighlighter;
    public GameObject selectionHighlightPrefab;
    private GameObject currentSelectionHighlight;

    // SE管理
    public AudioClip pieceSE;
    public AudioClip explosionSE;
    private AudioSource audioSource;

    // 爆発エフェクト用Prefab
    public GameObject explosionEffectPrefab;

    // CPU用
    public CPUPlayer cpuPlayer;

    void Awake()
    {
        Instance = this;
        InitializeSlotMap();
        UpdateTurnUI();
        audioSource = GetComponent<AudioSource>();
    }

    void InitializeSlotMap()
    {
        slotMap = new Dictionary<(Type, Team), SlotController>();
        foreach (var slot in FindObjectsByType<SlotController>(FindObjectsSortMode.None))
        {
            slotMap[(slot.type, slot.team)] = slot;
            slot.UpdateCount(0);
        }
    }

    // 駒台から持ち駒を選択
    public void OnCapturedPieceSelected(Type type, Team team)
    {
        if (CurrentPhase != TurnPhase.HumanInput || isGameOver || team != currentTurn) return;
        if (!slotMap.TryGetValue((type, team), out var slot) || slot.CurrentCount <= 0) return;

        // キャンセル
        if (isCapturedPieceSelected && selectedPieceData != null &&
            selectedPieceData.type == type && selectedPieceData.team == team)
        {
            ClearSelection();
            return;
        }

        // 選択情報セット
        selectedPieceData = new PieceData(type, team);
        selectedPieceObj = null;
        selectedX = selectedY = -1;
        isCapturedPieceSelected = true;

        // ハイライト
        ShowSelectionHighlight(slot.highlightTarget.position);
        List<Vector2Int> placeable = GetPlaceableCells();
        var worldPositions = new List<Vector3>();
        foreach (var idx in placeable)
            worldPositions.Add(cellGenerator.GetCellAt(idx.x, idx.y).transform.position);
        moveHighlighter.ShowHighlights(worldPositions);
    }

    // セルクリック時の処理
    public void OnCellClicked(int x, int y)
    {
        if (CurrentPhase != TurnPhase.HumanInput || isGameOver) return;

        if (isCapturedPieceSelected)
        {
            // 駒台から配置
            HandleCapturedPiecePlacement(x, y);
        }
        else if (selectedPieceObj == null)
        {
            // 盤上駒の選択
            HandlePieceSelection(x, y);
        }
        else
        {
            // 移動・キャプチャ
            HandlePieceMoveOrCapture(x, y);
        }
    }

    // 持ち駒配置処理
    public void HandleCapturedPiecePlacement(int x, int y)
    {
        if (CurrentPhase != TurnPhase.HumanInput) return;
        if (!IsMyTurn(selectedPieceData.team)) return;
        if (cellGenerator.GetPieceDataAt(x, y) != null) return;
        if (selectedPieceData.type == Type.Fu && IsNifu(x, selectedPieceData.team)) return;

        CurrentPhase = TurnPhase.ResolvingMove;

        GameObject prefab = selectedPieceData.GetPrefab();
        GameObject pieceObj = Instantiate(prefab,
            cellGenerator.GetCellAt(x, y).transform.position,
            Quaternion.identity,
            cellGenerator.pieceRoot);

        cellGenerator.SetPieceDataAt(x, y, selectedPieceData);
        cellGenerator.GetCellAt(x, y).GetComponent<Cell>().currentPiece = pieceObj;

        // SE再生
        if (pieceSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(pieceSE);
        }

        if (slotMap.TryGetValue((selectedPieceData.type, selectedPieceData.team), out var slot))
            slot.UpdateCount(slot.CurrentCount - 1);

        ClearSelection();
        EndTurn();
    }

    // 盤上駒の選択
    public void HandlePieceSelection(int x, int y)
    {
        GameObject pieceObj = cellGenerator.GetPieceAt(x, y);
        PieceData pieceData = cellGenerator.GetPieceDataAt(x, y);
        if (pieceObj == null || pieceData.team != currentTurn) return;

        selectedPieceObj = pieceObj;
        selectedPieceData = pieceData;
        selectedX = x; selectedY = y;

        ShowSelectionHighlight(cellGenerator.GetCellAt(x, y).transform.position);
        List<Vector2Int> movable = GetMovableCells(x, y);
        var worldPositions = new List<Vector3>();
        foreach (var idx in movable)
            worldPositions.Add(cellGenerator.GetCellAt(idx.x, idx.y).transform.position);
        moveHighlighter.ShowHighlights(worldPositions);

        Debug.Log($"({x},{y}) の駒を選択しました");
    }

    // 移動・キャプチャ処理
    public void HandlePieceMoveOrCapture(int x, int y)
    {
        if (CurrentPhase != TurnPhase.HumanInput) return;
        if (!IsMyTurn(selectedPieceData.team))
        {
            ClearSelection();
            return;
        }
        if (x == selectedX && y == selectedY)
        {
            ClearSelection();
            return;
        }
        if (!CanMove(selectedX, selectedY, x, y, selectedPieceData)) return;

        CurrentPhase = TurnPhase.ResolvingMove;

        PieceData destData = cellGenerator.GetPieceDataAt(x, y);
        bool capturedBomb = destData != null && destData.type == Type.Bomb;
        if (capturedBomb)
        {
            ResolveBombCapture(selectedX, selectedY, x, y, selectedPieceObj);
        }
        else
        {
            if (destData != null && destData.team != selectedPieceData.team)
                CapturePiece(x, y, destData);

            selectedPieceObj.transform.position = cellGenerator.GetCellAt(x, y).transform.position;
            cellGenerator.MovePiece(selectedX, selectedY, x, y);
        }

        // SE再生
        if (pieceSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(pieceSE);
        }

        if (!capturedBomb && !isGameOver && CanPromote(selectedPieceData, selectedY, y))
            PromoteAt(x, y);

        ClearSelection();
        if (!isGameOver) EndTurn();
    }

    // 敵駒捕獲
    void CapturePiece(int x, int y, PieceData dest)
    {
        // 王の捕獲判定
        if (dest.type == Type.Ou)
        {
            HandleKingCapture(dest.team);
            cellGenerator.RemovePieceAt(x, y);
            return;
        }
        // その他の駒は持ち駒に追加
        Type capType = dest.isPromoted ? GetOriginalType(dest.type) : dest.type;
        if (selectedPieceData != null && !IsSpecialPiece(capType) &&
            slotMap.TryGetValue((capType, selectedPieceData.team), out var slot))
        {
            slot.UpdateCount(slot.CurrentCount + 1);
        }

        cellGenerator.RemovePieceAt(x, y);
    }

    // 移動可否判定
    bool CanMove(int fromX, int fromY, int toX, int toY, PieceData data)
    {
        if (data == null) return false;
        if (IsFriendlyPieceAt(toX, toY, data.team)) return false;
        if (data.type == Type.Assassin)
        {
            PieceData target = cellGenerator.GetPieceDataAt(toX, toY);
            if (target != null && target.type == Type.Ou && target.team != data.team)
                return false;
            return true;
        }
        return IsValidMove(fromX, fromY, toX, toY, data);
    }

    // 選択解除
    void ClearSelection()
    {
        selectedPieceObj = null;
        selectedPieceData = null;
        selectedX = selectedY = -1;
        isCapturedPieceSelected = false;
        moveHighlighter.ClearHighlights();
        if (currentSelectionHighlight != null) Destroy(currentSelectionHighlight);
    }

    // 選択中駒ハイライト表示
    void ShowSelectionHighlight(Vector3 pos)
    {
        if (selectionHighlightPrefab == null) return;
        if (currentSelectionHighlight != null) Destroy(currentSelectionHighlight);
        currentSelectionHighlight = Instantiate(selectionHighlightPrefab, pos, Quaternion.identity, cellGenerator.pieceRoot);
    }

    // 移動可能セル一覧取得
    public List<Vector2Int> GetMovableCells(int fromX, int fromY)
    {
        var list = new List<Vector2Int>();
        for (int x = 0; x < cellGenerator.width; x++)
            for (int y = 0; y < cellGenerator.height; y++)
                if (CanMove(fromX, fromY, x, y, selectedPieceData))
                    list.Add(new Vector2Int(x, y));
        return list;
    }
    public List<Vector2Int> GetMovableCells(int fromX, int fromY, PieceData data)
    {
        var old = selectedPieceData;
        selectedPieceData = data;
        var result = GetMovableCells(fromX, fromY);
        selectedPieceData = old;
        return result;
    }

    // 配置可能セル一覧取得
    public List<Vector2Int> GetPlaceableCells()
    {
        var list = new List<Vector2Int>();
        for (int x = 0; x < cellGenerator.width; x++)
            for (int y = 0; y < cellGenerator.height; y++)
                if (cellGenerator.GetPieceDataAt(x, y) == null &&
                    !(selectedPieceData.type == Type.Fu && IsNifu(x, selectedPieceData.team)))
                    list.Add(new Vector2Int(x, y));
        return list;
    }

    void ResolveBombCapture(int fromX, int fromY, int toX, int toY, GameObject movingPiece)
    {
        cellGenerator.RemovePieceAt(toX, toY);
        movingPiece.transform.position = cellGenerator.GetCellAt(toX, toY).transform.position;
        cellGenerator.MovePiece(fromX, fromY, toX, toY);
        TriggerExplosion(toX, toY);
    }

    // 爆発処理
    void TriggerExplosion(int x, int y)
    {
        var bombCell = cellGenerator.GetCellAt(x, y).GetComponent<Cell>();
        bool blackKingDestroyed = false;
        bool whiteKingDestroyed = false;

        var centerData = cellGenerator.GetPieceDataAt(x, y);
        if (centerData != null && centerData.type == Type.Ou)
        {
            if (centerData.team == Team.Black) blackKingDestroyed = true;
            else whiteKingDestroyed = true;
        }
        cellGenerator.RemovePieceAt(x, y);

        // 爆発エフェクト
        if (explosionEffectPrefab != null)
        {
            var effect = Instantiate(explosionEffectPrefab,
                                     bombCell.transform.position,
                                     Quaternion.identity);
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                Destroy(effect, main.duration + main.startLifetime.constantMax);
            }
            else Destroy(effect, 2f);
        }

        // SE再生
        if (explosionSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSE);
        }

        // 周囲8マスを道連れ
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= cellGenerator.width || ny < 0 || ny >= cellGenerator.height)
                    continue;

                var neighborData = cellGenerator.GetPieceDataAt(nx, ny);
                if (neighborData == null) continue;

                if (neighborData.type == Type.Ou)
                {
                    if (neighborData.team == Team.Black) blackKingDestroyed = true;
                    else whiteKingDestroyed = true;
                }

                cellGenerator.RemovePieceAt(nx, ny);
            }

        if (blackKingDestroyed && whiteKingDestroyed)
            CompleteGame("引き分け！");
        else if (blackKingDestroyed)
            HandleKingCapture(Team.Black);
        else if (whiteKingDestroyed)
            HandleKingCapture(Team.White);
    }

    // 王判定と結果表示
    bool HandleKingCapture(Team kingTeam)
    {
        // kingTeam の王が取られた -> opponent wins
        string result = kingTeam == Team.Black ? "あなたの負け…" : "あなたの勝ち！";
        CompleteGame(result);
        return true;
    }

    void CompleteGame(string result)
    {
        if (isGameOver) return;
        isGameOver = true;
        CurrentPhase = TurnPhase.GameOver;
        CancelInvoke(nameof(TriggerCPUMove));
        UpdateTurnUI();
        ShowResult(result);
    }

    bool IsMyTurn(Team team)
    {
        if (team != currentTurn)
        {
            Debug.Log($"違う手番です: 現在の手番は{currentTurn}です");
            return false;
        }
        return true;
    }

    bool IsFriendlyPieceAt(int x, int y, Team team)
    {
        var pd = cellGenerator.GetPieceDataAt(x, y);
        return pd != null && pd.team == team;
    }

    bool IsValidMove(int fromX, int fromY, int toX, int toY, PieceData data)
    {
        var delta = new Vector2Int(toX - fromX, toY - fromY);
        if (data.moveDirections.Contains(delta)) return true;
        foreach (var dir in data.longMoveDirections)
            if (IsSameDirection(delta, dir) && IsPathClear(fromX, fromY, delta, dir)) return true;
        return false;
    }

    bool IsSameDirection(Vector2Int delta, Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return false;
        if (dir.x != 0 && dir.y != 0) return delta.x * dir.y == delta.y * dir.x;
        if (dir.x != 0) return delta.y == 0 && delta.x * dir.x > 0;
        return delta.x == 0 && delta.y * dir.y > 0;
    }

    bool IsPathClear(int fromX, int fromY, Vector2Int delta, Vector2Int dir)
    {
        int steps = dir.x != 0 ? Mathf.Abs(delta.x / dir.x) : Mathf.Abs(delta.y / dir.y);
        for (int i = 1; i < steps; i++)
        {
            int cx = fromX + dir.x * i;
            int cy = fromY + dir.y * i;
            if (cx < 0 || cx >= cellGenerator.width || cy < 0 || cy >= cellGenerator.height) return false;
            if (cellGenerator.GetPieceDataAt(cx, cy) != null) return false;
        }
        return true;
    }

    bool IsNifu(int x, Team team)
    {
        for (int y = 0; y < cellGenerator.height; y++)
        {
            var pd = cellGenerator.GetPieceDataAt(x, y);
            if (pd != null && pd.type == Type.Fu && pd.team == team && !pd.isPromoted) return true;
        }
        return false;
    }

    void PromoteAt(int x, int y)
    {
        var data = cellGenerator.GetPieceDataAt(x, y);
        if (data == null) return;  // 安全対策

        data.Promote();

        var cell = cellGenerator.GetCellAt(x, y)?.GetComponent<Cell>();
        if (cell == null || cell.currentPiece == null) return;

        Destroy(cell.currentPiece);
        var newPiece = Instantiate(data.GetPrefab(), cell.transform.position, Quaternion.identity, cellGenerator.pieceRoot);
        cell.currentPiece = newPiece;

        cellGenerator.SetPieceDataAt(x, y, data);
    }


    void EndTurn()
    {
        if (isGameOver) return;
        currentTurn = currentTurn == Team.Black ? Team.White : Team.Black;
        Debug.Log($"手番が{currentTurn}に移りました");
        UpdateTurnUI();
        if (currentTurn == Team.White && cpuPlayer != null)
        {
            CurrentPhase = TurnPhase.CpuThinking;
            // 少し遅延させてCPUが自然に動くように
            Debug.Log("CPUターンなのでTriggerCPUMoveを呼びます");
            Invoke(nameof(TriggerCPUMove), 0.5f);
        }
        else
        {
            CurrentPhase = TurnPhase.HumanInput;
        }
    }

    void UpdateTurnUI()
    {
        if (turnText != null)
        {
            if (isGameOver) turnText.text = "対局終了";
            else turnText.text = currentTurn == Team.Black ? "あなたの番です" : "相手の番です";
        }
    }

    void ShowResult(string result)
    {
        var panel = Instantiate(resultPanel);
        var text = panel.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = result;
    }

    bool IsSpecialPiece(Type type)
    {
        return type == Type.Assassin || type == Type.Bomb;
    }

    Type GetOriginalType(Type promoted)
    {
        switch (promoted)
        {
            case Type.Tokin: return Type.Fu;
            case Type.Narigin: return Type.Gin;
            case Type.Ryu: return Type.Hisha;
            case Type.Uma: return Type.Kaku;
            default: return promoted;
        }
    }

    bool CanPromote(PieceData piece, int fromY, int toY)
    {
        if (!piece.CanPromote()) return false;
        bool fromInZone = (piece.team == Team.White && fromY <= 1)
                       || (piece.team == Team.Black && fromY >= cellGenerator.height - 2);
        bool toInZone = (piece.team == Team.White && toY <= 1)
                     || (piece.team == Team.Black && toY >= cellGenerator.height - 2);
        return fromInZone || toInZone;
    }

    // CPU用移動
    public void MovePieceByCPU(int fromX, int fromY, int toX, int toY)
    {
        if (isGameOver || CurrentPhase != TurnPhase.CpuThinking || currentTurn != Team.White) return;
        ClearSelection();
        var data = cellGenerator.GetPieceDataAt(fromX, fromY);
        if (data == null) return;
        var fromCell = cellGenerator.GetCellAt(fromX, fromY);
        var toCell = cellGenerator.GetCellAt(toX, toY);
        if (fromCell == null || toCell == null) return;
        var pieceObj = fromCell.GetComponent<Cell>().currentPiece;
        if (pieceObj == null) return;
        CurrentPhase = TurnPhase.ResolvingMove;

        // 捕獲
        var dest = cellGenerator.GetPieceDataAt(toX, toY);
        bool capturedBomb = dest != null && dest.type == Type.Bomb;
        if (capturedBomb)
        {
            ResolveBombCapture(fromX, fromY, toX, toY, pieceObj);
        }
        else
        {
            if (dest != null && dest.team != data.team)
                CapturePiece(toX, toY, dest);
            pieceObj.transform.position = toCell.transform.position;
            cellGenerator.MovePiece(fromX, fromY, toX, toY);
        }

        // SE
        if (pieceSE != null && audioSource != null)
            audioSource.PlayOneShot(pieceSE);
        // 成り
        if (!capturedBomb && !isGameOver && CanPromote(data, fromY, toY)) PromoteAt(toX, toY);
        // ターン終了
        if (!isGameOver) EndTurn();
    }

    public void TriggerCPUMove()
    {
        Debug.Log("[TriggerCPUMove]");
        if (cpuPlayer != null && CurrentPhase == TurnPhase.CpuThinking && !isGameOver)
        {
            cpuPlayer.MakeMove();
        }
    }

    public void HandleCpuNoLegalMove()
    {
        if (isGameOver || CurrentPhase != TurnPhase.CpuThinking || currentTurn != Team.White) return;
        CompleteGame("相手は動けません。引き分け！");
    }

    public Team GetCurrentTurn()
    {
        return currentTurn;
    }
}
