/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Controls the visuals and buy logic of a single upgrade in the scroll list.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;

namespace AI_Capitalist.UI
{
	public class UpgradeItemUI : MonoBehaviour
	{
		#region UI References
		[SerializeField] private TMP_Text nameText;
		[SerializeField] private TMP_Text descriptionText;
		[SerializeField] private TMP_Text costText;
		[SerializeField] private Button buyButton;
		[SerializeField] private GameObject purchasedOverlay; // Shown when already bought
		#endregion

		#region Fields
		private UpgradeStaticData _myUpgradeData;
		private UpgradesManager _upgradesManager;
		private EconomyManager _economyManager;
		#endregion

		#region Initialization
		public void Initialize(UpgradeStaticData data)
		{
			_myUpgradeData = data;
			_upgradesManager = CoreManager.Instance.GetService<UpgradesManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			nameText.text = data.Name;
			descriptionText.text = data.Description;

			BigDouble cost = BigDouble.Parse(data.Cost);
			costText.text = $"${cost.ToCurrencyString()}";

			if (buyButton != null)
				buyButton.onClick.AddListener(OnBuyClicked);

			if (_economyManager != null)
				_economyManager.OnBalanceChanged += OnBalanceChanged;

			if (_upgradesManager != null)
				_upgradesManager.OnUpgradesChanged += RefreshState;

			RefreshState();
		}

		private void OnDestroy()
		{
			if (_economyManager != null)
				_economyManager.OnBalanceChanged -= OnBalanceChanged;

			if (_upgradesManager != null)
				_upgradesManager.OnUpgradesChanged -= RefreshState;
		}
		#endregion

		#region Logic
		private void OnBalanceChanged(BigDouble newBalance)
		{
			RefreshState();
		}

		private void RefreshState()
		{
			if (_upgradesManager == null || _myUpgradeData == null) return;

			bool isPurchased = _upgradesManager.IsUpgradePurchased(_myUpgradeData.UpgradeID);

			if (isPurchased)
			{
				buyButton.gameObject.SetActive(false);
				if (purchasedOverlay != null) purchasedOverlay.SetActive(true);
			}
			else
			{
				buyButton.gameObject.SetActive(true);
				if (purchasedOverlay != null) purchasedOverlay.SetActive(false);

				buyButton.interactable = _upgradesManager.CanAfford(_myUpgradeData);
			}
		}

		private void OnBuyClicked()
		{
			if (_upgradesManager != null && _myUpgradeData != null)
			{
				_upgradesManager.BuyUpgrade(_myUpgradeData.UpgradeID);
			}
		}
		#endregion
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------