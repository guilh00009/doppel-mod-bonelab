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
		if (!((Object)(object)head == (Object)null))
		{
			Watch(left, Player.LeftHand, head);
			Watch(right, Player.RightHand, head);
		}
	}

	private static void Watch(Side side, Hand hand, Transform head)
	{
		if ((Object)(object)hand == (Object)null || Time.deltaTime <= 0f)
		{
			side.has = false;
			return;
		}
		Vector3 val = head.InverseTransformPoint(((Component)hand).transform.position);
		Vector3 last = side.last;
		side.last = val;
		if (!side.has)
		{
			side.has = true;
			return;
		}
		if (!(val.y > Tuning.F("wave.see.height", -0.15f)) || !(val.z > -0.1f) || !((Object)(object)hand.m_CurrentAttachedGO == (Object)null))
		{
			side.turns.Clear();
			side.way = 0f;
			return;
		}
		float num = (val.x - last.x) / Time.deltaTime;
		if (Mathf.Abs(num) < Tuning.F("wave.see.speed", 0.5f))
		{
			return;
		}
		float num2 = Mathf.Sign(num);
		if (num2 != side.way)
		{
			side.way = num2;
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
