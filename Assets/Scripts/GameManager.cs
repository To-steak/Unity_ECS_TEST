using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TMP_Text fpsText;
    
    private float deltaTime = 0.0f;
    private float updateInterval = 0.5f; // 0.5초마다 업데이트
    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft;

    void Start()
    {
        timeLeft = updateInterval;
    }

    void Update()
    {
        // 매 프레임마다 deltaTime 계산
        deltaTime = Time.unscaledDeltaTime;
        
        timeLeft -= deltaTime;
        accum += 1.0f / deltaTime;
        frames++;

        // 일정 시간마다 FPS 업데이트 (너무 자주 업데이트하면 읽기 힘듦)
        if (timeLeft <= 0.0f)
        {
            float fps = accum / frames;
            fpsText.text = $"FPS: {fps:F1}";
            
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}