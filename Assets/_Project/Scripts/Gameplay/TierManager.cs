/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Dynamically spawns and initializes TierControllers based on the Economy JSON.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BussufGames.Core;
using System.Collections.Generic;
using UnityEngine;

namespace AI_Capitalist.Gameplay
{
	public class TierManager : MonoBehaviour, IService
	{
		[Header("References")]
		[Tooltip("The generic Prefab that contains the TierController script.")]
		[SerializeField] private GameObject tierPrefab;
		[Tooltip("The parent transform in the UI where tiers will be spawned.")]
		[SerializeField] private Transform tierContainer;

		private DataManager _dataManager;
		private EconomyManager _economyManager;

		public List<TierController> ActiveTiers { get; private set; } = new List<TierController>();

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

			if (tierPrefab == null || tierContainer == null)
			{
				this.LogError("TierPrefab or TierContainer is missing! Cannot spawn tiers.");
				return;
			}

			SpawnTiers();
		}

		private void SpawnTiers()
		{
			// In a real scenario, EconomyManager should expose the list of all static data.
			// For now, we assume we know there are 2 tiers based on our JSON test.
			// (We will add a method to EconomyManager to fetch all later).

			// Temporary hack for MVP to iterate known Tiers
			for (int i = 1; i <= 2; i++)
			{
				TierStaticData staticData = _economyManager.GetTierConfig(i);
				if (staticData == null) continue;

				TierDynamicData dynamicData = GetOrCreateDynamicData(i);

				GameObject tierObj = Instantiate(tierPrefab, tierContainer);
				tierObj.name = $"Tier_{i}_{staticData.BusinessName}";

				TierController controller = tierObj.GetComponent<TierController>();
				if (controller != null)
				{
					controller.Initialize(dynamicData, staticData);
					ActiveTiers.Add(controller);
				}
				else
				{
					this.LogError($"TierPrefab is missing the TierController script!");
				}
			}

			this.LogSuccess($"Successfully spawned {ActiveTiers.Count} Tiers.");
		}

		private TierDynamicData GetOrCreateDynamicData(int tierID)
		{
			var existing = _dataManager.GameData.TiersData.Find(t => t.TierID == tierID);
			if (existing != null) return existing;

			// If it doesn't exist in the save (new game), create it
			var newData = new TierDynamicData(tierID);
			_dataManager.GameData.TiersData.Add(newData);
			return newData;
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------