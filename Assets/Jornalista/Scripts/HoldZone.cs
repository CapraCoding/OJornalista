using UnityEngine;
using UnityEngine.EventSystems;

namespace Jornalista
{
    public class HoldZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        int pointers;
        public bool IsHeld => pointers > 0;

        public void OnPointerDown(PointerEventData e) => pointers++;
        public void OnPointerUp(PointerEventData e) => pointers = Mathf.Max(0, pointers - 1);
        void OnDisable() => pointers = 0;

        public static HoldZone Create(Transform parent, string name, float minX, float minY, float maxX, float maxY)
        {
            var img = UI.Img(parent, name, new Color(0, 0, 0, 0), null, true);
            img.rectTransform.Anchor(minX, minY, maxX, maxY);
            return img.gameObject.AddComponent<HoldZone>();
        }
    }
}
