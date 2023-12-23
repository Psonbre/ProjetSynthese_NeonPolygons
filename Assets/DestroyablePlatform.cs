using UnityEngine;

public class DestructablePlatform : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;
	private Texture2D texture;
	private PolygonCollider2D polygonCollider;
	private int pixelCount = 100;
	[SerializeField] private Pixel pixelPrefab;

	void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		texture = DuplicateTexture(spriteRenderer.sprite.texture);
		spriteRenderer.sprite = Sprite.Create(texture, spriteRenderer.sprite.rect, new Vector2(0.5f, 0.5f));
		polygonCollider = GetComponent<PolygonCollider2D>();
		UpdatePixelCount();
	}

	private Texture2D DuplicateTexture(Texture2D original)
	{
		Texture2D duplicate = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
		Graphics.CopyTexture(original, duplicate);
		return duplicate;
	}

	void Update()
	{
		if (Input.GetMouseButton(0))
		{
			Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			if (polygonCollider.pathCount > 1) Split();

			if (spriteRenderer.bounds.Contains(clickPos))
			{
				RemovePixels(clickPos, 20);
			}
		}
	}

	private void Split()
	{
		DestructablePlatform newPlatform = Instantiate(gameObject).GetComponent<DestructablePlatform>();
		newPlatform.polygonCollider.SetPath(0, polygonCollider.GetPath(1));
		newPlatform.polygonCollider.pathCount = 1;
		newPlatform.RemoveSplitPixels();

		polygonCollider.pathCount = 1;
		RemoveSplitPixels();
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



	private void RemovePixels(Vector2 position, int radius)
	{
		radius = Mathf.RoundToInt(radius / transform.localScale.x);
		bool removedPixels = false;
		Vector3 localPos = transform.InverseTransformPoint(position);
		float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
		Vector2 texturePos = new Vector2(localPos.x * pixelsPerUnit, localPos.y * pixelsPerUnit) + new Vector2(texture.width / 2, texture.height / 2);

		int leftBound = Mathf.Max((int)texturePos.x - radius, 0);
		int rightBound = Mathf.Min((int)texturePos.x + radius, texture.width);
		int bottomBound = Mathf.Max((int)texturePos.y - radius, 0);
		int topBound = Mathf.Min((int)texturePos.y + radius, texture.height);

		Color[] pixels = texture.GetPixels();
		for (int x = leftBound; x < rightBound; x++)
		{
			for (int y = bottomBound; y < topBound; y++)
			{
				if ((x - texturePos.x) * (x - texturePos.x) + (y - texturePos.y) * (y - texturePos.y) <= radius * radius)
				{
					int index = y * texture.width + x;
					if (pixels[index].a > 0f)
					{
						removedPixels = true;
						DisassemblePixel(GetPixelToWorld(x, y), pixels[index]);
						pixels[index] = new Color(0, 0, 0, 0);
						ReducePixelCountBy(1);
					}
				}
			}
		}

		if (removedPixels)
		{
			texture.SetPixels(pixels);
			texture.Apply();
			Destroy(polygonCollider);
			polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
		}
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
