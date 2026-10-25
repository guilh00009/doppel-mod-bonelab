using System;
using System.Collections.Generic;
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
		foreach (Seat item in UnityEngine.Object.FindObjectsOfType<Seat>())
		{
			Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
			if (poolee == null || !hashSet.Add(poolee.Pointer))
			{
				continue;
			}
			foreach (Rigidbody componentsInChild in poolee.GetComponentsInChildren<Rigidbody>(includeInactive: true))
			{
				if (componentsInChild != null && componentsInChild.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
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
			PhysicsRig physicsRig = ((item2.rig != null) ? item2.rig.physicsRig : null);
			if (physicsRig == null)
			{
				continue;
			}
			Collider[] array = null;
			foreach (Rigidbody item3 in list)
			{
				if (!done.Add((item2.rig.Pointer, item3.Pointer)))
				{
					continue;
				}
				if (array == null)
				{
					array = physicsRig.GetComponentsInChildren<Collider>(includeInactive: true);
				}
				foreach (Collider componentsInChild2 in item3.GetComponentsInChildren<Collider>(includeInactive: true))
				{
					if (componentsInChild2 == null || componentsInChild2.isTrigger || componentsInChild2.attachedRigidbody != item3)
					{
						continue;
					}
					Collider[] array2 = array;
					foreach (Collider collider in array2)
					{
						if (collider != null && !collider.isTrigger)
						{
							Physics.IgnoreCollision(componentsInChild2, collider, ignore: true);
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
