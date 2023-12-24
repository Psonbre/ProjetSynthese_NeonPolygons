using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class DestroyablePlatform : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;
	private Texture2D texture;
	private PolygonCollider2D polygonCollider;
	private int pixelCount = 100;
	private bool checkForSplit = false;
	[SerializeField] private Pixel pixelPrefab;

	void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		texture = DuplicateTexture(spriteRenderer.sprite.texture);
		spriteRenderer.sprite = Sprite.Create(texture, spriteRenderer.sprite.rect, new Vector2(0.5f, 0.5f));
		polygonCollider = GetComponent<PolygonCollider2D>();
		UpdatePixelCount();
	}

	void FixedUpdate()
	{
		if (checkForSplit) CheckForSplit();
	}

	private void OnTriggerStay2D(Collider2D collision)
	{
		RemovePixels(collision);
	}

	private Texture2D DuplicateTexture(Texture2D original)
	{
		Texture2D duplicate = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
		duplicate.filterMode = FilterMode.Point;
		Graphics.CopyTexture(original, duplicate);
		return duplicate;
	}



	private void RemovePixels(Collider2D collider)
	{
		Vector3 spriteCenterInWorld = spriteRenderer.bounds.center;
		Vector3 colliderCenterInWorld = collider.bounds.center;

		// Convert the center offset to local space
		Vector3 centerOffsetLocal = transform.InverseTransformPoint(colliderCenterInWorld) - transform.InverseTransformPoint(spriteCenterInWorld);
		float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

		// Calculate texture coordinates
		Vector2 textureCenter = new Vector2(texture.width / 2, texture.height / 2);
		Vector2 colliderCenterInTexture = textureCenter + new Vector2(centerOffsetLocal.x * pixelsPerUnit, centerOffsetLocal.y * pixelsPerUnit);

		// Calculate collider size in texture space
		Vector2 colliderSizeInTexture = new Vector2(collider.bounds.size.x * pixelsPerUnit, collider.bounds.size.y * pixelsPerUnit);

		int leftBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.x - colliderSizeInTexture.x / 2), 0, texture.width);
		int bottomBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.y - colliderSizeInTexture.y / 2), 0, texture.height);
		int rightBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.x + colliderSizeInTexture.x / 2), 0, texture.width);
		int topBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.y + colliderSizeInTexture.y / 2), 0, texture.height);

		int width = rightBound - leftBound;
		int height = topBound - bottomBound;

		if (width <= 0 || height <= 0)
		{
			return; // No valid area to modify
		}

		Color[] pixels = texture.GetPixels(leftBound, bottomBound, width, height);
		bool removedPixels = false;

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Vector2 pixelPoint = new Vector2(x + leftBound, y + bottomBound);
				Vector2 worldPoint = GetPixelToWorld((int)pixelPoint.x, (int)pixelPoint.y);

				if (collider.OverlapPoint(worldPoint))
				{
					int index = y * width + x;
					if (pixels[index].a > 0f)
					{
						removedPixels = true;
						DisassemblePixel(worldPoint, pixels[index]);
						pixels[index] = new Color(0, 0, 0, 0);
						ReducePixelCountBy(1);
					}
				}
			}
		}

		if (removedPixels)
		{
			texture.SetPixels(leftBound, bottomBound, width, height, pixels);
			texture.Apply();
			Destroy(polygonCollider);
			polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
			checkForSplit = true;
		}
	}




	bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
	{
		bool isInside = false;
		for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
		{
			if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
				(point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
			{
				isInside = !isInside;
			}
		}
		return isInside;
	}

	private void CheckForSplit()
	{
		checkForSplit = false;
		if (polygonCollider.pathCount > 1) 
		{
			Vector2[] mainPath = polygonCollider.GetPath(0);
			List<Vector2[]> remainingPaths = new();
			remainingPaths.Add(mainPath);

			for (int i = 1; i < polygonCollider.pathCount; i++)
			{
				Vector2[] currentPath = polygonCollider.GetPath(i);
				bool isContained = true;

				foreach (Vector2 point in currentPath)
				{
					if (!IsPointInPolygon(mainPath, point))
					{
						isContained = false;
						break;
					}
				}

				if (isContained) remainingPaths.Add(currentPath);

				else
				{
					DestroyablePlatform newPlatform = Instantiate(gameObject).GetComponent<DestroyablePlatform>();
					newPlatform.polygonCollider.pathCount = 1;
					newPlatform.polygonCollider.SetPath(0, currentPath);
					newPlatform.RemoveSplitPixels();
				}
			}
			polygonCollider.pathCount = 0;
			polygonCollider.pathCount = remainingPaths.Count;
			for (int i = 0; i < polygonCollider.pathCount; i++) polygonCollider.SetPath(i, remainingPaths[i]);
			RemoveSplitPixels();
		}
	}

	private void RemoveSplitPixels()
	{
		Rect boundingBox = GetColliderBoundingBox(polygonCollider);
		Color[] pixels = texture.GetPixels();
		Color transparent = new Color(0, 0, 0, 0);
		for (int y = 0; y < texture.height; y++)
		{
			for (int x = 0; x < texture.width; x++)
			{
				int i = y * texture.width + x;
				if (pixels[i].a > 0)
				{
					Vector2 worldPoint = GetPixelToWorld(x, y);

					if (!boundingBox.Contains(worldPoint) || !polygonCollider.OverlapPoint(worldPoint))
					{
						pixels[i] = transparent;
						ReducePixelCountBy(1);
					}
				}
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
	}

	private Rect GetColliderBoundingBox(PolygonCollider2D collider)
	{
		Vector2 min = collider.bounds.min;
		Vector2 max = collider.bounds.max;

		return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
	}

	private void DisassemblePixel(Vector2 pos, Color color)
	{
		PixelManager.Instance.GetPixel().Initialize(pos, (1f / spriteRenderer.sprite.pixelsPerUnit) * transform.localScale.x, color);
	}

	private void ReducePixelCountBy(int nb)
	{
		pixelCount-=nb;
		
		if (pixelCount <= 100 / transform.localScale.x) Destroy(gameObject);
	}

	private void UpdatePixelCount()
	{
		pixelCount = 0;
		Color[] pixels = texture.GetPixels();
		foreach (Color pixel in pixels)
		{
			if (pixel.a > 0f)
			{
				pixelCount++;
			}
		}
	}

	private Vector2 GetPixelToWorld(int x, int y)
	{
		Vector3 localPos = new Vector3((float)x / texture.width, (float)y / texture.height, 0);
		localPos.x *= spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
		localPos.y *= spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
		localPos -= (Vector3)spriteRenderer.sprite.pivot / spriteRenderer.sprite.pixelsPerUnit;
		return transform.TransformPoint(localPos);
	}
}
