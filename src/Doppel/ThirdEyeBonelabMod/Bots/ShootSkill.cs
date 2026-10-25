using System;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class ShootSkill
{
	public const int Within = 0;

	public const int Size = 1;

	public const int Gap = 2;

	public const int Burst = 3;

	private static readonly Practice core = new Practice("shoot skill", "shootskill.txt", new string[4] { "within", "size", "gap", "burst" }, new float[4] { 4f, 0.3f, 1f, 1f }, new float[4] { 2.5f, 0.22f, 0.6f, 0.5f }, new float[4] { 7f, 0.6f, 2.5f, 2f }, 0.05f, 0.35f);

	public const int Window = 15;

	public static void Reset()
	{
		core.Reset();
	}

	public static string Said()
	{
		return core.Said();
	}

	public static float[] Begin(IntPtr who, out bool trying)
	{
		return core.Begin(who, out trying);
	}

	public static void End(IntPtr who, bool trying, int shots, int hits, float seconds)
	{
		if (shots >= 5 && !(seconds < 0.5f))
		{
			float f = (float)hits / (float)shots;
			float score = (float)hits / seconds * Mathf.Sqrt(f);
			core.End(who, trying, score, hits + " of " + shots + " shots on it in " + seconds.ToString("0.0") + " s of shooting: " + score.ToString("0.00"));
		}
	}
}
