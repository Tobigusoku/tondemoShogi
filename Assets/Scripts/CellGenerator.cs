using UnityEngine;

public class CellGenerator : MonoBehaviour
{
    public int width = 5;
    public int height = 6;

    public GameObject cellPrefab;
    private readonly GameObject[,] cellArray = new GameObject[5, 6];
    private PieceData[,] ban56 = new PieceData[5, 6];

    public Transform cellRoot;
    public Transform pieceRoot;

    void Start()
    {
        InitializeBoard();
        GenerateCells();
        PlacePieces();
    }

    void InitializeBoard()
    {
        // 選ばれた特殊駒を取得（デフォルトは銀）
        string selected = PlayerPrefs.GetString("SpecialPiece", "Gin");
        Type specialType;
        switch (selected)
        {
            case "Assassin":
                specialType = Type.Assassin;
                break;
            case "Bomb":
                specialType = Type.Bomb;
                break;
            // 他に追加したい駒があればここに case を足す
            default:
                specialType = Type.Gin;
                break;
        }

        // 歩
        for (int x = 0; x < width; x++)
        {
            ban56[x, 1] = new PieceData(Type.Fu, Team.Black);
            ban56[x, 4] = new PieceData(Type.Fu, Team.White);
        }

        // 王
        ban56[2, 0] = new PieceData(Type.Ou, Team.Black);
        ban56[2, 5] = new PieceData(Type.Ou, Team.White);

        // 金
        ban56[1, 0] = new PieceData(Type.Kin, Team.Black);
        ban56[3, 5] = new PieceData(Type.Kin, Team.White);

        // 角
        ban56[0, 0] = new PieceData(Type.Kaku, Team.Black);
        ban56[4, 5] = new PieceData(Type.Kaku, Team.White);

        // 飛車
        ban56[4, 0] = new PieceData(Type.Hisha, Team.Black);
        ban56[0, 5] = new PieceData(Type.Hisha, Team.White);

        // 銀（相手）
        ban56[1, 5] = new PieceData(Type.Gin, Team.White);

        // 銀 or 暗殺者 or 爆弾（選択制）
        ban56[3, 0] = new PieceData(specialType, Team.Black);
    }


    void GenerateCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(
                    -2.53f + 1.25f * x,
                    -3.475f + 1.39f * y,
                    0f
                );
                GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                cell.transform.SetParent(cellRoot, worldPositionStays: true);
                cell.name = $"Cell_{x}_{y}";

                cellArray[x, y] = cell;
                var cs = cell.GetComponent<Cell>();
                if (cs != null) cs.Init(x, y);
            }
        }
    }

    void PlacePieces()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (ban56[x, y] != null)
                {
                    var cellObj = cellArray[x, y];
                    var cs = cellObj.GetComponent<Cell>();

                    var piece = Instantiate(ban56[x, y].GetPrefab(),
                                             cellObj.transform.position,
                                             Quaternion.identity,
                                             pieceRoot);
                    cs.currentPiece = piece;
                }
            }
    }

    public GameObject GetCellAt(int x, int y)
    {
        return cellArray[x, y];
    }

    public GameObject GetPieceAt(int x, int y)
    {
        return cellArray[x, y].GetComponent<Cell>().currentPiece;
    }

    public PieceData GetPieceDataAt(int x, int y)
    {
        return ban56[x, y];
    }

    public void SetPieceDataAt(int x, int y, PieceData data)
    {
        ban56[x, y] = data;
    }

    public void MovePiece(int oldX, int oldY, int newX, int newY)
    {
        var oldCell = cellArray[oldX, oldY].GetComponent<Cell>();
        var newCell = cellArray[newX, newY].GetComponent<Cell>();

        var data = ban56[oldX, oldY];
        ban56[oldX, oldY] = null;
        ban56[newX, newY] = data;

        var piece = oldCell.currentPiece;
        oldCell.currentPiece = null;
        newCell.currentPiece = piece;
    }

    public void RemovePieceAt(int x, int y)
    {
        var cell = cellArray[x, y].GetComponent<Cell>();
        if (cell.currentPiece != null)
        {
            Destroy(cell.currentPiece);
            cell.currentPiece = null;
        }
        ban56[x, y] = null;
    }
}
