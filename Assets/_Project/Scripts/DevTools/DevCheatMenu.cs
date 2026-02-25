/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-24
 * ----------------------------------------------------------------------------
 * Description:
 * Advanced Developer Terminal. Includes Economy, Time, and Tier management tabs.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Data;

namespace AI_Capitalist.DevTools
{
	public class DevCheatMenu : MonoBehaviour
	{
		[Header("Tab Navigation Buttons")]
		[SerializeField] private Button btnTabEconomy;
		[SerializeField] private Button btnTabTime;
		[SerializeField] private Button btnTabTiers;

		[Header("Tabs Configuration (Panels)")]
		[SerializeField] private GameObject economyTab;
		[SerializeField] private GameObject timeTab;
		[SerializeField] private GameObject tiersTab;

		[Header("Economy Buttons")]
		[SerializeField] private Button btnAdd1K;
		[SerializeField] private Button btnAdd1M;
		[SerializeField] private Button btnAdd1B;
		[SerializeField] private Button btnAdd1T;
		[SerializeField] private Button btnZeroFunds;
		[SerializeField] private Button btnResetSave;

		[Header("Time Buttons")]
		[SerializeField] private Button btnPause;
		[SerializeField] private Button btnPlay;
		[SerializeField] private Button btnTimeX3;
		[SerializeField] private Button btnTimeX10;
		[SerializeField] private Button btnGlobalStrike;

		[Header("Tiers (Selector) UI")]
		[SerializeField] private TMP_Text tierTargetText;
		[SerializeField] private TMP_Text tierLvlText;
		[SerializeField] private Button btnPrevTier;
		[SerializeField] private Button btnNextTier;

		[Header("Tiers (Modify) UI")]
		[SerializeField] private Button btnLvlMinus10;
		[SerializeField] private Button btnLvlMinus1;
		[SerializeField] private Button btnLvlPlus1;
		[SerializeField] private Button btnLvlPlus10;
		[SerializeField] private Button btnStateNone;
		[SerializeField] private Button btnStateHuman;
		[SerializeField] private TMP_Text btnStateHumanText; // To change between "HUMAN" and "STRIKE"
		[SerializeField] private Button btnStateAI;

		private int _currentTierIndex = 0;
		private TierManager _tierManager;

		private void Start()
		{
			// --- Tab Navigation Hooks ---
			if (btnTabEconomy != null) btnTabEconomy.onClick.AddListener(() => ShowTab(0));
			if (btnTabTime != null) btnTabTime.onClick.AddListener(() => ShowTab(1));
			if (btnTabTiers != null) btnTabTiers.onClick.AddListener(() => ShowTab(2));

			// --- Economy Hooks ---
			if (btnAdd1K != null) btnAdd1K.onClick.AddListener(() => AddMoney(new BigDouble(1_000)));
			if (btnAdd1M != null) btnAdd1M.onClick.AddListener(() => AddMoney(new BigDouble(1_000_000)));
			if (btnAdd1B != null) btnAdd1B.onClick.AddListener(() => AddMoney(new BigDouble(1_000_000_000)));
			if (btnAdd1T != null) btnAdd1T.onClick.AddListener(() => AddMoney(new BigDouble(1_000_000_000_000)));
			if (btnZeroFunds != null) btnZeroFunds.onClick.AddListener(ZeroFunds);
			if (btnResetSave != null) btnResetSave.onClick.AddListener(ResetSave);

			// --- Time Hooks ---
			if (btnPause != null) btnPause.onClick.AddListener(() => Time.timeScale = 0f);
			if (btnPlay != null) btnPlay.onClick.AddListener(() => Time.timeScale = 1f);
			if (btnTimeX3 != null) btnTimeX3.onClick.AddListener(() => Time.timeScale = 3f);
			if (btnTimeX10 != null) btnTimeX10.onClick.AddListener(() => Time.timeScale = 10f);
			if (btnGlobalStrike != null) btnGlobalStrike.onClick.AddListener(ForceGlobalStrike);

			// --- Tiers Hooks ---
			if (btnPrevTier != null) btnPrevTier.onClick.AddListener(() => NavigateTier(-1));
			if (btnNextTier != null) btnNextTier.onClick.AddListener(() => NavigateTier(1));
			if (btnLvlMinus10 != null) btnLvlMinus10.onClick.AddListener(() => ModifyTierLevel(-10));
			if (btnLvlMinus1 != null) btnLvlMinus1.onClick.AddListener(() => ModifyTierLevel(-1));
			if (btnLvlPlus1 != null) btnLvlPlus1.onClick.AddListener(() => ModifyTierLevel(1));
			if (btnLvlPlus10 != null) btnLvlPlus10.onClick.AddListener(() => ModifyTierLevel(10));

			if (btnStateNone != null) btnStateNone.onClick.AddListener(() => OverrideTierState(ManagerState.None));
			if (btnStateHuman != null) btnStateHuman.onClick.AddListener(OnHumanStateButtonClicked);
			if (btnStateAI != null) btnStateAI.onClick.AddListener(() => OverrideTierState(ManagerState.AI));

			// Default Tab Setup
			ShowTab(0);
		}

