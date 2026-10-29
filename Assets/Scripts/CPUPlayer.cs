using System.Collections.Generic;
using UnityEngine;

public class CPUPlayer : MonoBehaviour
{
    public GameManager gameManager;

    /// <summary>
    /// CPUの一手を決定して実行
    /// </summary>
    public void MakeMove()
    {
        var gm = gameManager != null ? gameManager : GameManager.Instance;
        if (gm == null || gm.isGameOver || gm.CurrentPhase != TurnPhase.CpuThinking ||
            gm.GetCurrentTurn() != Team.White) return;

        List<(int fx, int fy, Vector2Int to)> attackMoves = new();
        List<(int fx, int fy, Vector2Int to)> candidateMoves = new();

        // 全駒の合法手を収集
        for (int fx = 0; fx < gm.cellGenerator.width; fx++)
        {
            for (int fy = 0; fy < gm.cellGenerator.height; fy++)
            {
                var pd = gm.cellGenerator.GetPieceDataAt(fx, fy);
                if (pd == null || pd.team != Team.White) continue;

                // pd を直接渡して合法手を取得
                var moves = gm.GetMovableCells(fx, fy, pd);
                if (moves == null || moves.Count == 0) continue;

                foreach (var move in moves)
                {
                    var target = gm.cellGenerator.GetPieceDataAt(move.x, move.y);
                    if (target != null && target.team == Team.Black)
                        attackMoves.Add((fx, fy, move));
                    else
                        candidateMoves.Add((fx, fy, move));
                }
            }
        }

        // 取れる駒があれば優先、なければランダム
        (int fx, int fy, Vector2Int to) choice;
        if (attackMoves.Count > 0)
            choice = attackMoves[Random.Range(0, attackMoves.Count)];
        else if (candidateMoves.Count > 0)
            choice = candidateMoves[Random.Range(0, candidateMoves.Count)];
        else
        {
            gm.HandleCpuNoLegalMove();
            return;
        }

        gm.MovePieceByCPU(choice.fx, choice.fy, choice.to.x, choice.to.y);
    }
}
