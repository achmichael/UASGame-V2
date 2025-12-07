using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private MovementLogic movementLogic;

    void Start()
    {
        movementLogic = GetComponentInParent<MovementLogic>();
        Debug.Log("Movement logic value" + movementLogic);
        if (movementLogic == null)
        {
            Debug.LogError("[PlayerAnimator] MovementLogic tidak ditemukan di parent atau object ini!");
        }
    }

    // Method ini yang akan dipanggil oleh Animation Event
    public void PlayFootstepSound()
    {
        if (movementLogic != null)
        {
            movementLogic.PlayFootstepSound();
        }else
        {
            Debug.LogWarning("[PlayerAnimator] Tidak bisa memanggil PlayFootstepSound karena MovementLogic null.");
        }
    }
}
