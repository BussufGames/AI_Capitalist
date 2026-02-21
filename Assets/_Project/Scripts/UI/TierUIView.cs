/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The View layer for a single Tier. Dumb UI that only listens to events 
 * from the TierController and updates TextMeshPro/Images.
 * Uses DOTween for visual feedback (Juice).
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
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
		[SerializeField] private TMP_Text costText;
		[SerializeField] private TMP_Text stateText;

		[Header("UI Elements - Images")]
		[SerializeField] private Image iconImage;
		[SerializeField] private Image progressBarFill;

		[Header("Buttons")]
		[SerializeField] private Button actionButton; // Manual click / Pay Debt
		[SerializeField] private Button buyButton;

		private TierController _controller;
		private RectTransform _rectTransform;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();

			// Setup button listeners
			actionButton.onClick.AddListener(OnActionClicked);
			buyButton.onClick.AddListener(OnBuyClicked);
		}

		public void Initialize(TierController controller, TierVisualData visualData)
		{
			_controller = controller;

			// Apply visual data
			if (visualData != null)
			{
				nameText.text = visualData.DisplayName;
				if (visualData.TierIcon != null)
				{
					iconImage.sprite = visualData.TierIcon;
				}
			}
			else
			{
				nameText.text = $"Tier {_controller.StaticData.TierID}";
			}

			// Subscribe to logic events (Decoupling)
			_controller.OnProgressUpdated += UpdateProgressBar;
			_controller.OnDataChanged += RefreshDisplay;

			RefreshDisplay();
		}

		private void OnDestroy()
		{
			if (_controller != null)
			{
				_controller.OnProgressUpdated -= UpdateProgressBar;
				_controller.OnDataChanged -= RefreshDisplay;
			}
		}

		private void UpdateProgressBar(float normalizedProgress)
		{
			// Direct assignment is best for performance when called every frame
			progressBarFill.fillAmount = normalizedProgress;
		}

		private void RefreshDisplay()
		{
			levelText.text = $"Lvl: {_controller.DynamicData.OwnedUnits}";

			// Revenue calculation
			BigDouble baseRev = BigDouble.Parse(_controller.StaticData.Base_Rev);
			BigDouble totalRev = baseRev * _controller.DynamicData.OwnedUnits;
			revenueText.text = $"Rev: ${totalRev.ToCurrencyString()}";

			// Next Cost calculation (Using GameManager/Locator for Economy would be better, 
			// but we can ask the controller's system if needed. For now, we calculate locally or ask Core).
			var economy = Core.CoreManager.Instance.GetService<EconomyManager>();
			if (economy != null)
			{
				BigDouble nextCost = economy.CalculateNextUnitCost(_controller.StaticData.TierID, _controller.DynamicData.OwnedUnits);
				costText.text = $"Buy: ${nextCost.ToCurrencyString()}";
				buyButton.interactable = economy.CurrentBalance >= nextCost;
			}

			// State Management display
			if (_controller.DynamicData.CurrentState == Data.ManagerState.AI)
			{
				stateText.text = "AI RUNNING";
				stateText.color = Color.cyan;
				actionButton.interactable = false;
			}
			else if (_controller.DynamicData.CurrentState == Data.ManagerState.Human)
			{
				if (_controller.IsHumanOnStrike())
				{
					stateText.text = "STRIKE! PAY DEBT";
					stateText.color = Color.red;
					actionButton.interactable = true;
				}
				else
				{
					stateText.text = "HUMAN WORKING";
					stateText.color = Color.yellow;
					actionButton.interactable = false;
				}
			}
			else
			{
				stateText.text = "MANUAL";
				stateText.color = Color.white;
				actionButton.interactable = true;
			}
		}

		private void OnActionClicked()
		{
			// Simple Punch animation on click
			iconImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);

			// Notify logic
			_controller.ManualClick();
		}

		private void OnBuyClicked()
		{
			// Juicy punch effect on the whole panel when buying
			_rectTransform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 10, 1);
			_controller.BuyUnit();
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------