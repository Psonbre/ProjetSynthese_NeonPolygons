using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour
{
	private static PixelManager instance;

	public static PixelManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindFirstObjectByType<PixelManager>();
				if (instance == null)
				{
					GameObject obj = new GameObject("PixelManager");
					instance = obj.AddComponent<PixelManager>();
				}
			}
			return instance;
		}
	}

	[SerializeField] private int startingNumberOfPixels = 100;
	[SerializeField] private Pixel pixelPrefab;
	private List<Pixel> pixels = new List<Pixel>();

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		for (int i = 0; i < startingNumberOfPixels; i++)
		{
			Pixel pixel = Instantiate(pixelPrefab).GetComponent<Pixel>();
			pixel.gameObject.SetActive(false);
			pixels.Add(pixel);
			pixel.transform.parent = transform;
		}
	}

	public Pixel GetPixel()
	{
		Pixel pixel = pixels.Find(p => !p.isActiveAndEnabled);
		if (pixel == null)
		{
			pixel = Instantiate(pixelPrefab);
			pixel.gameObject.SetActive(false);
			pixels.Add(pixel);
		}
		pixel.gameObject.SetActive(true);
		pixel.transform.parent = transform;
		return pixel;
	}
}
