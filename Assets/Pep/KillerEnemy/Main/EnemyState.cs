/// <summary>
/// Defines the possible states for the Killer AI state machine.
/// </summary>
public enum EnemyState
{
    Idle,      // Standing still, waiting for input/trigger
    Patrol,    // Moving between predefined waypoints
    Chase,     // Actively pursuing a target
    Attack     // Executing an attack routine (brief, transitional state)
}