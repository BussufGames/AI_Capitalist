/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Sits on the main screen. Always shows and allows buying the cheapest available upgrade.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;

namespace AI_Capitalist.UI
{
	public class QuickBuyNextUIView : MonoBehaviour
	{
		#region UI References
		[SerializeField] private Button quickBuyButton;
		[SerializeField] private TMP_Text upgradeNameText;
		[SerializeField] private TMP_Text upgradeCostText;
		[SerializeField] private GameObject maxedOutContainer; // Shows when all upgrades are bought
		#endregion

		#region Fields
		private UpgradesManager _upgradesManager;
		private EconomyManager _economyManager;
		private UpgradeStaticData _currentCheapestUpgrade;
		#endregion

		#region Lifecycle
		private void Start()
		{
			_upgradesManager = CoreManager.Instance.GetService<UpgradesManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (quickBuyButton != null)
				quickBuyButton.onClick.AddListener(OnQuickBuyClicked);

			if (_economyManager != null)
				_economyManager.OnBalanceChanged += OnBalanceChanged;

			if (_upgradesManager != null)
				_upgradesManager.OnUpgradesChanged += RefreshTarget;

			RefreshTarget();
		}

		private void OnDestroy()
		{
			if (_economyManager != null)
				_economyManager.OnBalanceChanged -= OnBalanceChanged;

			if (_upgradesManager != null)
				_upgradesManager.OnUpgradesChanged -= RefreshTarget;
		}
		#endregion

		#region Logic
		private void RefreshTarget()
		{
			if (_upgradesManager == null) return;

			_currentCheapestUpgrade = _upgradesManager.GetCheapestAvailableUpgrade();

			if (_currentCheapestUpgrade == null)
			{
				// All upgrades purchased or locked
				quickBuyButton.gameObject.SetActive(false);
				if (maxedOutContainer != null) maxedOutContainer.SetActive(true);
				return;
			}

			quickBuyButton.gameObject.SetActive(true);
			if (maxedOutContainer != null) maxedOutContainer.SetActive(false);

			upgradeNameText.text = _currentCheapestUpgrade.Name;
			BigDouble cost = BigDouble.Parse(_currentCheapestUpgrade.Cost);
			upgradeCostText.text = $"${cost.ToCurrencyString()}";

			CheckAffordability();
		}

		private void OnBalanceChanged(BigDouble newBalance)
		{
			CheckAffordability();
		}

		private void CheckAffordability()
		{
			if (_currentCheapestUpgrade == null || _upgradesManager == null) return;
			quickBuyButton.interactable = _upgradesManager.CanAfford(_currentCheapestUpgrade);
		}

		private void OnQuickBuyClicked()
		{
			if (_currentCheapestUpgrade != null && _upgradesManager != null)
			{
				_upgradesManager.BuyUpgrade(_currentCheapestUpgrade.UpgradeID);
			}
		}
		#endregion
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------