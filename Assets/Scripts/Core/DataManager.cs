using System.IO;
using UnityEngine;
using Game.Data;

namespace Game.Core
{
    /// <summary>
    /// JSON 存档管理
    /// </summary>
    public class DataManager
    {
        private static DataManager instance;
        public static DataManager Instance => instance ??= new DataManager();

        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "save.json");

        public PlayerData Load()
        {
            if (!File.Exists(SavePath)) return null;
            try
            {
                var json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] 读档失败: {e.Message}");
                return null;
            }
        }

        public void Save(PlayerData data)
        {
            try
            {
                var dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] 存档失败: {e.Message}");
            }
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
