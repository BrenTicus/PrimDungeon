using UnityEngine;
using System.Collections;

public class PlayerInput : MonoBehaviour {
	public float TurnSpeed = 1.5f;
	public float MoveSpeed = 0.1f;
	
	// Update is called once per frame
	void Update () {
		float x = Input.GetAxis("Horizontal");
		float y = Input.GetAxis("Vertical");

		transform.Rotate(Vector3.up, x * TurnSpeed);
		transform.Translate(0, 0, MoveSpeed * y);
	}
}
