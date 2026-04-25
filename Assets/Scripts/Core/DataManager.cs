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
            if (!File.Exists(SavePath))
            {
                Logger.LogI("系统", "无存档文件");
                return null;
            }
            Logger.LogI("系统", "加载存档");
            try
            {
                var json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (System.Exception e)
            {
                Logger.LogE("DataManager", $"读档失败: {e.Message}");
                return null;
            }
        }

        public void Save(PlayerData data)
        {
            Logger.LogI("系统", "保存存档");
            try
            {
                var dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception e)
            {
                Logger.LogE("DataManager", $"存档失败: {e.Message}");
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
