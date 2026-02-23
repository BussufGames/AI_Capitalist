/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Description:
 * Handles Unity Gaming Services initialization, Authentication, and Cloud Save.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-31 - Bussuf Senior Dev - Added check to prevent 'Already Signed In' error on soft reboots.
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
		public bool IsAuthenticated { get; private set; }
		public string PlayerID { get; private set; }

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<UGSManager>(this);
			}
		}

		public async void Initialize()
		{
			this.Log("Initializing Unity Gaming Services...");
			try
			{
				await UnityServices.InitializeAsync();
				SignInAnonymouslyAsync();
			}
			catch (Exception e)
			{
				this.LogError($"Failed to initialize UGS: {e.Message}");
			}
		}

		private async void SignInAnonymouslyAsync()
		{
			try
			{
				// FIX: Check if we are ALREADY signed in (happens when reloading Scene 0 in the Editor)
				if (AuthenticationService.Instance.IsSignedIn)
				{
					PlayerID = AuthenticationService.Instance.PlayerId;
					IsAuthenticated = true;
					this.LogSuccess($"Already signed in automatically. PlayerID: {PlayerID}");
					return;
				}

				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				PlayerID = AuthenticationService.Instance.PlayerId;
				IsAuthenticated = true;
				this.LogSuccess($"Signed in anonymously. PlayerID: {PlayerID}");
			}
			catch (AuthenticationException ex)
			{
				this.LogError($"Sign in failed: {ex.Message}");
			}
			catch (RequestFailedException ex)
			{
				this.LogError($"Sign in failed: {ex.Message}");
			}
		}

		public async Task<string> LoadCloudDataAsync(string key)
		{
			if (!IsAuthenticated) return string.Empty;

			try
			{
				var query = new HashSet<string> { key };
				var data = await CloudSaveService.Instance.Data.Player.LoadAsync(query);

				if (data.TryGetValue(key, out var item))
				{
					return item.Value.GetAsString();
				}
			}
			catch (Exception e)
			{
				this.LogError($"Error loading from cloud: {e.Message}");
			}

			return string.Empty;
		}

		public async Task SaveCloudDataAsync(string key, string jsonValue)
		{
			if (!IsAuthenticated) return;

			try
			{
				var data = new Dictionary<string, object> { { key, jsonValue } };
				await CloudSaveService.Instance.Data.Player.SaveAsync(data);
				this.LogSuccess("Data successfully synced to UGS Cloud.");
			}
			catch (Exception e)
			{
				this.LogError($"Error saving to cloud: {e.Message}");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------