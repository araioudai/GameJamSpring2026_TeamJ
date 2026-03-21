using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static ResultManager Instance { get; private set; }
    #endregion

    #region private変数

    [Header("ゲームクリア")]
    [SerializeField] private GameObject GameClear;
    [Header("ゲームオーバー")]
    [SerializeField] private GameObject GameOver;
    [Header("Maskデータ")]
    [SerializeField] private MaskData data;
    [Header("Mask用キャンバスをセット")]
    [SerializeField] private GameObject canvasMask;

    private UIMaskFader fade;                       //フェード用スクリプト

    #endregion

    #region Unityイベント関数
    private void Awake()
    {
        //シングルトン管理
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); //既にInstanceがあれば自分を破棄
            return;
        }
        Instance = this;

        //キャンバスの子としてプレハブを生成
        //data.panelMaskは画像の一番上の「Unmasked Panel」プレハブを指すと想定
        GameObject maskRoot = Instantiate(data.panelMask, canvasMask.transform);

        //描画順を一番手前に
        maskRoot.transform.SetAsLastSibling();

        fade = maskRoot.GetComponent<UIMaskFader>();
    }

    void Start()
    {
        Init();

        LiftFade();
    }

    #endregion

    #region Start呼び出し関数
    /// <summary>
    /// 初期化
    /// </summary>
    void Init()
    {
        if (ClearStatus.Instance.GetGameClear())
        {
            GameClear.SetActive(true);
            GameOver.SetActive(false);
        }
        else
        {
            GameClear.SetActive(false);
            GameOver.SetActive(true);
        }
    }

    #region マスク処理
    /// <summary>
    /// フェードイン処理
    /// </summary>
    /// <returns></returns>
    private void LiftFade()
    {
        //広がるアニメーション
        StartCoroutine(fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN)));
    }
    #endregion

    #endregion

    #region ボタン押下時の処理
    public void PushTitle()
    {
        SoundManager.Instance.SePlay(0);

        // 1. フェードアウト（画面を閉じる）を開始
        // 2. 第二引数のラムダ式は、アニメーション終了後に実行される
        fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(LoadTitle());
        });
    }

    public void PushGame()
    {
        SoundManager.Instance.SePlay(0);

        // 1. フェードアウト（画面を閉じる）を開始
        // 2. 第二引数のラムダ式は、アニメーション終了後に実行される
        fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(LoadGame());
        });
    }

    private IEnumerator LoadTitle()
    {
        //SoundManager.Instance.PlaySE(0);

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("TitleScene");
    }

    private IEnumerator LoadGame()
    {
        //SoundManager.Instance.PlaySE(0);

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }
    #endregion
}
