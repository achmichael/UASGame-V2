using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementLogic : MonoBehaviour
{
    private Rigidbody rb;
    public float walkspeed = 0.1f, runspeed = 1f, jumppower = 10f, fallspeed = 0.5f, airMultiplier;
    private Transform PlayerOrientation;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    bool grounded = true, aerialBoost = true;
    public Animator anim;
    public bool TPSMode = true, AimMode = false;
    public float HitPoints = 100f;

    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        PlayerOrientation = this.GetComponent<Transform>();
    }

    void Update()
    {
        Movement();
        Jump();
        AirModeAdjuster();
        ShootLogic();

        if (Input.GetKeyDown(KeyCode.F)){
            PlayerGetHit(100f);
        }
    }


    private void PlayerGetHit(float damage){
        Debug.Log("Player Got Hit and took " + damage + " damage");
        HitPoints = HitPoints - damage;
        if (HitPoints == 0f || HitPoints < 0f){
            anim.SetBool("Death", true);
        }
    }

    private void Movement()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = PlayerOrientation.forward * verticalInput + PlayerOrientation.right * horizontalInput;
        
        if (grounded && moveDirection != Vector3.zero){
            if (Input.GetKey(KeyCode.LeftShift))
                {
                    anim.SetBool("Walk", false);
                    anim.SetBool("Run", true);
                    rb.AddForce(moveDirection.normalized * runspeed * 1f, ForceMode.Force);
                }
                else
                {
                    anim.SetBool("Walk", true);
                    anim.SetBool("Run", false);
                    rb.AddForce(moveDirection.normalized * walkspeed * 1f, ForceMode.Force);
                }
        }else {
            anim.SetBool("Walk", false);
            anim.SetBool("Run", false);
        }
    }

    private void ShootLogic (){
        if (Input.GetKey(KeyCode.Mouse0)){
            if (moveDirection.normalized != Vector3.zero){
                anim.SetBool("WalkShoot", true);
                anim.SetBool("IdleShoot", false);
            }else {
                anim.SetBool("WalkShoot", false);
                anim.SetBool("IdleShoot", true);
            }
        }else {
            anim.SetBool("WalkShoot", false);
            anim.SetBool("IdleShoot", false);
        }
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(transform.up * jumppower, ForceMode.Impulse);
            grounded = false;
            anim.SetBool("Jump", true);
        }
        else if (!grounded)
        {
            rb.AddForce(Vector3.down * fallspeed * rb.mass, ForceMode.Force);
            if (aerialBoost)
            {
                rb.AddForce(moveDirection.normalized * walkspeed * 10f * airMultiplier, ForceMode.Impulse);
                aerialBoost = false;
            }
        }
    }

    public void AirModeAdjuster(){
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (AimMode){
                TPSMode = true;
                AimMode = false;
                anim.SetBool("AimMode", false);
            }else if (TPSMode){
                TPSMode = false;
                AimMode = true;
                anim.SetBool("AimMode", true);
            }
            }
    }

    public void groundedchanger()
    {
        grounded = true;
        aerialBoost = true;
        anim.SetBool("Jump", false);
    }
}
