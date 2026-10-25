using System;
using System.Collections.Generic;
using BoneLib;
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
		if (t.aim == null)
		{
			return Vector3.zero;
		}
		IntPtr pointer = t.aim.Pointer;
		Vector3 point = t.Point;
		if (tracks.TryGetValue(pointer, out (Vector3, float, Vector3) value))
		{
			float num = Time.time - value.Item2;
			if (num < 0.05f)
			{
				return value.Item3;
			}
			Vector3 vector = ((num < 1.5f) ? Vector3.Lerp(value.Item3, (point - value.Item1) / num, 0.5f) : Vector3.zero);
			tracks[pointer] = (point, Time.time, vector);
			return vector;
		}
		tracks[pointer] = (point, Time.time, Vector3.zero);
		return Vector3.zero;
	}

	public static float Danger(Targets.Target t, Vector3 from)
	{
		Vector3 vector = t.Point - from;
		float magnitude = vector.magnitude;
		Vector3 lhs = Velocity(t);
		float num = ((magnitude > 0.01f) ? Mathf.Max(0f, 0f - Vector3.Dot(lhs, vector / magnitude)) : 0f);
		float num2 = 10f / (magnitude + 1f) + num * 1.5f;
		if (Time.time - PlayerWatch.FiredAt < 2.5f && (PlayerWatch.ShotAt - t.Point).magnitude < 2.5f)
		{
			num2 += 3f;
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
		for (int i = 0; i < 24; i++)
		{
			float y = (float)i * 15f + UnityEngine.Random.Range(-6f, 6f);
			float num2 = UnityEngine.Random.Range(2.5f, 10f);
			Vector3 vector = feet + Quaternion.Euler(0f, y, 0f) * Vector3.forward * num2;
			bool flag = false;
			try
			{
				if (NavMesh.SamplePosition(vector, out var hit, 1.5f, -1))
				{
					vector = hit.position;
					flag = true;
				}
			}
			catch (Exception)
			{
			}
			if (!flag)
			{
				if (!Physics.Raycast(vector + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 3f, -1, QueryTriggerInteraction.Ignore) || hitInfo.normal.y < 0.7f)
				{
					continue;
				}
				vector = hitInfo.point;
				if (Mathf.Abs(vector.y - feet.y) > 1.2f)
				{
					continue;
				}
			}
			Vector3 vector2 = vector + Vector3.up * 1.2f;
			bool flag2 = true;
			foreach (Vector3 enemy in enemies)
			{
				if (!Physics.Linecast(vector2, enemy, out var hitInfo2, -1, QueryTriggerInteraction.Ignore) || (hitInfo2.point - vector2).magnitude > 3f || (hitInfo2.rigidbody != null && !hitInfo2.rigidbody.isKinematic))
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				continue;
			}
			float num3 = (vector - feet).magnitude;
			foreach (Vector3 enemy2 in enemies)
			{
				num3 += Mathf.Max(0f, 6f - (enemy2 - vector).magnitude) * 2f;
			}
			if (num3 < num)
			{
				num = num3;
				cover = vector;
			}
		}
		return num < float.MaxValue;
	}

	public static bool InPlayersLine(Vector3 point, out Vector3 outOfIt)
	{
		outOfIt = Vector3.zero;
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		if (componentInHand == null)
		{
			componentInHand = Player.GetComponentInHand<Gun>(Player.LeftHand);
		}
		Transform transform = ((componentInHand != null) ? componentInHand.firePointTransform : null);
		if (transform == null)
		{
			return false;
		}
		Vector3 vector = point - transform.position;
		float num = Vector3.Dot(vector, transform.forward);
		if (num < 0.3f || num > 40f)
		{
			return false;
		}
		Vector3 vector2 = vector - transform.forward * num;
		vector2.y = 0f;
		if (vector2.magnitude > 0.8f + num * 0.02f)
		{
			return false;
		}
		outOfIt = ((vector2.sqrMagnitude > 0.0001f) ? vector2.normalized : Vector3.Cross(Vector3.up, transform.forward).normalized);
		return true;
	}

	public static bool InLineOf(Transform muzzle, Vector3 point, float wide, out Vector3 outOfIt)
	{
		outOfIt = Vector3.zero;
		if (muzzle == null)
		{
			return false;
		}
		Vector3 vector = point - muzzle.position;
		float num = Vector3.Dot(vector, muzzle.forward);
		if (num < 0.3f || num > 60f)
		{
			return false;
		}
		Vector3 vector2 = vector - muzzle.forward * num;
		vector2.y = 0f;
		if (vector2.magnitude > wide + num * 0.04f)
		{
			return false;
		}
		outOfIt = ((vector2.sqrMagnitude > 0.0001f) ? vector2.normalized : Vector3.Cross(Vector3.up, muzzle.forward).normalized);
		return true;
	}

	public static Transform PlayersMuzzle()
	{
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		if (componentInHand == null)
		{
			componentInHand = Player.GetComponentInHand<Gun>(Player.LeftHand);
		}
		if (!(componentInHand != null))
		{
			return null;
		}
		return componentInHand.firePointTransform;
	}

	public static bool RoomBehind(Vector3 chest, Vector3 back)
	{
		RaycastHit hitInfo;
		return !Physics.SphereCast(chest, 0.2f, back, out hitInfo, 1.2f, -1, QueryTriggerInteraction.Ignore);
	}
}
