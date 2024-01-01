using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 0.05f;
    [SerializeField] private float jumpForce = 0.15f;
    private Rigidbody2D rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        rb.velocityX = Input.GetAxis("Horizontal") * speed;
		if (Input.GetButtonDown("Jump")) rb.velocityY = jumpForce;
	}
}
