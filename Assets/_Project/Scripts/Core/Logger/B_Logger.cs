/*
 * ----------------------------------------------------------------------------
 * Project: Bussuf Games Core
 * Author:  Bussuf Dev Team
 * Date:    2023-10-27
 * ----------------------------------------------------------------------------
 * Description:
 * A custom logging wrapper for Unity's Debug.Log.
 * It includes Extension Methods to allow calling 'this.Log()' from any object.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-27 - Bussuf Dev Team - Initial implementation.
 * 2023-10-27 - Update: Changed prefix to dynamic object name.
 * 2023-10-27 - Update: Added Extension Methods for cleaner syntax (this.Log).
 * ----------------------------------------------------------------------------
 */

// ----------------------------------------------------------------------------
// QUICK USAGE GUIDE
// ----------------------------------------------------------------------------
// Option 1: Extension Method (Preferred for MonoBehaviours)
// Automatically captures 'this' as the context for the log.
// Usage:
//    this.Log("Player Jumped");           // Info (Blue)
//    this.LogSuccess("Level Loaded");     // Success (Green)
//    this.LogWarning("Low Health");       // Warning (Yellow)
//    this.LogError("Connection Failed");  // Error (Red)
//
// Option 2: Static Call (For static classes or no context)
// Usage:
//    B_Logger.Log("System Initialized");             // Prints as [System]
//    B_Logger.Log("Specific Event", otherObject);    // Prints as [otherObject]
// ----------------------------------------------------------------------------

using UnityEngine;

namespace BussufGames.Core
{
	/// <summary>
	/// Static wrapper for Unity logging.
	/// Ensures logs are stripped from release builds and formatted consistently.
	/// </summary>
	public static class B_Logger
	{
		private const string COLOR_INFO = "#2196F3";    // Blue
		private const string COLOR_SUCCESS = "#4CAF50"; // Green
		private const string COLOR_WARN = "#FFC107";    // Amber
		private const string COLOR_ERROR = "#F44336";   // Red

		/// <summary>
		/// Helper to get the prefix based on context.
		/// Returns [ObjectName] if context exists, otherwise [System].
		/// </summary>
		private static string GetPrefix(Object context)
		{
			return context != null ? $"<b>[{context.name}]</b> " : "<b>[System]</b> ";
		}

		// ------------------------------------------------------------------------
		// Core Static Methods
		// ------------------------------------------------------------------------

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void Log(string message, Object context = null)
		{
			Debug.Log($"{GetPrefix(context)}<color={COLOR_INFO}>{message}</color>", context);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogSuccess(string message, Object context = null)
		{
			Debug.Log($"{GetPrefix(context)}<color={COLOR_SUCCESS}>{message}</color>", context);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogWarning(string message, Object context = null)
		{
			Debug.LogWarning($"{GetPrefix(context)}<color={COLOR_WARN}>{message}</color>", context);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogError(string message, Object context = null)
		{
			Debug.LogError($"{GetPrefix(context)}<color={COLOR_ERROR}>{message}</color>", context);
		}
	}

	/// <summary>
	/// Extension methods to allow syntax like: this.Log("Message");
	/// This automatically passes 'this' as the context.
	/// </summary>
	public static class B_LoggerExtensions
	{
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void Log(this Object myObj, string message)
		{
			B_Logger.Log(message, myObj);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogSuccess(this Object myObj, string message)
		{
			B_Logger.LogSuccess(message, myObj);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogWarning(this Object myObj, string message)
		{
			B_Logger.LogWarning(message, myObj);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		public static void LogError(this Object myObj, string message)
		{
			B_Logger.LogError(message, myObj);
		}
	}
}

// ----------------------------------------------------------------------------
// End of Document
// ----------------------------------------------------------------------------