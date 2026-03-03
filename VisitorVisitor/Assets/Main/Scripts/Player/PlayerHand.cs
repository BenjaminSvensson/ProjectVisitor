using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer handMesh;
    [SerializeField] private Animator handAnimator;
    [SerializeField] private Camera playerCamera;

    [Header("Hand Images")]
    [SerializeField] private Texture normalHandImage;
    [SerializeField] private Texture interactHandImage;
    [SerializeField] private string textureProperty = "_BaseMap";
    [SerializeField] private string fallbackTextureProperty = "_MainTex";

    [Header("Interact Detection")]
    [SerializeField] private LayerMask interactibleMask;
    [SerializeField] private float interactCheckDistance = 3f;

    [Header("Animation States")]
    [SerializeField] private string handIdleState = "HandIdle";
    [SerializeField] private string handInteractState = "HandInteract";
    [SerializeField] private string handRunState = "HandRun";
    [SerializeField] private string handCrouchState = "HandCrouch";
    [SerializeField] private string handJumpState = "HandJump";

    private bool isRunning;
    private bool isCrouching;
    private bool isLookingAtInteractible;

    private string currentAnimationState;
    private bool showingInteractHand;
    private bool hasAppliedHandVisual;
    private string activeOneShotState;
    private float oneShotEndTime;
    private Dictionary<string, float> clipLengthsByName;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        BuildClipLengthCache();
        ApplyHandVisual(false);
    }

    private void Update()
    {
        UpdateInteractLookState();
        RefreshVisualAndAnimation();
    }

    public void SetMovementState(bool running, bool crouching)
    {
        isRunning = running;
        isCrouching = crouching;
    }

    public void TryTriggerInteract()
    {
        if (!isLookingAtInteractible)
        {
            return;
        }

        TriggerOneShot(handInteractState);
    }

    public void TriggerJump()
    {
        TriggerOneShot(handJumpState);
    }

    private void UpdateInteractLookState()
    {
        if (playerCamera == null)
        {
            isLookingAtInteractible = false;
            return;
        }

        Transform camTransform = playerCamera.transform;
        isLookingAtInteractible = Physics.Raycast(
            camTransform.position,
            camTransform.forward,
            interactCheckDistance,
            interactibleMask,
            QueryTriggerInteraction.Ignore);
    }

    private void RefreshVisualAndAnimation()
    {
        ApplyHandVisual(isLookingAtInteractible);

        if (IsOneShotPlaying())
        {
            return;
        }

        string targetState = ResolveAnimationState();
        if (handAnimator == null)
        {
            return;
        }

        if (targetState == currentAnimationState)
        {
            EnsureBaseStateKeepsPlaying(targetState);
            return;
        }

        handAnimator.CrossFadeInFixedTime(targetState, 0.1f);
        currentAnimationState = targetState;
    }

    private string ResolveAnimationState()
    {
        if (isCrouching)
        {
            return handCrouchState;
        }

        if (isRunning)
        {
            return handRunState;
        }

        return handIdleState;
    }

    private void TriggerOneShot(string stateName)
    {
        if (handAnimator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        handAnimator.CrossFadeInFixedTime(stateName, 0.05f);
        currentAnimationState = stateName;
        activeOneShotState = stateName;

        float clipLength = GetClipLength(stateName);
        oneShotEndTime = Time.time + Mathf.Max(clipLength, 0.1f);
    }

    private bool IsOneShotPlaying()
    {
        if (string.IsNullOrEmpty(activeOneShotState))
        {
            return false;
        }

        if (!IsCurrentState(activeOneShotState))
        {
            activeOneShotState = null;
            return false;
        }

        if (Time.time < oneShotEndTime)
        {
            return true;
        }

        activeOneShotState = null;
        return false;
    }

    private void BuildClipLengthCache()
    {
        clipLengthsByName = new Dictionary<string, float>();

        if (handAnimator == null || handAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = handAnimator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || clipLengthsByName.ContainsKey(clip.name))
            {
                continue;
            }

            clipLengthsByName.Add(clip.name, clip.length);
        }
    }

    private float GetClipLength(string stateName)
    {
        if (clipLengthsByName != null && clipLengthsByName.TryGetValue(stateName, out float length))
        {
            return length;
        }

        return 0.1f;
    }

    private void EnsureBaseStateKeepsPlaying(string stateName)
    {
        AnimatorStateInfo currentStateInfo = handAnimator.GetCurrentAnimatorStateInfo(0);
        if (currentStateInfo.loop)
        {
            return;
        }

        if (currentStateInfo.normalizedTime < 1f)
        {
            return;
        }

        handAnimator.Play(stateName, 0, 0f);
    }

    private bool IsCurrentState(string stateName)
    {
        if (handAnimator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int targetHash = Animator.StringToHash(stateName);
        AnimatorStateInfo currentStateInfo = handAnimator.GetCurrentAnimatorStateInfo(0);
        return currentStateInfo.shortNameHash == targetHash;
    }

    private void ApplyHandVisual(bool useInteractVisual)
    {
        if (handMesh == null)
        {
            return;
        }

        if (hasAppliedHandVisual && showingInteractHand == useInteractVisual)
        {
            return;
        }

        Texture nextTexture = useInteractVisual ? interactHandImage : normalHandImage;
        if (nextTexture == null)
        {
            return;
        }

        Material material = handMesh.material;

        if (material.HasProperty(textureProperty))
        {
            material.SetTexture(textureProperty, nextTexture);
        }
        else if (material.HasProperty(fallbackTextureProperty))
        {
            material.SetTexture(fallbackTextureProperty, nextTexture);
        }

        showingInteractHand = useInteractVisual;
        hasAppliedHandVisual = true;
    }
}
