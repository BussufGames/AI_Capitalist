/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-30
 * ----------------------------------------------------------------------------
 * Description:
 * The UI Panel that sits at the bottom of the scroll view, waiting for the 
 * player to buy the next available business tier.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using DG.Tweening;
using AI_Capitalist.Core;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;

namespace AI_Capitalist.UI
{
	public class UnlockPanelUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text costText;
		[SerializeField] private Button unlockButton;

		private BigDouble _unlockCost;
		private EconomyManager _economyManager;
		private RectTransform _rectTransform;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();
			unlockButton.onClick.AddListener(OnUnlockClicked);
		}

		public void Initialize(TierStaticData nextConfig)
		{
			_unlockCost = BigDouble.Parse(nextConfig.Unlock_Cost);

			titleText.text = $"Unlock: {nextConfig.BusinessName}";
			costText.text = $"${_unlockCost.ToCurrencyString()}";

			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged += CheckBalance;
				CheckBalance(_economyManager.CurrentBalance);
			}
		}

		private void OnDestroy()
		{
			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged -= CheckBalance;
			}
		}

		private void CheckBalance(BigDouble currentBalance)
		{
			unlockButton.interactable = currentBalance >= _unlockCost;
		}

		private void OnUnlockClicked()
		{
			_rectTransform.DOComplete();
			_rectTransform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 10, 1);

			var tierManager = CoreManager.Instance.GetService<TierManager>();
			if (tierManager != null)
			{
				tierManager.TryUnlockNextTier();
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------