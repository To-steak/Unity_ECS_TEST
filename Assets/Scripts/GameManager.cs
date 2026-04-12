using UnityEngine;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public TMP_Text fpsText;
    public TMP_Text avgText;
    public TMP_Text currentSystemText;

    private float deltaTime = 0.0f;
    private float updateInterval = 0.5f;
    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft;

    // 최근 N개 프레임 기반 평균 FPS
    private Queue<float> recentFpsValues = new Queue<float>();
    private const int MAX_FPS_SAMPLES = 10; // 최근 10개 샘플

    // ECS World
    private World _world;
    private string _currentSystem = "Best";

#if UNITY_EDITOR
    private float elapsedTime = 0.0f;
    private bool hasPaused = false;
    private int frameCount = 0;
    public bool freezeActive = false;
#endif

    void Start()
    {
        timeLeft = updateInterval;
        _world = World.DefaultGameObjectInjectionWorld;

        // 초기 상태: Best만 활성화
        ActivateBestSystem();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!hasPaused && freezeActive)
        {
            frameCount++;
            if (frameCount > 5)
            {
                elapsedTime += Time.unscaledDeltaTime;
                if (elapsedTime >= 3.0f)
                {
                    UnityEditor.EditorApplication.isPaused = true;
                    hasPaused = true;
                }
            }
        }
#endif

        // 매 프레임마다 deltaTime 계산
        deltaTime = Time.unscaledDeltaTime;

        timeLeft -= deltaTime;
        accum += 1.0f / deltaTime;
        frames++;

        // 일정 시간마다 FPS 업데이트
        if (timeLeft <= 0.0f)
        {
            // 현재 FPS
            float fps = accum / frames;
            fpsText.text = $"FPS: {fps:F1}";

            // 최근 N개 샘플에 추가
            recentFpsValues.Enqueue(fps);
            if (recentFpsValues.Count > MAX_FPS_SAMPLES)
            {
                recentFpsValues.Dequeue(); // 가장 오래된 샘플 제거
            }

            // 평균 FPS 계산
            float avgFps = CalculateAverageFps();
            avgText.text = $"AVG: {avgFps:F1}";

            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }

    private float CalculateAverageFps()
    {
        if (recentFpsValues.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (float fps in recentFpsValues)
        {
            sum += fps;
        }
        return sum / recentFpsValues.Count;
    }

    // UI 버튼용 public 메서드들
    public void ActivateWorstSystem()
    {
        _currentSystem = "Worst";
        SetSystemEnabled(typeof(WorstCollisionSystem), true);
        SetSystemEnabled(typeof(BestCollisionSystem), false);
        SetSystemEnabled(typeof(BitCollisionSystem), false);
        UpdateSystemUI();
        ResetAverage();
    }

    public void ActivateBestSystem()
    {
        _currentSystem = "Best";
        SetSystemEnabled(typeof(WorstCollisionSystem), false);
        SetSystemEnabled(typeof(BestCollisionSystem), true);
        SetSystemEnabled(typeof(BitCollisionSystem), false);
        UpdateSystemUI();
        ResetAverage();
    }

    public void ActivateBitSystem()
    {
        _currentSystem = "Bit";
        SetSystemEnabled(typeof(WorstCollisionSystem), false);
        SetSystemEnabled(typeof(BestCollisionSystem), false);
        SetSystemEnabled(typeof(BitCollisionSystem), true);
        UpdateSystemUI();
        ResetAverage();
    }

    private void SetSystemEnabled(System.Type systemType, bool enabled)
    {
        SystemHandle systemHandle = _world.GetExistingSystem(systemType);

        if (systemHandle != SystemHandle.Null)
        {
            ref var state = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.Enabled = enabled;
        }
    }

    private void UpdateSystemUI()
    {
        if (currentSystemText != null)
        {
            currentSystemText.text = $"System: {_currentSystem}";
        }
        Debug.Log($"Switched to {_currentSystem} Collision System");
    }

    private void ResetAverage()
    {
        recentFpsValues.Clear();
        avgText.text = "AVG: --";
    }
}