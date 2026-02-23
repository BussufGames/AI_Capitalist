/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-31 - Bussuf Senior Dev - Added dynamic tracking of exact next token threshold.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakInfinity;
using DG.Tweening;
using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;

namespace AI_Capitalist.UI
{
	public class PrestigeUI : MonoBehaviour
	{
		[Header("UI Elements")]
		[SerializeField] private TMP_Text tokensText;
		[SerializeField] private TMP_Text pendingText;
		[SerializeField] private Button ascendButton;

		[Header("Juice")]
		[SerializeField] private Color activeColor = Color.magenta;
		[SerializeField] private Color inactiveColor = Color.gray;

		private PrestigeManager _prestigeManager;
		private EconomyManager _economyManager;
		private RectTransform _rectTransform;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();
			ascendButton.onClick.AddListener(OnAscendClicked);
		}

		private void Start()
		{
			_prestigeManager = CoreManager.Instance.GetService<PrestigeManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged += RefreshUI;
				RefreshUI(_economyManager.CurrentBalance);
			}
		}

		private void OnDestroy()
		{
			if (_economyManager != null)
			{
				_economyManager.OnBalanceChanged -= RefreshUI;
			}
		}

		private void RefreshUI(BigDouble _ = default)
		{
			if (_prestigeManager == null || _economyManager == null) return;

			// Display current stash
			BigDouble currentTokens = _economyManager.PrestigeTokens;
			tokensText.text = $"Tokens: {currentTokens}";

			// Calculate pending and next goal
			BigDouble pending = _prestigeManager.GetPendingTokensToClaim();
			BigDouble nextThreshold = _prestigeManager.GetLifetimeNeededForNextToken();

			if (pending > 0)
			{
				// Shows "+ X (Next at $4.00M)"
				pendingText.text = $"+ {pending} (Next: ${nextThreshold.ToCurrencyString()})";
				pendingText.color = activeColor;
				ascendButton.interactable = true;
			}
			else
			{
				// Accurately tells the player exactly how much money they need to hit the next token!
				pendingText.text = $"Need ${nextThreshold.ToCurrencyString()} Lifetime";
				pendingText.color = inactiveColor;
				ascendButton.interactable = false;
			}
		}

		private void OnAscendClicked()
		{
			_rectTransform.DOComplete();
			_rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);

			if (_prestigeManager != null)
			{
				ascendButton.interactable = false;
				_prestigeManager.ClaimPrestigeAndReset();
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------