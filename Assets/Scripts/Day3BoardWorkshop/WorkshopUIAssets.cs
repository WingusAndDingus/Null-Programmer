using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NullProgrammer.Day3Workshop
{
    public sealed class WorkshopUIAssets
    {
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private bool warned;
        public Sprite Get(string name)
        {
            if (!sprites.TryGetValue(name, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>("Day3WorkshopUI/" + name);
                sprites[name] = sprite;
                if (sprite == null && !warned)
                {
                    Debug.LogWarning("Day 3 UI art is missing from Resources/Day3WorkshopUI. Plain-color fallback will be used.");
                    warned = true;
                }
            }
            return sprite;
        }
        public void Skin(Image image, string name, bool sliced = false)
        {
            Sprite sprite = Get(name);
            image.sprite = sprite;
            image.type = sprite != null && sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sprite != null) image.color = Color.white;
        }
        public void SkinButton(Button button)
        {
            Skin(button.image, "button-idle", true);
            if (button.image.sprite == null) return;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState {
                highlightedSprite = Get("button-hover"),
                pressedSprite = Get("button-pressed"),
                selectedSprite = Get("button-focus"),
                disabledSprite = Get("button-disabled")
            };
        }
    }
}
