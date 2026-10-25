using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

internal static class GunWeight
{
	private class Was
	{
		public GameObject root;

		public string name;

		public List<Rigidbody> bodies;

		public List<float> masses;

		public float k;

		public bool saidBack;
	}

	private static readonly Dictionary<IntPtr, Was> lightened = new Dictionary<IntPtr, Was>();

	private static float sweptAt;

	public static Transform RootOf(Component c)
	{
		MarrowEntity componentInParent = c.GetComponentInParent<MarrowEntity>();
		if (!(componentInParent != null))
		{
			return c.transform.root;
		}
		return componentInParent.transform;
	}

	public static void Lighten(Gun g)
	{
		try
		{
			if (g == null)
			{
				return;
			}
			Transform transform = RootOf(g);
			if (lightened.ContainsKey(transform.gameObject.Pointer))
			{
				return;
			}
			float num = Tuning.F("hold.onehand.kg", 0.8f);
			Was was = new Was
			{
				root = transform.gameObject,
				name = g.name,
				bodies = new List<Rigidbody>(),
				masses = new List<float>()
			};
			float num2 = 0f;
			foreach (Rigidbody componentsInChild in transform.GetComponentsInChildren<Rigidbody>(includeInactive: true))
			{
				if (!(componentsInChild == null) && !componentsInChild.isKinematic && !componentsInChild.name.Contains("Tracker") && !(componentsInChild.GetComponentInParent<Magazine>() != null))
				{
					was.bodies.Add(componentsInChild);
					was.masses.Add(componentsInChild.mass);
					num2 += componentsInChild.mass;
				}
			}
			if (num2 <= num || num2 <= 0f)
			{
				return;
			}
			float num3 = (was.k = num / num2);
			foreach (Rigidbody body in was.bodies)
			{
				body.mass *= num3;
			}
			lightened[transform.gameObject.Pointer] = was;
			string text = "";
			for (int i = 0; i < was.bodies.Count; i++)
			{
				text = text + ((i > 0) ? ", " : "") + was.bodies[i].name + " " + was.masses[i].ToString("0.00");
			}
			Lab.Event("gun weight: " + g.name + " " + num2.ToString("0.00") + " kg -> " + num.ToString("0.00") + " kg while a bot holds it (" + text + ")");
		}
		catch (Exception)
		{
		}
	}

	private static void KeepLight(Was was)
	{
		try
		{
			bool flag = false;
			for (int i = 0; i < was.bodies.Count; i++)
			{
				Rigidbody rigidbody = was.bodies[i];
				float num = was.masses[i] * was.k;
				if (rigidbody != null && rigidbody.mass > num * 1.05f)
				{
					rigidbody.mass = num;
					flag = true;
				}
			}
			if (flag && !was.saidBack)
			{
				was.saidBack = true;
				Lab.Event("gun weight: the game put " + was.name + "'s own weight back: made light again");
			}
		}
		catch (Exception)
		{
		}
	}

	public static void Sweep()
	{
		if (Time.time - sweptAt < 0.1f)
		{
			return;
		}
		sweptAt = Time.time;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Bot item in BotManager.All)
		{
			item.LockGunInHand();
			foreach (Gun item2 in item.GunsInHand())
			{
				Lighten(item2);
				hashSet.Add(RootOf(item2).gameObject.Pointer);
			}
		}
		List<IntPtr> list = null;
		foreach (KeyValuePair<IntPtr, Was> item3 in lightened)
		{
			if (item3.Value.root != null && hashSet.Contains(item3.Key))
			{
				KeepLight(item3.Value);
			}
			else
			{
				(list ?? (list = new List<IntPtr>())).Add(item3.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (IntPtr item4 in list)
		{
			Was was = lightened[item4];
			lightened.Remove(item4);
			try
			{
				if (was.root == null)
				{
					continue;
				}
				float num = 0f;
				for (int i = 0; i < was.bodies.Count; i++)
				{
					if (!(was.bodies[i] == null))
					{
						was.bodies[i].mass = was.masses[i];
						num += was.masses[i];
					}
				}
				Lab.Event("gun weight: " + was.name + " let go, its own " + num.ToString("0.00") + " kg back");
			}
			catch (Exception)
			{
			}
		}
	}
}
