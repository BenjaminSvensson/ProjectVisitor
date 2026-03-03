using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] private MeshRenderer handMesh;
    [SerializeField] private Animator handAnimator;
    private int currentHandVisual;
}
