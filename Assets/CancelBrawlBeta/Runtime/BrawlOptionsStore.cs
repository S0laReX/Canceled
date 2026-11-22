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
        // Version 2 (SYS-026). Missing in version 1 files: the initializers below are their migrated values.
        public float moveDeadzone = .2f;
        public float aimDeadzone = .25f;
        public float stickResponse = 1f;
        public float vibration = .75f;
        // Version 3 (SYS-071).
        public float textScale = 1f;
        public float hudScale = 1f;
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
        public const int CurrentVersion = 3;
        private static readonly string[] FieldsV1 = { "\"format\"", "\"version\"", "\"reducedEffects\"", "\"sfx\"", "\"music\"", "\"keyboard\"", "\"gamepad\"" };
        private static readonly string[] FieldsV2 = { "\"moveDeadzone\"", "\"aimDeadzone\"", "\"stickResponse\"", "\"vibration\"" };
        private static readonly string[] FieldsV3 = { "\"textScale\"", "\"hudScale\"" };
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
            if (TryRead(path, out var data, out bool repaired, out bool migrated))
            {
                status = repaired ? OptionsLoadStatus.RepairedFields : migrated ? OptionsLoadStatus.MigratedLegacy : OptionsLoadStatus.Loaded;
                return data;
            }
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

        private static bool TryRead(string path, out BrawlOptionsData data, out bool repaired) => TryRead(path, out data, out repaired, out _);

        private static bool TryRead(string path, out BrawlOptionsData data, out bool repaired, out bool migrated)
        {
            data = null; repaired = false; migrated = false;
            try { return File.Exists(path) && TryParse(File.ReadAllText(path), out data, out repaired, out migrated); }
            catch (Exception) { return false; }
        }

        public static bool TryParse(string json, out BrawlOptionsData data, out bool repaired) => TryParse(json, out data, out repaired, out _);

        /// <summary>
        /// Parses and validates. Wrong format/version/syntax fails; out-of-range values are repaired. An older version
        /// is migrated: fields it never had take their safe defaults and <paramref name="migrated"/> is set.
        /// </summary>
        public static bool TryParse(string json, out BrawlOptionsData data, out bool repaired, out bool migrated)
        {
            data = null; repaired = false; migrated = false;
            if (string.IsNullOrWhiteSpace(json)) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
            try { data = JsonUtility.FromJson<BrawlOptionsData>(trimmed); }
            catch (Exception) { data = null; return false; }
            if (data == null || data.format != Format || data.version < 1) { data = null; return false; }
            if (data.version > CurrentVersion) repaired = true; // Newer file: keep known fields only.
            // Every expected field must be present; JsonUtility would silently default missing ones.
            foreach (string field in FieldsV1)
                if (trimmed.IndexOf(field, StringComparison.Ordinal) < 0) { data = null; return false; }
            var defaults = new BrawlOptionsData();
            if (data.version >= 2)
            {
                foreach (string field in FieldsV2)
                    if (trimmed.IndexOf(field, StringComparison.Ordinal) < 0) { data = null; return false; }
            }
            else
            {
                // Version 1 → 2: stick tuning and vibration did not exist yet.
                data.moveDeadzone = defaults.moveDeadzone; data.aimDeadzone = defaults.aimDeadzone;
                data.stickResponse = defaults.stickResponse; data.vibration = defaults.vibration;
                migrated = true;
            }
            if (data.version >= 3)
            {
                foreach (string field in FieldsV3)
                    if (trimmed.IndexOf(field, StringComparison.Ordinal) < 0) { data = null; return false; }
            }
            else
            {
                // Version 2 → 3: text and HUD size did not exist yet.
                data.textScale = defaults.textScale; data.hudScale = defaults.hudScale;
                migrated = true;
            }
            data.sfx = Sanitize(data.sfx, .8f, ref repaired);
            data.music = Sanitize(data.music, .5f, ref repaired);
            data.vibration = Sanitize(data.vibration, .75f, ref repaired);
            data.moveDeadzone = SanitizeRange(data.moveDeadzone, .2f, BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone, ref repaired);
            data.aimDeadzone = SanitizeRange(data.aimDeadzone, .25f, BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone, ref repaired);
            data.stickResponse = SanitizeRange(data.stickResponse, 1f, BrawlInputTuning.MinResponse, BrawlInputTuning.MaxResponse, ref repaired);
            data.textScale = SanitizeRange(data.textScale, 1f, BrawlHudLayout.MinScaleOption, BrawlHudLayout.MaxScaleOption, ref repaired);
            data.hudScale = SanitizeRange(data.hudScale, 1f, BrawlHudLayout.MinScaleOption, BrawlHudLayout.MaxScaleOption, ref repaired);
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

        private static float SanitizeRange(float value, float fallback, float min, float max, ref bool repaired)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { repaired = true; return fallback; }
            float clamped = Mathf.Clamp(value, min, max);
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
