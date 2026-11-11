using System;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Catalog of imported character models (Art/Characters). Filled by the editor builder; read at runtime.</summary>
    [CreateAssetMenu(menuName = "Cancel Brawl/Character art")]
    public sealed class BrawlCharacterArt : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Folder name in Art/Characters, referenced from BrawlKit.Skins.")]
            public string id;
            [Tooltip("Imported FBX (static mesh, Read/Write enabled). The rig is generated at runtime.")]
            public GameObject model;
            public Texture2D albedo;
        }

        public Entry[] entries = new Entry[0];

        public Entry Find(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            foreach (Entry entry in entries)
                if (entry != null && entry.id == id && entry.model != null) return entry;
            return null;
        }
    }
}
