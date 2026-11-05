using UnityEngine;

namespace Jornalista
{
    public class Pulse : MonoBehaviour
    {
        public bool On;
        public float Amount = 0.05f;
        public float Speed = 5f;
        float t;

        void Update()
        {
            if (On)
            {
                t += Time.deltaTime * Speed;
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t) * Amount);
            }
            else if (t != 0f)
            {
                t = 0f;
                transform.localScale = Vector3.one;
            }
        }
    }
}
