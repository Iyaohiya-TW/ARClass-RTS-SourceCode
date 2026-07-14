using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections; // 必須引入此命名空間以使用協程

public class GameManager : MonoBehaviour
{
    [Header("核心引用")]
    public PlayerController playerController;

    [Header("UI 面板組件")]
    public GameObject gameOverPanel;
    public Button mainMenuButton;
    public Button RestartGameButton;

    [Header("場景設定")]
    public string mainMenuSceneName = "Menu";
    public string GameSceneName = "SCN_Gymnasiums";

    [Header("背景音樂設定 (BGM)")] 
    [Tooltip("拖入遊戲進行中的背景音樂 Clip")]
    public AudioClip gameplayBGM;
    [Range(0f, 1f)]
    [Tooltip("背景音樂的音量大小")]
    public float bgmVolume = 0.5f;

    [Header("遊戲結束音效設定")] 
    [Tooltip("拖入遊戲結束的音效 Clip")]
    public AudioClip gameOverSFX;

    [Tooltip("音效播放的指定秒數（時間到會自動停止）")]
    public float sfxPlayDuration = 3.0f;

    [Range(0f, 1f)]
    [Tooltip("遊戲結束音效的音量大小")]
    public float sfxVolume = 1.0f;

    [Tooltip("最後要花幾秒淡出（必須小於播放秒數，0 代表不淡出直接切斷）")]
    public float fadeOutDuration = 0.5f;

    private AudioSource bgmAudioSource;
    private AudioSource sfxAudioSource;

    private bool isGameOver = false;
    private bool isGameStarted = false;
    private float startBufferTime = 1.0f;

    [Tooltip("Panel關閉")]
    public GameObject Panel_TechTree;
    public GameObject Panel_HUD;
    public GameObject Panel_Hits;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (RestartGameButton != null) RestartGameButton.onClick.AddListener(RestartGame);

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        InitAudioSources();

        PlayGameplayBGM();
    }

    void Update()
    {
        if (isGameOver) return;

        if (!isGameStarted)
        {
            startBufferTime -= Time.deltaTime;
            if (startBufferTime <= 0) isGameStarted = true;
            return;
        }

        if (playerController != null && playerController.AllUnitList != null)
        {
            if (playerController.AllUnitList.Count == 0)
            {
                TriggerGameOver();
            }
        }
    }

    /// <summary>
    /// 初始化兩個不同的 AudioSource，避免聲音互相覆蓋
    /// </summary>
    private void InitAudioSources()
    {
        // 建立專屬的 BGM 播放器
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true; // BGM 預設循環播放
        bgmAudioSource.playOnAwake = false;

        // 建立專屬的 SFX 播放器
        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.loop = false;
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.ignoreListenerPause = true; // 確保 Time.timeScale = 0 時音效仍能播放
    }

    /// <summary>
    /// 開始播放背景音樂
    /// </summary>
    private void PlayGameplayBGM()
    {
        if (bgmAudioSource != null && gameplayBGM != null)
        {
            bgmAudioSource.clip = gameplayBGM;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("GameManager: 未指定背景音樂 (GameplayBGM)！");
        }
    }

    void TriggerGameOver()
    {
        Panel_HUD.SetActive(false);
        Panel_TechTree.SetActive(false);
        Panel_Hits.SetActive(false);

        isGameOver = true;
        Debug.Log("Game Over!");

        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        StartCoroutine(PlaySFXWithDurationRoutine());

        Time.timeScale = 0f;
    }

    /// <summary>
    /// 協程：控制音效播放秒數
    /// </summary>
    private IEnumerator PlaySFXWithDurationRoutine()
    {
        if (sfxAudioSource != null && gameOverSFX != null)
        {
            sfxAudioSource.clip = gameOverSFX;
            sfxAudioSource.volume = sfxVolume;
            sfxAudioSource.Play();

            float normalPlayDuration = Mathf.Max(0f, sfxPlayDuration - fadeOutDuration);

            yield return new WaitForSecondsRealtime(normalPlayDuration);

            if (fadeOutDuration > 0f)
            {
                float elapsed = 0f;
                float startVolume = sfxAudioSource.volume;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    sfxAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }
            }

            sfxAudioSource.Stop();
            sfxAudioSource.volume = sfxVolume;
        }
        else
        {
            Debug.LogWarning("GameManager: 未指定音效或找不到 AudioSource！");
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }
}