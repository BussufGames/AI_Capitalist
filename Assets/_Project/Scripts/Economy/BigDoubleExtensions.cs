/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Provides formatting extensions for BreakInfinity.BigDouble to convert 
 * massive numbers into UI-friendly strings (e.g., 1.5M, 2.3B, 4.1AA).
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using BreakInfinity;
using System;

namespace AI_Capitalist.Economy
{
	public static class BigDoubleExtensions
	{
		private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "AA", "AB", "AC", "AD", "AE", "AF" };

		/// <summary>
		/// Formats a BigDouble into a suffix-based string (e.g., 1.50M).
		/// </summary>
		public static string ToCurrencyString(this BigDouble value)
		{
			if (value < 1000)
			{
				return Math.Floor(value.ToDouble()).ToString("F0");
			}

			int exponent = (int)Math.Floor(BigDouble.Log10(value));
			int suffixIndex = exponent / 3;

			if (suffixIndex >= Suffixes.Length)
			{
				// Fallback to scientific notation if we run out of letters
				return value.ToString("0.00e0");
			}

			BigDouble mantissa = value / BigDouble.Pow(10, suffixIndex * 3);
			return mantissa.ToDouble().ToString("F2") + Suffixes[suffixIndex];
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------