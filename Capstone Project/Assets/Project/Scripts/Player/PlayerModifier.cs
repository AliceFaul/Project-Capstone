public class PlayerModifier
{
    private bool _canMove = true;
    public bool CanMove { get => _canMove; set => _canMove = value; }
    
    private bool _canAttack = true;
    public bool CanAttack { get => _canAttack; set => _canAttack = value; }
    
    private bool _isInvincible = false;
    public bool IsInvincible { get => _isInvincible; set => _isInvincible = value; }

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
}