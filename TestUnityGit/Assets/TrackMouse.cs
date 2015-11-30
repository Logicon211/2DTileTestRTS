using UnityEngine;
using System.Collections;

public class TrackMouse : MonoBehaviour {

	public GameObject rocket;

	// Use this for initialization
	void Start () {
	
	}

	void Update() {

		bool faceRight = transform.GetComponentInParent<RealMan>().facingRight;
		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
		Vector3 dir = Input.mousePosition - pos;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

		//We need 2 rotations stored so that we can display them correctly when the model is flipped left, but also need the actual rotate for when we spawn new rockets down below
		Quaternion leftDisplayRotate = Quaternion.AngleAxis(angle, -Vector3.forward);
		Quaternion actualRotate = Quaternion.AngleAxis(angle, Vector3.forward);
		if(!faceRight) {
			transform.rotation = leftDisplayRotate; 

		} else {
			transform.rotation = actualRotate;
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
		//if(!(transform.parent.GetComponent<RealMan>()).facingRight) {
		//	Flip();
		//}
	}

	//void Flip() {
	//	facingRight = !facingRight;
//
	//	Vector3 theScale = transform.localScale;
	//	theScale.x *= -1;
	//	transform.localScale = theScale;
	//}
}
