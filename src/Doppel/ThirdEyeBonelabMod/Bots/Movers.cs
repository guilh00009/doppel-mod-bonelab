using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Movers
{
	public class Mover
	{
		public RigManager rig;

		public Transform head;

		public Bot bot;

		public bool you;

		public Vector3 velocity;

		internal Vector3 last;

		internal bool has;

		internal int seen;
	}

	public static readonly List<Mover> All = new List<Mover>();

	private static readonly Dictionary<int, Mover> known = new Dictionary<int, Mover>();

	private static int frame;

	private static float tidyAt;

	public static void Update()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime <= 0f)
		{
			return;
		}
		frame++;
		All.Clear();
		try
		{
			Add(Player.RigManager, Player.Head, null, you: true, deltaTime);
			foreach (RigManager item in NetBots.OtherPlayers())
			{
				Add(item, ((Object)(object)item != (Object)null && (Object)(object)item.physicsRig != (Object)null) ? ((Rig)item.physicsRig).m_head : null, null, you: false, deltaTime);
			}
			foreach (Bot item2 in BotManager.All)
			{
				if (item2.Alive)
				{
					Add(item2.rig, item2.Head, item2, you: false, deltaTime);
				}
			}
		}
		catch (Exception)
		{
		}
		if (!(Time.time >= tidyAt))
		{
			return;
		}
		tidyAt = Time.time + 5f;
		List<int> list = null;
		foreach (KeyValuePair<int, Mover> item3 in known)
		{
			if (item3.Value.seen != frame)
			{
				(list ?? (list = new List<int>())).Add(item3.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item4 in list)
		{
			known.Remove(item4);
		}
	}

	private static void Add(RigManager rig, Transform head, Bot bot, bool you, float dt)
	{
		if ((Object)(object)rig == (Object)null || (Object)(object)head == (Object)null)
		{
			return;
		}
		int instanceID = ((Object)rig).GetInstanceID();
		if (!known.TryGetValue(instanceID, out var value))
		{
			value = (known[instanceID] = new Mover());
		}
		value.rig = rig;
		value.head = head;
		value.bot = bot;
		value.you = you;
		value.seen = frame;
		Vector3 position = head.position;
		if (value.has)
		{
			Vector3 val = position - value.last;
			if (((Vector3)(ref val)).magnitude < 3f)
			{
				value.velocity = Vector3.Lerp(value.velocity, (position - value.last) / dt, Mathf.Clamp01(dt * 8f));
				goto IL_00ca;
			}
		}
		value.velocity = Vector3.zero;
		goto IL_00ca;
		IL_00ca:
		value.last = position;
		value.has = true;
		All.Add(value);
	}

	public static void Forget()
	{
		All.Clear();
		known.Clear();
	}
}
