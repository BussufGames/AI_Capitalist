/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * The Welcome Back popup. Displays the amount of money generated offline.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Economy;

namespace AI_Capitalist.UI
{
	public class OfflinePopupUI : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private TMP_Text timeAwayText;
		[SerializeField] private TMP_Text earningsText;
		[SerializeField] private Button claimButton;
		[SerializeField] private GameObject contentPanel;

		private void Awake()
		{
			claimButton.onClick.AddListener(OnClaimClicked);
		}

		public void OpenPopup(TimeSpan timeAway, BigDouble earnings)
		{
			timeAwayText.text = $"While you were gone for {(int)timeAway.TotalHours}h {timeAway.Minutes}m...";
			earningsText.text = $"<color=#4CAF50>+${earnings.ToCurrencyString()}</color>";

			gameObject.SetActive(true);

			// Juice: Pop-in animation
			contentPanel.transform.localScale = Vector3.zero;
			contentPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

			// Juice: Emphasize the money
			earningsText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1).SetDelay(0.3f);
		}

		private void OnClaimClicked()
		{
			claimButton.interactable = false; // Prevent double clicks

			// Tell the manager we saw it
			var offlineMgr = CoreManager.Instance.GetService<OfflineProgressManager>();
			if (offlineMgr != null)
			{
				offlineMgr.AcknowledgeOfflineProgress();
			}

			// Juice: Pop-out and destroy
			contentPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
			{
				Destroy(gameObject);
			});
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------