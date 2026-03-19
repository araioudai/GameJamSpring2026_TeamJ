using UnityEngine;
using UnityEngine.SceneManagement;

public class StageIndex : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static StageIndex Instance { get; private set; }
    #endregion

    #region private変数
    private int stageIndex; //ステージ番号
    private bool firstTime; //最初のプレイだったらチュートリアル
    #endregion

    #region Set関数
    //ステージ番号セット
    public void SetIndex(int index) { stageIndex = index; }

    //ステージ番号を次へ（ランキングパネルや次のステージへなど）
    public void SetNextIndex(int index) { stageIndex += index; if (stageIndex > 14) stageIndex = 1; }

    //ステージ番号を前へ（ランキングパネルなど）
    public void SetBeforeIndex(int index) { stageIndex -= index; if (stageIndex < 1) stageIndex = 14; }

    //最初のプレイかどうかセット用
    public void SetFirst(bool first) { firstTime = first; }
    #endregion

    #region Get関数
    //ステージ番号入手用
    public int GetIndex() { return stageIndex; }

    //最初のプレイかどうか入手用
    public bool GetFirst() {  return firstTime; }

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
        if (SceneManager.GetActiveScene().name == "TitleScene")
        {
            firstTime = false;
        }
    }
    #endregion
}
