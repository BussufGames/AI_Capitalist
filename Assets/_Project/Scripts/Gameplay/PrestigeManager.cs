/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Handles the logic for prestige resets.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Fixed Memory Ghost bug (Now reloads Scene 0 to rebuild CoreManager).
 * 2026-02-28 - Fixed LifetimeEarnings wipe bug (Lifetime is now preserved).
 * 2026-02-28 - Fixed Token Double-Dipping bug (Only adds pending tokens).
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BussufGames.Core;

namespace AI_Capitalist.Gameplay
{
	public class PrestigeManager : MonoBehaviour, IService
	{
		// To start getting tokens, player needs at least 1 Million total lifetime earnings
		private readonly BigDouble PRESTIGE_THRESHOLD = new BigDouble(1_000_000);

		private EconomyManager _economyManager;
		private DataManager _dataManager;

		private void Awake()
		{
			if (CoreManager.Instance != null)
				CoreManager.Instance.RegisterService<PrestigeManager>(this);
		}

		public void Initialize()
		{
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_dataManager = CoreManager.Instance.GetService<DataManager>();
		}

		public BigDouble CalculatePendingPrestigeTokens()
		{
			if (_economyManager == null) return BigDouble.Zero;

			BigDouble lifetime = _economyManager.LifetimeEarnings;

			// Formula: (Lifetime / 1,000,000) ^ 0.5
			if (lifetime < PRESTIGE_THRESHOLD) return BigDouble.Zero;

			BigDouble tokens = BigDouble.Pow(lifetime / PRESTIGE_THRESHOLD, 0.5);
			return BigDouble.Floor(tokens);
		}

		// Calculate only the NEW tokens they will get if they reset right now
		public BigDouble GetPendingTokensToClaim()
		{
			BigDouble totalPossible = CalculatePendingPrestigeTokens();
			BigDouble alreadyClaimed = _economyManager.PrestigeTokens;

			BigDouble pending = totalPossible - alreadyClaimed;
			return pending > 0 ? pending : BigDouble.Zero;
		}

		// Calculates the exact dollar amount needed to reach the NEXT token!
		public BigDouble GetLifetimeNeededForNextToken()
		{
			BigDouble nextTokenTarget = CalculatePendingPrestigeTokens() + 1;

			// Inverse of Square Root formula: Target^2 * Threshold
			return PRESTIGE_THRESHOLD * BigDouble.Pow(nextTokenTarget, 2);
		}

		public void PerformPrestige()
		{
			BigDouble pendingTokens = GetPendingTokensToClaim();

			if (pendingTokens <= 0)
			{
				this.LogWarning("Prestige aborted: No pending tokens to claim.");
				return;
			}

			// 1. Add NEW tokens to the existing stash
			BigDouble currentTokens = _economyManager.PrestigeTokens;
			_dataManager.GameData.PrestigeTokens = (currentTokens + pendingTokens).ToString();

			// 2. Reset Economy (We DO NOT reset Lifetime Earnings!)
			_economyManager.SetBalance(BigDouble.Zero);

			// 3. Reset Tiers and Managers
			_dataManager.GameData.TiersData.Clear();
			_dataManager.GameData.HighestUnlockedTier = 1;

			// 4. Reset Upgrades
			_dataManager.GameData.PurchasedUpgrades.Clear();

			// Save the wiped state
			_dataManager.SaveGame();
			this.LogSuccess($"Prestige successful! Claimed {pendingTokens} new tokens.");

			// 5. Reload Game cleanly to prevent memory state issues (Ghost Objects)
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.ClearAllServices();
				Destroy(CoreManager.Instance.gameObject);
			}

			DG.Tweening.DOTween.KillAll();
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------