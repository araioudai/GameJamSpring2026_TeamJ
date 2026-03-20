using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool isPaused = false;

    void Start()
    {
        SetPause(false);    // ゲーム開始時はポーズ状態を解除する
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;   // ポーズ状態を切り替える
            SetPause(isPaused);     // ポーズ状態を設定する
        }
    }

    /// <summary>
    /// ゲームのポーズ状態を一括管理するメソッド
    /// </summary>
    /// <param name="shouldPause">true: ポーズ状態, false: ポーズ解除</param>
    
    public void SetPause(bool shouldPause)
    {
        print("SetPause called with shouldPause: " + shouldPause);
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(shouldPause);     // ポーズパネルの表示/非表示を切り替える
            print("Pause panel active state set to: " + shouldPause);
        }

        Time.timeScale = shouldPause ? 0f : 1f; // ポーズ状態に応じて時間の流れを制御する
    }

    /// <summary>
    /// ゲームを再開するためのメソッド
    /// </summary>
    public void ResumeGame()
    {
        SetPause(false);    // ポーズ状態を解除する
    }
}
