using UnityEngine;
using System.Collections.Generic;

public class PieceData
{
    public Type type;
    public Type promotedType;
    public Team team;
    public List<Vector2Int> moveDirections;         // 1マスだけ進める方向
    public List<Vector2Int> longMoveDirections;     // 飛車・角のように何マスでも進める方向
    public bool canMoveMultiple = false; // 線で動ける駒か
    public bool isPromoted; // 成っているか

    public PieceData(Type type, Team team)
    {
        this.type = type;
        this.promotedType = GetPromotedType(type);
        this.team = team;
        moveDirections = Direction.ForType(type, team, out canMoveMultiple, out longMoveDirections);
        isPromoted = false;
    }

    Type GetPromotedType(Type baseType)
    {
        switch (baseType)
        {
            case Type.Fu: return Type.Tokin;
            case Type.Gin: return Type.Narigin;
            case Type.Hisha: return Type.Ryu;
            case Type.Kaku: return Type.Uma;
            default: return Type.None; // 成れない駒
        }
    }

    public GameObject GetPrefab()
    {
        string prefabName = $"{team.ToString().ToLower()}_{type.ToString().ToLower()}";
        return Resources.Load<GameObject>($"Pieces/{prefabName}");
    }

    public bool CanPromote()
    {
        // そもそも歩・銀・角・飛だけが成れる
        return type == Type.Fu || type == Type.Gin || type == Type.Kaku || type == Type.Hisha;
    }

    public void Promote()
    {
        if (!isPromoted && promotedType != Type.None)
        {
            type = promotedType;
            isPromoted = true;
            moveDirections = Direction.ForType(type, team, out canMoveMultiple, out longMoveDirections);
        }
    }
}
