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
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added Milestones, States, Multi-buy split text.
 * 2023-10-29 - Bussuf Senior Dev - Removed "MAX" text replacing it with true calculation UX.
 * 2023-10-29 - Bussuf Senior Dev - Added initial progress bar sync to prevent full bar glitch.
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

			var economy = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (economy != null)
			{
				economy.OnBuyModeChanged += RefreshDisplay;
				economy.OnBalanceChanged += (balance) => RefreshBuyButtonInteractability();
			}

			RefreshDisplay();

			// FIX VISUAL GLITCH: Force sync the progress bar on load so it matches data
			UpdateProgressBar(_controller.GetNormalizedProgress());
		}

		private void OnDestroy()
		{
			if (_controller != null)
			{
				_controller.OnProgressUpdated -= UpdateProgressBar;
				_controller.OnDataChanged -= RefreshDisplay;
			}

			if (Core.CoreManager.Instance != null)
			{
				var economy = Core.CoreManager.Instance.GetService<EconomyManager>();
				if (economy != null) economy.OnBuyModeChanged -= RefreshDisplay;
			}
		}

		private void UpdateProgressBar(float normalizedProgress)
		{
			progressBarFill.fillAmount = normalizedProgress;
		}

		private void RefreshDisplay()
		{
			levelText.text = $"Lvl: {_controller.DynamicData.OwnedUnits}";
			milestoneBarFill.fillAmount = _controller.GetMilestoneProgress();

			BigDouble baseRev = BigDouble.Parse(_controller.StaticData.Base_Rev);
			BigDouble totalRev = baseRev * _controller.DynamicData.OwnedUnits * _controller.GetMilestoneMultiplier();
			revenueText.text = $"Rev: ${totalRev.ToCurrencyString()}";

			UpdateBuyButtonDisplay();
			UpdateStateImageDisplay();
		}

		private void UpdateBuyButtonDisplay()
		{
			var economy = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (economy == null) return;

			BigDouble nextCost = economy.GetBuyCostAndAmount(_controller.StaticData.TierID, _controller.DynamicData.OwnedUnits, out int amountToBuy);

			buyAmountText.text = economy.CurrentBuyMode == BuyMode.Max ? $"Buy {amountToBuy}" : $"Buy {amountToBuy}";
			buyCostText.text = $"${nextCost.ToCurrencyString()}";

			RefreshBuyButtonInteractability();
		}

		private void RefreshBuyButtonInteractability()
		{
			var economy = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (economy == null) return;

			BigDouble cost = economy.GetBuyCostAndAmount(_controller.StaticData.TierID, _controller.DynamicData.OwnedUnits, out int amountToBuy);
			buyButton.interactable = economy.CurrentBalance >= cost && amountToBuy > 0;
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