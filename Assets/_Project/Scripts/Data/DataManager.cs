/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Orchestrates the 'Local First, Cloud Second' save data architecture.
 * Safely compares Cloud timestamps before applying, preventing Reference Loss.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Fixed Cloud Save sync logic (LoadCloudDataAsync returns string).
 * 2026-02-28 - Replaced IsSignedIn with IsAuthenticated to match UGSManager.
 * 2026-02-28 - Added safe ResetDataToDefault method guaranteeing Tier 1 unlock.
 * ----------------------------------------------------------------------------
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using AI_Capitalist.Core;
using AI_Capitalist.Services;
using BussufGames.Core;

namespace AI_Capitalist.Data
{
	public class DataManager : MonoBehaviour, IService
	{
		#region Save & Economy Settings
		[Header("Save Settings")]
		[Tooltip("How often to auto-save to the Cloud (in seconds).")]
		[SerializeField] private float autoSaveInterval = 30f;
		[Tooltip("Minimum seconds between cloud saves to prevent Rate Limiting by UGS.")]
		[SerializeField] private float cloudSaveThrottle = 5f;

		[Header("Economy Settings")]
		[Tooltip("Starting balance for a completely fresh save (string to support BigDouble).")]
		[SerializeField] private string initialBalance = "0";

		private const string SAVE_KEY = "ai_cap_save_v1";
		#endregion

		#region Properties & Fields
		public PlayerSaveData GameData { get; private set; }

		private string _saveFilePath;
		private float _lastCloudSaveTime = 0f;
		#endregion

		#region Initialization
		private void Awake()
		{
			_saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
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
				GameData.CurrentBalance = initialBalance;
				GameData.HighestUnlockedTier = 1;
				GameData.LastSaveTime = DateTime.UtcNow.ToString("O");

				SaveLocalOnly();
			}

			SyncWithCloudBackground();

			InvokeRepeating(nameof(SaveGame), autoSaveInterval, autoSaveInterval);
		}
		#endregion

		#region Save/Load Logic
		private void LoadLocalData()
		{
			if (PlayerPrefs.HasKey(SAVE_KEY))
			{
				try
				{
					string json = PlayerPrefs.GetString(SAVE_KEY);
					GameData = JsonConvert.DeserializeObject<PlayerSaveData>(json);

					if (GameData == null) GameData = new PlayerSaveData();

					if (GameData.TiersData == null) GameData.TiersData = new List<TierDynamicData>();
					if (GameData.PurchasedUpgrades == null) GameData.PurchasedUpgrades = new List<string>();

					// Failsafe: Ensure at least Tier 1 is always unlocked
					if (GameData.HighestUnlockedTier < 1) GameData.HighestUnlockedTier = 1;

					this.LogSuccess("Local game data loaded.");
				}
				catch (Exception e)
				{
					this.LogError($"Failed to load local data: {e.Message}");
					GameData = new PlayerSaveData();
					GameData.HighestUnlockedTier = 1;
					GameData.CurrentBalance = initialBalance;
				}
			}
			else
			{
				GameData = new PlayerSaveData();
				GameData.HighestUnlockedTier = 1;
				GameData.CurrentBalance = initialBalance;
				this.Log("No local save found. Created new game data.");
			}
		}

		private void SaveLocalOnly()
		{
			if (GameData == null) return;
			string json = JsonConvert.SerializeObject(GameData, Formatting.None);
			PlayerPrefs.SetString(SAVE_KEY, json);
			PlayerPrefs.Save();
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

		// FIX: Correctly parses the direct string returned by UGSManager and uses IsAuthenticated
		private async void SyncWithCloudBackground()
		{
			var ugs = CoreManager.Instance.GetService<UGSManager>();
			if (ugs == null) return;

			await Task.Delay(2000);

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
		#endregion

		#region DEV TOOLS / RESET SUPPORT
		// SECURE RESET METHOD: Ensures tier 1 is alive and resets all arrays correctly.
		public void ResetDataToDefault()
		{
			GameData = new PlayerSaveData();
			GameData.HighestUnlockedTier = 1;
			GameData.CurrentBalance = initialBalance;
			GameData.TiersData = new List<TierDynamicData>();
			GameData.PurchasedUpgrades = new List<string>();

			SaveGame();
			this.LogWarning("Player data has been hard-reset to default safely.");
		}
		#endregion
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------