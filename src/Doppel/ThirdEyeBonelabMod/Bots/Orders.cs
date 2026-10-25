using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
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
			if (!((Object)(object)head == (Object)null) && !((Object)(object)leftHand == (Object)null) && !((Object)(object)rightHand == (Object)null))
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
		if ((Object)(object)left.m_CurrentAttachedGO != (Object)null || (Object)(object)right.m_CurrentAttachedGO != (Object)null)
		{
			return;
		}
		Vector3 position = ((Component)left).transform.position;
		Vector3 position2 = ((Component)right).transform.position;
		Vector3 val = position - position2;
		float magnitude = ((Vector3)(ref val)).magnitude;
		Vector3 val2 = (position + position2) * 0.5f - head.position;
		Vector3 val3 = val2;
		val = Flat(head.forward);
		bool flag = Vector3.Dot(val3, ((Vector3)(ref val)).normalized) > 0.1f && ((Vector3)(ref val2)).magnitude < 0.8f;
		if (magnitude < Tuning.F("orders.together", 0.22f) && flag)
		{
			togetherAt = Time.time;
		}
		if (Time.time - togetherAt > Tuning.F("orders.break.within", 0.6f) || Time.time - breakAt < 1.5f)
		{
			return;
		}
		float num;
		if (!((Object)(object)left.rb != (Object)null))
		{
			num = 0f;
		}
		else
		{
			val = left.rb.velocity;
			num = ((Vector3)(ref val)).magnitude;
		}
		float num2 = num;
		float num3;
		if (!((Object)(object)right.rb != (Object)null))
		{
			num3 = 0f;
		}
		else
		{
			val = right.rb.velocity;
			num3 = ((Vector3)(ref val)).magnitude;
		}
		float num4 = num3;
		float num5 = Tuning.F("orders.break.speed", 1.8f);
		if (magnitude > Tuning.F("orders.apart", 0.6f) && num2 > num5 && num4 > num5)
		{
			breakAt = Time.time;
			togetherAt = -10f;
			Bot bot = Looked(head);
			Lab.Event("orders: break gesture (hands " + magnitude.ToString("0.00") + " m apart at " + num2.ToString("0.0") + "/" + num4.ToString("0.0") + " m/s)" + ((bot != null) ? (": " + bot.Name + " is ready, point at what it is to go for") : ": no bot of yours to call"));
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
			if (!item.enemy && !item.dead && !item.Puppet && !((Object)(object)head2 == (Object)null))
			{
				Vector3 val = head2.position - head.position;
				float num3 = Vector3.Angle(head.forward, val);
				if (((Vector3)(ref val)).magnitude < 45f && num3 < num)
				{
					num = num3;
					bot = item;
				}
				if (((Vector3)(ref val)).magnitude < num2)
				{
					num2 = ((Vector3)(ref val)).magnitude;
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
		if ((Object)(object)controllerRig == (Object)null)
		{
			return;
		}
		Transform val = null;
		Vector3 val2 = Vector3.zero;
		(Hand, BaseController)[] array = new(Hand, BaseController)[2]
		{
			(left, ((ControllerRig)controllerRig).leftController),
			(right, ((ControllerRig)controllerRig).rightController)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (val3, val4) = array[i];
			if (!((Object)(object)val3 == (Object)null) && !((Object)(object)val4 == (Object)null) && !((Object)(object)val3.m_CurrentAttachedGO != (Object)null) && !(val4._solvedGrip < Tuning.F("orders.point.grip", 0.6f)) && !(val4._primaryAxis > Tuning.F("orders.point.trigger", 0.3f)))
			{
				Vector3 position = ((Component)val3).transform.position;
				Vector3 forward = ((Component)val4).transform.forward;
				if (Pick(position, forward, out var found, out var point))
				{
					val = found;
					val2 = point;
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			pointSince = -1f;
			pointed = null;
		}
		else if ((Object)(object)val != (Object)(object)pointed)
		{
			pointed = val;
			pointSince = Time.time;
		}
		else if (!(Time.time - pointSince < Tuning.F("orders.point.hold", 0.35f)))
		{
			Bot bot = ready;
			ready = null;
			bot.Ordered(val);
			string[] obj = new string[7]
			{
				"orders: ",
				bot.Name,
				" sent after ",
				((Object)val).name,
				" (",
				null,
				null
			};
			Vector3 val5 = val2 - head.position;
			obj[5] = ((Vector3)(ref val5)).magnitude.ToString("0");
			obj[6] = " m)";
			Lab.Event(string.Concat(obj));
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
			if ((Object)(object)item3.transform != (Object)null)
			{
				list.Add((item3.transform, item3.Point));
			}
		}
		foreach (Bot item4 in BotManager.All)
		{
			if (item4 != ready && !item4.dead && (Object)(object)item4.Head != (Object)null)
			{
				list.Add((((Component)item4.rig).transform, item4.Head.position - Vector3.up * 0.3f));
			}
		}
		foreach (var item5 in list)
		{
			Transform item = item5.Item1;
			Vector3 item2 = item5.Item2;
			Vector3 val = item2 - from;
			if (!(((Vector3)(ref val)).magnitude > 90f))
			{
				float num2 = Vector3.Angle(dir, val);
				if (num2 < num)
				{
					num = num2;
					found = item;
					point = item2;
				}
			}
		}
		return (Object)(object)found != (Object)null;
	}

	private static void Give(Transform head, Hand left, Hand right)
	{
		Bot bot = null;
		Transform val = null;
		Hand[] array = (Hand[])(object)new Hand[2] { left, right };
		foreach (Hand val2 in array)
		{
			GameObject currentAttachedGO = val2.m_CurrentAttachedGO;
			if ((Object)(object)currentAttachedGO == (Object)null)
			{
				continue;
			}
			MarrowEntity componentInParent = currentAttachedGO.GetComponentInParent<MarrowEntity>();
			Transform val3 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : currentAttachedGO.transform.root);
			if ((Object)(object)((Component)val3).GetComponentInChildren<Gun>() == (Object)null && !Bot.IsWeapon(val3))
			{
				continue;
			}
			foreach (Bot item in BotManager.All)
			{
				if (item.enemy || item.dead || item.Puppet || item.Riding || item.InLesson)
				{
					continue;
				}
				Transform chest = item.Chest;
				if ((Object)(object)chest != (Object)null)
				{
					Vector3 val4 = ((Component)val2).transform.position - chest.position;
					if (((Vector3)(ref val4)).magnitude < Tuning.F("orders.give.near", 0.6f))
					{
						bot = item;
						val = val3;
					}
				}
			}
		}
		if (bot == null)
		{
			giveSince = -1f;
			giveTo = null;
		}
		else if (bot != giveTo || ((Il2CppObjectBase)(object)val).Pointer != giveThing)
		{
			giveTo = bot;
			giveThing = ((Il2CppObjectBase)(object)val).Pointer;
			giveSince = Time.time;
		}
		else if (!(Time.time - giveSince < Tuning.F("orders.give.hold", 0.6f)))
		{
			giveSince = Time.time + 2f;
			bot.TakeFromYou(val);
		}
	}

	private static Vector3 Flat(Vector3 v)
	{
		return new Vector3(v.x, 0f, v.z);
	}
}
