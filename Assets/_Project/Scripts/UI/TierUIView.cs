/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * The View layer for a single Tier. 
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Added initial progress bar sync.
 * 2026-02-24 - Bussuf Senior Dev - Fixed Rev display to include Global Prestige Multiplier.
 * 2026-02-24 - Bussuf Senior Dev - Bound UpdateBuyButtonDisplay to balance changes for live MAX updates.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BreakInfinity;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;

namespace AI_Capitalist.UI
{
	public class TierUIView : MonoBehaviour
	{
		[Header("UI Elements - Text")]
		[SerializeField] private TMP_Text nameText;
		[SerializeField] private TMP_Text levelText;
		[SerializeField] private TMP_Text revenueText;

		[Header("Buy Button Texts")]
		[SerializeField] private TMP_Text buyAmountText;
		[SerializeField] private TMP_Text buyCostText;

		[Header("UI Elements - Images & Bars")]
		[SerializeField] private Image iconImage;
		[SerializeField] private Image progressBarFill;
		[SerializeField] private Image milestoneBarFill;
		[SerializeField] private Image actionButtonStateImage;

		[Header("Buttons")]
		[SerializeField] private Button actionButton;
		[SerializeField] private Button buyButton;
		[SerializeField] private Button iconButton;

		private TierController _controller;
		private TierVisualData _visualData;
		private EconomyManager _economyManager;
		private RectTransform _rectTransform;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();

			actionButton.onClick.AddListener(OnActionClicked);
			buyButton.onClick.AddListener(OnBuyClicked);

			if (iconButton != null)
				iconButton.onClick.AddListener(OnIconClicked);
		}

		public void Initialize(TierController controller, TierVisualData visualData)
		{
			_controller = controller;
			_visualData = visualData;
			_economyManager = Core.CoreManager.Instance.GetService<EconomyManager>();

			if (visualData != null)
			{
				nameText.text = visualData.DisplayName;
				if (visualData.TierIcon != null) iconImage.sprite = visualData.TierIcon;
			}
			else
			{
				nameText.text = $"Tier {_controller.StaticData.TierID}";
			}

			_controller.OnProgressUpdated += UpdateProgressBar;
			_controller.OnDataChanged += RefreshDisplay;

			if (_economyManager != null)
			{
				_economyManager.OnBuyModeChanged += RefreshDisplay;
				// FIX: Now triggers full button display update to keep MAX amounts accurate
				_economyManager.OnBalanceChanged += OnBalanceChangedHandler;
			}

			RefreshDisplay();
			UpdateProgressBar(_controller.GetNormalizedProgress());
		}

		private void OnDestroy()
		{
			if (_controller != null)
			{
				_controller.OnProgressUpdated -= UpdateProgressBar;
				_controller.OnDataChanged -= RefreshDisplay;
			}

			if (_economyManager != null)
			{
				_economyManager.OnBuyModeChanged -= RefreshDisplay;
				_economyManager.OnBalanceChanged -= OnBalanceChangedHandler;
			}
		}

		private void OnBalanceChangedHandler(BigDouble currentBalance)
		{
			UpdateBuyButtonDisplay();
		}

		private void UpdateProgressBar(float normalizedProgress)
		{
			progressBarFill.fillAmount = normalizedProgress;
		}

		private void RefreshDisplay()
		{
			levelText.text = $"Lvl: {_controller.DynamicData.OwnedUnits}";
			milestoneBarFill.fillAmount = _controller.GetMilestoneProgress();

			// FIX: Multiply visual revenue by Prestige Global Multiplier
			BigDouble baseRev = BigDouble.Parse(_controller.StaticData.Base_Rev);
			double prestigeBonus = _economyManager != null ? _economyManager.GetGlobalPrestigeMultiplier() : 1.0;
			BigDouble totalRev = baseRev * _controller.DynamicData.OwnedUnits * _controller.GetMilestoneMultiplier() * prestigeBonus;

			revenueText.text = $"Rev: ${totalRev.ToCurrencyString()}";

			UpdateBuyButtonDisplay();
			UpdateStateImageDisplay();
		}

		private void UpdateBuyButtonDisplay()
		{
			if (_economyManager == null) return;

			BigDouble nextCost = _economyManager.GetBuyCostAndAmount(_controller.StaticData.TierID, _controller.DynamicData.OwnedUnits, out int amountToBuy);

			buyAmountText.text = $"Buy {amountToBuy}";
			buyCostText.text = $"${nextCost.ToCurrencyString()}";

			buyButton.interactable = _economyManager.CurrentBalance >= nextCost && amountToBuy > 0;
		}

		private void UpdateStateImageDisplay()
		{
			if (_visualData == null) return;

			if (_controller.DynamicData.CurrentState == Data.ManagerState.AI)
			{
				actionButtonStateImage.sprite = _visualData.StateAIRunning;
			}
			else if (_controller.DynamicData.CurrentState == Data.ManagerState.Human)
			{
				if (_controller.IsHumanOnStrike())
				{
					actionButtonStateImage.sprite = _visualData.StateHumanStrike;
				}
				else
				{
					actionButtonStateImage.sprite = _visualData.StateHumanWorking;
				}
			}
			else
			{
				actionButtonStateImage.sprite = _visualData.StateManual;
			}
		}

		private void OnActionClicked()
		{
			iconImage.transform.DOComplete();
			iconImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);

			if (_controller.DynamicData.CurrentState == Data.ManagerState.AI) return;
			if (_controller.DynamicData.CurrentState == Data.ManagerState.Human && !_controller.IsHumanOnStrike()) return;
			if (_controller.DynamicData.CurrentState == Data.ManagerState.None && _controller.IsWorkingManually) return;

			if (_controller.DynamicData.CurrentState == Data.ManagerState.Human && _controller.IsHumanOnStrike())
			{
				_controller.PayHumanDebt();
			}
			else
			{
				_controller.ManualClick();
			}
		}

		private void OnBuyClicked()
		{
			_rectTransform.DOComplete();
			_rectTransform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 10, 1);

			_controller.BuyUnits();
		}

		private void OnIconClicked()
		{
			iconImage.transform.DOComplete();
			iconImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);

			var uiManager = Core.CoreManager.Instance.GetService<UIManager>();
			if (uiManager != null)
			{
				uiManager.OpenManagerPopup(_controller);
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------