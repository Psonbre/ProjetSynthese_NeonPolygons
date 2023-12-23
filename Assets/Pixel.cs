using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pixel : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Initialize(Vector2 position, float scale, Color color)
    {
        rb.velocity = new Vector2(Random.value - 0.5f, Random.value - 0.5f) * 2f;
        transform.position = position;
        spriteRenderer.color = color;
        transform.localScale = new(scale, scale, scale);
    }
    void Update()
    {
        if (transform.position.y < -5f) gameObject.SetActive(false);
    }
}
