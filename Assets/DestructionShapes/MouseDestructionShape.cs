using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDestructionShape : MonoBehaviour
{
    Camera cam;
    Collider2D shapeCollider;
    // Start is called before the first frame update
    void Start()
    {
		shapeCollider = GetComponent<Collider2D>();
        shapeCollider.isTrigger = true;
        shapeCollider.enabled = false;
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 pos = cam.ScreenToWorldPoint(Input.mousePosition);
		transform.position = pos;
        if (Input.GetMouseButtonDown(0)) shapeCollider.enabled = true;
        if (Input.GetMouseButtonUp(0)) shapeCollider.enabled = false;
	}
}
