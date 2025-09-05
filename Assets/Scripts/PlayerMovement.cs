using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input System")]
    public InputManager playerInput;

    [Header("Animations")]
    private Animator animator;

    [Header("Physics System")]
    private Rigidbody2D rb;
    private float gravity;

    [Header("Movement")]
    public float moveSpeed = 7f;

    public int facingDirection = 1;
    private Vector2 movement;
    private bool faceright = true;

    [Header("Jump")]
    public float jumpSpeed = 20f;
    public int maxJumps = 2;
    [Space]
    public float speedReduction = 0.2f;

    private int jumpsLeft;
    private bool canJump = false;

    [Header("Fall Control")]
    public float fallSpeed = 15f;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 4f;
    public float leaveWallSlideTime = 0.2f;

    private float leaveWallSlideTimer;

    [Header("Wall Jump")]
    public float wallJumpSpeed = 15f;
    public float wallJumpXDirection = 1f;
    public float wallJumpYDirection = 2f;
    public float wallJumpTime = 0.12f;
    [Space]
    public float wallJumpImpulse = 4f;
    public float wallJumpSpeedReduction = -1f;

    private float wallJumpTimer;
    private Vector2 wallJumpDirection;
    private bool getImpulse;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashTime = 0.12f;
    public float dashCooldownTime = 0.3f;
    public float upwardDashCorrection = 1f;
    public int maxDashes = 1;
    public float dashHoldingTime = 0.03f;
    [SerializeField]
    private int dashesLeft;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashHoldingTimer;
    private Vector2 dashVector;
    private bool dashFromWall = false;

    [Header("Surroundings")]
    public float groundWidth = 0.7f;
    public float groundHeight = 0.2f;
    public float wallCheckDistance = 0.2f;
    [Space]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask whatIsGround;

    private Vector2 groundSize;

    [Header("Input Values")]
    private Vector2 movementInput;

    [Header("State List")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isAerial;
    [SerializeField] private bool isTouchingWall;
    [SerializeField] public bool isWallSliding;
    [SerializeField] private bool isWallJumping = false;
    [SerializeField] private bool isDashing = false;
    [SerializeField] private bool isHoldingAfterDash = false;
    [SerializeField] private bool isLeavingWall = false;

    //AWAKE
    private void Awake()
    {
        //Initialises and configures the player input and actions
        playerInput = new InputManager();
        playerInput.Player.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        playerInput.Player.Jump.started += ctx => Jump();
        playerInput.Player.Jump.canceled += ctx => EndJump();
        playerInput.Player.Dash.started += ctx => Dash();
    }

    //START
    private void Start()
    {
        //Gets the rigidbody the player is using
        rb = GetComponent<Rigidbody2D>();
        //Gets the animator for the animations of the player
        animator = GetComponent<Animator>();
        //Configures the box which detects grounded state
        groundSize = new Vector2(groundWidth, groundHeight);
        //Initialises the available jumps to its maximum
        jumpsLeft = maxJumps;
        //Initialises the jumping and wall jumping capability
        canJump = false;
        wallJumpTimer = wallJumpTime;
        isWallJumping = false;
        //Initialises the wall Jump Direction and option to get impulse after it
        wallJumpDirection = new Vector2(wallJumpXDirection, wallJumpYDirection);
        wallJumpDirection.Normalize();
        wallJumpDirection = wallJumpSpeed * wallJumpDirection;
        getImpulse = true;
        //Initialises timer to leave wall slide
        leaveWallSlideTimer = leaveWallSlideTime;
        //Saves the gravity value for later
        gravity = rb.gravityScale;
        //Initialises dash information
        dashTimer = dashTime;
        dashCooldownTimer = -1f;
        dashesLeft = maxDashes;
        dashHoldingTimer = dashHoldingTime;
        //Facing direction
        faceright = true;
        if (faceright)
        {
            facingDirection = 1;
        }
        else
        {
            facingDirection = -1;
        }
    }

    //UPDATE
    private void Update()
    {
        //Checks the Surroundings of the player for player state
        CheckSurroundings();
        //Manages the jumps the player has based on the state
        JumpManager();
        //Manages the facing direction
        FacingManager();
        //Allows for wall sliding
        WallSlide();
        //Leaves Wall Sliding after a certain delay
        LeaveWallSlide();
        //Manages the animations
        UpdateAnimations();
    }

    //FIXED UPDATE
    private void FixedUpdate()
    {
        //Controls Fallspeed
        FallControl();
        //Moves the player
        Move();
        //Wall Jumping is done via Coroutines after registering input. Thats why its not here
    }

    //CUSTOM FUNCTIONS

    //Animations
    private void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(movement.x));
        animator.SetFloat("ySpeed", rb.velocity.y);
        animator.SetBool("isAerial", isAerial);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWallJumping", isWallJumping);
        animator.SetBool("isWallSliding", isWallSliding);
        animator.SetBool("isDashing", isDashing);
        animator.SetBool("isTouchingWall", isTouchingWall);
        animator.SetBool("isLeavingWall", isLeavingWall);
    }

    //State Declaration
    private void CheckSurroundings()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, wallCheck.right, wallCheckDistance, whatIsGround); //Checks for walls
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundSize, 0f, whatIsGround) && !isWallJumping; //Checks for ground
        isWallSliding = !isGrounded && isTouchingWall && rb.velocity.y < 0f;
        if (!isGrounded && !isWallSliding) //Checks if the player is in the air

        {
            isAerial = true;
        }
        else
        {
            isAerial = false;
        }
    }

    //Horizontal Movement
    private void Move()
    {
        if ((!isGrounded && isTouchingWall) || isWallJumping || isDashing) //If the player is wall sliding moving horizontally or dashing is not available. When moving and dashing some inertia is left
        {
            return;
        }
        movement = moveSpeed * movementInput; //Set movement according to Input
        rb.velocity = new Vector2(movement.x, rb.velocity.y); //Set player speed
    }
    //Horizontal Orientation
    private void FacingManager()
    {
        if((!isGrounded && isTouchingWall) || isWallJumping || isDashing) //If the player is wall sliding flipping isn't available
        {
            return;
        }
        if(faceright && movementInput.x < 0 || !faceright && movementInput.x > 0) //Checks if flipping is needed
        {
            Flip();
        }
    }
    private void Flip()
    {
        faceright = !faceright;
        facingDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
    }

    //Vertical Movement
    //JumpManager and DashManager
    private void JumpManager()
    {
        if (isGrounded || isTouchingWall) //Replenishes the number of jumps to the maximum if player is grounded or wall sliding
        {
            jumpsLeft = maxJumps;
            dashesLeft = maxDashes;
        }
        else if(jumpsLeft == maxJumps) //Solves a bug where the player could have 1 extra jump over the maximum
        {
            jumpsLeft--;
        }
    }
    //Check If Can Jump or Wall Jump
    private bool CheckIfCanJump()
    {
        if (isDashing) //Cannot jump while dashing
        {
            return false;
        }
        if (isHoldingAfterDash && jumpsLeft > 0) //After the dash you can jump while standing (last dash phase)
        {
            isHoldingAfterDash = false;
            return false;
        }
        if (jumpsLeft > 0 && !isTouchingWall) //Allows for double jump and avoids simple jumps from walls or while dashing
        {
            canJump = true;
            return canJump;
        }
        else if (!isGrounded && isTouchingWall) //Allows for wall jumping
        {
            canJump = false;
            SetWallJump();
            return canJump;
        }
        else if (isTouchingWall && isGrounded) //Allows for jumping in corners
        {
            canJump = true;
            return canJump;
        }
        else //If no jumps left no jumping is allowed
        {
            canJump = false;
        }

        return canJump; //Always returns canJump
    }
    //Jump
    private void Jump()
    {
        if (!CheckIfCanJump())
        {
            return;
        }
        jumpsLeft--;
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed); //Sets jump
    }
    //Dynamic Jump & Wall Jump
    private void EndJump()
    {
        if (!isDashing && !isWallJumping && rb.velocity.y > 0) //Ends a jump upon button release
        {
            rb.velocity = new Vector2(rb.velocity.x, speedReduction * rb.velocity.y);
        }
        else if (isWallJumping) //Avoids getting an impulse after a wall jump upon button release
        {
            getImpulse = false;
        }
    }
    //Fall Control
    private void FallControl()
    {
        if(isAerial && rb.velocity.y <= -fallSpeed) //Changes gravity according to whether the maximum falling speed is acquired or not
        {
            rb.gravityScale = 0f;
        }
        else
        {
            rb.gravityScale = gravity;
        }
    }

    //Wall Movement
    //Wall Slide
    private void WallSlide()
    {
        if (!isTouchingWall || isGrounded) //Exits if not wall sliding
        {
            return;
        }
        if (rb.velocity.y <= -wallSlideSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed); //Sets Wall Slide Speed
        }
    }
    //Leave Wall Slide
    private void LeaveWallSlide() //PUEDE DAR ERRORES
    {
        if (!isTouchingWall || isGrounded) //Exits if not wall sliding
        {
            return;
        }

        if (faceright && movementInput.x < 0) //These following statements regulate the player wanting to leave the wall
        {
            leaveWallSlideTimer -= Time.deltaTime;
        }
        else if (!faceright && movementInput.x > 0)
        {
            leaveWallSlideTimer -= Time.deltaTime;
        }
        else
        {
            leaveWallSlideTimer = leaveWallSlideTime;
        }

        if (leaveWallSlideTimer <= 0) //If the timer runs out the player leaves the wall
        {
            StartCoroutine("LeaveWallSlideCoroutine");
            leaveWallSlideTimer = leaveWallSlideTime;
        }
    }
    private IEnumerator LeaveWallSlideCoroutine()
    {
        //This coroutine helps with an animation bug
        isLeavingWall = true;
        UpdateAnimations();
        yield return new WaitForEndOfFrame();
        UpdateAnimations();
        Flip();
        leaveWallSlideTimer = leaveWallSlideTime;
        isLeavingWall = false;
        StopCoroutine("LeaveWallSlideCoroutine");
    }
    //Wall Jump
    private void SetWallJump()
    {
        isWallJumping = true;
        animator.SetBool("isWallJumping", isWallJumping);
        wallJumpDirection = new Vector2(wallJumpXDirection * (-facingDirection), wallJumpYDirection); //Sets wall jump direction
        wallJumpDirection.Normalize();
        wallJumpDirection = wallJumpSpeed * wallJumpDirection; //Sets the speed

        rb.velocity = wallJumpDirection; //Initial impulse: helps animator
        UpdateAnimations();

        Flip(); //Flips character

        wallJumpTimer = wallJumpTime; //Sets the timer for the wall jump
        StartCoroutine("WallJumpCoroutine"); //Starts the coroutine where the character moves without input (except for dashes)
    }
    //Wall Jump Timer Coroutine
    private IEnumerator WallJumpCoroutine()
    {
        while (wallJumpTimer > 0f && !isDashing) //If the player doesnt dash during the wall jump the timer goes on and the player is moved
        {
            wallJumpTimer -= Time.fixedDeltaTime;
            rb.velocity = wallJumpDirection;
            yield return new WaitForFixedUpdate();
        }
        if (getImpulse && wallJumpTimer <= 0f) //If the button was released during the wall jump the player gets impulse
        {
            rb.velocity += new Vector2(0f, wallJumpImpulse);
        }
        else if(wallJumpTimer <= 0f) //Else they dont. The condition avoids getting impulse when dashing while wall jumping
        {
            rb.velocity += new Vector2(0f, wallJumpSpeedReduction * wallJumpImpulse);
        }
        isWallJumping = false;
        wallJumpTimer = wallJumpTime;
        getImpulse = true;
        StopCoroutine("WallJumpCoroutine");
    }
    
    //Dashing
    //Check if Can Dash
    private bool CheckIfCanDash()
    {
        if(dashCooldownTimer > 0f || dashesLeft <= 0) //If theres a cooldown or there are no dashes left dash isnt available
        {
            //case dashing not permitted
            return false;
        }
        if (!isGrounded && isTouchingWall) //If dashing from the wall will change direction
        {
            dashFromWall = true;
        }
        else //If dashing normally wont change direction
        {
            dashFromWall = false;
        }
        return true;
    }
    //Dash
    private void Dash()
    {
        if (!CheckIfCanDash())
        {
            return;
        }
        //start dashing
        isDashing = true;
        dashCooldownTimer = dashCooldownTime;
        StartCoroutine("DashTimerCoroutine");
    }
    //Dash Timer Coroutine
    private IEnumerator DashTimerCoroutine()
    {
        if (dashFromWall) //The player dashes opposite the wall if it's touching it
        {
            Flip();
        }
        dashVector = new Vector2(dashSpeed * facingDirection, upwardDashCorrection); //This is the velocity of the player during the dash
        dashesLeft--; // One dash is spent 
        while (dashTimer > 0f) //Dashes while the timer is still active
        {
            rb.velocity = dashVector;
            dashTimer -= Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        //end dash - program time stopped in air: will require another while() yield return...
        isDashing = false;
        isHoldingAfterDash = true;
        while (dashHoldingTimer > 0f && isHoldingAfterDash)
        {
            rb.velocity = new Vector2 (movement.x, upwardDashCorrection);
            dashHoldingTimer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        if (!isHoldingAfterDash && !isTouchingWall)
        {
            jumpsLeft--;
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        }
        else if (!isHoldingAfterDash && isTouchingWall)
        {
            SetWallJump();
        }
        isHoldingAfterDash = false;
        rb.gravityScale = gravity;
        dashTimer = dashTime;
        dashHoldingTimer = dashHoldingTime;
        StartCoroutine("DashCooldownCoroutine");
        StopCoroutine("DashTimerCoroutine");
    }
    //Dash Cooldown Timer Coroutine
    private IEnumerator DashCooldownCoroutine()
    {
        while(dashCooldownTimer > 0f)
        {
            //dash is on cooldown
            dashCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        //dash is available again the cd is reset on Dash() (while cd is <= 0 it is possible to dash else it isn't)
        StopCoroutine("DashCooldownCoroutine");
    }

    //ON ENABLE & ON DISABLE GAMEOBJECT
    private void OnEnable()
    {
        playerInput.Enable();
    }
    private void OnDisable()
    {
        playerInput.Disable();
    }

    // GIZMOS
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(groundCheck.position, groundSize); //groundCheck drawer
        Gizmos.DrawRay(wallCheck.position, wallCheckDistance * wallCheck.right); //wallCheck drawer
    }
}