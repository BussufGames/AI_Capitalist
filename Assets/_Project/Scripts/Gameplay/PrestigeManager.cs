/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-31 - Bussuf Senior Dev - Changed formula to Square Root for better scaling.
 * Added GetLifetimeNeededForNextToken for dynamic UI tracking.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
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

		private DataManager _dataManager;
		private EconomyManager _economyManager;

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<PrestigeManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing PrestigeManager...");
			_dataManager = CoreManager.Instance.GetService<DataManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
		}

		// Calculate how many tokens the player WOULD have based on their entire history.
		private BigDouble CalculateTotalLifetimeTokens()
		{
			if (_economyManager.LifetimeEarnings < PRESTIGE_THRESHOLD)
				return BigDouble.Zero;

			BigDouble ratio = _economyManager.LifetimeEarnings / PRESTIGE_THRESHOLD;

			// Square Root provides a very solid, standard incremental game progression curve.
			BigDouble rawTokens = BigDouble.Floor(BigDouble.Pow(ratio, 0.5));

			return rawTokens;
		}

		// Calculate only the NEW tokens they will get if they reset right now
		public BigDouble GetPendingTokensToClaim()
		{
			BigDouble totalPossible = CalculateTotalLifetimeTokens();
			BigDouble alreadyClaimed = _economyManager.PrestigeTokens;

			BigDouble pending = totalPossible - alreadyClaimed;
			return pending > 0 ? pending : BigDouble.Zero;
		}

		// NEW: Calculates the exact dollar amount needed to reach the NEXT token!
		public BigDouble GetLifetimeNeededForNextToken()
		{
			BigDouble nextTokenTarget = CalculateTotalLifetimeTokens() + 1;

			// Inverse of Square Root formula: Target^2 * Threshold
			return PRESTIGE_THRESHOLD * BigDouble.Pow(nextTokenTarget, 2);
		}

		[ContextMenu("FORCE PRESTIGE RESET (DEBUG)")]
		public async void ClaimPrestigeAndReset()
		{
			BigDouble tokensToClaim = GetPendingTokensToClaim();

			if (tokensToClaim <= 0)
			{
				this.LogWarning("Not enough earnings to prestige yet!");
				return;
			}

			this.LogWarning($"<color=magenta>PRESTIGE ACTIVATED! Claiming {tokensToClaim} tokens!</color>");

			// 1. Add new tokens to current stash
			BigDouble currentTokens = BigDouble.Parse(_dataManager.GameData.PrestigeTokens);
			_dataManager.GameData.PrestigeTokens = (currentTokens + tokensToClaim).ToString();

			// 2. Wipe standard game progress (Soft Reset)
			_dataManager.GameData.CurrentBalance = "0";
			_dataManager.GameData.HighestUnlockedTier = 1;
			_dataManager.GameData.TiersData.Clear(); // Nuke the businesses!

			// 3. Save immediately
			_dataManager.SaveGame();

			// 4. Destroy Core & Reboot to re-initialize everything clean
			if (CoreManager.Instance != null)
			{
				Destroy(CoreManager.Instance.gameObject);
			}

			await Task.Delay(150);
			SceneManager.LoadScene(0);
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------