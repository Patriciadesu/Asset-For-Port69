using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class TimedEvent
{
    [Tooltip("Time in seconds when this event should fire")]
    public float triggerTime;

    [Tooltip("Event to invoke when time is reached")]
    public UnityEvent onTimeReached;

    // Internal flag to avoid firing multiple times
    [HideInInspector] public bool hasTriggered = false;
}

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Optional UI Text to display timer")]
    public TMP_Text timerText;

    [Header("Timer Settings")]
    public bool countDown = false;   // If true -> timer goes down
    public float startTime = 60f;    // Used only if countDown == true

    [Header("Timed Events")]
    public List<TimedEvent> timedEvents = new List<TimedEvent>();

    public float currentTime;

    void Start()
    {
        currentTime = countDown ? startTime : 0f;
        UpdateUIText();
    }

    void Update()
    {
        // Update time
        currentTime += countDown ? -Time.deltaTime : Time.deltaTime;
        currentTime = Mathf.Max(0, currentTime); // clamp at 0 if counting down

        // Update UI
        UpdateUIText();

        // Check events
        foreach (var tEvent in timedEvents)
        {
            if (!tEvent.hasTriggered)
            {
                bool shouldTrigger = countDown
                    ? currentTime <= tEvent.triggerTime
                    : currentTime >= tEvent.triggerTime;

                if (shouldTrigger)
                {
                    tEvent.hasTriggered = true;
                    tEvent.onTimeReached?.Invoke();
                }
            }
        }
    }

    private void UpdateUIText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // Optional public API
    public void ResetTimer()
    {
        currentTime = countDown ? startTime : 0f;
        foreach (var tEvent in timedEvents)
        {
            tEvent.hasTriggered = false;
        }
    }

    public float GetCurrentTime() => currentTime;
}
