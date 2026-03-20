using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    [SerializeField] Canvas fadeCanvas;
    [SerializeField] CanvasGroup fadePanel;
    [SerializeField] float fadeTime = 1f;
    [SerializeField] float postLoadDelay = 0.5f; // シーン切り替え後の追加待機時間


    // オブジェクトが生成されたときに呼び出されるイベントハンドラー
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // オブジェクトが有効になったときに呼び出されるイベントハンドラー
    void OnEnable()
    {
        // シーンがロードされたときにフェードインするためのイベント登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // オブジェクトが無効になったときに呼び出されるイベントハンドラー
    void OnDisable()
    {       
        // イベントの解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // シーンが切り替わった直後に実行される
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1フレーム待ってからカメラを探す（シーンの初期化待ち）
        StartCoroutine(DelayedCameraSetup());
    }

    IEnumerator DelayedCameraSetup()
    {
        print("シーン切り替え後のカメラセットアップ開始");
        // 新しいシーンのカメラが起動するまで1フレーム待つ
        yield return null;

        if (fadeCanvas != null)
        {
            // ここで元の「ScreenSpaceCamera」に戻す
            fadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;

            // 新しいシーンのメインカメラを強制的にセット
            fadeCanvas.worldCamera = Camera.main;
            
            // 重要：カメラのすぐ前に表示されるように距離を設定
            fadeCanvas.planeDistance = 1; 
            
            print("カメラをセットし直しました: " + (Camera.main != null ? Camera.main.name : "カメラが見つかりません！"));
        }
    }
    
    // シーン切り替えとフェード処理を同時に行う
    public void OnClickLoadScene(string SceneName)
    {
        print("ボタンが押されました");
        StartCoroutine(FadeAndLoad(SceneName));
    }

    IEnumerator FadeAndLoad(string SceneName)
    {
        // 追加：フェード開始時にオーバーレイにして最前面（999）に固定
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        fadePanel.alpha = 0;

        // フェードアウト（透明→黒）
        yield return StartCoroutine(Fade(0f, 1f));

        // 完全に真っ黒であることを保証（チラつき防止）
        fadePanel.alpha = 1f; 
        print("フェードアウト完了：真っ黒固定");

        // シーン切り替え
        SceneManager.LoadScene(SceneName);
        print("シーン切り替え完了");

        // シーン内のカメラなどが安定するのを待つ（必須）
        // 1フレーム待機を追加して、新しいシーンのレンダリング準備を確実にさせる
        yield return null; 
        yield return new WaitForEndOfFrame();
        
        // 追加で少し待つ（安定のため）
        yield return new WaitForSeconds(postLoadDelay);
        print("シーン安定待機完了");

        // フェードイン（黒→透明）
        yield return StartCoroutine(Fade(1f, 0f));
        print("フェードイン完了");
    }

    // フェード処理
    IEnumerator Fade(float from, float to)
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        fadePanel.alpha = to;
        print("フェード処理完了");
    }
}