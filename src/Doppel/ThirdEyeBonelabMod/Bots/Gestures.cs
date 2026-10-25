using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Gestures
{
	private class Side
	{
		public Vector3 last;

		public bool has;

		public float way;

		public readonly List<float> turns = new List<float>();
	}

	public static float WavedAt = -100f;

	public static Vector3 WaveFacing = Vector3.forward;

	private static readonly Side left = new Side();

	private static readonly Side right = new Side();

	public static void Update()
	{
		Transform head = Player.Head;
		if (!(head == null))
		{
			Watch(left, Player.LeftHand, head);
			Watch(right, Player.RightHand, head);
		}
	}

	private static void Watch(Side side, Hand hand, Transform head)
	{
		if (hand == null || Time.deltaTime <= 0f)
		{
			side.has = false;
			return;
		}
		Vector3 last = head.InverseTransformPoint(hand.transform.position);
		Vector3 last2 = side.last;
		side.last = last;
		if (!side.has)
		{
			side.has = true;
			return;
		}
		if (!(last.y > Tuning.F("wave.see.height", -0.15f)) || !(last.z > -0.1f) || !(hand.m_CurrentAttachedGO == null))
		{
			side.turns.Clear();
			side.way = 0f;
			return;
		}
		float f = (last.x - last2.x) / Time.deltaTime;
		if (Mathf.Abs(f) < Tuning.F("wave.see.speed", 0.5f))
		{
			return;
		}
		float num = Mathf.Sign(f);
		if (num != side.way)
		{
			side.way = num;
			side.turns.Add(Time.time);
			while (side.turns.Count > 0 && Time.time - side.turns[0] > 1.6f)
			{
				side.turns.RemoveAt(0);
			}
			if (side.turns.Count >= 3 && Time.time - WavedAt > 2.5f)
			{
				WavedAt = Time.time;
				WaveFacing = head.forward;
				side.turns.Clear();
				Lab.Event("you waved");
			}
		}
	}
}
