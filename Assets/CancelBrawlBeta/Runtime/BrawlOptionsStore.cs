using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Persisted options (SYS-068). Field initializers are the safe defaults used by SYS-069.</summary>
    [Serializable]
    public sealed class BrawlOptionsData
    {
        public string format = BrawlOptionsStore.Format;
        public int version = BrawlOptionsStore.CurrentVersion;
        public bool reducedEffects;
        public float sfx = .8f;
        public float music = .5f;
        public bool localCoop;
        public bool p1UsesGamepad;
        public List<BrawlBindingEntry> keyboard = new List<BrawlBindingEntry>();
        public List<BrawlBindingEntry> gamepad = new List<BrawlBindingEntry>();
    }

    public enum OptionsLoadStatus { Loaded, Defaults, MigratedLegacy, RecoveredBackup, RepairedFields, ResetCorrupt }

    /// <summary>
    /// Versioned JSON file with recoverable writes: write a temporary file, flush it, then swap it in while the
    /// previous good file becomes the backup. A damaged or incomplete file is quarantined, never trusted.
    /// </summary>
    public static class BrawlOptionsStore
    {
        public const string Format = "funados-options";
        public const int CurrentVersion = 1;
        public const string FileName = "funados-options.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);
        public static string BackupPath(string path) => path + ".bak";
        public static string TempPath(string path) => path + ".tmp";

        public static void Save(string path, BrawlOptionsData data)
        {
            data.format = Format; data.version = CurrentVersion;
            string json = JsonUtility.ToJson(data, true);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temp = TempPath(path), backup = BackupPath(path);
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
            { writer.Write(json); writer.Flush(); stream.Flush(true); }
            // Never replace a good file with something we cannot read back.
            if (!TryParse(File.ReadAllText(temp), out _, out _)) { File.Delete(temp); throw new IOException("Opciones: verificación de escritura fallida"); }
            if (File.Exists(path))
            {
                // Keep the last good file as backup only when it is itself valid.
                bool currentValid = TryRead(path, out _, out _);
                if (currentValid) File.Replace(temp, path, backup, true);
                else { File.Delete(path); File.Move(temp, path); }
            }
            else File.Move(temp, path);
        }

        /// <summary>Loads options; always returns usable values and says how they were obtained.</summary>
        public static BrawlOptionsData Load(string path, out OptionsLoadStatus status, out string detail)
        {
            detail = "";
            // A stale temp file means a write was interrupted before the swap: the main file is still authoritative.
            try { if (File.Exists(TempPath(path))) File.Delete(TempPath(path)); } catch (Exception) { }
            if (TryRead(path, out var data, out bool repaired)) { status = repaired ? OptionsLoadStatus.RepairedFields : OptionsLoadStatus.Loaded; return data; }
            bool primaryExisted = File.Exists(path);
            if (primaryExisted) { detail = Quarantine(path); }
            if (TryRead(BackupPath(path), out data, out _))
            {
                status = OptionsLoadStatus.RecoveredBackup;
                try { Save(path, data); } catch (Exception) { }
                return data;
            }
            status = primaryExisted ? OptionsLoadStatus.ResetCorrupt : OptionsLoadStatus.Defaults;
            return new BrawlOptionsData();
        }

        private static bool TryRead(string path, out BrawlOptionsData data, out bool repaired)
        {
            data = null; repaired = false;
            try { return File.Exists(path) && TryParse(File.ReadAllText(path), out data, out repaired); }
            catch (Exception) { return false; }
        }

        /// <summary>Parses and validates. Wrong format/version/syntax fails; out-of-range values are repaired.</summary>
        public static bool TryParse(string json, out BrawlOptionsData data, out bool repaired)
        {
            data = null; repaired = false;
            if (string.IsNullOrWhiteSpace(json)) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
            try { data = JsonUtility.FromJson<BrawlOptionsData>(trimmed); }
            catch (Exception) { data = null; return false; }
            if (data == null || data.format != Format || data.version < 1) { data = null; return false; }
            if (data.version > CurrentVersion) repaired = true; // Newer file: keep known fields only.
            // Every expected field must be present; JsonUtility would silently default missing ones.
            foreach (string field in new[] { "\"format\"", "\"version\"", "\"reducedEffects\"", "\"sfx\"", "\"music\"", "\"keyboard\"", "\"gamepad\"" })
                if (trimmed.IndexOf(field, StringComparison.Ordinal) < 0) { data = null; return false; }
            data.sfx = Sanitize(data.sfx, .8f, ref repaired);
            data.music = Sanitize(data.music, .5f, ref repaired);
            if (data.keyboard == null) { data.keyboard = new List<BrawlBindingEntry>(); repaired = true; }
            if (data.gamepad == null) { data.gamepad = new List<BrawlBindingEntry>(); repaired = true; }
            return true;
        }

        private static float Sanitize(float value, float fallback, ref bool repaired)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { repaired = true; return fallback; }
            float clamped = Mathf.Clamp01(value);
            if (clamped != value) repaired = true;
            return clamped;
        }

        private static string Quarantine(string path)
        {
            try
            {
                string target = path + ".danado-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                if (File.Exists(target)) File.Delete(target);
                File.Move(path, target);
                return target;
            }
            catch (Exception) { try { File.Delete(path); } catch (Exception) { } return ""; }
        }
    }
}
