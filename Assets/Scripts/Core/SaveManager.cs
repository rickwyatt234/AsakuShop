using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AsakuShop.Core
{
    [DefaultExecutionOrder(-900)]
    public class SaveManager : MonoBehaviour
    {

        public const int CurrentSaveVersion = 1;

        public static SaveManager Instance { get; private set; }

        private readonly List<ISaveParticipant> _participants = new List<ISaveParticipant>();

        /// <summary>Absolute path to the save file on disk.</summary>
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[SaveManager] Duplicate instance detected — destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        public void Register(ISaveParticipant participant)
        {
            if (participant == null) return;
            if (!_participants.Contains(participant))
                _participants.Add(participant);
        }

        public void Unregister(ISaveParticipant participant)
        {
            _participants.Remove(participant);
        }


        public void Save()
        {
            CoreEvents.RaiseBeforeSave();

            GameClock clock = GameBootstrapper.Clock;
            GameStateController state = GameBootstrapper.State;

            SaveData data = new SaveData
            {
                SaveVersion    = CurrentSaveVersion,
                SaveTimestamp  = DateTime.UtcNow.ToString("o"),
                DayIndex       = clock != null ? clock.CurrentTime.DayIndex   : 0,
                DayOfWeek      = clock != null ? clock.CurrentTime.DayOfWeek.ToString() : GameDayOfWeek.Monday.ToString(),
                Hour           = clock != null ? clock.CurrentTime.Hour       : 0,
                Minute         = clock != null ? clock.CurrentTime.Minute     : 0,
                LastPhase      = state != null ? state.CurrentPhase : GamePhase.MainMenu,
                SystemData     = new System.Collections.Generic.Dictionary<string, string>(),
            };

            foreach (ISaveParticipant participant in _participants)
            {
                try
                {
                    object capturedState = participant.CaptureState();
                    string json          = JsonUtility.ToJson(capturedState);
                    data.SystemData[participant.SaveKey] = json;
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[SaveManager] Failed to capture state for '{participant.SaveKey}': {e.Message}");
                }
            }

            try
            {
                // JsonUtility does not serialise Dictionary; use a manual approach.
                string saveJson = SerializeSaveData(data);
                File.WriteAllText(SaveFilePath, saveJson);
                Debug.Log($"[SaveManager] Game saved to: {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {e.Message}");
            }
        }

        public void Load()
        {
            if (!SaveFileExists())
            {
                Debug.LogWarning("[SaveManager] No save file found. Cannot load.");
                return;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData data = DeserializeSaveData(json);

                data = MigrateIfNeeded(data);

                // Restore clock state.
                GameClock clock = GameBootstrapper.Clock;
                if (clock != null)
                {
                    clock.CurrentTime = GameTime.FromMinutes(data.DayIndex,
                        data.Hour * TimeConstants.MinutesPerHour + data.Minute);
                }

                // Restore each participant.
                foreach (ISaveParticipant participant in _participants)
                {
                    if (data.SystemData.TryGetValue(participant.SaveKey, out string participantJson))
                    {
                        try
                        {
                            participant.RestoreState(participantJson);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(
                                $"[SaveManager] Failed to restore state for '{participant.SaveKey}': {e.Message}");
                        }
                    }
                }

                CoreEvents.RaiseAfterLoad();
                Debug.Log("[SaveManager] Game loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load save file: {e.Message}");
            }
        }

        public bool SaveFileExists()
        {
            return File.Exists(SaveFilePath);
        }

        public void DeleteSave()
        {
            if (SaveFileExists())
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveManager] Save file deleted.");
            }
        }

        private SaveData MigrateIfNeeded(SaveData data)
        {
            if (data.SaveVersion < CurrentSaveVersion)
            {
                Debug.LogWarning(
                    $"[SaveManager] Migration from v{data.SaveVersion} to v{CurrentSaveVersion} not yet implemented.");
            }
            return data;
        }

        private static string SerializeSaveData(SaveData data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append($"\"SaveVersion\":{data.SaveVersion},");
            sb.Append($"\"SaveTimestamp\":{JsonString(data.SaveTimestamp)},");
            sb.Append($"\"DayIndex\":{data.DayIndex},");
            sb.Append($"\"DayOfWeek\":{JsonString(data.DayOfWeek)},");
            sb.Append($"\"Hour\":{data.Hour},");
            sb.Append($"\"Minute\":{data.Minute},");
            sb.Append($"\"LastPhase\":{JsonString(data.LastPhase.ToString())},");
            sb.Append("\"SystemData\":{");

            bool first = true;
            foreach (KeyValuePair<string, string> kv in data.SystemData)
            {
                if (!first) sb.Append(",");
                // The value is already a valid JSON string from JsonUtility.ToJson.
                sb.Append($"{JsonString(kv.Key)}:{JsonString(kv.Value)}");
                first = false;
            }

            sb.Append("}");
            sb.Append("}");
            return sb.ToString();
        }

        private static SaveData DeserializeSaveData(string json)
        {
            // Use a surrogate class that matches the fields JsonUtility can handle.
            SaveDataSurrogate surrogate = JsonUtility.FromJson<SaveDataSurrogate>(json);

            SaveData data = new SaveData
            {
                SaveVersion   = surrogate.SaveVersion,
                SaveTimestamp = surrogate.SaveTimestamp,
                DayIndex      = surrogate.DayIndex,
                DayOfWeek     = surrogate.DayOfWeek,
                Hour          = surrogate.Hour,
                Minute        = surrogate.Minute,
                LastPhase     = surrogate.LastPhase,
                SystemData    = new Dictionary<string, string>(),
            };

            // Manually parse the SystemData object from the raw JSON string.
            ExtractSystemData(json, data.SystemData);
            return data;
        }

        private static void ExtractSystemData(string json, Dictionary<string, string> target)
        {
            const string marker = "\"SystemData\":{";
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return;

            start += marker.Length;
            int depth = 1;
            int pos   = start;

            while (pos < json.Length && depth > 0)
            {
                char c = json[pos];
                if (c == '{') depth++;
                else if (c == '}') depth--;
                pos++;
            }

            // Content between the outer braces of SystemData.
            string inner = json.Substring(start, pos - start - 1).Trim();
            if (string.IsNullOrEmpty(inner)) return;

            // Simple key:"value" pairs — values are JSON strings so we expect
            // "key":"escapedJsonString" entries separated by commas.
            int i = 0;
            while (i < inner.Length)
            {
                // Skip whitespace/comma.
                while (i < inner.Length && (inner[i] == ',' || inner[i] == ' ')) i++;
                if (i >= inner.Length) break;

                // Read key.
                string key = ReadJsonString(inner, ref i);
                if (key == null) break;

                // Skip colon.
                while (i < inner.Length && inner[i] != ':') i++;
                i++; // consume ':'

                // Read value.
                string value = ReadJsonString(inner, ref i);
                if (value != null) target[key] = value;
            }
        }

        private static string ReadJsonString(string s, ref int pos)
        {
            while (pos < s.Length && s[pos] != '"') pos++;
            if (pos >= s.Length) return null;
            pos++; // consume opening '"'

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (pos < s.Length && s[pos] != '"')
            {
                if (s[pos] == '\\' && pos + 1 < s.Length)
                {
                    pos++;
                    switch (s[pos])
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            // \uXXXX — consume the four hex digits and convert to a char.
                            if (pos + 4 < s.Length)
                            {
                                string hex = s.Substring(pos + 1, 4);
                                if (int.TryParse(hex,
                                        System.Globalization.NumberStyles.HexNumber,
                                        null, out int codePoint))
                                {
                                    sb.Append((char)codePoint);
                                    pos += 4;
                                }
                                else
                                {
                                    sb.Append('u'); // malformed — keep literal
                                }
                            }
                            break;
                        default:   sb.Append(s[pos]); break;
                    }
                }
                else
                {
                    sb.Append(s[pos]);
                }
                pos++;
            }
            pos++; // consume closing '"'
            return sb.ToString();
        }

        private static string JsonString(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                               .Replace("\n", "\\n").Replace("\r", "\\r")
                               .Replace("\t", "\\t") + "\"";
        }

        [Serializable]
        private class SaveDataSurrogate
        {
            public int       SaveVersion;
            public string    SaveTimestamp;
            public int       DayIndex;
            public string    DayOfWeek;
            public int       Hour;
            public int       Minute;
            public GamePhase LastPhase;
        }
    }
}