using UnityEngine;

public class ClearStatus : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static ClearStatus Instance { get; private set; }
    #endregion

    #region private変数
    private bool gameClear; //ゲームクリアしているかどうか

    #endregion

    #region Set関数
    /// <summary>
    /// ゲームクリアしているかどうか
    /// </summary>
    /// <param name="clear">クリア状況</param>
    public void SetGameClear(bool clear)
    {
        gameClear = clear;
    }

    #endregion

    #region Get関数
    /// <summary>
    /// クリア状況取得
    /// </summary>
    /// <returns>クリア状況</returns>
    public bool GetGameClear()
    {
        return gameClear;
    }
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

    #endregion
}
