/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Handles Unity Gaming Services (UGS) initialization, Anonymous Auth, 
 * and Cloud Save push/pull operations.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using BussufGames.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace AI_Capitalist.Services
{
	public class UGSManager : MonoBehaviour, Core.IService
	{
		public bool IsAuthenticated => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

		private void Awake()
		{
			// Register this service to the Locator
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<UGSManager>(this);
			}
		}

		public async void Initialize()
		{
			try
			{
				this.Log("Initializing Unity Gaming Services...");
				await UnityServices.InitializeAsync();
				await SignInAnonymouslyAsync();
			}
			catch (Exception e)
			{
				this.LogError($"UGS Initialization Failed (Offline Mode?): {e.Message}");
			}
		}

		private async Task SignInAnonymouslyAsync()
		{
			try
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				this.LogSuccess($"Signed in anonymously. PlayerID: {AuthenticationService.Instance.PlayerId}");
			}
			catch (Exception e)
			{
				this.LogError($"Sign in failed: {e.Message}");
			}
		}

		public async Task<string> LoadCloudDataAsync(string key)
		{
			if (!IsAuthenticated) return null;
			try
			{
				var query = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
				if (query.TryGetValue(key, out var item))
				{
					return item.Value.GetAsString();
				}
			}
			catch (Exception e)
			{
				this.LogError($"Cloud Load Failed: {e.Message}");
			}
			return null;
		}

		public async Task SaveCloudDataAsync(string key, string json)
		{
			if (!IsAuthenticated) return;
			try
			{
				var data = new Dictionary<string, object> { { key, json } };
				await CloudSaveService.Instance.Data.Player.SaveAsync(data);
				this.LogSuccess("Data successfully synced to UGS Cloud.");
			}
			catch (Exception e)
			{
				this.LogError($"Cloud Save Failed: {e.Message}");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------