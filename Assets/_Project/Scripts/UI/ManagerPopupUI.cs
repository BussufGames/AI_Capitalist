/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * A single reusable popup that displays Human/AI hiring options.
 * Calculates total AI cost (including active human debt).
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Restored original beautiful UI logic (DOTween + Status Colors).
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;
using BreakInfinity;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AI_Capitalist.UI
{
	public class ManagerPopupUI : MonoBehaviour
	{
		#region UI References
		[Header("Global Popup Elements")]
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private Button closeButton;
		[SerializeField] private GameObject contentPanel;

		[Header("Human Section (Top)")]
		[SerializeField] private TMP_Text humanCostText;
		[SerializeField] private TMP_Text humanStatusText;
		[SerializeField] private Button hireHumanButton;

		[Header("AI Section (Bottom)")]
		[SerializeField] private TMP_Text aiBaseCostText;
		[SerializeField] private TMP_Text aiDebtText;
		[SerializeField] private TMP_Text aiTotalCostText;
		[SerializeField] private TMP_Text aiStatusText;
		[SerializeField] private Button hireAIButton;
		#endregion

		private TierController _activeController;
		private EconomyManager _economyManager;

		private void Awake()
		{
			closeButton.onClick.AddListener(ClosePopup);
			hireHumanButton.onClick.AddListener(OnHireHumanClicked);
			hireAIButton.onClick.AddListener(OnHireAIClicked);
		}

		public void OpenPopup(TierController controller, TierVisualData visualData)
		{
			_activeController = controller;
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			if (_economyManager == null || _activeController == null) return;

			// Use the ScriptableObject name if available, otherwise fallback
			string displayName = visualData != null ? visualData.DisplayName : controller.StaticData.BusinessName;
			titleText.text = $"Manage: {displayName}";

			_activeController.OnDataChanged += RefreshUI;
			_economyManager.OnBalanceChanged += OnBalanceChangedHandler;

			gameObject.SetActive(true);
			RefreshUI();

			contentPanel.transform.localScale = Vector3.zero;
			contentPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
		}

		public void ClosePopup()
		{
			if (_activeController != null)
			{
				_activeController.OnDataChanged -= RefreshUI;
			}
			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged -= OnBalanceChangedHandler;
			}

			contentPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
			{
				gameObject.SetActive(false);
				_activeController = null;
			});
		}

		private void OnBalanceChangedHandler(BigDouble newBalance)
		{
			if (gameObject.activeSelf) RefreshUI();
		}

		private void RefreshUI()
		{
			if (_activeController == null || _economyManager == null) return;

			BigDouble balance = _economyManager.CurrentBalance;
			var data = _activeController.DynamicData;
			var staticData = _activeController.StaticData;

			// --- HUMAN SECTION ---
			BigDouble humanCost = BigDouble.Parse(staticData.Human_Hire_Cost);
			humanCostText.text = $"Cost: ${humanCost.ToCurrencyString()}";

			if (data.CurrentState == Data.ManagerState.Human)
			{
				humanStatusText.text = "ALREADY EMPLOYED";
				humanStatusText.color = Color.green;
				hireHumanButton.interactable = false;
			}
			else if (data.CurrentState == Data.ManagerState.AI)
			{
				humanStatusText.text = "REPLACED BY AI";
				humanStatusText.color = Color.gray;
				hireHumanButton.interactable = false;
			}
			else
			{
				humanStatusText.text = "AVAILABLE";
				humanStatusText.color = Color.white;
				hireHumanButton.interactable = balance >= humanCost;
			}

			// --- AI SECTION ---
			BigDouble aiBaseCost = BigDouble.Parse(staticData.AI_Hire_Cost);
			BigDouble currentDebt = BigDouble.Parse(data.AccumulatedDebt);
			BigDouble aiTotalCost = aiBaseCost + currentDebt;

			aiBaseCostText.text = $"Hardware Cost: ${aiBaseCost.ToCurrencyString()}";

			if (currentDebt > 0)
			{
				aiDebtText.gameObject.SetActive(true);
				aiDebtText.text = $"Human Severance (Debt): ${currentDebt.ToCurrencyString()}";
			}
			else
			{
				aiDebtText.gameObject.SetActive(false);
			}

			aiTotalCostText.text = $"Total Deployment: ${aiTotalCost.ToCurrencyString()}";

			if (data.CurrentState == Data.ManagerState.AI)
			{
				aiStatusText.text = "AI ONLINE";
				aiStatusText.color = Color.cyan;
				hireAIButton.interactable = false;
			}
			else
			{
				aiStatusText.text = "UPGRADE TO AI";
				aiStatusText.color = Color.white;
				hireAIButton.interactable = balance >= aiTotalCost;
			}
		}

		private void OnHireHumanClicked()
		{
			if (_activeController != null) _activeController.HireHumanManager();
		}

		private void OnHireAIClicked()
		{
			if (_activeController != null) _activeController.HireAIManager();
		}
	}
}