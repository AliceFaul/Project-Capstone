using System.Collections;
using UnityEngine;

public class GachaChestVisual : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private ParticleSystem openVFX;

    [Header("Cấu hình")]
    [SerializeField] private float revealDelay = 1.2f;     // = độ dài clip mở rương, và chỉnh theo gói pack
    [SerializeField] private string idleStateName = "Idle"; // tên state rương đóng trong Animator

    private static readonly int OpenHash = Animator.StringToHash("Open");

    public void PlayOpenAnimation(Rarity rarity, System.Action onFinished = null)
    {
        if (chestAnimator != null)
            chestAnimator.SetTrigger(OpenHash);

        if (openVFX != null)
        {
            var mainModule = openVFX.main;
            mainModule.startColor = RarityColors.Get(rarity); // tia sáng theo màu và theo độ hiếm
            openVFX.Play();
        }

        StartCoroutine(WaitThenFinish(onFinished));
    }

    private IEnumerator WaitThenFinish(System.Action onFinished)
    {
        yield return new WaitForSeconds(revealDelay);
        onFinished?.Invoke();
    }

    // Gọi khi đóng popup kết quả để rương về trạng thái đóng
    public void ResetChest()
    {
        if (chestAnimator != null)
        {
            chestAnimator.ResetTrigger(OpenHash);
            chestAnimator.Play(idleStateName, 0, 0f);
        }
        if (openVFX != null) openVFX.Stop();
    }
}