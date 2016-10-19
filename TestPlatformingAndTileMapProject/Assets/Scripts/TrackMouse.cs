using UnityEngine;
using System.Collections;

public class TrackMouse : MonoBehaviour {

	public GameObject rocket;

	private bool upsideDown = false;

	// Use this for initialization
	void Start () {
	
	}

	void Update() {

		bool faceRight = transform.GetComponentInParent<Unit>().facingRight;

		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
		Vector3 dir = Input.mousePosition - pos;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

		//We need 2 rotations stored so that we can display them correctly when the model is flipped left, but also need the actual rotate for when we spawn new rockets down below
		Quaternion actualRotate = Quaternion.AngleAxis(angle, Vector3.forward);
		transform.rotation = actualRotate;

		if (angle > 90f || angle < -90f) {
			if (!upsideDown) {
				YFlip ();
			}
		} else {
			if (upsideDown) {
				YFlip ();
			}
		}

		if(Input.GetMouseButtonDown(0)) {
			GameObject rocketLaunched = null;
			if(faceRight) {
				rocketLaunched = Instantiate(rocket, transform.position, actualRotate) as GameObject;
			} else {
				Vector3 posit = transform.localPosition;
				posit = new Vector3(-posit.x,posit.y,posit.z);
				rocketLaunched = Instantiate(rocket, transform.position, actualRotate) as GameObject;
			}
			rocketLaunched.GetComponent<Rigidbody2D>().velocity = rocketLaunched.transform.right * 20;
		}
	}

	// Update is called once per frame
	void FixedUpdate () {
		//if(!(transform.parent.GetComponent<Unit>()).facingRight) {
		//	Flip();
		//}
	}

	//Used for when the tracked object goes past 90 or -90 degrees so we flip it vertically so it's no longer upside down
	void YFlip() {
		upsideDown = !upsideDown;

		Vector3 theScale = transform.localScale;
		theScale.y *= -1;
		transform.localScale = theScale;
	}
}
