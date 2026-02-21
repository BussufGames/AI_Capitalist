/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Orchestrates the 'Local First, Cloud Second' save data architecture.
 * Serializes the game state using Newtonsoft.Json.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Services;
using BussufGames.Core;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace AI_Capitalist.Data
{
	public class DataManager : MonoBehaviour, IService
	{
		private const string SAVE_KEY = "ai_cap_save_v1";
		public PlayerSaveData GameData { get; private set; }

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

			// 1. Load Local Data Instantly
			LoadLocalData();

			// 2. Initialize new profile if completely empty
			if (GameData == null)
			{
				this.LogWarning("No local save found. Creating new player profile.");
				GameData = new PlayerSaveData();
				GameData.LastSaveTime = DateTime.UtcNow.ToString("O");
			}

			// 3. Fire off Cloud Sync in the background (Non-blocking)
			SyncWithCloudBackground();
		}

		public void SaveGame()
		{
			GameData.LastSaveTime = DateTime.UtcNow.ToString("O");

			// Save Locally First
			string json = JsonConvert.SerializeObject(GameData, Formatting.None);
			PlayerPrefs.SetString(SAVE_KEY, json);
			PlayerPrefs.Save();
			this.Log("Game Saved Locally.");

			// Push to Cloud
			var ugs = CoreManager.Instance.GetService<UGSManager>();
			if (ugs != null && ugs.IsAuthenticated)
			{
				_ = ugs.SaveCloudDataAsync(SAVE_KEY, json);
			}
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

			// Wait briefly to ensure UGS has time to authenticate
			await System.Threading.Tasks.Task.Delay(2000);

			if (ugs.IsAuthenticated)
			{
				string cloudJson = await ugs.LoadCloudDataAsync(SAVE_KEY);
				if (!string.IsNullOrEmpty(cloudJson))
				{
					PlayerSaveData cloudData = JsonConvert.DeserializeObject<PlayerSaveData>(cloudJson);

					// Basic Resolution MVP: If cloud has data, overwrite local.
					// (In a full production build, we compare LastSaveTime here).
					GameData = cloudData;
					SaveGame(); // Resave locally to ensure parity
					this.LogSuccess("Cloud save fetched and applied.");
				}
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------