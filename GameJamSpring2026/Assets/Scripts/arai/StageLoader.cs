using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageLoader : MonoBehaviour
{
    #region 列挙対
    private enum StageObj
    {
        FLOOR,     //床
        WALL,      //壁
        GOAL,      //ゴール
        PLAYER,    //プレイヤー
        CARDBOARD, //段ボール
        SOFA,      //ソファー
        BED,       //ベッド


        MAX        //最大数
    }

    #endregion
    #region private変数
    [Header("マップ関連")]
    [SerializeField] private Tilemap floorTilemap; //地面用Tilemap
    [SerializeField] private Tilemap wallTilemap;  //壁用Tilemap
    [SerializeField] private TileBase floorTile;   //木床タイル
    [SerializeField] private TileBase wallTile;    //石床タイル

    [Header("生成オブジェクト")]
    [SerializeField, EnumIndex(typeof(StageObj))]
    private GameObject[] stageObj = new GameObject[(int)StageObj.MAX];

    [Header("判定用グリッド")]
    [SerializeField] private StageGrid stageGrid;

    private int stageIndex;
    #endregion

    #region public変数
    public string csvFileName;                     //csvファイル名
    #endregion

    #region Unityイベント関数

    // Start is called before the first frame update
    void Start()
    {
        Init();
        //Debug.Log(stageIndex);
    }

    #endregion

    #region Start呼び出し関数

    #region 初期化
    void Init()
    {
        stageIndex = 1/*StageIndex.Instance.GetIndex()*/;
        csvFileName = "CSV/Stage" + stageIndex;
        LoadMapFromCSV(csvFileName);
    }
    #endregion

    #region ステージ読み込み
    /// <summary>
    /// CSVの情報からステージを読み込む
    /// </summary>
    /// <param name="fileName">CSVファイル名</param>
    void LoadMapFromCSV(string fileName)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);
        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つかりません: " + fileName);
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        string[] lines = csvFile.text.Trim().Split('\n');
        int[,] mapData = new int[lines.Length, lines[0].Trim().Split(',').Length];

        if (stageGrid == null)
        {
            stageGrid = FindFirstObjectByType<StageGrid>();
        }

        if (stageGrid != null)
        {
            stageGrid.SetMapData(mapData);
        }

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        //カメラの縦方向の半分の長さ（orthographicSize は縦の半分を表す）
        float camHalfHeight = cam.orthographicSize;

        //カメラの横方向の半分の長さ（縦の半分 × アスペクト比 = 横の半分）
        float camHalfWidth = camHalfHeight * cam.aspect;

        //カメラのワールド座標を基準に「画面の左上ワールド座標」を求める
        Vector3 topLeftWorld = cam.transform.position;
        topLeftWorld.x -= camHalfWidth;  //中心から左へ移動
        topLeftWorld.y += camHalfHeight; //中心から上へ移動

        //左上のワールド座標をタイルマップのセル座標に変換
        Vector3Int topLeftCell = floorTilemap.WorldToCell(topLeftWorld);

        //タイル配置が 1 行分ずれるので補正（Unity の座標系と CSV の行の対応差を修正）
        topLeftCell.y -= 1;

        for (int y = 0; y < lines.Length; y++)
        {
            string[] values = lines[y].Trim().Split(',');

            for (int x = 0; x < values.Length; x++)
            {
                if (int.TryParse(values[x], out int value))
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    mapData[y, x] = ConvertToMapCellValue(value);

                    //タイルの描画場所
                    Vector3Int cellPos = new Vector3Int(topLeftCell.x + x, topLeftCell.y - y, 0);
                    //タイルと同じ描画だとずれるからその分ずらす
                    Vector3 objPos = new Vector3(topLeftCell.x + x + 0.5f, topLeftCell.y - y + 0.5f, 0);

                    switch (value)
                    {
                        case (int)StageObj.FLOOR:
                            floorTilemap.SetTile(cellPos, floorTile); //床タイル
                            break;
                        case (int)StageObj.WALL:
                            wallTilemap.SetTile(cellPos, wallTile);   //壁タイル
                            break;
                        case (int)StageObj.GOAL:
                        case (int)StageObj.PLAYER: 
                        case (int)StageObj.CARDBOARD:
                        case (int)StageObj.SOFA:
                        case (int)StageObj.BED:
                            //最初に床を置く
                            floorTilemap.SetTile(cellPos, floorTile); //床タイル

                            //セットされているなら指定オブジェクトを生成
                            SpawnStageObject(value, objPos, gridPos);

                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 指定した場所に指定オブジェクトを生成する
    /// </summary>
    /// <param name="objIndex">生成するオブジェクト</param>
    /// <param name="position">生成する場所</param>
    /// <param name="gridPos">CSV上のグリッド座標</param>
    void SpawnStageObject(int objIndex, Vector3 position, Vector2Int gridPos)
    {
        //配列の範囲内かチェック
        //インスペクターで中身がセットされているかチェック
        if (objIndex >= 0 && objIndex < stageObj.Length && stageObj[objIndex] != null)
        {
            GameObject spawnedObject = Instantiate(stageObj[objIndex], position, Quaternion.identity);

            if (spawnedObject.TryGetComponent(out FurnitureTurn furnitureTurn))
            {
                InitializeFurniture(furnitureTurn, gridPos);
            }
        }
        else
        {
            //セットされていない場合は何もしない
        }
    }

    int ConvertToMapCellValue(int value)
    {
        switch (value)
        {
            case (int)StageObj.WALL:
                return 1;

            case (int)StageObj.GOAL:
                return 2;

            default:
                return 0;
        }
    }

    void InitializeFurniture(FurnitureTurn furnitureTurn, Vector2Int gridPos)
    {
        furnitureTurn.SetGridPosition(gridPos);

        if (stageGrid == null)
        {
            return;
        }

        furnitureTurn.SetStageGrid(stageGrid);
        stageGrid.RegisterFurniture(furnitureTurn);
    }
    #endregion

    #endregion
}
