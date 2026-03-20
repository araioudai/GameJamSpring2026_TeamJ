using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    #region 定数
    //プレイヤーサウンド状態用定数
    private const int JUMP = 0;
    #endregion

    #region private変数
    [Header("バーチャルスティック参照")]
    [SerializeField] private RectTransform stickRoot;   //スティックの親（背景）
    [SerializeField] private RectTransform stickHandle; //スティックの子（動く丸）

    [Header("設定")]
    [SerializeField] private float speed = 5f;          //プレイヤーの移動スピード
    [SerializeField] private float stickRadius = 60f;   //スティックが動ける範囲（半径）

    Rigidbody2D rb;                                     //Rigidbody2D 物理挙動用
    Animator animator;                                  //アニメーション用
    PlayerAction controls;                              //InputSystem用
    Vector2 moveInput;                                  //最終的な移動入力値
    Vector2 startTouchPos;                              //タッチを開始した座標
    bool isUsingVirtual;                                //バーチャルパッド使用中フラグ
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
        controls.Player.Virtual.started += OnStartTouch;
        controls.Player.Virtual.performed += OnMovePerformed;
        controls.Player.Virtual.canceled += OnCanceledTouch;
    }

    void Update()
    {
        //移動処理の呼び出し
        Move();
    }
    #endregion

    #region Start呼び出し関数
    void Init()
    {
        rb = GetComponent<Rigidbody2D>();    //Rigidbody2D取得
        animator = GetComponent<Animator>(); //Animator取得

        //初期状態ではスティックを隠す
        if (stickRoot != null) stickRoot.gameObject.SetActive(false);
    }
    #endregion

    #region Update呼び出し関数

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

        //向きの制御（localScaleのXを入れ替えて反転）
        if (snapInput.x > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (snapInput.x < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }

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
        if (Pointer.current == null) return;
        Vector2 touchPos = Pointer.current.position.ReadValue();

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
        if (!isUsingVirtual || Pointer.current == null) return;

        Vector2 currentPos = Pointer.current.position.ReadValue();
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
        controls.Player.Virtual.started -= OnStartTouch;
        controls.Player.Virtual.performed -= OnMovePerformed;
        controls.Player.Virtual.canceled -= OnCanceledTouch;

        controls.Player.Disable();
    }
    #endregion
}