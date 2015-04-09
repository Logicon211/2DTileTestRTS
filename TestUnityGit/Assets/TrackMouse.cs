using UnityEngine;
using System.Collections;

public class TrackMouse : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
		Vector3 dir = Input.mousePosition - pos;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		if(!(transform.parent.GetComponent<RealMan>()).facingRight) {
			transform.rotation = Quaternion.AngleAxis(angle, -Vector3.forward); 
		} else {
			transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		}
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
