﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPlatformerController : PhysicsObject
{
    public float baseZ = -1f;

    public float standingHeight = 2f;
    public float standingWidth = 1f;
    public float slidingHeight = 1f;
    public float slidingWidth = 2f;

    public float minSpeed = 2;
    public float maxSpeed = 10;
    public float crawlingSpeed = 1;
    public float jumpTakeOffSpeed = 7f;
    public float jumpFallSpeed = -7f;
    public float onWallFallSpeed = -2f;

    public float holdTime = 1f;

    public float fallGravModifier = 2.5f;
    public float jumpGravModifier = 2f;

    public float slideDecay = 0.05f;

    public float sensitivity = 0.03f;
    public float gravity = 0.2f;
    public float deadzone = 0.01f;

    public float wallCheckDistance = 0.01f;

    public float wallJumpCheckDistance = 0.1f;
    public float wallStickTime = 0.1f;
    public float wallJumpTime = 0.1f;
    public float wallJumpVelocityXPercent = 0.5f;

    public float noobJumpTime = 0.1f;

    private bool jumpButtonHeld = false;

    private float horzInput = 0f;
    private bool down = false;

    private bool sliding = false;
    private bool onWall = false;
    private bool wallOnRight = false;
    private bool jumpedWall = false;
    private float jumpedWallTime = 0.1f;

    private bool noobJump = false;
    private bool noobJumpResetCalled = false;

    private bool facingLeft = false;

    private bool disableMove = false;

    private float speed = 0f;

    private RaycastHit2D[] results = new RaycastHit2D[16];
    
    private Animator animator;
    private GameObject colRun;
    private GameObject colSlide;

    // Use this for initialization
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        colRun = transform.Find("colRun").gameObject;
        colSlide = transform.Find("colSlide").gameObject;
    }

    void Update()
    {
        targetVelocity = Vector2.zero;
        disableMove = false;
        SetDown();
        Slide();
        Slash();
        CheckWallJump();
        SetHorzInput();
        ComputeVelocity();
    }

    private void Slash()
    {
        if (Input.GetKeyDown("j") && (animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Run") || animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Idle") ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Aerial") || animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Jump")))
        {
            animator.SetTrigger("slashTrigger");
        }
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Slash"))
        {
            disableMove = true;
            if (facingLeft)
            {
                horzInput = -1;
            }
            else
            {
                horzInput = 1;
            }
        }
    }

    protected override void ComputeVelocity()
    {
        Vector2 move = Vector2.zero;

        Jump();

        speed = horzInput * maxSpeed;

        move.x = speed;

        bool flipSprite = (facingLeft ? (move.x > 0.001f) : (move.x < -0.001f));
        if (flipSprite)
        {
            transform.localScale = new Vector3(-1 * transform.localScale.x, transform.localScale.y, transform.localScale.z);
            facingLeft = !facingLeft;
        }

        animator.SetBool("grounded", grounded);
		animator.SetFloat ("ySpeed", velocity.y);
        animator.SetFloat("speed", Mathf.Abs(horzInput));
        animator.SetFloat("realSpeed", Mathf.Abs(horzInput * maxSpeed));
        animator.SetBool("slidingBool", sliding);
        animator.SetFloat("crawlPercent", Mathf.Abs((horzInput * maxSpeed) / crawlingSpeed));

        targetVelocity = move;
    }

    /*private void flipCollider()
    {
        colRun.localScale = new Vector3 (-1 * colRun.localScale.x, colRun.localScale.y, colRun.localScale.z);
    }*/

    private void Jump()
    {
        if (Input.GetButtonDown("Jump") && onWall && !sliding && !disableMove)
        {
            velocity.y = jumpTakeOffSpeed;
            if (wallOnRight)
            {
                horzInput = -wallJumpVelocityXPercent;
            }
            else
            {
                horzInput = wallJumpVelocityXPercent;
            }
            jumpButtonHeld = true;
            onWall = false;
            jumpedWall = true;
            Invoke("ResetJumpedWall", jumpedWallTime);
            Invoke("ResetJumpHold", holdTime);
            animator.SetTrigger("wallJumpTrigger");
        }
        else if (Input.GetButtonDown("Jump") && (grounded || noobJump) && !sliding && !disableMove)
        {
            noobJump = false;
            velocity.y = jumpTakeOffSpeed;
            jumpButtonHeld = true;
            Invoke("ResetJumpHold", holdTime);
            animator.SetTrigger("jumpTrigger");
        }
        else if (Input.GetButtonUp("Jump") || velocity.y < 0)
        {
            jumpButtonHeld = false;
        }

        if (velocity.y > 0)
        {
            gravityModifier = jumpGravModifier;
        }
        else if (velocity.y < 0)
        {
            gravityModifier = fallGravModifier;
        }

        if (jumpButtonHeld)
        {
            gravityModifier = 1f;
            velocity.y = jumpTakeOffSpeed;
        }
        else if (onWall && velocity.y < onWallFallSpeed && !disableMove && !sliding)
        {
            gravityModifier = 1f;
            velocity.y = onWallFallSpeed;
        }
        else if (velocity.y < jumpFallSpeed)
        {
            gravityModifier = 1f;
            velocity.y = jumpFallSpeed;
        }

        if (grounded)
        {
            gravityModifier = 1f;
            onWall = false;
            jumpedWall = false;
            noobJump = true;
        }
        else if (!grounded && noobJump && !noobJumpResetCalled)
        {
            Invoke("ResetNoobJump", noobJumpTime);
            noobJumpResetCalled = true;
        }
    }

    private void ResetJumpHold()
    {
        jumpButtonHeld = false;
    }

    private void SetHorzInput()
    {
        float oldHorzInput = horzInput;

        if (!disableMove)
        {
            if (onWall)
            {
                horzInput = 0;
                if ((Input.GetAxis("Horizontal") < 0 && rb2d.Cast(Vector2.right, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0) ||
                   (Input.GetAxis("Horizontal") > 0 && rb2d.Cast(Vector2.left, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0))
                {
                    Invoke("SetOnWall", wallStickTime);
                }
            }
            else if (jumpedWall)
            {
                if (wallOnRight)
                {
                    horzInput = -maxSpeed * wallJumpVelocityXPercent;
                }
                else
                {
                    horzInput = maxSpeed * wallJumpVelocityXPercent;
                }
            }
            else if (sliding)
            {
                if (Mathf.Abs(horzInput) > crawlingSpeed / maxSpeed)
                {
                    if (grounded)
                    {
                        horzInput -= slideDecay * Mathf.Sign(horzInput);
                    }
                }
                else if (Input.GetKey("a") && Input.GetKey("d"))
                {
                    horzInput -= gravity * Mathf.Sign(horzInput);
                }
                else if (Input.GetAxis("Horizontal") > 0)
                {
                    if (horzInput < 0)
                    {
                        horzInput = 0;
                    }
                    horzInput = crawlingSpeed / maxSpeed;
                }
                else if (Input.GetAxis("Horizontal") < 0)
                {
                    if (horzInput > 0)
                    {
                        horzInput = 0;
                    }
                    horzInput = -crawlingSpeed / maxSpeed;
                }
                else if (horzInput != 0)
                {
                    horzInput -= gravity * Mathf.Sign(horzInput);
                }
            }
            else
            {
                if (Input.GetKey("a") && Input.GetKey("d"))
                {
                    horzInput -= gravity * Mathf.Sign(horzInput);
                }
                else if (Input.GetAxis("Horizontal") > 0)
                {
                    if (horzInput < 0)
                    {
                        horzInput = 0;
                    }
                    else if (horzInput < minSpeed / maxSpeed)
                    {
                        horzInput = minSpeed / maxSpeed;
                    }
                    horzInput += sensitivity;
                }
                else if (Input.GetAxis("Horizontal") < 0)
                {
                    if (horzInput > 0)
                    {
                        horzInput = 0;
                    }
                    else if (horzInput > -minSpeed / maxSpeed)
                    {
                        horzInput = -minSpeed / maxSpeed;
                    }
                    horzInput -= sensitivity;
                }
                else if (horzInput != 0)
                {
                    horzInput -= gravity * Mathf.Sign(horzInput);
                }
            }

            if (((Input.GetAxis("Horizontal") > 0 && rb2d.Cast(Vector2.right, contactFilter, results, wallCheckDistance + shellRadius) > 0) ||
                (Input.GetAxis("Horizontal") < 0 && rb2d.Cast(Vector2.left, contactFilter, results, wallCheckDistance + shellRadius) > 0)) &&
                velocity.x < 0.001)
            {
                horzInput = 0;
            }

            if ((Mathf.Sign(horzInput) != Mathf.Sign(oldHorzInput) || Mathf.Abs(horzInput) < deadzone) && (Input.GetAxis("Horizontal") == 0 || (Input.GetKey("a") && Input.GetKey("d"))))
            {
                horzInput = 0f;
            }

            if (Mathf.Abs(horzInput) > 1)
            {
                horzInput = Mathf.Sign(horzInput) * 1;
            }
        }
    }

    private void SetDown()
    {
        down = Input.GetButton("Down");
    }

    private void Slide()
    {
        if (Input.GetButtonDown("Down") && horzInput != 0 && (animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Run") || animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Aerial") ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("Base.Jump")))
        {
            SwitchHitbox(false);
            if (rb2d.Cast(Vector2.left, contactFilter, results, shellRadius) == 0
                && rb2d.Cast(Vector2.right, contactFilter, results, shellRadius) == 0)
            {
                sliding = true;
                animator.SetTrigger("slideTrigger");
            }
            else
            {
                SwitchHitbox(true);
            }
        }

        if (Input.GetButtonUp("Down") && sliding)
        {
            SwitchHitbox(true);
            if (rb2d.Cast(Vector2.up, contactFilter, results, shellRadius) == 0)
            {
                sliding = false;
            }
            else
            {
                SwitchHitbox(false);
            }
        }

        if (sliding && !down)
        {
            SwitchHitbox(true);
            if (rb2d.Cast(Vector2.up, contactFilter, results, shellRadius) == 0)
            {
                sliding = false;
            }
            else
            {
                SwitchHitbox(false);
            }
        }
    }

    private void SwitchHitbox(bool toStanding)
    {
        colRun.SetActive(toStanding);
        colSlide.SetActive(!toStanding);
    }

    private void CheckWallJump()
    {
        if (((Input.GetAxis("Horizontal") > 0 && rb2d.Cast(Vector2.right, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0) ||
            (Input.GetAxis("Horizontal") < 0 && rb2d.Cast(Vector2.left, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0)) &&
            velocity.x < 0.001 && !grounded)
        {
            onWall = true;
            if (rb2d.Cast(Vector2.right, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0)
            {
                wallOnRight = true;
            }
            else if (rb2d.Cast(Vector2.left, contactFilter, results, wallJumpCheckDistance + shellRadius) > 0)
            {
                wallOnRight = false;
            }
        }
        else if (rb2d.Cast(Vector2.right, contactFilter, results, wallJumpCheckDistance + shellRadius) == 0 &&
                 rb2d.Cast(Vector2.left, contactFilter, results, wallJumpCheckDistance + shellRadius) == 0)
        {
            SetOnWall();
        }
    }

    private void SetOnWall()
    {
        onWall = false;
    }

    private void ResetJumpedWall()
    {
        jumpedWall = false;
    }

    private void ResetNoobJump()
    {
        noobJump = false;
        noobJumpResetCalled = false;
    }
}
