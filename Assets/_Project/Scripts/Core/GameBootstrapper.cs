/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Description:
 * The entry point for the application. Triggers the CoreManager sequence.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-31 - Bussuf Senior Dev - Changed Start to run sequentially to fix Race Conditions.
 * ----------------------------------------------------------------------------
 */

using System.Collections;
using UnityEngine;

namespace AI_Capitalist.Core
{
	public class GameBootstrapper : MonoBehaviour
	{
		private IEnumerator Start()
		{
			// Wait for end of frame to ensure ALL Awakes have finished registering their services
			yield return new WaitForEndOfFrame();

			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.StartGameSequence();
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------