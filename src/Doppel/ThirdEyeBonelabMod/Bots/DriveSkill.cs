using System;

namespace ThirdEyeBonelabMod.Bots;

public static class DriveSkill
{
	public const int Pace = 0;

	public const int Side = 1;

	public const int Brake = 2;

	public const int Steer = 3;

	public const int Look = 4;

	public const int Grip = 5;

	public const int Early = 6;

	public static bool Practice;

	private static readonly Practice core = new Practice("drive skill", "driveskill.txt", new string[7] { "pace", "side", "brake", "steer", "look", "grip", "early" }, new float[7] { 0.8f, 0.8f, 3f, 1.3f, 0.7f, 0.9f, 1f }, new float[7] { 0.45f, 0.4f, 1.5f, 0.7f, 0.35f, 0.5f, 0.4f }, new float[7] { 1.05f, 1.6f, 7f, 2.4f, 1.4f, 1.6f, 2.2f }, 0.12f, 0.8f);

	public static void Reset()
	{
		core.Reset();
	}

	public static string Said()
	{
		return core.Said() + (Practice ? "; practising" : "");
	}

	public static float[] Begin(IntPtr who, out bool trying)
	{
		return core.Begin(who, out trying);
	}

	public static void End(IntPtr who, bool trying, float covered, float seconds, int backs, int abouts, bool failed)
	{
		if (!(seconds < 3f))
		{
			float score = covered / (seconds + 4f * (float)backs + 2f * (float)abouts + (failed ? 15f : 0f));
			core.End(who, trying, score, covered.ToString("0") + " m in " + seconds.ToString("0") + " s, backed out " + backs + ", backed round " + abouts + (failed ? ", gave up" : "") + ": " + score.ToString("0.00") + " m/s");
		}
	}
}
