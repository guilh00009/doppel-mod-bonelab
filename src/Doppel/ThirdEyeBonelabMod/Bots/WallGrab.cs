using System;
using System.Collections.Generic;
using BoneLib;
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
		if (hand != null)
		{
			return held.ContainsKey(hand.Pointer);
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
			Hand[] array = new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			OpenControllerRig controllerRig = Player.ControllerRig;
			BaseController[] array2 = new BaseController[2]
			{
				(controllerRig != null) ? controllerRig.leftController : null,
				(controllerRig != null) ? controllerRig.rightController : null
			};
			for (int i = 0; i < 2; i++)
			{
				Hand hand = array[i];
				if (hand == null || array2[i] == null)
				{
					continue;
				}
				if (!(array2[i]._solvedGrip >= OpenController.grabThreshold))
				{
					closedAt[i] = -1f;
					Release(hand);
					continue;
				}
				if (closedAt[i] < 0f)
				{
					closedAt[i] = Time.time;
				}
				if (hand.m_CurrentAttachedGO != null)
				{
					Release(hand);
				}
				else if (!Holding(hand) && Time.time - closedAt[i] > Tuning.F("wallgrab.wait", 0.08f) && Time.time - closedAt[i] < 0.6f)
				{
					Take(hand, players: true);
				}
			}
			List<IntPtr> list = null;
			foreach (KeyValuePair<IntPtr, Held> item in held)
			{
				if (item.Value.hand == null || item.Value.joint == null)
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
		if (hand == null)
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
		if (rb == null)
		{
			return false;
		}
		Vector3 vector = ((hand.palmPositionTransform != null) ? hand.palmPositionTransform.position : hand.transform.position);
		float radius = Tuning.F("wallgrab.reach", 0.1f);
		bool flag = false;
		float num = float.MaxValue;
		foreach (Collider item in Physics.OverlapSphere(vector, radius, -1, QueryTriggerInteraction.Ignore))
		{
			if (item == null || item.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Rigidbody attachedRigidbody = item.attachedRigidbody;
			if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
			{
				continue;
			}
			Vector3 vector2 = vector;
			try
			{
				MeshCollider meshCollider = item.TryCast<MeshCollider>();
				if (meshCollider == null || meshCollider.convex)
				{
					vector2 = item.ClosestPoint(vector);
				}
			}
			catch (Exception)
			{
			}
			float magnitude = (vector2 - vector).magnitude;
			if (magnitude < num)
			{
				num = magnitude;
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		ConfigurableJoint configurableJoint = rb.gameObject.AddComponent<ConfigurableJoint>();
		configurableJoint.autoConfigureConnectedAnchor = false;
		configurableJoint.anchor = rb.transform.InverseTransformPoint(vector);
		configurableJoint.connectedAnchor = vector;
		ConfigurableJointMotion configurableJointMotion2 = (configurableJoint.zMotion = ConfigurableJointMotion.Locked);
		ConfigurableJointMotion xMotion = (configurableJoint.yMotion = configurableJointMotion2);
		configurableJoint.xMotion = xMotion;
		configurableJointMotion2 = (configurableJoint.angularZMotion = ConfigurableJointMotion.Free);
		xMotion = (configurableJoint.angularYMotion = configurableJointMotion2);
		configurableJoint.angularXMotion = xMotion;
		configurableJoint.breakForce = float.PositiveInfinity;
		configurableJoint.breakTorque = float.PositiveInfinity;
		held[hand.Pointer] = new Held
		{
			hand = hand,
			joint = configurableJoint,
			players = players
		};
		if (players)
		{
			Lessons.OnWall((!(hand == Player.LeftHand)) ? 1 : 0, on: true);
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
		if (!(hand == null) && held.TryGetValue(hand.Pointer, out var value))
		{
			held.Remove(hand.Pointer);
			if (value.joint != null)
			{
				UnityEngine.Object.Destroy(value.joint);
			}
			if (value.players)
			{
				Lessons.OnWall((!(hand == Player.LeftHand)) ? 1 : 0, on: false);
			}
		}
	}

	public static void DropAll()
	{
		foreach (Held value in held.Values)
		{
			if (value.joint != null)
			{
				UnityEngine.Object.Destroy(value.joint);
			}
		}
		held.Clear();
	}
}
