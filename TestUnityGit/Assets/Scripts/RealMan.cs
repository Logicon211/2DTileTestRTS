using UnityEngine;
using System.Collections;

public class RealMan : MonoBehaviour {

	bool facingRight = true;
	public float speed = 10f;
	public float jumpForce;

	private Rigidbody2D RB;
	private Animator anim;
	private Transform groundCheck;

	// Use this for initialization
	void Start () {
		RB = GetComponent<Rigidbody2D>();
		groundCheck = transform.FindChild ("groundCheck");
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		anim = GetComponent<Animator>();
		float move = Input.GetAxis("Horizontal");
		float jump = Input.GetAxis ("Vertical");

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

		if (jump > 0 /* and grounnd check */) {
			Debug.Log("Jump");
			RB.AddForce(new Vector2(0, jumpForce));
		}
	}

	void Flip() {
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
