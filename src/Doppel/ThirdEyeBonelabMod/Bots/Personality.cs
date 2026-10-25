using System;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public class Personality
{
	public const string Mark = "BotSeed_";

	public readonly int seed;

	public readonly float favourites;

	public readonly float mirror;

	public readonly float tempo;

	public readonly float pace;

	public readonly float posture;

	public readonly float reaction;

	public readonly float distance;

	public readonly float burst;

	public readonly float aim;

	public readonly float energy;

	public readonly float curiosity;

	public readonly float shooter;

	public readonly float calm;

	public const float Drift = 150f;

	public Personality(int seed)
	{
		this.seed = seed;
		System.Random r = new System.Random(seed);
		favourites = R(0.3f, 0.6f);
		mirror = ((r.NextDouble() < 0.25) ? R(0.7f, 0.9f) : R(0.1f, 0.4f));
		tempo = R(0.9f, 1.1f);
		pace = R(0.85f, 1.12f);
		posture = R(-0.06f, 0f);
		reaction = R(0.7f, 1.4f);
		distance = R(0.75f, 1.3f);
		burst = R(0.6f, 1.5f);
		energy = R(0.4f, 1.6f);
		curiosity = R(0.3f, 1.6f);
		shooter = R(0.2f, 1.6f);
		calm = R(0.5f, 1.8f);
		aim = R(0.15f, 1f);
		float R(float a, float b)
		{
			return a + (float)r.NextDouble() * (b - a);
		}
	}

	public static Personality Of(RigManager rig)
	{
		Transform transform = rig.transform;
		for (int i = 0; i < transform.childCount; i++)
		{
			string name = transform.GetChild(i).name;
			if (name.StartsWith("BotSeed_", StringComparison.Ordinal) && int.TryParse(name.Substring("BotSeed_".Length), out var result))
			{
				return new Personality(result);
			}
		}
		int num = UnityEngine.Random.Range(1, int.MaxValue);
		new GameObject("BotSeed_" + num).transform.SetParent(transform, worldPositionStays: false);
		return new Personality(num);
	}

	public float Likes(string move)
	{
		float num = Time.time / 150f + (float)(seed % 1000) / 1000f;
		int num2 = Mathf.FloorToInt(num);
		float t = Mathf.SmoothStep(0f, 1f, num - (float)num2);
		return Mathf.Lerp(Taste(move, num2), Taste(move, num2 + 1), t);
	}

	private float Taste(string move, int era)
	{
		uint num = 2166136261u;
		foreach (char c in move)
		{
			num = (num ^ c) * 16777619;
		}
		num = (num ^ (uint)seed) * 16777619;
		num = (num ^ (uint)era) * 16777619;
		num ^= num >> 15;
		num *= 2246822519u;
		num ^= num >> 13;
		if (!((float)num / 4.2949673E+09f < favourites))
		{
			return 0.4f;
		}
		return 3f;
	}

	public string Describe()
	{
		return ((mirror > 0.5f) ? "left-handed" : "right-handed") + ((pace > 1.04f) ? ", quick on its feet" : ((pace < 0.93f) ? ", unhurried" : "")) + ((reaction < 0.9f) ? ", fast trigger" : ((reaction > 1.2f) ? ", careful shot" : "")) + ((distance < 0.9f) ? ", fights up close" : ((distance > 1.15f) ? ", keeps its distance" : "")) + ((burst > 1.2f) ? ", long bursts" : ((burst < 0.8f) ? ", short bursts" : "")) + ((energy > 1.2f) ? ", restless" : ((calm > 1.4f) ? ", laid-back" : "")) + ((curiosity > 1.2f) ? ", a climber" : "") + ((shooter > 1.2f) ? ", trigger-happy" : "") + ((aim > 0.8f) ? ", a sharp shot" : ((aim < 0.4f) ? ", a poor shot" : ""));
	}
}
