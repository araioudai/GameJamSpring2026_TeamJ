using UnityEngine;
using UnityEngine.SceneManagement;

public class StageIndex : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static StageIndex Instance { get; private set; }
    #endregion

    #region private変数
    private int stageIndex;      //ステージ番号
    private bool isFirst = true; //最初はフェード処理しないフラグ
    #endregion

    #region Set関数
    /// <summary>
    /// ステージ番号セット
    /// </summary>
    /// <param name="index">ステージ番号</param>
    public void SetIndex(int index) { stageIndex = index; }

    /// <summary>
    /// ステージ番号を次へ（次のステージへなど）
    /// </summary>
    /// <param name="index">ステージ番号</param>
    public void SetNextIndex(int index) { stageIndex += index; if (stageIndex > 14) stageIndex = 1; }

    /// <summary>
    /// ステージ番号を前へ
    /// </summary>
    /// <param name="index">ステージ番号</param>
    public void SetBeforeIndex(int index) { stageIndex -= index; if (stageIndex < 1) stageIndex = 14; }

    #endregion

    #region Get関数
    /// <summary>
    /// ステージ番号入手用
    /// </summary>
    /// <returns>ステージ番号</returns>
    public int GetIndex() { return stageIndex; }

    /// <summary>
    /// ゲーム開始時点かどうか
    /// </summary>
    /// <returns>最初</returns>
    public bool GetIsFirst() { return isFirst; }
    #endregion

    #region Unityイベント関数
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }
    #endregion

    #region Start呼び出し関数
    void Init()
    {
    }
    #endregion
}
