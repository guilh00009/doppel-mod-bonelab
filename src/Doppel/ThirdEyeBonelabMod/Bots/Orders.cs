using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Orders
{
	private static float togetherAt = -10f;

	private static float breakAt = -10f;

	private static float readyUntil = -10f;

	private static float giveSince = -1f;

	private static float pointSince = -1f;

	private static float saidAt = -10f;

	private static Bot ready;

	private static Bot giveTo;

	private static Transform pointed;

	private static IntPtr giveThing;

	public static void Step()
	{
		if (Tuning.F("orders", 0f) < 0.5f)
		{
			return;
		}
		try
		{
			Transform head = Player.Head;
			Hand leftHand = Player.LeftHand;
			Hand rightHand = Player.RightHand;
			if (!(head == null) && !(leftHand == null) && !(rightHand == null))
			{
				Give(head, leftHand, rightHand);
				Break(head, leftHand, rightHand);
				Point(head, leftHand, rightHand);
			}
		}
		catch (Exception ex)
		{
			if (Time.time - saidAt > 5f)
			{
				saidAt = Time.time;
				Lab.Event("orders: " + ex.Message);
			}
		}
	}

	private static void Break(Transform head, Hand left, Hand right)
	{
		if (left.m_CurrentAttachedGO != null || right.m_CurrentAttachedGO != null)
		{
			return;
		}
		Vector3 position = left.transform.position;
		Vector3 position2 = right.transform.position;
		float magnitude = (position - position2).magnitude;
		Vector3 lhs = (position + position2) * 0.5f - head.position;
		bool flag = Vector3.Dot(lhs, Flat(head.forward).normalized) > 0.1f && lhs.magnitude < 0.8f;
		if (magnitude < Tuning.F("orders.together", 0.22f) && flag)
		{
			togetherAt = Time.time;
		}
		if (Time.time - togetherAt > Tuning.F("orders.break.within", 0.6f) || Time.time - breakAt < 1.5f)
		{
			return;
		}
		float num = ((left.rb != null) ? left.rb.velocity.magnitude : 0f);
		float num2 = ((right.rb != null) ? right.rb.velocity.magnitude : 0f);
		float num3 = Tuning.F("orders.break.speed", 1.8f);
		if (magnitude > Tuning.F("orders.apart", 0.6f) && num > num3 && num2 > num3)
		{
			breakAt = Time.time;
			togetherAt = -10f;
			Bot bot = Looked(head);
			Lab.Event("orders: break gesture (hands " + magnitude.ToString("0.00") + " m apart at " + num.ToString("0.0") + "/" + num2.ToString("0.0") + " m/s)" + ((bot != null) ? (": " + bot.Name + " is ready, point at what it is to go for") : ": no bot of yours to call"));
			if (bot != null)
			{
				ready = bot;
				readyUntil = Time.time + Tuning.F("orders.ready", 6f);
				pointSince = -1f;
				pointed = null;
				bot.Ready();
				Spawnables.Notify(bot.Name + " is ready: point at the target");
			}
		}
	}

	private static Bot Looked(Transform head)
	{
		Bot bot = null;
		float num = Tuning.F("orders.look.cone", 35f);
		float num2 = 20f;
		Bot bot2 = null;
		foreach (Bot item in BotManager.All)
		{
			Transform head2 = item.Head;
			if (!item.enemy && !item.dead && !item.Puppet && !(head2 == null))
			{
				Vector3 to = head2.position - head.position;
				float num3 = Vector3.Angle(head.forward, to);
				if (to.magnitude < 45f && num3 < num)
				{
					num = num3;
					bot = item;
				}
				if (to.magnitude < num2)
				{
					num2 = to.magnitude;
					bot2 = item;
				}
			}
		}
		return bot ?? bot2;
	}

	private static void Point(Transform head, Hand left, Hand right)
	{
		if (ready == null)
		{
			return;
		}
		if (Time.time > readyUntil || ready.dead)
		{
			Lab.Event("orders: " + ready.Name + " stood down (nothing pointed at)");
			ready = null;
			return;
		}
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (controllerRig == null)
		{
			return;
		}
		Transform transform = null;
		Vector3 vector = Vector3.zero;
		(Hand, BaseController)[] array = new(Hand, BaseController)[2]
		{
			(left, controllerRig.leftController),
			(right, controllerRig.rightController)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (hand, baseController) = array[i];
			if (!(hand == null) && !(baseController == null) && !(hand.m_CurrentAttachedGO != null) && !(baseController._solvedGrip < Tuning.F("orders.point.grip", 0.6f)) && !(baseController._primaryAxis > Tuning.F("orders.point.trigger", 0.3f)))
			{
				Vector3 position = hand.transform.position;
				Vector3 forward = baseController.transform.forward;
				if (Pick(position, forward, out var found, out var point))
				{
					transform = found;
					vector = point;
				}
			}
		}
		if (transform == null)
		{
			pointSince = -1f;
			pointed = null;
		}
		else if (transform != pointed)
		{
			pointed = transform;
			pointSince = Time.time;
		}
		else if (!(Time.time - pointSince < Tuning.F("orders.point.hold", 0.35f)))
		{
			Bot bot = ready;
			ready = null;
			bot.Ordered(transform);
			Lab.Event("orders: " + bot.Name + " sent after " + transform.name + " (" + (vector - head.position).magnitude.ToString("0") + " m)");
			Spawnables.Notify(bot.Name + ": going for it");
		}
	}

	private static bool Pick(Vector3 from, Vector3 dir, out Transform found, out Vector3 point)
	{
		found = null;
		point = Vector3.zero;
		float num = Tuning.F("orders.point.cone", 10f);
		List<(Transform, Vector3)> list = new List<(Transform, Vector3)>();
		foreach (Targets.Target item3 in Targets.Hostiles())
		{
			if (item3.transform != null)
			{
				list.Add((item3.transform, item3.Point));
			}
		}
		foreach (Bot item4 in BotManager.All)
		{
			if (item4 != ready && !item4.dead && item4.Head != null)
			{
				list.Add((item4.rig.transform, item4.Head.position - Vector3.up * 0.3f));
			}
		}
		foreach (var item5 in list)
		{
			Transform item = item5.Item1;
			Vector3 item2 = item5.Item2;
			Vector3 to = item2 - from;
			if (!(to.magnitude > 90f))
			{
				float num2 = Vector3.Angle(dir, to);
				if (num2 < num)
				{
					num = num2;
					found = item;
					point = item2;
				}
			}
		}
		return found != null;
	}

	private static void Give(Transform head, Hand left, Hand right)
	{
		Bot bot = null;
		Transform transform = null;
		Hand[] array = new Hand[2] { left, right };
		foreach (Hand hand in array)
		{
			GameObject currentAttachedGO = hand.m_CurrentAttachedGO;
			if (currentAttachedGO == null)
			{
				continue;
			}
			MarrowEntity componentInParent = currentAttachedGO.GetComponentInParent<MarrowEntity>();
			Transform transform2 = ((componentInParent != null) ? componentInParent.transform : currentAttachedGO.transform.root);
			if (transform2.GetComponentInChildren<Gun>() == null && !Bot.IsWeapon(transform2))
			{
				continue;
			}
			foreach (Bot item in BotManager.All)
			{
				if (!item.enemy && !item.dead && !item.Puppet && !item.Riding && !item.InLesson)
				{
					Transform chest = item.Chest;
					if (chest != null && (hand.transform.position - chest.position).magnitude < Tuning.F("orders.give.near", 0.6f))
					{
						bot = item;
						transform = transform2;
					}
				}
			}
		}
		if (bot == null)
		{
			giveSince = -1f;
			giveTo = null;
		}
		else if (bot != giveTo || transform.Pointer != giveThing)
		{
			giveTo = bot;
			giveThing = transform.Pointer;
			giveSince = Time.time;
		}
		else if (!(Time.time - giveSince < Tuning.F("orders.give.hold", 0.6f)))
		{
			giveSince = Time.time + 2f;
			bot.TakeFromYou(transform);
		}
	}

	private static Vector3 Flat(Vector3 v)
	{
		return new Vector3(v.x, 0f, v.z);
	}
}
