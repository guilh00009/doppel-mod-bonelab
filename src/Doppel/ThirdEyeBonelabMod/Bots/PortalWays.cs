using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Portals.MonoBehaviours;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class PortalWays
{
	public struct Way
	{
		public Vector3 inAt;

		public Vector3 inOpen;

		public Vector3 outAt;

		public Vector3 outOpen;

		public IntPtr id;

		public Component portal;

		public Component other;
	}

	private static readonly List<Way> ways = new List<Way>();

	private static float lookedAt = -10f;

	private static bool missing;

	public static Way PlayerTook;

	public static float PlayerTookAt = -100f;

	private static Vector3 playerWas;

	private static bool playerHas;

	public static List<Way> All
	{
		get
		{
			if (!missing && Time.time - lookedAt > 1f)
			{
				lookedAt = Time.time;
				ways.Clear();
				try
				{
					Look();
				}
				catch (Exception ex) when (ex is TypeLoadException || ex is FileNotFoundException || ex is MissingMethodException)
				{
					missing = true;
				}
				catch (Exception ex2)
				{
					Lab.Event("portals: could not be looked for: " + ex2.Message);
				}
			}
			return ways;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Look()
	{
		foreach (Portal item in Object.FindObjectsOfType<Portal>())
		{
			if (!((Object)(object)item == (Object)null) && ((Component)item).gameObject.activeInHierarchy)
			{
				Portal otherPortal = item.OtherPortal;
				if (!((Object)(object)otherPortal == (Object)null) && ((Component)otherPortal).gameObject.activeInHierarchy)
				{
					ways.Add(new Way
					{
						inAt = ((Component)item).transform.position,
						inOpen = Open(((Component)item).transform),
						outAt = ((Component)otherPortal).transform.position,
						outOpen = Open(((Component)otherPortal).transform),
						id = ((Il2CppObjectBase)(object)item).Pointer,
						portal = (Component)(object)item,
						other = (Component)(object)otherPortal
					});
				}
			}
		}
	}

	private static Vector3 Open(Transform portal)
	{
		return portal.forward;
	}

	private static bool Blocked(Vector3 at, Vector3 dir)
	{
		foreach (RaycastHit item in Physics.RaycastAll(at + dir * 0.02f, dir, 0.5f, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current = item;
			if ((Object)(object)((RaycastHit)(ref current)).collider != (Object)null && (Object)(object)((RaycastHit)(ref current)).collider.attachedRigidbody == (Object)null)
			{
				return true;
			}
		}
		return false;
	}

	public static void Update()
	{
		Transform head = Player.Head;
		if ((Object)(object)head == (Object)null)
		{
			playerHas = false;
			return;
		}
		Vector3 position = head.position;
		if (playerHas)
		{
			Vector3 val = position - playerWas;
			if (((Vector3)(ref val)).magnitude > 2f)
			{
				foreach (Way item in All)
				{
					val = item.inAt - playerWas;
					if (((Vector3)(ref val)).magnitude < 2.5f)
					{
						val = item.outAt - position;
						if (((Vector3)(ref val)).magnitude < 3.5f)
						{
							PlayerTook = item;
							PlayerTookAt = Time.time;
							Lab.Event("portals: you went through one");
							break;
						}
					}
				}
			}
		}
		playerWas = position;
		playerHas = true;
	}

	public static bool Shorter(Vector3 from, Vector3 to, out Way best)
	{
		best = default(Way);
		Vector3 val = to - from;
		float num = ((Vector3)(ref val)).magnitude * Tuning.F("portal.worth", 0.7f) - 2f;
		bool result = false;
		foreach (Way item in All)
		{
			if (!(item.inOpen.y < -0.5f) && !(item.inAt.y - from.y > 2.2f))
			{
				val = item.inAt - from;
				float magnitude = ((Vector3)(ref val)).magnitude;
				val = to - item.outAt;
				float num2 = magnitude + ((Vector3)(ref val)).magnitude;
				if (num2 < num)
				{
					num = num2;
					best = item;
					result = true;
				}
			}
		}
		return result;
	}
}
