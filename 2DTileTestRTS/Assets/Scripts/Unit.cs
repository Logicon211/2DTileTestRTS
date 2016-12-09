using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {

	[HideInInspector]
	public bool jump = false;				// Condition for whether the player should jump.

	public bool facingRight = true;
	public float speed = 10f;
	public float jumpSpeed = 20f;

	public int width = 4;
	public int height = 6;

	private Rigidbody2D RB;
	private Animator anim;

	private Transform groundCheck1;
	private Transform groundCheck2;
	private bool grounded = false;			// Whether or not the player is grounded.

	// Use this for initialization
	void Start () {
		RB = GetComponent<Rigidbody2D>();
		groundCheck1 = transform.FindChild ("groundCheck1");
		groundCheck2 = transform.FindChild ("groundCheck2");
	}

	void Update() {
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		bool grounded1 = Physics2D.Linecast(transform.position, groundCheck1.position, 1 << LayerMask.NameToLayer("Ground")); 
		bool grounded2 = Physics2D.Linecast(transform.position, groundCheck2.position, 1 << LayerMask.NameToLayer("Ground"));
		//Check in between the 2 ground checks
		bool grounded3 = Physics2D.Linecast(groundCheck1.position, groundCheck2.position, 1 << LayerMask.NameToLayer("Ground"));
		grounded = grounded1 || grounded2 || grounded3;
		
		// If the jump button is pressed and the player is grounded then the player should jump.
		if(Input.GetAxis("Vertical") > 0 && grounded) {
			jump = true;
		}

	}
	
	// Update is called once per frame
	void FixedUpdate () {

		anim = GetComponent<Animator>();
		float move = Input.GetAxis("Horizontal");

		if(move != 0/*Input.GetKey(KeyCode.RightArrow*/) {
			if(move > 0) {
				if(!facingRight) {
					Flip();
				}
			} else {
				if(facingRight) {
					Flip();
				}
			}
			RB.velocity = new Vector2(speed * move, RB.velocity.y);
			anim.SetBool("Moving", true);
		} else {
			anim.SetBool("Moving", false);
		}

		if (jump) {
			//RB.AddForce(new Vector2(0f, jumpForce));
			RB.velocity = new Vector2(RB.velocity.x, jumpSpeed);
			anim.SetBool("Jumping", true);
			jump = false; //reset the jump flag so it doesn't happen again immediately
		}
	}

	void OnCollisionEnter2D (Collision2D col) 
	{
		if(col.gameObject.layer == LayerMask.NameToLayer("Ground")) {
			anim.SetBool("Jumping", false);
		}
	}

	void Flip() {
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;

		foreach (Transform child in transform) {
			Vector3 childScale = child.localScale;
			childScale.x *= -1;
			child.localScale = childScale;
		}
	}
}
