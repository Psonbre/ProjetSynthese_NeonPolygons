using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDestructionShape : MonoBehaviour
{
    Camera cam;
    PolygonCollider2D polygonCollider;
    // Start is called before the first frame update
    void Start()
    {
        polygonCollider = GetComponent<PolygonCollider2D>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 pos = cam.ScreenToWorldPoint(Input.mousePosition);
		transform.position = pos;
        if (Input.GetMouseButtonDown(0)) polygonCollider.enabled = true;
        if (Input.GetMouseButtonUp(0)) polygonCollider.enabled = false;
	}
}
