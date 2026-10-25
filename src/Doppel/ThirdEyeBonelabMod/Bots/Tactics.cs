using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdEyeBonelabMod.Bots;

public static class Tactics
{
	private static readonly Dictionary<IntPtr, (Vector3 at, float time, Vector3 velocity)> tracks = new Dictionary<IntPtr, (Vector3, float, Vector3)>();

	public static void ForgetLevel()
	{
		tracks.Clear();
	}

	public static Vector3 Velocity(Targets.Target t)
	{
		if ((Object)(object)t.aim == (Object)null)
		{
			return Vector3.zero;
		}
		IntPtr pointer = ((Il2CppObjectBase)(object)t.aim).Pointer;
		Vector3 point = t.Point;
		if (tracks.TryGetValue(pointer, out (Vector3, float, Vector3) value))
		{
			float num = Time.time - value.Item2;
			if (num < 0.05f)
			{
				return value.Item3;
			}
			Vector3 val = ((num < 1.5f) ? Vector3.Lerp(value.Item3, (point - value.Item1) / num, 0.5f) : Vector3.zero);
			tracks[pointer] = (point, Time.time, val);
			return val;
		}
		tracks[pointer] = (point, Time.time, Vector3.zero);
		return Vector3.zero;
	}

	public static float Danger(Targets.Target t, Vector3 from)
	{
		Vector3 val = t.Point - from;
		float magnitude = ((Vector3)(ref val)).magnitude;
		Vector3 val2 = Velocity(t);
		float num = ((magnitude > 0.01f) ? Mathf.Max(0f, 0f - Vector3.Dot(val2, val / magnitude)) : 0f);
		float num2 = 10f / (magnitude + 1f) + num * 1.5f;
		if (Time.time - PlayerWatch.FiredAt < 2.5f)
		{
			Vector3 val3 = PlayerWatch.ShotAt - t.Point;
			if (((Vector3)(ref val3)).magnitude < 2.5f)
			{
				num2 += 3f;
			}
		}
		return num2;
	}

	public static bool FindCover(Vector3 feet, List<Vector3> enemies, out Vector3 cover)
	{
		cover = feet;
		if (enemies.Count == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		NavMeshHit val2 = default(NavMeshHit);
		RaycastHit val3 = default(RaycastHit);
		RaycastHit val5 = default(RaycastHit);
		for (int i = 0; i < 24; i++)
		{
			float num2 = (float)i * 15f + Random.Range(-6f, 6f);
			float num3 = Random.Range(2.5f, 10f);
			Vector3 val = feet + Quaternion.Euler(0f, num2, 0f) * Vector3.forward * num3;
			bool flag = false;
			try
			{
				if (NavMesh.SamplePosition(val, ref val2, 1.5f, -1))
				{
					val = ((NavMeshHit)(ref val2)).position;
					flag = true;
				}
			}
			catch (Exception)
			{
			}
			if (!flag)
			{
				if (!Physics.Raycast(val + Vector3.up * 1.5f, Vector3.down, ref val3, 3f, -1, (QueryTriggerInteraction)1) || ((RaycastHit)(ref val3)).normal.y < 0.7f)
				{
					continue;
				}
				val = ((RaycastHit)(ref val3)).point;
				if (Mathf.Abs(val.y - feet.y) > 1.2f)
				{
					continue;
				}
			}
			Vector3 val4 = val + Vector3.up * 1.2f;
			bool flag2 = true;
			Vector3 val6;
			foreach (Vector3 enemy in enemies)
			{
				if (Physics.Linecast(val4, enemy, ref val5, -1, (QueryTriggerInteraction)1))
				{
					val6 = ((RaycastHit)(ref val5)).point - val4;
					if (!(((Vector3)(ref val6)).magnitude > 3f) && (!((Object)(object)((RaycastHit)(ref val5)).rigidbody != (Object)null) || ((RaycastHit)(ref val5)).rigidbody.isKinematic))
					{
						continue;
					}
				}
				flag2 = false;
				break;
			}
			if (!flag2)
			{
				continue;
			}
			val6 = val - feet;
			float num4 = ((Vector3)(ref val6)).magnitude;
			foreach (Vector3 enemy2 in enemies)
			{
				float num5 = num4;
				val6 = enemy2 - val;
				num4 = num5 + Mathf.Max(0f, 6f - ((Vector3)(ref val6)).magnitude) * 2f;
			}
			if (num4 < num)
			{
				num = num4;
				cover = val;
			}
		}
		return num < float.MaxValue;
	}

	public static bool InPlayersLine(Vector3 point, out Vector3 outOfIt)
	{
		outOfIt = Vector3.zero;
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		if ((Object)(object)componentInHand == (Object)null)
		{
			componentInHand = Player.GetComponentInHand<Gun>(Player.LeftHand);
		}
		Transform val = (((Object)(object)componentInHand != (Object)null) ? componentInHand.firePointTransform : null);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Vector3 val2 = point - val.position;
		float num = Vector3.Dot(val2, val.forward);
		if (num < 0.3f || num > 40f)
		{
			return false;
		}
		Vector3 val3 = val2 - val.forward * num;
		val3.y = 0f;
		if (((Vector3)(ref val3)).magnitude > 0.8f + num * 0.02f)
		{
			return false;
		}
		Vector3 normalized;
		if (!(((Vector3)(ref val3)).sqrMagnitude > 0.0001f))
		{
			Vector3 val4 = Vector3.Cross(Vector3.up, val.forward);
			normalized = ((Vector3)(ref val4)).normalized;
		}
		else
		{
			normalized = ((Vector3)(ref val3)).normalized;
		}
		outOfIt = normalized;
		return true;
	}

	public static bool InLineOf(Transform muzzle, Vector3 point, float wide, out Vector3 outOfIt)
	{
		outOfIt = Vector3.zero;
		if ((Object)(object)muzzle == (Object)null)
		{
			return false;
		}
		Vector3 val = point - muzzle.position;
		float num = Vector3.Dot(val, muzzle.forward);
		if (num < 0.3f || num > 60f)
		{
			return false;
		}
		Vector3 val2 = val - muzzle.forward * num;
		val2.y = 0f;
		if (((Vector3)(ref val2)).magnitude > wide + num * 0.04f)
		{
			return false;
		}
		Vector3 normalized;
		if (!(((Vector3)(ref val2)).sqrMagnitude > 0.0001f))
		{
			Vector3 val3 = Vector3.Cross(Vector3.up, muzzle.forward);
			normalized = ((Vector3)(ref val3)).normalized;
		}
		else
		{
			normalized = ((Vector3)(ref val2)).normalized;
		}
		outOfIt = normalized;
		return true;
	}

	public static Transform PlayersMuzzle()
	{
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		if ((Object)(object)componentInHand == (Object)null)
		{
			componentInHand = Player.GetComponentInHand<Gun>(Player.LeftHand);
		}
		if (!((Object)(object)componentInHand != (Object)null))
		{
			return null;
		}
		return componentInHand.firePointTransform;
	}

	public static bool RoomBehind(Vector3 chest, Vector3 back)
	{
		RaycastHit val = default(RaycastHit);
		return !Physics.SphereCast(chest, 0.2f, back, ref val, 1.2f, -1, (QueryTriggerInteraction)1);
	}
}
