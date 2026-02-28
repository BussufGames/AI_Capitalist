/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Spawns and manages the list of all upgrades in the Upgrades Page.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using System.Linq;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Economy;

namespace AI_Capitalist.UI
{
	public class UpgradesUIView : MonoBehaviour
	{
		[SerializeField] private Transform contentContainer;
		[SerializeField] private GameObject upgradeItemPrefab;

		private bool _isInitialized = false;

		private void OnEnable()
		{
			// Only spawn the list the first time the page is opened
			if (!_isInitialized)
			{
				PopulateList();
				_isInitialized = true;
			}
		}

		private void PopulateList()
		{
			var ecoManager = CoreManager.Instance.GetService<EconomyManager>();
			if (ecoManager == null) return;

			var allUpgrades = ecoManager.GetAllUpgrades();
			if (allUpgrades == null || allUpgrades.Count == 0) return;

			// Clear placeholder children if any
			foreach (Transform child in contentContainer) Destroy(child.gameObject);

			// Sort upgrades by cost (Cheapest first)
			var sortedUpgrades = allUpgrades.Values
				.OrderBy(u => BigDouble.Parse(u.Cost))
				.ToList();

			foreach (var upgrade in sortedUpgrades)
			{
				GameObject go = Instantiate(upgradeItemPrefab, contentContainer);
				UpgradeItemUI itemUI = go.GetComponent<UpgradeItemUI>();
				if (itemUI != null)
				{
					itemUI.Initialize(upgrade);
				}
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------