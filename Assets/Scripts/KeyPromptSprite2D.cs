using UnityEngine;

namespace Metacraft.Interaction
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class KeyPromptSprite2D : MonoBehaviour
    {
        [SerializeField] private Color keyColor = new Color(1f, 0.9f, 0.15f, 1f);
        [SerializeField] private Color letterColor = Color.black;
        [SerializeField, Min(24)] private int textureSize = 24;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 24f;

        private Texture2D generatedTexture;
        private Sprite generatedSprite;

        private void Awake()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            generatedTexture = BuildTexture();
            generatedSprite = Sprite.Create(
                generatedTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                Vector2.one * 0.5f,
                pixelsPerUnit);
            generatedSprite.name = "Generated_Key_F";
            spriteRenderer.sprite = generatedSprite;
        }

        private void OnDestroy()
        {
            if (generatedSprite != null)
            {
                Destroy(generatedSprite);
            }

            if (generatedTexture != null)
            {
                Destroy(generatedTexture);
            }
        }

        private Texture2D BuildTexture()
        {
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color borderColor = Color.black;
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    bool border = x == 0 || y == 0 || x == textureSize - 1 || y == textureSize - 1;
                    texture.SetPixel(x, y, border ? borderColor : keyColor);
                }
            }

            DrawRect(texture, 8, 6, 3, 13, letterColor);
            DrawRect(texture, 8, 16, 9, 3, letterColor);
            DrawRect(texture, 8, 11, 7, 3, letterColor);

            texture.SetPixel(0, 0, clear);
            texture.SetPixel(textureSize - 1, 0, clear);
            texture.SetPixel(0, textureSize - 1, clear);
            texture.SetPixel(textureSize - 1, textureSize - 1, clear);
            texture.Apply(false, true);
            return texture;
        }

        private static void DrawRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }
}
