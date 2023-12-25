using System.Collections.Generic;
using UnityEngine;

public class DestroyablePlatform : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;
	private Texture2D texture;
	private PolygonCollider2D polygonCollider;
	private int pixelCount = 100;
	private SpritePixelCollider2D pixelPerfectColliderScript;
	[SerializeField] private Pixel pixelPrefab;

	void Awake()
	{
		pixelPerfectColliderScript = GetComponent<SpritePixelCollider2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		texture = DuplicateTexture(spriteRenderer.sprite.texture);
		spriteRenderer.sprite = Sprite.Create(texture, spriteRenderer.sprite.rect, new Vector2(0.5f, 0.5f));
		polygonCollider = GetComponent<PolygonCollider2D>();
		UpdatePixelCount();
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

		Vector3 centerOffsetLocal = transform.InverseTransformPoint(colliderCenterInWorld) - transform.InverseTransformPoint(spriteCenterInWorld);
		float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

		Vector2 textureCenter = new Vector2(texture.width / 2, texture.height / 2);
		Vector2 colliderCenterInTexture = textureCenter + new Vector2(centerOffsetLocal.x * pixelsPerUnit, centerOffsetLocal.y * pixelsPerUnit);

		Vector2 colliderSizeInTexture = new Vector2(collider.bounds.size.x * pixelsPerUnit, collider.bounds.size.y * pixelsPerUnit);

		int leftBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.x - colliderSizeInTexture.x / 2), 0, texture.width);
		int bottomBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.y - colliderSizeInTexture.y / 2), 0, texture.height);
		int rightBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.x + colliderSizeInTexture.x / 2), 0, texture.width);
		int topBound = Mathf.Clamp(Mathf.RoundToInt(colliderCenterInTexture.y + colliderSizeInTexture.y / 2), 0, texture.height);

		int width = rightBound - leftBound;
		int height = topBound - bottomBound;

		if (width <= 0 || height <= 0) return;

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
			pixelPerfectColliderScript.Regenerate();
			if (polygonCollider.pathCount > 1) CheckForSplit();
		}
	}

	private void CheckForSplit()
	{
		checkForSplit = false;
		if (polygonCollider.pathCount > 1)
		{
			Vector2[] mainPath = polygonCollider.GetPath(0);
			List<Vector2[]> remainingPaths = new();
			remainingPaths.Add(mainPath);

			GameObject tempObject = new GameObject("TempCollider");
			PolygonCollider2D tempCollider = tempObject.AddComponent<PolygonCollider2D>();
			tempCollider.pathCount = 1;
			tempCollider.SetPath(0, mainPath);

			for (int i = 1; i < polygonCollider.pathCount; i++)
			{
				Vector2[] currentPath = polygonCollider.GetPath(i);
				bool isContained = true;

				foreach (Vector2 point in currentPath)
				{
					if (!tempCollider.OverlapPoint(point))
					{
						isContained = false;
						break;
					}
				}

				if (isContained)
				{
					remainingPaths.Add(currentPath);
				}
				else
				{
					DestroyablePlatform newPlatform = Instantiate(gameObject).GetComponent<DestroyablePlatform>();
					newPlatform.polygonCollider.pathCount = 1;
					newPlatform.polygonCollider.SetPath(0, currentPath);
					newPlatform.RemoveSplitPixels();
				}
			}

			Destroy(tempObject);

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
		PixelManager.Instance.GetPixel().Initialize(pos, (100f / spriteRenderer.sprite.pixelsPerUnit) * transform.localScale.x, color);
	}

	private void ReducePixelCountBy(int nb)
	{
		pixelCount-=nb;
		
		if (pixelCount <= 25 / transform.localScale.x) Destroy(gameObject);
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
