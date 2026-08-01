public interface IAnimationHandler
{
    void TriggerAnimation(CharacterStateType oldState, CharacterStateType newState);
    void UpdateAnimation();
}