using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	[SerializeField] private float speed = 5f;
	[SerializeField] private float stepHeight = 1f;
	[SerializeField] private float stepCheckDistance = 1f;
	[SerializeField] private float groundCheckDistance = 1f;
	private CapsuleCollider2D capsuleCollider;
	private Rigidbody2D rb;
	private bool isGrounded;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		capsuleCollider = GetComponent<CapsuleCollider2D>();
	}

	void Update()
	{
		float move = Input.GetAxis("Horizontal");
		Vector2 moveDirection = new Vector2(move * speed, rb.velocity.y);
		rb.velocity = moveDirection;

		RaycastHit2D groundHit = Physics2D.Raycast(new(transform.position.x, capsuleCollider.bounds.min.y - 0.05f), -Vector2.up, groundCheckDistance);
		if (groundHit.collider) isGrounded = groundHit.collider.CompareTag("Ground");

		if (move != 0)
		{
			RaycastHit2D stepHit = new();
			if (move < 0) stepHit = Physics2D.Raycast(new Vector2(capsuleCollider.bounds.min.x - 0.05f, transform.TransformPoint(0,stepHeight,0).y), new Vector2(move, 0), stepCheckDistance);
			if (move > 0) stepHit = Physics2D.Raycast(new Vector2(capsuleCollider.bounds.max.x + 0.05f, transform.TransformPoint(0, stepHeight, 0).y), new Vector2(move, 0), stepCheckDistance);
			if (stepHit.collider != null && isGrounded)
			{
				Vector3 newPos = new Vector3(transform.position.x + Mathf.Sign(move), transform.position.y + (stepHeight + transform.localScale.y / 2f) * 2f, transform.position.z);
				RaycastHit2D newPosHit = Physics2D.Raycast(newPos, Vector2.zero);
				if (newPosHit.collider == null || !newPosHit.collider.CompareTag("Ground")) transform.position = newPos;
			}
		}

		if (isGrounded && groundHit.collider) transform.parent = groundHit.collider.transform;
	}
}
