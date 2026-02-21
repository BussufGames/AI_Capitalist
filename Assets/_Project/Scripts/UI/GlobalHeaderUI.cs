/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Manages the global UI header (Total Balance, Global Buy Multiplier).
 * Listens directly to the EconomyManager.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Economy;
using BreakInfinity;
using BussufGames.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AI_Capitalist.UI
{
	public class GlobalHeaderUI : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private TMP_Text balanceText;
		[SerializeField] private Button toggleBuyModeButton;
		[SerializeField] private TMP_Text buyModeText;

		private EconomyManager _economyManager;

		private void Start()
		{
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_economyManager != null)
			{
				// Subscribe to events
				_economyManager.OnBalanceChanged += UpdateBalanceDisplay;
				_economyManager.OnBuyModeChanged += UpdateBuyModeDisplay;

				// Setup Button
				toggleBuyModeButton.onClick.AddListener(() => _economyManager.ToggleBuyMode());

				// Initial Display
				UpdateBalanceDisplay(_economyManager.CurrentBalance);
				UpdateBuyModeDisplay();
			}
			else
			{
				this.LogError("GlobalHeaderUI could not find EconomyManager!");
			}
		}

		private void OnDestroy()
		{
			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged -= UpdateBalanceDisplay;
				_economyManager.OnBuyModeChanged -= UpdateBuyModeDisplay;
			}
		}

		private void UpdateBalanceDisplay(BigDouble newBalance)
		{
			balanceText.text = $"${newBalance.ToCurrencyString()}";
		}

		private void UpdateBuyModeDisplay()
		{
			buyModeText.text = _economyManager.CurrentBuyMode == BuyMode.Max ? "MAX" : $"x{(int)_economyManager.CurrentBuyMode}";
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------