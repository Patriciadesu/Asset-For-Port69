/// <summary>
/// Defines the possible states for the Killer AI state machine.
/// </summary>
public enum EnemyState
{
    Idle,      // Standing still, waiting for input/trigger
    Patrol,    // Moving between predefined waypoints
    Check,     // Investigating lockers when the player is hiding
    Chase,     // Actively pursuing a target
    Attack,    // Executing a melee attack routine (brief, transitional state)
    Stunned,   // Enemy is stunned and cannot act (placeholder for future expansion)
    Shooting   // Performing a ranged attack (projectile throw)
}