		private void Update()
		{
			if (tiersTab != null && tiersTab.activeSelf)
			{
				UpdateTierDisplay();
			}
		}

		// --- TAB MANAGEMENT ---
		public void ShowTab(int tabIndex)
		{
			if (economyTab != null) economyTab.SetActive(tabIndex == 0);
			if (timeTab != null) timeTab.SetActive(tabIndex == 1);
			if (tiersTab != null) tiersTab.SetActive(tabIndex == 2);

			if (tabIndex == 2) UpdateTierDisplay();
		}

		// --- ECONOMY ---
		private void AddMoney(BigDouble amount)
		{
			var eco = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (eco != null) eco.AddIncome(amount);
		}

		private void ZeroFunds()
		{
			var eco = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (eco != null) eco.SetBalance(BigDouble.Zero);
		}

		private async void ResetSave()
		{
			Time.timeScale = 1f; // Always reset time scale on reboot
			var eco = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (eco != null) eco.SetBalance(BigDouble.Zero);

			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();

			var ugs = Core.CoreManager.Instance.GetService<Services.UGSManager>();
			if (ugs != null) await ugs.SaveCloudDataAsync("saveGame", "{}");

			if (Core.CoreManager.Instance != null)
			{
				Core.CoreManager.Instance.ClearAllServices();
				Destroy(Core.CoreManager.Instance.gameObject);
			}

			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}

		// --- TIME ---
		private void ForceGlobalStrike()
		{
			_tierManager ??= Core.CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null) return;

			foreach (var tier in _tierManager.ActiveTiers)
			{
				if (tier.DynamicData.CurrentState == ManagerState.Human)
				{
					tier.ForceStrike();
				}
			}
			Debug.Log("<color=orange>DEV: Forced global strike on all Human managers!</color>");
		}

		// --- TIERS ---
		private void NavigateTier(int direction)
		{
			_tierManager ??= Core.CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null || _tierManager.ActiveTiers.Count == 0) return;

			_currentTierIndex += direction;
			if (_currentTierIndex < 0) _currentTierIndex = _tierManager.ActiveTiers.Count - 1;
			if (_currentTierIndex >= _tierManager.ActiveTiers.Count) _currentTierIndex = 0;

			UpdateTierDisplay();
		}

		private void UpdateTierDisplay()
		{
			_tierManager ??= Core.CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null || _tierManager.ActiveTiers.Count == 0)
			{
				if (tierTargetText != null) tierTargetText.text = "NO TIERS ACTIVE";
				return;
			}

			if (_currentTierIndex >= _tierManager.ActiveTiers.Count) _currentTierIndex = 0;

			var currentTier = _tierManager.ActiveTiers[_currentTierIndex];
			if (tierTargetText != null) tierTargetText.text = $"[ {currentTier.StaticData.TierID}: {currentTier.StaticData.BusinessName} ]";
			if (tierLvlText != null) tierLvlText.text = $"UNITS: {currentTier.DynamicData.OwnedUnits}";

			if (btnStateHumanText != null)
			{
				if (currentTier.DynamicData.CurrentState == ManagerState.Human)
				{
					btnStateHumanText.text = "STRIKE";
				}
				else
				{
					btnStateHumanText.text = "HUMAN";
				}
			}
		}

		private void ModifyTierLevel(int amount)
		{
			var currentTier = GetSelectedTier();
			if (currentTier == null) return;

			currentTier.DynamicData.OwnedUnits += amount;
			if (currentTier.DynamicData.OwnedUnits < 1) currentTier.DynamicData.OwnedUnits = 1; // Minimum 1 if unlocked

			Core.CoreManager.Instance.GetService<DataManager>()?.SaveGame();
		}

		private void OnHumanStateButtonClicked()
		{
			var currentTier = GetSelectedTier();
			if (currentTier == null) return;

			if (currentTier.DynamicData.CurrentState == ManagerState.Human)
			{
				currentTier.ForceStrike(); // Override button acts as Strike
			}
			else
			{
				OverrideTierState(ManagerState.Human);
			}
		}

		private void OverrideTierState(ManagerState newState)
		{
			var currentTier = GetSelectedTier();
			if (currentTier == null) return;

			currentTier.DynamicData.CurrentState = newState;
			currentTier.DynamicData.AccumulatedDebt = "0"; // Clear any strike debt on override
			currentTier.DynamicData.IsWorkingManually = false;

			Core.CoreManager.Instance.GetService<DataManager>()?.SaveGame();
		}

		private TierController GetSelectedTier()
		{
			_tierManager ??= Core.CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null || _tierManager.ActiveTiers.Count == 0) return null;
			if (_currentTierIndex >= _tierManager.ActiveTiers.Count) return null;
			return _tierManager.ActiveTiers[_currentTierIndex];
		}
	}
}