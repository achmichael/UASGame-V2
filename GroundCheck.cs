using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    MovementLogic logicmovement;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Grounded");
        logicmovement.SetGrounded(true);
    }
    
    void Start()
    {
        logicmovement = this.GetComponentInParent<MovementLogic>();
    }

    void Update()
    {
        
    }
}
