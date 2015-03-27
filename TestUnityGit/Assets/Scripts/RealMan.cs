using UnityEngine;
using System.Collections;

public class RealMan : MonoBehaviour {

	bool facingRight = true;
	public float speed = 10f;

	private Rigidbody2D RB;
	private Animator anim;

	// Use this for initialization
	void Start () {
		RB = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		anim = GetComponent<Animator>();

		if(Input.GetKey(KeyCode.RightArrow)) {
			if(!facingRight) {
				Flip();
			}
			RB.velocity = new Vector2(speed, RB.velocity.y);
			anim.SetBool("Moving", true);
		} else if(Input.GetKey(KeyCode.LeftArrow)) {
			if(facingRight) {
				Flip();
			}
			RB.velocity = new Vector2(-speed, RB.velocity.y);
			anim.SetBool("Moving", true);
		} else {
			anim.SetBool("Moving", false);
		}
	}

	void Flip() {
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
