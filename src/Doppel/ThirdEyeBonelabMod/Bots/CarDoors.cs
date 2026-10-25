using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class CarDoors
{
	private static float sweptAt = -10f;

	private static readonly HashSet<(IntPtr, IntPtr)> done = new HashSet<(IntPtr, IntPtr)>();

	public static void Sweep()
	{
		if (Time.time - sweptAt < 2f || Tuning.F("doors.through", 1f) < 0.5f)
		{
			return;
		}
		sweptAt = Time.time;
		List<Rigidbody> list = new List<Rigidbody>();
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Seat item in Object.FindObjectsOfType<Seat>())
		{
			Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
			if ((Object)(object)val == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val).Pointer))
			{
				continue;
			}
			foreach (Rigidbody componentsInChild in ((Component)val).GetComponentsInChildren<Rigidbody>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && ((Object)componentsInChild).name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					list.Add(componentsInChild);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (Bot item2 in BotManager.All)
		{
			PhysicsRig val2 = (((Object)(object)item2.rig != (Object)null) ? item2.rig.physicsRig : null);
			if ((Object)(object)val2 == (Object)null)
			{
				continue;
			}
			Collider[] array = null;
			foreach (Rigidbody item3 in list)
			{
				if (!done.Add((((Il2CppObjectBase)(object)item2.rig).Pointer, ((Il2CppObjectBase)(object)item3).Pointer)))
				{
					continue;
				}
				if (array == null)
				{
					array = ((Component)val2).GetComponentsInChildren<Collider>(true);
				}
				foreach (Collider componentsInChild2 in ((Component)item3).GetComponentsInChildren<Collider>(true))
				{
					if ((Object)(object)componentsInChild2 == (Object)null || componentsInChild2.isTrigger || (Object)(object)componentsInChild2.attachedRigidbody != (Object)(object)item3)
					{
						continue;
					}
					Collider[] array2 = array;
					foreach (Collider val3 in array2)
					{
						if ((Object)(object)val3 != (Object)null && !val3.isTrigger)
						{
							Physics.IgnoreCollision(componentsInChild2, val3, true);
						}
					}
				}
				num++;
			}
		}
		if (num > 0)
		{
			Lab.Event("doors: " + list.Count + " car doors of " + hashSet.Count + " cars, bots now pass through them (" + num + " new bot-door pairs)");
		}
	}
}
