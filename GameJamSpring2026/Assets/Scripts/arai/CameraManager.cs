using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    #region シングルトン
    public static CameraManager Instance { get; private set; } //他のスクリプトからInstanceでアクセスできるようにする
    #endregion

    #region private変数

    [Header("デッドゾーンのX・Y範囲")]
    [SerializeField] private Vector2 deadZone = new Vector2(2f, 1.5f); //デッドゾーンのX・Y範囲（中心からの距離）
    [Header("追従速度をセット")]
    [SerializeField] private float followSpeed = 5f;                   //追従速度（Lerpの係数）

    private Transform target;                                          //追従対象（プレイヤーのTransform）
    private bool isSlider;

    #endregion
    
    #region Get関数

    public bool GetSlide() { return isSlider; }

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
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PlayerTransform());
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LateUpdate()
    {
        if (target == null) return;                          //追従対象がセットされていなければ処理終了
        if (isSlider) { return; }

        Vector3 pos = transform.position;
        Vector3 tpos = target.position;

        //X 軸の処理
        float dx = tpos.x - pos.x;
        if (Mathf.Abs(dx) > deadZone.x)
        {
            // デッドゾーン外に出ている場合のみLerpで滑らかに追従
            pos.x = Mathf.Lerp(pos.x,
                               tpos.x - Mathf.Sign(dx) * deadZone.x,
                               followSpeed * Time.deltaTime);
        }

        //Y 軸の処理
        float dy = tpos.y - pos.y;
        if (Mathf.Abs(dy) > deadZone.y)
        {
            // デッドゾーン外に出ている場合のみLerpで滑らかに追従
            pos.y = Mathf.Lerp(pos.y,
                               tpos.y - Mathf.Sign(dy) * deadZone.y,
                               followSpeed * Time.deltaTime);
        }

        // カメラ位置を更新（Z軸は維持）
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    #endregion


    #region Start呼び出し関数

    IEnumerator PlayerTransform()
    {
        yield return new WaitForSeconds(0.5f);

        target = GameObject.Find("Player(Clone)").transform;
    }

    #endregion

    #region Update呼び出し関数


    #endregion
}
