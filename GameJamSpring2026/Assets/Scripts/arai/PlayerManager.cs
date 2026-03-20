using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    RectTransform stickRoot;                            //スティックの親（背景）
    RectTransform stickHandle;                          //スティックの子（動く丸）
    Rigidbody2D rb;                                     //Rigidbody2D 物理挙動用
    Animator animator;                                  //アニメーション用
    PlayerAction controls;                              //InputSystem用
    InputAction pressAction;                            //押した時
    InputAction positionAction;                         //移動アクション

    Vector2 moveInput;                                  //最終的な移動入力値
    Vector2 startTouchPos;                              //タッチを開始した座標
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
    }

    void Update()
    {
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
        rb = GetComponent<Rigidbody2D>();                                                 //Rigidbody2D取得
        animator = GetComponent<Animator>();                                              //Animator取得

        //初期状態ではスティックを隠す
        if (stickRoot != null) stickRoot.gameObject.SetActive(false);
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
        if (moveInput.magnitude < 0.1f) return;

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
        var move = new Vector3(snapInput.x, snapInput.y, 0).normalized * speed * Time.deltaTime;

        //向きの制御
        /*        if (snapInput.x > 0)
                {
                    transform.localScale = new Vector3(1f, 1f, 1f);
                }
                else if (snapInput.x < 0)
                {
                    transform.localScale = new Vector3(-1f, 1f, 1f);
                }*/
        //右
        if (snapInput.x > 0 && snapInput.y == 0)
        {
            SetHeight(false);
            SetDirection((int)Direction.Right);
            SetCoordinate(1, 0);
        }
        //左
        else if (snapInput.x < 0 && snapInput.y == 0)
        {
            SetHeight(false);
            SetDirection((int)Direction.Left);
            SetCoordinate(-1, 0);
        }
        //上
        else if (snapInput.x == 0 && snapInput.y > 0)
        {
            SetHeight(true);
            SetDirection((int)Direction.Up);
            SetCoordinate(0, 1);
        }
        //下
        else
        {
            SetHeight(true);
            SetDirection((int)Direction.Down);
            SetCoordinate(0, -1);
        }

        //移動中フラグをセット
        SetIsMove(true);

        //移動実行
        transform.Translate(move);
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
    void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!isUsingVirtual) return;

        Vector2 currentPos = positionAction.ReadValue<Vector2>();
        Vector2 diff = currentPos - startTouchPos;

        //スティックの見た目を半径内に制限
        Vector2 clampedDiff = Vector2.ClampMagnitude(diff, stickRadius);
        stickHandle.anchoredPosition = clampedDiff;

        //入力値として正規化（0～1）してmoveInputに代入
        moveInput = clampedDiff / stickRadius;
    }

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

    private void OnDisable()
    {
        if(controls == null) { return; }

        //イベント解除
        pressAction.started -= OnStartTouch;
        pressAction.canceled -= OnCanceledTouch;

        controls.Player.Disable();
    }
    #endregion
}