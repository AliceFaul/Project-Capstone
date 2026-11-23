public class PlayerModifier
{
    private bool _canMove = true;
    private bool _canAttack = true;
    private bool _isInvincible = false;
    private bool _spawnProtection = false;
    private bool _isDead = false;

    public bool CanMove { get => _canMove && !_isDead; set => _canMove = value; }
    public bool CanAttack { get => _canAttack && !_isDead; set => _canAttack = value; }
    public bool IsInvincible { get => _isInvincible || _spawnProtection; set => _isInvincible = value; }

    public bool IsDead => _isDead;
    public bool HasSpawnProtection => _spawnProtection;

    public void MoveModifier(bool canMove)
    {
        _canMove = canMove;
    }

    public void AttackModifier(bool canAttack)
    {
        _canAttack = canAttack;
    }

    public void SetInvincible(bool isInvincible)
    {
        _isInvincible = isInvincible;
    }

    public void SetSpawnProtection(bool active)
    {
        _spawnProtection = active;
    }

    public void SetDead(bool isDead)
    {
        _isDead = isDead;
    }
}