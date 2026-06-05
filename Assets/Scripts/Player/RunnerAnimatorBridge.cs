using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunnerAnimatorBridge : MonoBehaviour
{
    [SerializeField] private RunnerController runner;
    [SerializeField] private Animator animator;
    [SerializeField] private float runningAnimationSpeed = 5.35f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int FreeFallId = Animator.StringToHash("FreeFall");
    private static readonly int SlideId = Animator.StringToHash("Slide");

    private bool hasSpeed;
    private bool hasMotionSpeed;
    private bool hasGrounded;
    private bool hasJump;
    private bool hasFreeFall;
    private bool hasSlide;

    private void Awake()
    {
        if (runner == null)
        {
            runner = GetComponentInParent<RunnerController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheParameters();
    }

    private void Update()
    {
        if (runner == null || animator == null)
        {
            return;
        }

        var isPlaying = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        var grounded = runner.IsGrounded;
        var animationSpeed = isPlaying ? runningAnimationSpeed : 0f;

        if (hasSpeed)
        {
            animator.SetFloat(SpeedId, animationSpeed);
        }

        if (hasMotionSpeed)
        {
            animator.SetFloat(MotionSpeedId, isPlaying ? 1f : 0f);
        }

        if (hasGrounded)
        {
            animator.SetBool(GroundedId, grounded);
        }

        if (hasJump)
        {
            animator.SetBool(JumpId, isPlaying && !grounded && !runner.IsSliding);
        }

        if (hasFreeFall)
        {
            animator.SetBool(FreeFallId, isPlaying && !grounded);
        }

        if (hasSlide)
        {
            animator.SetBool(SlideId, isPlaying && runner.IsSliding);
        }
    }

    private void CacheParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == SpeedId)
            {
                hasSpeed = true;
            }
            else if (parameter.nameHash == MotionSpeedId)
            {
                hasMotionSpeed = true;
            }
            else if (parameter.nameHash == GroundedId)
            {
                hasGrounded = true;
            }
            else if (parameter.nameHash == JumpId)
            {
                hasJump = true;
            }
            else if (parameter.nameHash == FreeFallId)
            {
                hasFreeFall = true;
            }
            else if (parameter.nameHash == SlideId)
            {
                hasSlide = true;
            }
        }
    }

    // Starter Assets locomotion clips still emit these animation events.
    // This project drives movement through RunnerController, so we accept
    // the events here to avoid missing-receiver warnings on the character model.
    private void OnFootstep(AnimationEvent animationEvent)
    {
    }

    private void OnLand(AnimationEvent animationEvent)
    {
    }
}
