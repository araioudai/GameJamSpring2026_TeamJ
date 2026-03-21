using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    #region 列挙対
    //向き
    enum Direction
    {
        Down, 
        Up, 
        Right, 
        Left
    }
    #endregion

    #region private変数
    [Header("設定")]
    [SerializeField] private float speed = 5f;          //プレイヤーの移動スピード
    [SerializeField] private float stickRadius = 60f;   //スティックが動ける範囲（半径）
    [Header("掴む設定")]
    [SerializeField] private Transform holdPoint;       //掴む場所
    private GameObject grabbedObject = null;            //今掴んでいる家具
    [Header("家具との当たり判定用")]
    [SerializeField] private LayerMask furnitureLayer;  //家具当たり判定用のレイヤー
    [Header("設置不可の判定用")]
    [SerializeField] private LayerMask obstacleLayer;   //壁(Wall)や他の家具(Furniture)を含める
    [Header("ゴール判定用")]
    [SerializeField] private LayerMask goalLayer;       //ゴール判定用のレイヤー

    RectTransform stickRoot;                            //スティックの親（背景）
    RectTransform stickHandle;                          //スティックの子（動く丸）
    Rigidbody2D rb;                                     //Rigidbody2D 物理挙動用
    Animator animator;                                  //アニメーション用
    PlayerAction controls;                              //InputSystem用
    InputAction pressAction;                            //押した時
    InputAction positionAction;                         //移動アクション
    Button grabButton;                                  //掴む用ボタン
    Text gradText;                                      //掴む離す切り替え用テキスト

    Vector2 moveInput;                                  //最終的な移動入力値
    Vector2 startTouchPos;                              //タッチを開始した座標
    Vector2 moveDirection = Vector2.down;               //初期は下向き
    bool isUsingVirtual;                                //バーチャルパッド使用中フラグ

    #endregion

    #region Set関数

    /// <summary>
    /// 進んでいる方向の座標をセット処理
    /// </summary>
    /// <param name="x">x座標</param>
    /// <param name="y">y座標</param>
    void SetCoordinate(float x, float y)
    {
        animator.SetFloat("x", x);
        animator.SetFloat("y", y);
    }

    /// <summary>
    /// 移動状態フラグセット処理
    /// </summary>
    /// <param name="move">移動状態</param>
    void SetIsMove(bool move)
    {
        animator.SetBool("isMove", move);
    }

    /// <summary>
    /// 移動が縦か横かの処理
    /// </summary>
    /// <param name="height">縦かどうか</param>
    void SetHeight(bool height)
    {
        animator.SetBool("height", height);
    }

    /// <summary>
    /// 向きをセットする処理
    /// </summary>
    /// <param name="value">向き</param>
    void SetDirection(int value)
    {
        animator.SetInteger("stats", value);
    }

    #endregion

    #region Unityイベント
    private void Awake()
    {
        //入力クラスのインスタンス化
        controls = new PlayerAction();
    }

    void Start()
    {
        //初期化処理の呼び出し
        Init();

        //ここで初めて入力を有効にする（Startならデバイス認識が完了しています）
        if (controls == null) controls = new PlayerAction();

        controls.Player.Enable();

        //イベント登録もここで行う
        pressAction = controls.Player.VirtualPress;
        positionAction = controls.Player.VirtualPosition;

        pressAction.started += OnStartTouch;
        pressAction.canceled += OnCanceledTouch;
        controls.Player.Grab.started += OnGrad;
    }

    void Update()
    {
        if (!GameManager.Instance.GetIsStart()) { return; }

        //バーチャルスティック操作中
        VirtualControl();

        //移動処理の呼び出し
        Move();
    }
    #endregion

    #region Start呼び出し関数
    void Init()
    {
        stickRoot = GameObject.Find("StickBack").GetComponent<RectTransform>();           //スティックの背景
        stickHandle = stickRoot.GetChild(0).gameObject.GetComponent<RectTransform>();     //スティック
        grabButton = GameObject.Find("ButtonFurniture").GetComponent<Button>();           //ボタン取得
        gradText = GameObject.Find("TextFurniture").GetComponent<Text>();                 //テキスト取得
        rb = GetComponent<Rigidbody2D>();                                                 //Rigidbody2D取得
        animator = GetComponent<Animator>();                                              //Animator取得

        gradText.text = "掴む";

        //初期状態ではスティックを隠す
        if (stickRoot != null) stickRoot.gameObject.SetActive(false);

        //ボタンが設定されていれば、クリックイベントを登録
        if (grabButton != null)
        {
            //ボタンが押された時にPushGrabを実行するように予約
            grabButton.onClick.AddListener(PushGrab);
        }
    }
    #endregion

    #region Update呼び出し関数

    #region バーチャルパッド制御
    void VirtualControl()
    {
        //バーチャルスティック操作中
        if (isUsingVirtual)
        {
            Vector2 currentPos = positionAction.ReadValue<Vector2>();
            Vector2 diff = currentPos - startTouchPos;

            Vector2 clampedDiff = Vector2.ClampMagnitude(diff, stickRadius);
            stickHandle.anchoredPosition = clampedDiff;

            moveInput = clampedDiff / stickRadius;
        }
    }
    #endregion

    #region 移動
    /// <summary>
    /// プレイヤー移動
    /// </summary>
    void Move()
    {
        //バーチャルパッドを使っていない時のみ、キーボード入力を読み取る
        if (!isUsingVirtual)
        {
            moveInput = controls.Player.Move.ReadValue<Vector2>();
        }

        //入力が極端に小さい場合は無視
        if (moveInput.magnitude < 0.1f) 
        {
            SetIsMove(false);

            //止まっているときは速度を0にする
            rb.linearVelocity = Vector2.zero;

            return; 
        }

        Vector2 snapInput = moveInput;

        //XとYの絶対値を比較して、大きい方の入力を使用し、小さい方を0にする（4方向スナップ）
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            snapInput.y = 0; //左右移動を優先
        }
        else
        {
            snapInput.x = 0; //上下移動を優先
        }

        //軸を固定したベクトルで移動計算
        //var move = new Vector3(snapInput.x, snapInput.y, 0).normalized * speed * Time.deltaTime;

        //向きの制御
        /*        if (snapInput.x > 0)
                {
                    transform.localScale = new Vector3(1f, 1f, 1f);
                }
                else if (snapInput.x < 0)
                {
                    transform.localScale = new Vector3(-1f, 1f, 1f);
                }*/

        //移動処理
        Vector2 velocity = snapInput.normalized * speed;
        rb.linearVelocity = velocity;

        //移動中フラグをセット
        SetIsMove(true);

        //アニメーション変更
        UpdateAnimator(snapInput);

        //移動実行
        //transform.Translate(move);
    }

    /// <summary>
    /// プレイヤーのアニメーション更新
    /// </summary>
    /// <param name="snapInput"></param>
    void UpdateAnimator(Vector2 snapInput)
    {
        //右
        if (snapInput.x > 0 && snapInput.y == 0)
        {
            moveDirection = Vector2.right;
            holdPoint.localPosition = new Vector2(0.8f, 0);
            SetHeight(false);
            SetDirection((int)Direction.Right);
            SetCoordinate(1, 0);
        }
        //左
        else if (snapInput.x < 0 && snapInput.y == 0)
        {
            moveDirection = Vector2.left;
            holdPoint.localPosition = new Vector2(-0.8f, 0);
            SetHeight(false);
            SetDirection((int)Direction.Left);
            SetCoordinate(-1, 0);
        }
        //上
        else if (snapInput.x == 0 && snapInput.y > 0)
        {
            moveDirection = Vector2.up;
            holdPoint.localPosition = new Vector2(0, 0.8f);
            SetHeight(true);
            SetDirection((int)Direction.Up);
            SetCoordinate(0, 1);
        }
        //下
        else if (snapInput.x == 0 && snapInput.y < 0)
        {
            moveDirection = Vector2.down;
            holdPoint.localPosition = new Vector2(0, -0.7f);
            SetHeight(true);
            SetDirection((int)Direction.Down);
            SetCoordinate(0, -1);
        }
    }
    #endregion

    #region バーチャルパッド移動制御
    /// <summary>
    /// スティックの表示設定と位置固定
    /// </summary>
    void SetupJoystick(Vector2 target, bool status)
    {
        if (stickRoot == null) return;

        stickRoot.gameObject.SetActive(status);
        if (status)
        {
            //スクリーン座標(target)を、UIのローカル座標に変換して代入
            RectTransform parentRect = stickRoot.parent as RectTransform;
            Canvas canvas = stickRoot.GetComponentInParent<Canvas>();
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, target, cam, out localPos);

            stickRoot.anchoredPosition = localPos;

            //Z軸を強制的に0にする
            stickRoot.transform.localPosition = new Vector3(stickRoot.transform.localPosition.x, stickRoot.transform.localPosition.y, 0f);

            //中身の位置をリセット
            stickHandle.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 押し始めた時の処理（startedイベントに登録）
    /// </summary>
    void OnStartTouch(InputAction.CallbackContext context)
    {
        Vector2 touchPos = positionAction.ReadValue<Vector2>();

        //UI（ボタン等）の上なら無視する
        if (IsOverUI(touchPos)) return;

        isUsingVirtual = true;
        startTouchPos = touchPos;
        SetupJoystick(touchPos, true);
    }

    /// <summary>
    /// 押しながら動かしている時の処理（performedイベントに登録）
    /// </summary>
/*    void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!isUsingVirtual) return;

        Vector2 currentPos = positionAction.ReadValue<Vector2>();
        Vector2 diff = currentPos - startTouchPos;

        //スティックの見た目を半径内に制限
        Vector2 clampedDiff = Vector2.ClampMagnitude(diff, stickRadius);
        stickHandle.anchoredPosition = clampedDiff;

        //入力値として正規化（0～1）してmoveInputに代入
        moveInput = clampedDiff / stickRadius;
    }*/

    /// <summary>
    /// 離された時の処理（canceledイベントに登録）
    /// </summary>
    void OnCanceledTouch(InputAction.CallbackContext context)
    {
        isUsingVirtual = false;
        moveInput = Vector2.zero;
        SetupJoystick(Vector2.zero, false); //パッドを非表示
        SetIsMove(false);
    }

    /// <summary>
    /// マウスやタップがUI要素の上にあるか判定
    /// </summary>
    private bool IsOverUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = pos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var hit in results)
        {
            //ButtonなどのSelectableコンポーネントがあればUIとみなす
            if (hit.gameObject.GetComponent<UnityEngine.UI.Selectable>() != null) return true;
        }
        return false;
    }
    #endregion

    #region 掴む処理

    void OnGrad(InputAction.CallbackContext context)
    {
        PushGrab();
    }

    /// <summary>
    /// ボタンが押された時に前に家具があれば掴む
    /// </summary>
    public void PushGrab()
    {
        //すでに何か持っているなら離す
        if (grabbedObject != null)
        {
            //離すのに成功した時だけテキストを変える
            if (ReleaseFurniture())
            {
                gradText.text = "掴む";
            }
            return;
        }

        //前方に家具があるかチェック（Rayの距離設定）
        float rayLength = 0.5f;
        //始点をプレイヤーの中心から少し「向いている方向」にずらす
        Vector2 origin = (Vector2)transform.position + (moveDirection * 0.2f);
        //指定したレイヤーに対してのみ当たり判定を行う
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, furnitureLayer);

        //家具に当たった場合
        if (hit.collider != null)
        {
            //当たったゲームオブジェクトを引数に渡して掴む
            GrabFurniture(hit.collider.gameObject);

            if (gradText.text == "掴む")
            {
                gradText.text = "離す";
            }
        }
    }

    /// <summary>
    /// 当たったゲームオブジェクトを引数に渡して掴む
    /// </summary>
    /// <param name="obj">オブジェクト</param>
    private void GrabFurniture(GameObject obj)
    {
        //変数に掴んだオブジェクトを格納
        grabbedObject = obj;

        //物理演算（Rigidbody2D）の設定変更
        if (grabbedObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D objRb))
        {
            //bodyTypeをKinematicに変更（物理挙動を無効化し、スクリプト制御にする）
            objRb.bodyType = RigidbodyType2D.Kinematic;
            //当たり判定などは維持させる
            objRb.simulated = true;
        }

        //プレイヤー側のHoldPointの子要素に設定
        grabbedObject.transform.SetParent(holdPoint);

        //HoldPointからの相対座標をゼロにして位置を合わせる
        grabbedObject.transform.localPosition = Vector2.zero;
    }

    /// <summary>
    /// 離す処理（置けたらtrue、置けなかったらfalseを返す）
    /// </summary>
    private bool ReleaseFurniture()
    {
        if (grabbedObject == null) return false;

        //設置可能かチェック
        Vector2 boxSize = Vector2.one * 0.8f;
        if (grabbedObject.TryGetComponent<Collider2D>(out Collider2D col))
        {
            boxSize = col.bounds.size * 0.9f;
        }

        //範囲内のコライダーをすべて取得
        Collider2D[] hits = Physics2D.OverlapBoxAll(holdPoint.position, boxSize, 0f, obstacleLayer);

        bool isBlocked = false;
        foreach (var hit in hits)
        {
            //もし当たったのが「今掴んでいる物」でも「プレイヤー自身」でもなければ、それは本当の障害物
            if (hit.gameObject != grabbedObject && hit.gameObject != this.gameObject)
            {
                Debug.Log("ここには置けません！障害物: " + hit.name);
                isBlocked = true;
                break; //1つでも障害物があればループを抜ける
            }
        }

        if (isBlocked)
        {
            return false; //置けない
        }

        Collider2D goalHit = Physics2D.OverlapBox(holdPoint.position, boxSize, 0f, goalLayer);
        if (goalHit != null)
        {
            Debug.Log("ゴールに到達！オブジェクトを削除します: " + grabbedObject.name);

            //UIを元に戻す
            gradText.text = "掴む";

            //掴んでいたオブジェクトを削除
            Destroy(grabbedObject);

            //ゲームマネージャのオブジェクト数を減らす
            GameManager.Instance.SetObjMinus(1);

            //参照をクリア
            grabbedObject = null;

            return true; //成功として終了
        }

        //離す処理
        gradText.text = "掴む";
        grabbedObject.transform.SetParent(null);

        if (grabbedObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D objRb))
        {
            objRb.bodyType = RigidbodyType2D.Dynamic;
        }

        grabbedObject = null;
        return true;
    }

    #region 家具との当たり判定

    /// <summary>
    /// 向いている方向にRayを飛ばして家具との当たり判定を行う
    /// </summary>
    /// <returns></returns>
    bool IsHitFurniture()
    {
        float rayLength = 0.5f;              //Rayの距離
        Vector2 origin = transform.position; //Rayの始点

        //向いている方向にRaycast（Layerに当たったらhit）
        RaycastHit2D hit = Physics2D.Raycast(origin, moveDirection, rayLength, furnitureLayer);

        //デバッグ用にRayを表示
        Debug.DrawRay(origin, moveDirection * rayLength, Color.green);

        return hit.collider != null;         //家具に当たったらtrue
    }

    #endregion

    #endregion

    private void OnDisable()
    {
        if(controls == null) { return; }

        //イベント解除
        pressAction.started -= OnStartTouch;
        pressAction.canceled -= OnCanceledTouch;
        controls.Player.Grab.started -= OnGrad;

        controls.Player.Disable();
    }
    #endregion
}