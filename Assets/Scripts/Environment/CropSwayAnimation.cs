using UnityEngine;

namespace Farm2Shelf.Environment
{
    public class CropSwayAnimation : MonoBehaviour
    {
        private float phaseOffset;
        private float swaySpeed;
        private float swayAmount;
        private float popTimer = 0f;
        private bool isPopping = true;
        private Vector3 targetScale = Vector3.one;

        private void Awake()
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            swaySpeed = Random.Range(1.8f, 2.4f);
            swayAmount = Random.Range(1.0f, 1.6f);
        }

        public void SetTargetScale(Vector3 scale)
        {
            targetScale = scale;
            transform.localScale = Vector3.zero;
            popTimer = 0f;
            isPopping = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Pop Büyüme Animasyonu (Tarlaya oturan tatlı büyüme efekti)
            if (isPopping)
            {
                popTimer += dt * 3.5f;
                if (popTimer >= 1.0f)
                {
                    popTimer = 1.0f;
                    isPopping = false;
                    transform.localScale = targetScale;
                }
                else
                {
                    float t = popTimer;
                    float scaleFactor = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.05f;
                    if (t > 0.7f) scaleFactor = Mathf.Lerp(1.05f, 1.0f, (t - 0.7f) / 0.3f);
                    transform.localScale = targetScale * scaleFactor;
                }
            }

            // 2. Hafif Rüzgar Salınımı (Taşmayı önleyen kontrollü esinti)
            float swayX = Mathf.Sin((Time.time * swaySpeed) + phaseOffset) * swayAmount;
            float swayZ = Mathf.Cos((Time.time * (swaySpeed * 0.85f)) + phaseOffset) * (swayAmount * 0.75f);
            transform.localRotation = Quaternion.Euler(swayX, 0f, swayZ);
        }
    }
}
