public class PlayerModifier
{
    private bool _canMove = true;
    public bool CanMove { get => _canMove; set => _canMove = value; }
    
    private bool _canAttack = true;
    public bool CanAttack { get => _canAttack; set => _canAttack = value; }

    public void MoveModifier(bool canMove)
    {
        _canMove = canMove;
    }

    public void AttackModifier(bool canAttack)
    {
        _canAttack = canAttack;
    }
}