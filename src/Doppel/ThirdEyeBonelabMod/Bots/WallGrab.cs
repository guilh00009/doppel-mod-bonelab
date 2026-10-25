using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class WallGrab
{
	private class Held
	{
		public Hand hand;

		public ConfigurableJoint joint;

		public bool players;
	}

	private static readonly Dictionary<IntPtr, Held> held = new Dictionary<IntPtr, Held>();

	private static readonly float[] closedAt = new float[2] { -1f, -1f };

	private static float saidAt = -10f;

	public static bool Holding(Hand hand)
	{
		if ((Object)(object)hand != (Object)null)
		{
			return held.ContainsKey(((Il2CppObjectBase)(object)hand).Pointer);
		}
		return false;
	}

	public static void Update()
	{
		if (Tuning.F("wallgrab", 1f) < 0.5f)
		{
			if (held.Count > 0)
			{
				DropAll();
			}
			return;
		}
		try
		{
			Hand[] array = (Hand[])(object)new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			OpenControllerRig controllerRig = Player.ControllerRig;
			BaseController[] array2 = (BaseController[])(object)new BaseController[2]
			{
				((Object)(object)controllerRig != (Object)null) ? ((ControllerRig)controllerRig).leftController : null,
				((Object)(object)controllerRig != (Object)null) ? ((ControllerRig)controllerRig).rightController : null
			};
			for (int i = 0; i < 2; i++)
			{
				Hand val = array[i];
				if ((Object)(object)val == (Object)null || (Object)(object)array2[i] == (Object)null)
				{
					continue;
				}
				if (!(array2[i]._solvedGrip >= OpenController.grabThreshold))
				{
					closedAt[i] = -1f;
					Release(val);
					continue;
				}
				if (closedAt[i] < 0f)
				{
					closedAt[i] = Time.time;
				}
				if ((Object)(object)val.m_CurrentAttachedGO != (Object)null)
				{
					Release(val);
				}
				else if (!Holding(val) && Time.time - closedAt[i] > Tuning.F("wallgrab.wait", 0.08f) && Time.time - closedAt[i] < 0.6f)
				{
					Take(val, players: true);
				}
			}
			List<IntPtr> list = null;
			foreach (KeyValuePair<IntPtr, Held> item in held)
			{
				if ((Object)(object)item.Value.hand == (Object)null || (Object)(object)item.Value.joint == (Object)null)
				{
					(list ?? (list = new List<IntPtr>())).Add(item.Key);
				}
			}
			if (list == null)
			{
				return;
			}
			foreach (IntPtr item2 in list)
			{
				held.Remove(item2);
			}
		}
		catch (Exception)
		{
		}
	}

	public static bool Hold(Hand hand, bool on)
	{
		if ((Object)(object)hand == (Object)null)
		{
			return false;
		}
		if (!on)
		{
			Release(hand);
			return false;
		}
		if (!Holding(hand))
		{
			return Take(hand, players: false);
		}
		return true;
	}

	private static bool Take(Hand hand, bool players)
	{
		Rigidbody rb = hand.rb;
		if ((Object)(object)rb == (Object)null)
		{
			return false;
		}
		Vector3 val = (((Object)(object)hand.palmPositionTransform != (Object)null) ? hand.palmPositionTransform.position : ((Component)hand).transform.position);
		float num = Tuning.F("wallgrab.reach", 0.1f);
		bool flag = false;
		float num2 = float.MaxValue;
		foreach (Collider item in Physics.OverlapSphere(val, num, -1, (QueryTriggerInteraction)1))
		{
			if ((Object)(object)item == (Object)null || (Object)(object)((Component)item).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Rigidbody attachedRigidbody = item.attachedRigidbody;
			if ((Object)(object)attachedRigidbody != (Object)null && !attachedRigidbody.isKinematic)
			{
				continue;
			}
			Vector3 val2 = val;
			try
			{
				MeshCollider val3 = ((Il2CppObjectBase)(object)item).TryCast<MeshCollider>();
				if ((Object)(object)val3 == (Object)null || val3.convex)
				{
					val2 = item.ClosestPoint(val);
				}
			}
			catch (Exception)
			{
			}
			Vector3 val4 = val2 - val;
			float magnitude = ((Vector3)(ref val4)).magnitude;
			if (magnitude < num2)
			{
				num2 = magnitude;
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		ConfigurableJoint val5 = ((Component)rb).gameObject.AddComponent<ConfigurableJoint>();
		((Joint)val5).autoConfigureConnectedAnchor = false;
		((Joint)val5).anchor = ((Component)rb).transform.InverseTransformPoint(val);
		((Joint)val5).connectedAnchor = val;
		ConfigurableJointMotion val6 = (ConfigurableJointMotion)0;
		val5.zMotion = (ConfigurableJointMotion)0;
		ConfigurableJointMotion xMotion = (val5.yMotion = val6);
		val5.xMotion = xMotion;
		val6 = (ConfigurableJointMotion)2;
		val5.angularZMotion = (ConfigurableJointMotion)2;
		xMotion = (val5.angularYMotion = val6);
		val5.angularXMotion = xMotion;
		((Joint)val5).breakForce = float.PositiveInfinity;
		((Joint)val5).breakTorque = float.PositiveInfinity;
		held[((Il2CppObjectBase)(object)hand).Pointer] = new Held
		{
			hand = hand,
			joint = val5,
			players = players
		};
		if (players)
		{
			Lessons.OnWall((!((Object)(object)hand == (Object)(object)Player.LeftHand)) ? 1 : 0, on: true);
		}
		if (players && Time.time - saidAt > 5f)
		{
			saidAt = Time.time;
			Lab.Event("wall grab: you have hold of a surface");
		}
		return true;
	}

	private static void Release(Hand hand)
	{
		if (!((Object)(object)hand == (Object)null) && held.TryGetValue(((Il2CppObjectBase)(object)hand).Pointer, out var value))
		{
			held.Remove(((Il2CppObjectBase)(object)hand).Pointer);
			if ((Object)(object)value.joint != (Object)null)
			{
				Object.Destroy((Object)(object)value.joint);
			}
			if (value.players)
			{
				Lessons.OnWall((!((Object)(object)hand == (Object)(object)Player.LeftHand)) ? 1 : 0, on: false);
			}
		}
	}

	public static void DropAll()
	{
		foreach (Held value in held.Values)
		{
			if ((Object)(object)value.joint != (Object)null)
			{
				Object.Destroy((Object)(object)value.joint);
			}
		}
		held.Clear();
	}
}
