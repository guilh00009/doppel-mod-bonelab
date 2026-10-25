using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BoneLib;
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
		foreach (Portal item in UnityEngine.Object.FindObjectsOfType<Portal>())
		{
			if (!(item == null) && item.gameObject.activeInHierarchy)
			{
				Portal otherPortal = item.OtherPortal;
				if (!(otherPortal == null) && otherPortal.gameObject.activeInHierarchy)
				{
					ways.Add(new Way
					{
						inAt = item.transform.position,
						inOpen = Open(item.transform),
						outAt = otherPortal.transform.position,
						outOpen = Open(otherPortal.transform),
						id = item.Pointer,
						portal = item,
						other = otherPortal
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
		foreach (RaycastHit item in Physics.RaycastAll(at + dir * 0.02f, dir, 0.5f, -1, QueryTriggerInteraction.Ignore))
		{
			if (item.collider != null && item.collider.attachedRigidbody == null)
			{
				return true;
			}
		}
		return false;
	}

	public static void Update()
	{
		Transform head = Player.Head;
		if (head == null)
		{
			playerHas = false;
			return;
		}
		Vector3 position = head.position;
		if (playerHas && (position - playerWas).magnitude > 2f)
		{
			foreach (Way item in All)
			{
				if ((item.inAt - playerWas).magnitude < 2.5f && (item.outAt - position).magnitude < 3.5f)
				{
					PlayerTook = item;
					PlayerTookAt = Time.time;
					Lab.Event("portals: you went through one");
					break;
				}
			}
		}
		playerWas = position;
		playerHas = true;
	}

	public static bool Shorter(Vector3 from, Vector3 to, out Way best)
	{
		best = default(Way);
		float num = (to - from).magnitude * Tuning.F("portal.worth", 0.7f) - 2f;
		bool result = false;
		foreach (Way item in All)
		{
			if (!(item.inOpen.y < -0.5f) && !(item.inAt.y - from.y > 2.2f))
			{
				float num2 = (item.inAt - from).magnitude + (to - item.outAt).magnitude;
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
