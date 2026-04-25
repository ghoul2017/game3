using System;
using System.Diagnostics;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 统一日志系统：等级控制 + 条件编译 + 格式化输出
    /// </summary>
    public static class Logger
    {
        /// <summary>日志等级</summary>
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        /// <summary>全局日志等级阈值，低于此等级的日志不输出</summary>
        public static LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>格式化输出: [时间][等级][标签] 消息</summary>
        private static string Format(LogLevel level, string tag, string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            return $"[{time}][{levelStr}][{tag}] {message}";
        }

        /// <summary>Debug 级别日志，仅在 UNITY_EDITOR 中输出</summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogD(string tag, string message)
        {
            if (MinLevel > LogLevel.Debug) return;
            UnityEngine.Debug.Log(Format(LogLevel.Debug, tag, message));
        }

        /// <summary>Info 级别日志</summary>
        public static void LogI(string tag, string message)
        {
            if (MinLevel > LogLevel.Info) return;
            UnityEngine.Debug.Log(Format(LogLevel.Info, tag, message));
        }

        /// <summary>Warning 级别日志</summary>
        public static void LogW(string tag, string message)
        {
            if (MinLevel > LogLevel.Warning) return;
            UnityEngine.Debug.LogWarning(Format(LogLevel.Warning, tag, message));
        }

        /// <summary>Error 级别日志</summary>
        public static void LogE(string tag, string message)
        {
            if (MinLevel > LogLevel.Error) return;
            UnityEngine.Debug.LogError(Format(LogLevel.Error, tag, message));
        }
    }
}
