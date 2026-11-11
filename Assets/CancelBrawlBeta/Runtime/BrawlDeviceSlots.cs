using System;
using UnityEngine.InputSystem;

namespace CancelBrawl.Beta
{
    /// <summary>Round-scoped device ownership. Collection reordering never transfers a fighter.</summary>
    public sealed class BrawlDeviceSlots
    {
        private readonly Gamepad[] pads = new Gamepad[BrawlMatch.MaxPlayers];
        private readonly bool[] humans = new bool[BrawlMatch.MaxPlayers];
        public bool KeyboardP1 { get; private set; } = true;
        public int Count { get; private set; }
        public static bool Available(Gamepad pad) => pad != null && pad.added && pad.enabled;
        public Gamepad Pad(int slot) => slot >= 0 && slot < Count ? pads[slot] : null;
        public bool IsHuman(int slot) => slot >= 0 && slot < Count && humans[slot];

        public void Clear()
        {
            Array.Clear(pads, 0, pads.Length); Array.Clear(humans, 0, humans.Length); Count = 0;
        }

        public void Configure(int count, bool local, bool padOnly, bool training)
        {
            Clear(); Count = count; KeyboardP1 = !(local && padOnly); humans[0] = true;
            int next = 0;
            for (int i = 0; i < count; i++)
            {
                if (training && i > 0) continue;
                if (i == 0 && local && KeyboardP1) continue;
                if (i > 0 && !local) continue;
                while (next < Gamepad.all.Count && !Available(Gamepad.all[next])) next++;
                if (next >= Gamepad.all.Count) continue;
                pads[i] = Gamepad.all[next++]; humans[i] = true;
            }
        }

        public int MissingMask
        {
            get
            {
                int mask = 0;
                for (int i = 0; i < Count; i++)
                    if (humans[i] && ((pads[i] != null && !Available(pads[i])) || (i == 0 && !KeyboardP1 && pads[i] == null))) mask |= 1 << i;
                return mask;
            }
        }

        public bool Owns(Gamepad pad)
        {
            for (int i = 0; i < Count; i++) if (pads[i] == pad) return true;
            return false;
        }

        // Explicit replacement only: a newly connected pad never steals a live slot or turns a bot human.
        public bool ClaimMissing(Gamepad pad)
        {
            if (!Available(pad) || Owns(pad)) return false;
            int missing = MissingMask;
            for (int i = 0; i < Count; i++)
                if ((missing & (1 << i)) != 0) { pads[i] = pad; return true; }
            return false;
        }
    }
}
