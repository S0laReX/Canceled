using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Closes the hit window after fighters, projectiles and zones have all stepped (SYS-045). The match opens the
    /// window first (execution order -100); this runs last (100), so every hit of the step is applied together.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class BrawlHitResolver : MonoBehaviour
    {
        private BrawlMatch match;
        public void Configure(BrawlMatch owner) => match = owner;
        private void FixedUpdate() { if (match != null) match.FlushHits(); }
    }
}
