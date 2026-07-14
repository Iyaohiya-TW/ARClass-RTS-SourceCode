using UnityEngine;
using UnityEngine.SceneManagement; // 載入場景必備
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI 面板參考")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("設定參數")]
    [Tooltip("請輸入你遊戲關卡場景的正確名稱")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        // 確保遊戲啟動時，顯示主選單、隱藏設定面板
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    // ========== 按鈕功能區 ==========

    // 1. 開始遊戲按鈕呼叫
    public void PlayGame()
    {
        Debug.Log("準備載入場景：" + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    // 2. 打開設定頁面
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // 3. 關閉設定頁面（返回主選單）
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // 4. 音量拉桿 (Slider) 數值改變時呼叫
    public void OnVolumeChanged(float volume)
    {
        // 直接控制全域音量（0.0 ~ 1.0）
        AudioListener.volume = volume;
    }

    // 5. 離開遊戲按鈕呼叫
    public void QuitGame()
    {
        Debug.Log("執行離開遊戲！(註：在編輯器中按退出沒反應是正常的，打包成執行檔後有效)");
        Application.Quit();
    }
}