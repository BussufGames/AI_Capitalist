/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Orchestrates the 'Local First, Cloud Second' save data architecture.
 * Safely compares Cloud timestamps before applying, preventing Reference Loss.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added Auto-Save and ApplicationQuit hooks.
 * 2023-10-29 - Bussuf Senior Dev - Exposed autoSaveInterval, added Cloud Save throttling.
 * 2023-10-29 - Bussuf Senior Dev - Added Timestamp comparison to avoid Lost Reference bug.
 * 2023-10-29 - Bussuf Senior Dev - Added initialBalance parameter for fresh saves.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Services;
using BussufGames.Core;
using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.Data
{
	public class DataManager : MonoBehaviour, IService
	{
		[Header("Save Settings")]
		[Tooltip("How often to auto-save to the Cloud (in seconds).")]
		[SerializeField] private float autoSaveInterval = 30f;

		[Tooltip("Minimum seconds between cloud saves to prevent Rate Limiting by UGS.")]
		[SerializeField] private float cloudSaveThrottle = 5f;

		[Header("Economy Settings")]
		[Tooltip("Starting balance for a completely fresh save (string to support BigDouble).")]
		[SerializeField] private string initialBalance = "0";

		private const string SAVE_KEY = "ai_cap_save_v1";
		public PlayerSaveData GameData { get; private set; }

		private float _lastCloudSaveTime = 0f;

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<DataManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing DataManager...");

			LoadLocalData();

			if (GameData == null)
			{
				this.LogWarning("No local save found. Creating new player profile.");
				GameData = new PlayerSaveData();
				GameData.CurrentBalance = initialBalance; // Set custom initial balance!
				GameData.LastSaveTime = DateTime.UtcNow.ToString("O");

				SaveLocalOnly();
			}

			SyncWithCloudBackground();

			InvokeRepeating(nameof(SaveGame), autoSaveInterval, autoSaveInterval);
		}

		public void SaveGame()
		{
			if (GameData == null) return;

			GameData.LastSaveTime = DateTime.UtcNow.ToString("O");
			string json = JsonConvert.SerializeObject(GameData, Formatting.None);

			PlayerPrefs.SetString(SAVE_KEY, json);
			PlayerPrefs.Save();

			if (Time.unscaledTime - _lastCloudSaveTime >= cloudSaveThrottle)
			{
				_lastCloudSaveTime = Time.unscaledTime;
				UGSManager ugs = CoreManager.Instance.GetService<UGSManager>();
				if (ugs != null && ugs.IsAuthenticated)
				{
					_ = ugs.SaveCloudDataAsync(SAVE_KEY, json);
				}
			}
		}

		private void SaveLocalOnly()
		{
			if (GameData == null) return;
			string json = JsonConvert.SerializeObject(GameData, Formatting.None);
			PlayerPrefs.SetString(SAVE_KEY, json);
			PlayerPrefs.Save();
		}

		private void LoadLocalData()
		{
			if (PlayerPrefs.HasKey(SAVE_KEY))
			{
				string json = PlayerPrefs.GetString(SAVE_KEY);
				GameData = JsonConvert.DeserializeObject<PlayerSaveData>(json);
				this.LogSuccess("Local game data loaded.");
			}
		}

		private async void SyncWithCloudBackground()
		{
			var ugs = CoreManager.Instance.GetService<UGSManager>();
			if (ugs == null) return;

			await System.Threading.Tasks.Task.Delay(2000);

			if (ugs.IsAuthenticated)
			{
				string cloudJson = await ugs.LoadCloudDataAsync(SAVE_KEY);
				if (!string.IsNullOrEmpty(cloudJson) && cloudJson != "{}")
				{
					try
					{
						PlayerSaveData cloudData = JsonConvert.DeserializeObject<PlayerSaveData>(cloudJson);
						if (cloudData == null) return;

						DateTime cloudTime = DateTime.MinValue;
						DateTime localTime = DateTime.MinValue;

						DateTime.TryParse(cloudData.LastSaveTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out cloudTime);
						if (GameData != null && !string.IsNullOrEmpty(GameData.LastSaveTime))
						{
							DateTime.TryParse(GameData.LastSaveTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out localTime);
						}

						if (cloudTime > localTime)
						{
							this.LogWarning("Newer Cloud Save detected! Updating local and rebooting...");
							GameData = cloudData;

							_lastCloudSaveTime = 0f;
							SaveGame();

							if (CoreManager.Instance != null) Destroy(CoreManager.Instance.gameObject);
							SceneManager.LoadScene(0);
						}
						else
						{
							this.Log("Local save is up to date with Cloud. Continuing.");
						}
					}
					catch (Exception e)
					{
						this.LogError($"Failed to parse cloud data: {e.Message}");
					}
				}
			}
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus && GameData != null)
			{
				_lastCloudSaveTime = 0f;
				SaveGame();
			}
		}

		private void OnApplicationQuit()
		{
			if (GameData != null)
			{
				_lastCloudSaveTime = 0f;
				SaveGame();
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------