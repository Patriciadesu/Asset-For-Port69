using UnityEngine;

/// <summary>
/// Re-usable configuration block for modules that need to trigger abilities
/// on a random interval while a specific state is active.
/// </summary>
[System.Serializable]
public class RandomTriggerSettings
{
    [Header("Chance Settings")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that the trigger will fire when a roll occurs.")]
    public float TriggerChance = 0.2f;

    [Tooltip("Minimum/maximum seconds between trigger rolls after the first one.")]
    public Vector2 Interval = new Vector2(2f, 4f);

    [Tooltip("Initial delay window before the very first roll happens.")]
    public Vector2 InitialDelay = new Vector2(0.5f, 1.5f);

    private float nextRollTime;
    private bool isPrimed;

    /// <summary>
    /// Ensures the trigger has a scheduled roll time.
    /// </summary>
    public void PrimeIfNeeded()
    {
        if (!isPrimed)
        {
            Prime();
        }
    }

    /// <summary>
    /// Immediately schedules the next roll using the initial delay window.
    /// </summary>
    public void Prime()
    {
        isPrimed = true;
        float minDelay = Mathf.Max(0.01f, InitialDelay.x);
        float maxDelay = Mathf.Max(minDelay, InitialDelay.y);
        nextRollTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    /// <summary>
    /// Attempts to consume a trigger roll. Returns true if the roll succeeded.
    /// </summary>
    public bool TryConsumeTrigger()
    {
        PrimeIfNeeded();

        if (Time.time < nextRollTime)
            return false;

        ScheduleNext();
        return Random.value <= TriggerChance;
    }

    /// <summary>
    /// Forces the trigger to wait for a specific amount of seconds before the next roll.
    /// </summary>
    public void BlockFor(float duration)
    {
        isPrimed = true;
        nextRollTime = Time.time + Mathf.Max(0.01f, duration);
    }

    private void ScheduleNext()
    {
        float minInterval = Mathf.Max(0.01f, Interval.x);
        float maxInterval = Mathf.Max(minInterval, Interval.y);
        nextRollTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}

