using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{    
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    public EntityFX fx { get; private set; }
    #endregion

    [Header("Knockback info")]
    [SerializeField] protected Vector2 knockbackDirection;
    [SerializeField] protected float knockbackDuration;
    protected bool isKnocked;


    [Header("Collision info")]
    public Transform attackCheck;
    public float attackCheckRadius;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckDistance;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance;
    [SerializeField] protected LayerMask whatIsGround;

    public int facingxDir { get; private set; } = 1;
    protected bool facingRight = true;
    public int facingyDir { get; private set; } = -1;
    protected bool facingUp = false;

    protected virtual void Awake()
    {

    }
    
    protected virtual void Start()
    {
        fx = GetComponentInChildren<EntityFX>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

    }

    protected virtual void Update()
    {

    }
    public virtual void  Damage()
    {
        fx.StartCoroutine("FlashFX");
        StartCoroutine("HitKnockback");
        Debug.Log(gameObject.name + " was damaged");
    }

    protected virtual IEnumerator HitKnockback()
    {
        isKnocked = true;

        rb.velocity = new Vector2(knockbackDirection.x * -facingxDir, knockbackDirection.y);

        yield return new WaitForSeconds(knockbackDuration);

        isKnocked = false;
    }

    #region Velocity
    public void SetZeroVelocity()
    { 
        if(isKnocked)
        {
            return;
        }

        rb.velocity = new Vector2(0, 0);
    } 

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        if (isKnocked)
        {
            return;
        }

        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity, _yVelocity);
    }
    #endregion
    #region Collision
    //public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public virtual bool IsGroundDetected() { return true; }
    //public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    public virtual bool IsWallDetected() { return false; }
    protected virtual void OnDrawGizmos()
    {
        /*
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
        */
    }
    #endregion
    #region Flip
    public virtual void Flipx()
    {
        facingxDir = facingxDir * -1;
        facingRight = !facingRight;
        //transform.Rotate(0, 180, 0);
    }
    public virtual void Flipy()
    {
        facingyDir = facingyDir * -1;
        facingUp = !facingUp;
    }

    public virtual void FlipController(float _x, float _y)
    {
        if (_x > 0 && !facingRight)
        {
            Flipx();
        }
        else if (_x < 0 && facingRight)
        {
            Flipx();
        }
        if (_y > 0 && !facingUp)
        {
            Flipy();
        }
        else if (_y < 0 && facingUp)
        {
            Flipy();
        }
    }
    #endregion
}

