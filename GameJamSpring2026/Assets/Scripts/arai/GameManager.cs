using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static GameManager Instance { get; private set; }
    #endregion

    #region private変数

    [Header("Maskデータ")]
    [SerializeField] private MaskData data;
    [Header("Mask用キャンバスをセット")]
    [SerializeField] private GameObject canvasMask;
    [Header("カウントダウン用のテキスト")]
    [SerializeField] private Text countdownText;            //カウントダウン用テキスト

    private UIMaskFader fade;                               //フェード用スクリプト
    private bool isStart;                                   //ゲームがスタートしているかどうか
    private bool gameClear;                                 //ゲームクリアフラグ
    private int objIndex;                                   //ステージで生成したオブジェクト数
    private Vector2 initialCountdownPos;                    //カウントダウン用テキストの元の位置を保存する変数
    #endregion

    #region Set関数
    /// <summary>
    /// 生成した家具のオブジェクト数をセット
    /// </summary>
    /// <returns>家具のオブジェクト数</returns>
    public void SetObjIndex(int _objIndex)
    {
        objIndex = _objIndex;
    }

    /// <summary>
    /// 運び終えた時減らす処理
    /// </summary>
    /// <param name="value">運んだ個数</param>
    public void SetObjMinus(int value)
    {
        objIndex -= value;
    }

    #endregion

    #region Get関数
    /// <summary>
    /// 現在スタートしているかどうか
    /// </summary>
    /// <returns>スタート状況</returns>
    public bool GetIsStart() { return isStart; }

    #endregion

    #region Unityイベント関数
    void Awake()
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //初期化
        Init();

        //フェード処理
        MaskFade();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStart) { return; }

        GameJudge();
        GameClear();
    }

    #endregion

    #region Start呼び出し関数

    #region 初期化
    void Init()
    {
        isStart = false;
        gameClear = false;
        objIndex = 0;

        //最初に現在の位置（インスペクターで設定した中央など）を覚えておく
        if (countdownText != null)
        {
            initialCountdownPos = countdownText.rectTransform.anchoredPosition;
        }
    }

    #endregion

    #region Maskのフェード処理とゲームスタートカウントダウン

    void MaskFade()
    {
        StartCoroutine(LiftFade());
    }

    /// <summary>
    /// フェード処理が終わったら、カウントダウン
    /// </summary>
    /// <returns></returns>
    private IEnumerator LiftFade()
    {
        //広がるアニメーション
        yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));

        //カウントダウン開始
        yield return StartCoroutine(StartCountdown());
    }


    /// <summary>
    /// カウントダウン処理
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        // 3, 2, 1 のループ
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            PlayCountdownAnimation();
            yield return new WaitForSeconds(1.0f);
        }

        //最後は数字ではなく「GO!」や「START!」
        countdownText.text = "START!";
        countdownText.fontSize = 200;
        PlayCountdownAnimation();
        yield return new WaitForSeconds(1.0f);

        countdownText.gameObject.SetActive(false);

        //全て終わったらゲーム開始
        isStart = true;
    }

    /// <summary>
    /// カウントダウンのアニメーション
    /// </summary>
    private void PlayCountdownAnimation()
    {
        //位置とスケール、透明度を「最初」の状態にリセット！
        // これを忘れると、数字が出るたびにどんどん上にズレていきます
        countdownText.rectTransform.anchoredPosition = initialCountdownPos;
        countdownText.transform.localScale = Vector3.zero;
        countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 1);

        //ポンッと出るアニメーション
        countdownText.transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutBack);

        //UI専用の移動命令「DOAnchorPosY」を使う！
        // initialCountdownPos.y（元の高さ）から +50 くらい上に浮かせる
        countdownText.rectTransform.DOAnchorPosY(initialCountdownPos.y + 50f, 0.8f);

        //じわじわ消える
        countdownText.DOFade(0, 0.5f).SetDelay(0.5f);
    }
    #endregion

    #endregion

    #region Update呼び出し関数
    /// <summary>
    /// ゲームクリア判定処理
    /// </summary>
    void GameJudge()
    {
        if (objIndex < 0)
        {
            gameClear = true;
        }
    }

    /// <summary>
    /// ゲームクリア時の処理
    /// </summary>
    void GameClear()
    {
        if (gameClear) 
        {
            Debug.Log("くりあ");
        }
    }
    #endregion
}
