using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerJumpController : MonoBehaviour
{
    //Variable jump inspired by: https://answers.unity.com/questions/1324980/jump-higher-when-holding-button.html?childToView=1327081#answer-1327081
    [SerializeField] float _jumpHeight;
    [SerializeField] private float _counterJumpForce;
    [SerializeField] ActionBasedContinuousMoveProvider _abcmp;
    [SerializeField] LocomotionSystem _ls;

    Rigidbody _rb;
    bool jumpKeyHeld;
    bool isJumping;


    [SerializeField] InputActionProperty jumpActionProperty;

    bool isGrounded = true;
 

    // Start is called before the first frame update
    void Start()
    {
 
;
        _rb = GetComponent<Rigidbody>();   
    }


    // Update is called once per frame
    void Update()
    {
        if (jumpActionProperty.action.IsPressed())
        {
            jumpKeyHeld = true;
            if (isGrounded && !isJumping)
            {
               float jumpForce = CalculateJumpForce(Physics.gravity.magnitude, _jumpHeight);

                _rb.AddForce(Vector3.up * jumpForce * _rb.mass, ForceMode.Impulse);
                isJumping = true;
                _abcmp.enabled = false;
                _ls.enabled = false;
            }
        }
        
        if (!jumpActionProperty.action.IsPressed())
        {
            jumpKeyHeld = false;
        }
    }

    public static float CalculateJumpForce(float gravityStrength, float jumpHeight)
    {
        return Mathf.Sqrt(2 * gravityStrength * jumpHeight);
    }

    private void FixedUpdate()
    {
        if (isJumping)
        {
            if (!jumpKeyHeld && _rb.velocity.y > 0)
            {
                _rb.AddForce(Vector3.down * _counterJumpForce * _rb.mass);
            }
        }
    }

    void OnCollisionStay()
    {
        Debug.Log("grounded");
        _abcmp.enabled = true;
        _ls.enabled = true;
        isGrounded = true;
        isJumping = false;
    }
    void OnCollisionExit()
    {
        Debug.Log("Not grounded");
        isGrounded = false;
    }

}
