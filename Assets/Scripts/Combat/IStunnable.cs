// Common surface PlayerMovement and EnemyAI both expose, so status effects like
// Ice can slow or stun either without caring which one they hit.
public interface IStunnable
{
    float SpeedMultiplier { get; set; }
    bool IsStunned { get; }
    void ApplyStun(float duration);
}
