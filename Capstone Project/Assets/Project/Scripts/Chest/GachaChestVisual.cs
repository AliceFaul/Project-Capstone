//using System.Collections;
//using UnityEngine;

//public class GachaChestVisual : MonoBehaviour
//{
//    [Header("Components")]
//    [SerializeField] private Animator chestAnimator;
//    [SerializeField] private ParticleSystem openVFX;

//    [Header("Cấu hình")]
//    [SerializeField] private float revealDelay = 1.2f;     // = độ dài clip mở rương, và chỉnh theo gói pack
//    [SerializeField] private string idleStateName = "Idle"; // tên state rương đóng trong Animator

//    private static readonly int OpenHash = Animator.StringToHash("Open");

//    public void PlayOpenAnimation(Rarity rarity, System.Action onFinished = null)
//    {
//        if (chestAnimator != null)
//            chestAnimator.SetTrigger(OpenHash);

//        if (openVFX != null)
//        {
//            var mainModule = openVFX.main;
//            mainModule.startColor = RarityColors.Get(rarity); // tia sáng theo màu và theo độ hiếm
//            openVFX.Play();
//        }

//        StartCoroutine(WaitThenFinish(onFinished));
//    }

//    private IEnumerator WaitThenFinish(System.Action onFinished)
//    {
//        yield return new WaitForSeconds(revealDelay);
//        onFinished?.Invoke();
//    }

//    // Gọi khi đóng popup kết quả để rương về trạng thái đóng
//    public void ResetChest()
//    {
//        if (chestAnimator != null)
//        {
//            chestAnimator.ResetTrigger(OpenHash);
//            chestAnimator.Play(idleStateName, 0, 0f);
//        }
//        if (openVFX != null) openVFX.Stop();
//    }
//}

using System.Collections;
using UnityEngine;

// Điều khiển rương 3D: Intro -> Idle (khi mở giao diện), Opening
// Chạy bằng TÊN STATE nên KHÔNG cần nối transition trong Animator.

public class GachaChestVisual : MonoBehaviour
{
    [Header("Animation - tên STATE trong Animator (bấm vào state, đọc ô Name)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string introStateName = "CrownedChest_Intro_ANIM";
    [SerializeField] private string idleStateName = "CrownedChest_Idle_ANIM";
    [SerializeField] private string openingStateName = "CrownedChest_Opening_ANIM";
    [SerializeField] private float crossFadeTime = 0.15f;

    [Header("Mở rương")]
    [Tooltip("Sau bao nhiêu giây kể từ lúc bắt đầu Opening thì nắp bật mở -> lóe sáng.")]
    [SerializeField] private float burstDelay = 0.8f;
    [Tooltip("Chờ tối đa bấy nhiêu giây cho animation Opening chạy xong (phòng kẹt).")]
    [SerializeField] private float maxOpenWait = 4f;

    [Header("VFX (tuỳ chọn)")]
    [SerializeField] private ParticleSystem burstVfx;
    [SerializeField] private Light glowLight;
    [SerializeField] private float lightPeak = 6f;
    [SerializeField] private float lightFadeTime = 0.8f;

    private Coroutine introRoutine;
    private Coroutine lightRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (glowLight != null)
            glowLight.intensity = 0f;
    }

    //Chạy Intro, xong tự chuyển sang Idle (lặp) cho tới khi người chơi chọn mở
    public void PlayIntro()
    {
        if (animator == null)
            return;

        StopIntro();
        introRoutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        if (!string.IsNullOrEmpty(introStateName))
        {
            animator.Play(introStateName, 0, 0f);
            animator.Update(0f);
            yield return WaitStateFinished(introStateName, 6f);
        }

        GoIdle(true);
        introRoutine = null;
    }

    private void StopIntro()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
    }

    public IEnumerator PlayOpen(Color burstColor)
    {
        StopIntro();

        if (animator != null && !string.IsNullOrEmpty(openingStateName))
        {
            animator.Play(openingStateName, 0, 0f);
            animator.Update(0f);
        }

        yield return new WaitForSeconds(burstDelay);
        Burst(burstColor);

        if (animator != null && !string.IsNullOrEmpty(openingStateName))
            yield return WaitStateFinished(openingStateName, maxOpenWait);
    }

    public void PlayOpenAnimation(Rarity rarity, System.Action onFinished = null)
    {
        StartCoroutine(PlayOpenAnimationRoutine(rarity, onFinished));
    }

    private IEnumerator PlayOpenAnimationRoutine(Rarity rarity, System.Action onFinished)
    {
        Color burstColor = RarityColors.Get(rarity);
        yield return StartCoroutine(PlayOpen(burstColor));
        onFinished?.Invoke();
    }

    public void ResetChest()
    {
        ResetToClosed();
    }

    //Đưa rương về Idle (đóng), sẵn sàng cho lần mở tiếp theo.
    public void ResetToClosed()
    {
        StopIntro();

        if (lightRoutine != null)
        {
            StopCoroutine(lightRoutine);
            lightRoutine = null;
        }

        if (glowLight != null)
            glowLight.intensity = 0f;

        GoIdle(false);
    }

    private void GoIdle(bool blend)
    {
        if (animator == null || string.IsNullOrEmpty(idleStateName))
            return;

        if (blend)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(idleStateName))
                animator.CrossFade(idleStateName, crossFadeTime, 0);
        }
        else
        {
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    private IEnumerator WaitStateFinished(string stateName, float timeout)
    {
        float elapsed = 0f;
        yield return null; // chờ Animator cập nhật sang state mới

        while (elapsed < timeout)
        {
            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(stateName) || info.normalizedTime >= 1f)
                    yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void Burst(Color color)
    {
        if (burstVfx != null)
        {
            var main = burstVfx.main;
            main.startColor = color;
            burstVfx.Play(true);
        }

        if (glowLight != null)
        {
            glowLight.color = color;

            if (lightRoutine != null)
                StopCoroutine(lightRoutine);

            lightRoutine = StartCoroutine(FadeLight());
        }
    }

    private IEnumerator FadeLight()
    {
        float t = 0f;
        while (t < lightFadeTime)
        {
            t += Time.deltaTime;
            glowLight.intensity = Mathf.Lerp(lightPeak, 0f, t / lightFadeTime);
            yield return null;
        }

        glowLight.intensity = 0f;
        lightRoutine = null;
    }

    // Test nhanh trong Play Mode: bấm dấu ⋮ trên tiêu đề component và chọn lệnh
    [ContextMenu("Test: Intro -> Idle")]
    private void TestIntro() { if (Application.isPlaying) PlayIntro(); }

    [ContextMenu("Test: Mở rương (tia sáng vàng)")]
    private void TestOpen() { if (Application.isPlaying) StartCoroutine(PlayOpen(new Color(1f, 0.76f, 0.03f))); }

    [ContextMenu("Test: Đóng lại (Idle)")]
    private void TestReset() { if (Application.isPlaying) ResetToClosed(); }
}
