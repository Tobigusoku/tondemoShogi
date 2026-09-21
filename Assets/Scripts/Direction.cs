using UnityEngine;
using System.Collections.Generic;

public static class Direction
{
    public static readonly Vector2Int UpLeft = new Vector2Int(-1, 1);
    public static readonly Vector2Int UpRight = new Vector2Int(1, 1);
    public static readonly Vector2Int DownLeft = new Vector2Int(-1, -1);
    public static readonly Vector2Int DownRight = new Vector2Int(1, -1);

    public static List<Vector2Int> ForType(Type type, Team team, out bool canMoveMultiple, out List<Vector2Int> longMoveDirections)
    {
        canMoveMultiple = false;
        longMoveDirections = new List<Vector2Int>();
        switch (type)
        {
            case Type.Fu:
                return new List<Vector2Int> { team == Team.Black ? Vector2Int.up : Vector2Int.down };

            case Type.Ou:
                return new List<Vector2Int> {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    UpLeft, UpRight, DownLeft, DownRight
                };

            case Type.Hisha:
                canMoveMultiple = true;
                longMoveDirections = new List<Vector2Int> {
                    Vector2Int.up, Vector2Int.down,
                    Vector2Int.left, Vector2Int.right
                };
                return new List<Vector2Int>();

            case Type.Kaku:
                canMoveMultiple = true;
                longMoveDirections = new List<Vector2Int> {
                    UpLeft, UpRight, DownLeft, DownRight
                };
                return new List<Vector2Int>();

            case Type.Kin:
                return team == Team.Black
                    ? new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.left, Vector2Int.right,
                        UpLeft, UpRight, Vector2Int.down
                    }
                    : new List<Vector2Int>
                    {
                        Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        DownLeft, DownRight, Vector2Int.up
                    };

            case Type.Gin:
                return team == Team.Black
                    ? new List<Vector2Int>
                    {
                        Vector2Int.up, UpLeft, UpRight,
                        DownLeft, DownRight
                    }
                    : new List<Vector2Int>
                    {
                        Vector2Int.down, DownLeft, DownRight,
                        UpLeft, UpRight
                    };

            // 成り駒: と金, 成銀, 竜, 馬
            case Type.Tokin:
            case Type.Narigin:
                return team == Team.Black
                    ? new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.left, Vector2Int.right,
                        UpLeft, UpRight, Vector2Int.down
                    }
                    : new List<Vector2Int>
                    {
                        Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        DownLeft, DownRight, Vector2Int.up
                    };

            case Type.Ryu:
                canMoveMultiple = true;
                // 縦横を long（何マスでもOK）
                longMoveDirections = new List<Vector2Int>
                {
                    Vector2Int.up, Vector2Int.down,
                    Vector2Int.left, Vector2Int.right
                };
                // 対角は1マスだけ
                return new List<Vector2Int> { UpLeft, UpRight, DownLeft, DownRight };

            case Type.Uma:
                canMoveMultiple = true;
                longMoveDirections = new List<Vector2Int>
                {
                    UpLeft, UpRight, DownLeft, DownRight
                };
                return new List<Vector2Int>
                {
                    Vector2Int.up, Vector2Int.down,
                    Vector2Int.left, Vector2Int.right
                };
            case Type.Assassin:
                return new List<Vector2Int> { };
            case Type.Bomb:
                return new List<Vector2Int> { Vector2Int.up, UpLeft, UpRight };

            default:
                return new List<Vector2Int>();
        }
    }
}
