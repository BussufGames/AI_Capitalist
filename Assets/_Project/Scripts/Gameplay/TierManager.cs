/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-30
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-30 - Bussuf Senior Dev - Refactored to only spawn Unlocked tiers.
 * Added UnlockNextTier logic and OnTierUnlocked event.
 * 2023-10-31 - Bussuf Senior Dev - Unlocked tiers now start at Level 1 & begin work immediately.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BreakInfinity;
using BussufGames.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI_Capitalist.Gameplay
{
	public class TierManager : MonoBehaviour, IService
	{
		public List<TierController> ActiveTiers { get; private set; } = new List<TierController>();

		// Fired when a new tier is purchased, sending the NEW controller to the UI.
		public event Action<TierController> OnTierUnlocked;

		private DataManager _dataManager;
		private EconomyManager _economyManager;

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<TierManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing TierManager (Dynamic Spawning)...");

			_dataManager = CoreManager.Instance.GetService<DataManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_dataManager == null || _economyManager == null)
			{
				this.LogError("Missing Core Services. TierManager cannot initialize.");
				return;
			}

			SpawnUnlockedTiers();
		}

		private void SpawnUnlockedTiers()
		{
			int highestUnlocked = _dataManager.GameData.HighestUnlockedTier;

			// Spawn only up to the highest unlocked tier
			for (int i = 1; i <= highestUnlocked; i++)
			{
				if (_economyManager.TryGetTierConfig(i, out TierStaticData staticData))
				{
					SpawnSingleTierLogic(staticData);
				}
			}

			this.LogSuccess($"Successfully loaded {ActiveTiers.Count} Unlocked Tiers.");
		}

		private TierController SpawnSingleTierLogic(TierStaticData staticData)
		{
			GameObject tierObj = new GameObject($"Tier_{staticData.TierID}_{staticData.BusinessName}");
			tierObj.transform.SetParent(this.transform);

			TierController controller = tierObj.AddComponent<TierController>();
			TierDynamicData dynamicData = GetOrCreateDynamicData(staticData.TierID);

			controller.Initialize(dynamicData, staticData);
			ActiveTiers.Add(controller);

			return controller;
		}

		private TierDynamicData GetOrCreateDynamicData(int tierID)
		{
			var savedTiers = _dataManager.GameData.TiersData;
			var existingData = savedTiers.Find(t => t.TierID == tierID);

			if (existingData != null)
			{
				return existingData;
			}

			TierDynamicData newData = new TierDynamicData(tierID);
			savedTiers.Add(newData);
			_dataManager.SaveGame();
			return newData;
		}

		public bool TryUnlockNextTier()
		{
			int nextTierID = _dataManager.GameData.HighestUnlockedTier + 1;

			if (!_economyManager.TryGetTierConfig(nextTierID, out TierStaticData nextConfig))
			{
				this.Log("No more tiers to unlock!");
				return false;
			}

			BigDouble unlockCost = BigDouble.Parse(nextConfig.Unlock_Cost);

			if (_economyManager.TrySpend(unlockCost))
			{
				// Update Save Data
				_dataManager.GameData.HighestUnlockedTier = nextTierID;

				// Spawn the logic controller
				TierController newController = SpawnSingleTierLogic(nextConfig);

				// MODIFICATION: Start at Level 1 and begin work immediately!
				newController.DynamicData.OwnedUnits = 1;
				newController.DynamicData.IsWorkingManually = true;

				_dataManager.SaveGame();

				this.LogSuccess($"Unlocked Tier {nextTierID}!");

				// Tell the UI to animate and draw it!
				OnTierUnlocked?.Invoke(newController);
				return true;
			}

			return false;
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------