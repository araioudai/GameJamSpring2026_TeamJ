using UnityEditor;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Tilemaps;

public class TitleManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static TitleManager Instance { get; private set; }
    #endregion

    #region private変数
    [Header("タイトルパネル")]
    [SerializeField] private GameObject titlePanel;
    [Header("選択パネル")]
    [SerializeField] private GameObject selectPanel;
    [Header("マスクデータ")]
    [SerializeField] private MaskData data;
    [Header("マスクを置くキャンバスをセット")]
    [SerializeField] private GameObject canvasMask;

    private UIMaskFader fade;
    #endregion
    #region private変数

    private GameObject objctName;                      //オブジェクト名

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

    private void Start()
    {
        titlePanel.SetActive(true);
        selectPanel.SetActive(false);

        MaskFade();
    }

    #endregion

    #region ボタン押下処理
    public void PushSelect()
    {
        titlePanel.SetActive(false);
        selectPanel.SetActive(true);
    }

    #endregion

    #region マスク処理
    void MaskFade()
    {
        if (StageIndex.Instance.GetIsFirst()) { return; }

        LiftFade();
    }

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

    #region タイトルでステージの何かが押された時

    public void GameStart()
    {
        objctName = EventSystem.current.currentSelectedGameObject;
        string name = objctName.name;

        //ステージ番号に変換
        if (name.StartsWith("Stage"))
        {
            string numberPart = name.Replace("Stage", "");

            //TryParseで安全に整数に変換（失敗してもクラッシュしない）
            if (int.TryParse(numberPart, out int number))
            {
                StageIndex.Instance.SetIndex(number); //選択されたステージ番号を保存

                // 1.フェードアウト（画面を閉じる）を開始
                // 2.アニメーション終了後に実行
                fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
                {
                    //画面が閉じきったタイミングでシーン遷移を開始
                    StartCoroutine(StageLoad());
                });
            }
            else
            {
                //Debug.LogWarning("ステージ名に数値が含まれていません: " + name);
            }
        }
    }

    IEnumerator StageLoad()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }

    #endregion
}
