/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-24 - Bussuf Senior Dev - Bound UpdateBuyButtonDisplay to balance changes.
 * 2026-02-24 - Bussuf Senior Dev - Added 5-Lights UX for Human Manager fatigue/debt.
 * 2026-02-28 - Bussuf Senior Dev - Added Overdrive UI logic (Scrolling RawImage & Per Sec Text).
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BreakInfinity;
using System;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;
using AI_Capitalist.Data;

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

		[Header("Overdrive UX (Flowing Bar)")]
		[Tooltip("The RawImage with repeating stripes texture. Must be child of the Progress Bar.")]
		[SerializeField] private RawImage overdriveFlowImage;
		[SerializeField] private float overdriveScrollSpeed = 2f;

		[Header("Human Manager UX (5 Lights)")]
		[SerializeField] private GameObject lightsContainer;
		// Holds all 5 lights (hide/show)
		[SerializeField] private Image[] lightImages;            // Array of exactly 5 images
		[SerializeField] private Color lightOnColor = Color.green;
		[SerializeField] private Color lightOffColor = Color.gray;

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
				_economyManager.OnBalanceChanged += OnBalanceChangedHandler;
			}

			// Hide overdrive by default
			if (overdriveFlowImage != null) overdriveFlowImage.gameObject.SetActive(false);

			RefreshDisplay();
			UpdateProgressBar(_controller.GetNormalizedProgress());
		}

		private void Update()
		{
			// Parallax scroll effect for Overdrive stripes
			if (_controller != null && _controller.IsOverdriveActive && overdriveFlowImage != null && overdriveFlowImage.gameObject.activeSelf)
			{
				Rect uvRect = overdriveFlowImage.uvRect;
				uvRect.x -= Time.deltaTime * overdriveScrollSpeed;
				overdriveFlowImage.uvRect = uvRect;
			}
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
			if (_controller.IsOverdriveActive)
			{
				progressBarFill.fillAmount = 1f; // Lock full
			}
			else
			{
				progressBarFill.fillAmount = normalizedProgress;
			}
		}

		private void RefreshDisplay()
		{
			levelText.text = $"Lvl: {_controller.DynamicData.OwnedUnits}";
			milestoneBarFill.fillAmount = _controller.GetMilestoneProgress();

			BigDouble revPerCycle = _controller.GetRevenuePerCycle();

			// Toggle Overdrive Visuals & Text
			if (_controller.IsOverdriveActive)
			{
				if (overdriveFlowImage != null) overdriveFlowImage.gameObject.SetActive(true);

				BigDouble revPerSec = revPerCycle / _controller.GetCurrentCycleTime();
				revenueText.text = $"${revPerSec.ToCurrencyString()}/ sec";
			}
			else
			{
				if (overdriveFlowImage != null) overdriveFlowImage.gameObject.SetActive(false);
				revenueText.text = $"${revPerCycle.ToCurrencyString()}";
			}

			UpdateBuyButtonDisplay();
			UpdateStateImageDisplay();
			UpdateHumanLightsUX();
		}

		private void UpdateHumanLightsUX()
		{
			if (lightsContainer == null || lightImages == null || lightImages.Length != 5) return;
			// Only show lights if there is a human manager
			if (_controller.DynamicData.CurrentState == Data.ManagerState.Human)
			{
				lightsContainer.SetActive(true);

				BigDouble salary = BigDouble.Parse(_controller.StaticData.Base_Human_Salary_Per_Cycle);
				BigDouble currentDebt = BigDouble.Parse(_controller.DynamicData.AccumulatedDebt);

				int missedPayments = salary > BigDouble.Zero ? (int)Math.Floor((currentDebt / salary).ToDouble()) : 0;

				// Max 5 lights.
				// If missed 0 -> 5 active. If missed 5 -> 0 active.
				int activeLights = Mathf.Clamp(5 - missedPayments, 0, 5);
				for (int i = 0; i < lightImages.Length; i++)
				{
					lightImages[i].color = (i < activeLights) ? lightOnColor : lightOffColor;
				}
			}
			else
			{
				lightsContainer.SetActive(false);
			}
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
			var uiManager = Core.CoreManager.Instance.GetService<UIManager>();
			if (uiManager == null) return;

			if (_controller.DynamicData.CurrentState == Data.ManagerState.AI)
				actionButtonStateImage.sprite = uiManager.StateAIRunning;
			else if (_controller.DynamicData.CurrentState == Data.ManagerState.Human)
				actionButtonStateImage.sprite = _controller.IsHumanOnStrike() ? uiManager.StateHumanStrike : uiManager.StateHumanWorking;
			else
				actionButtonStateImage.sprite = uiManager.StateManual;
		}

		private void OnActionClicked()
		{
			iconImage.transform.DOComplete();
			iconImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);

			if (_controller.DynamicData.CurrentState == Data.ManagerState.AI) return;
			if (_controller.DynamicData.CurrentState == Data.ManagerState.Human && !_controller.IsHumanOnStrike()) return;
			if (_controller.DynamicData.CurrentState == Data.ManagerState.None && _controller.IsWorkingManually) return;

			if (_controller.DynamicData.CurrentState == Data.ManagerState.Human && _controller.IsHumanOnStrike())
				_controller.PayHumanDebt();
			else
				_controller.ManualClick();
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
			if (uiManager != null) uiManager.OpenManagerPopup(_controller);
		}
	}
}