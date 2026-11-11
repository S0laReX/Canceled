using UnityEngine;
namespace CancelBrawl.Beta
{
    /// <summary>Short-lived decorative object. Freezes while the match is paused.</summary>
    public sealed class BrawlTransient : MonoBehaviour
    {
        private Vector3 velocity;
        private float remaining, duration, growTo;
        private BrawlMatch match;
        public void Initialize(Vector3 motion, float seconds, float scaleTo = 0)
        {
            velocity = motion; remaining = duration = Mathf.Max(.01f, seconds); growTo = scaleTo;
            match = FindAnyObjectByType<BrawlMatch>();
            if (growTo > 0) transform.localScale = Vector3.one * .1f;
        }
        private void Update()
        {
            if (match && match.Phase == MatchPhase.Paused) return;
            transform.position += velocity * Time.deltaTime;
            remaining -= Time.deltaTime;
            if (growTo > 0) transform.localScale = Vector3.one * Mathf.Lerp(.1f, growTo, 1 - remaining / duration);
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
