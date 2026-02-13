using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public Transform player;
    public int textureSize = 512;
    public float worldSize = 1000f; // Scale to match terrain
    public float revealRadius = 20f;

    private Texture2D m_fogTexture;
    private Color[] m_pixels;
    
    // UI/Camera can use this texture as a mask
    public RenderTexture fogRenderTexture;

    private void Start()
    {
        m_fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        m_pixels = new Color[textureSize * textureSize];

        // Initialize with black (hidden)
        for (int i = 0; i < m_pixels.Length; i++)
            m_pixels[i] = Color.black;

        m_fogTexture.SetPixels(m_pixels);
        m_fogTexture.Apply();
    }

    private void Update()
    {
        if (player == null) return;

        RevealArea(player.position);
    }

    private void RevealArea(Vector3 worldPos)
    {
        // Convert world pos to texture coordinates
        int x = Mathf.RoundToInt(((worldPos.x + worldSize / 2f) / worldSize) * textureSize);
        int z = Mathf.RoundToInt(((worldPos.z + worldSize / 2f) / worldSize) * textureSize);

        int radiusInPixels = Mathf.RoundToInt((revealRadius / worldSize) * textureSize);

        bool changed = false;
        for (int i = -radiusInPixels; i <= radiusInPixels; i++)
        {
            for (int j = -radiusInPixels; j <= radiusInPixels; j++)
            {
                if (i * i + j * j <= radiusInPixels * radiusInPixels)
                {
                    int px = x + i;
                    int pz = z + j;
                    if (px >= 0 && px < textureSize && pz >= 0 && pz < textureSize)
                    {
                        int index = pz * textureSize + px;
                        if (m_pixels[index].a < 1f)
                        {
                            m_pixels[index] = new Color(0, 0, 0, 1f); // Using alpha as "revealed"
                            changed = true;
                        }
                    }
                }
            }
        }

        if (changed)
        {
            m_fogTexture.SetPixels(m_pixels);
            m_fogTexture.Apply();
            
            // To pass this to a UI material, we usually use m_fogTexture directly
            // or blit it to a RenderTexture if required for shader effects
            if (fogRenderTexture != null)
            {
                Graphics.Blit(m_fogTexture, fogRenderTexture);
            }
        }
    }

    public byte[] GetFogData()
    {
        byte[] data = new byte[m_pixels.Length];
        for (int i = 0; i < m_pixels.Length; i++)
            data[i] = (byte)(m_pixels[i].a * 255);
        return data;
    }

    public void LoadFogData(byte[] data)
    {
        if (data == null || data.Length != m_pixels.Length) return;
        
        for (int i = 0; i < m_pixels.Length; i++)
        {
            float alpha = data[i] / 255f;
            m_pixels[i] = new Color(0, 0, 0, alpha);
        }

        m_fogTexture.SetPixels(m_pixels);
        m_fogTexture.Apply();
    }
}
