using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour {

	private Rigidbody2D rigidbody;

	// Use this for initialization
	void Start () {
		rigidbody = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.LeftArrow)) 
		{
			rigidbody.velocity += (new Vector2(-transform.right.x, -transform.right.y) * 100f * Time.deltaTime);
			//rigidbody.AddForce(-transform.right * 5000f * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.RightArrow)) {
			rigidbody.AddForce(transform.right * 5000f * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.UpArrow)) {
			rigidbody.AddForce(transform.up * 5000f * Time.deltaTime);
		}

		if(Input.GetMouseButton(0)){
			Vector2 mousePos = new Vector2 (Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
			Debug.Log(mousePos);
			Debug.Log(transform.position);
			Debug.Log(new Vector2(transform.position.x - mousePos.x, transform.position.y - mousePos.y).normalized);
			rigidbody.AddForce(new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y).normalized * 2500f * Time.deltaTime);
		
			Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}
	}
}
