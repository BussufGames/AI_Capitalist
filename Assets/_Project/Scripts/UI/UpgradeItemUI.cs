/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Controls the visuals for a single upgrade item prefab in the list.
 * Displaying target (global/business), type (speed/revenue), multiplier, and cost.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Removed description. Added TierIcon, TypeIcon, and Multiplier Text.
 * 2026-02-28 - Added failsafe UIManager retrieval on initialization.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;
using BussufGames.Core;

namespace AI_Capitalist.UI
{
	public class UpgradeItemUI : MonoBehaviour
	{
		#region UI References
		[Header("Icons & Multiplier")]
		[Tooltip("Shows the specific business icon (e.g. Lemon) or Global upgrade icon.")]
		[SerializeField] private Image tierIconImage;

		[Tooltip("Shows Time (Clock) or Money (Dollar) upgrade icon.")]
		[SerializeField] private Image typeIconImage;

		[Tooltip("Minimal text showing the multiplication, e.g., 'X3'.")]
		[SerializeField] private TMP_Text multiplierText;

		[Header("Text & Data")]
		[SerializeField] private TMP_Text nameText;
		[SerializeField] private TMP_Text costText;

		[Header("Controls")]
		[SerializeField] private Button buyButton;
		[SerializeField] private GameObject purchasedOverlay;
		#endregion

		#region Fields
		private UpgradeStaticData _myUpgradeData;
		private UpgradesManager _upgradesManager;
		private EconomyManager _economyManager;
		private UIManager _uiManager;
		#endregion

		#region Lifecycle
		public void Initialize(UpgradeStaticData data)
		{
			_myUpgradeData = data;

			// Cache managers
			_upgradesManager = CoreManager.Instance.GetService<UpgradesManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_uiManager = CoreManager.Instance.GetService<UIManager>();

			// --- Setup Basic Text ---
			nameText.text = data.Name;
			multiplierText.text = $"X{data.Multiplier}";

			BigDouble cost = BigDouble.Parse(data.Cost);
			costText.text = $"${cost.ToCurrencyString()}";

			// --- Setup Visual Icons via UIManager ---
			if (_uiManager != null)
			{
				// 1. Set Target Icon (Global vs Specific Business)
				if (data.TargetTierID == 0)
				{
					// Global (Use global icon from UIManager)
					if (tierIconImage != null) tierIconImage.sprite = _uiManager.GlobalBonusIcon;
				}
				else
				{
					// Specific Tier (Fetch unique icon via UIManager dictionary)
					var visualData = _uiManager.GetVisualDataForTier(data.TargetTierID);
					if (visualData != null && tierIconImage != null)
					{
						tierIconImage.sprite = visualData.TierIcon;
					}
				}

				// 2. Set Type Icon (Revenue vs Speed)
				if (typeIconImage != null)
				{
					typeIconImage.sprite = data.UpgradeType == "Speed"
						? _uiManager.TimeUpgradeIcon
						: _uiManager.RevenueUpgradeIcon;
				}
			}
			else
			{
				this.LogError("UpgradeItemUI: UIManager not found. Visual icons will not display!");
			}

			// --- Events ---
			if (buyButton != null)
			{
				buyButton.onClick.RemoveAllListeners(); // Prevent double clicks on object reuse
				buyButton.onClick.AddListener(OnBuyClicked);
			}

			if (_economyManager != null) _economyManager.OnBalanceChanged += OnBalanceChanged;
			if (_upgradesManager != null) _upgradesManager.OnUpgradesChanged += RefreshState;

			// Set initial visual state
			RefreshState();
		}

		private void OnDestroy()
		{
			if (_economyManager != null) _economyManager.OnBalanceChanged -= OnBalanceChanged;
			if (_upgradesManager != null) _upgradesManager.OnUpgradesChanged -= RefreshState;
		}
		#endregion

		#region Logic & Visual Updates
		private void OnBalanceChanged(BigDouble newBalance)
		{
			// Optimization: only update button state, overlay doesn't change on money change
			if (_upgradesManager != null && _myUpgradeData != null && buyButton.gameObject.activeSelf)
			{
				buyButton.interactable = _upgradesManager.CanAfford(_myUpgradeData);
			}
		}

		private void RefreshState()
		{
			if (_upgradesManager == null || _myUpgradeData == null) return;

			bool isPurchased = _upgradesManager.IsUpgradePurchased(_myUpgradeData.UpgradeID);

			if (isPurchased)
			{
				// Show purchased overlay, hide buy button
				if (buyButton != null) buyButton.gameObject.SetActive(false);
				if (purchasedOverlay != null) purchasedOverlay.SetActive(true);
			}
			else
			{
				// Show buy button, check if affordable, hide overlay
				if (buyButton != null)
				{
					buyButton.gameObject.SetActive(true);
					buyButton.interactable = _upgradesManager.CanAfford(_myUpgradeData);
				}

				if (purchasedOverlay != null) purchasedOverlay.SetActive(false);
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