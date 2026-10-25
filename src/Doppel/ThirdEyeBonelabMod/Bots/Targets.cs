using System;
using System.Collections.Generic;
using System.Reflection;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Pool;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Targets
{
	public struct Target
	{
		public Transform transform;

		public Transform aim;

		public Vector3 offset;

		public bool hostile;

		public AIBrain brain;

		public Transform zombieHead;

		public Rigidbody prop;

		public Vector3 propWasAt;

		public bool player;

		public Bot bot;

		public Vector3 Point
		{
			get
			{
				if (!((Object)(object)aim != (Object)null))
				{
					if (!((Object)(object)transform != (Object)null))
					{
						return Vector3.zero;
					}
					return transform.position;
				}
				return aim.position + offset;
			}
		}
	}

	private static readonly Dictionary<IntPtr, float> shotProps = new Dictionary<IntPtr, float>();

	private const float PropRest = 25f;

	public static bool Practice = true;

	private static MethodInfo livingHeads;

	private static bool lookedForZombies;

	private static AIBrain[] brains = (AIBrain[])(object)new AIBrain[0];

	private static float brainsAt = -10f;

	public static IntPtr OnPlayerFor;

	public static float OnPlayerAt = -10f;

	public static string LastBlocker = "";

	public static bool Alive(Target t)
	{
		if (t.player)
		{
			if ((Object)(object)t.transform != (Object)null && ((Component)t.transform).gameObject.activeInHierarchy)
			{
				return (Object)(object)t.aim != (Object)null;
			}
			return false;
		}
		if ((Object)(object)t.transform == (Object)null || (Object)(object)t.aim == (Object)null || !((Component)t.transform).gameObject.activeInHierarchy)
		{
			return false;
		}
		if (t.bot != null)
		{
			if (t.bot.Alive && !t.bot.dead && t.bot.Whole)
			{
				return !t.bot.surrendered;
			}
			return false;
		}
		if ((Object)(object)t.zombieHead != (Object)null)
		{
			return Zombies().Contains(t.zombieHead);
		}
		if ((Object)(object)t.brain != (Object)null)
		{
			if (!t.brain.isDead)
			{
				return ((Component)t.brain).gameObject.activeInHierarchy;
			}
			return false;
		}
		if ((Object)(object)t.prop != (Object)null)
		{
			Vector3 val = t.prop.worldCenterOfMass - t.propWasAt;
			return ((Vector3)(ref val)).magnitude < 0.5f;
		}
		return true;
	}

	public static void Shot(Target t)
	{
		if ((Object)(object)t.prop != (Object)null)
		{
			shotProps[((Il2CppObjectBase)(object)t.prop).Pointer] = Time.time;
		}
	}

	public static void ForgetLevel()
	{
		brains = (AIBrain[])(object)new AIBrain[0];
		brainsAt = -10f;
		shotProps.Clear();
	}

	public static List<Target> OtherPlayers()
	{
		List<Target> list = new List<Target>();
		foreach (RigManager item in NetBots.OtherPlayers())
		{
			try
			{
				Transform val = (((Object)(object)item != (Object)null && (Object)(object)item.physicsRig != (Object)null) ? ((Rig)item.physicsRig).m_head : null);
				if (!((Object)(object)val == (Object)null) && ((Component)item).gameObject.activeInHierarchy)
				{
					Vector3 offset = ((Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
					list.Add(new Target
					{
						transform = ((Component)item).transform,
						aim = val,
						offset = offset,
						hostile = true,
						player = true
					});
				}
			}
			catch (Exception)
			{
			}
		}
		return list;
	}

	public static Transform NearestPlayerHead(Vector3 from)
	{
		Transform val = Player.Head;
		float num;
		Vector3 val2;
		if (!((Object)(object)val != (Object)null))
		{
			num = float.MaxValue;
		}
		else
		{
			val2 = val.position - from;
			num = ((Vector3)(ref val2)).sqrMagnitude;
		}
		float num2 = num;
		foreach (RigManager item in NetBots.OtherPlayers())
		{
			try
			{
				Transform val3 = (((Object)(object)item != (Object)null && (Object)(object)item.physicsRig != (Object)null) ? ((Rig)item.physicsRig).m_head : null);
				if (!((Object)(object)val3 == (Object)null) && ((Component)item).gameObject.activeInHierarchy)
				{
					val2 = val3.position - from;
					float sqrMagnitude = ((Vector3)(ref val2)).sqrMagnitude;
					if (sqrMagnitude < num2)
					{
						num2 = sqrMagnitude;
						val = val3;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		return val;
	}

	public static Transform MuzzleOf(Transform playerBody)
	{
		try
		{
			RigManager val = (((Object)(object)playerBody != (Object)null) ? ((Component)playerBody).GetComponent<RigManager>() : null);
			if ((Object)(object)val == (Object)null || (Object)(object)val == (Object)(object)Player.RigManager || (Object)(object)val.physicsRig == (Object)null)
			{
				return Tactics.PlayersMuzzle();
			}
			Gun componentInHand = Player.GetComponentInHand<Gun>(val.physicsRig.rightHand);
			if ((Object)(object)componentInHand == (Object)null)
			{
				componentInHand = Player.GetComponentInHand<Gun>(val.physicsRig.leftHand);
			}
			return ((Object)(object)componentInHand != (Object)null) ? componentInHand.firePointTransform : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static bool PlayerTarget(out Target t)
	{
		t = default(Target);
		RigManager rigManager = Player.RigManager;
		Transform head = Player.Head;
		if ((Object)(object)rigManager == (Object)null || (Object)(object)head == (Object)null || !((Component)rigManager).gameObject.activeInHierarchy)
		{
			return false;
		}
		Vector3 offset = ((Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
		t = new Target
		{
			transform = ((Component)rigManager).transform,
			aim = head,
			offset = offset,
			hostile = true,
			player = true
		};
		return true;
	}

	public static List<Target> OtherSide(RigManager self)
	{
		List<Target> list = new List<Target>();
		bool flag = BotManager.IsEnemy(self);
		foreach (Bot item in BotManager.All)
		{
			if (!((Object)(object)item.rig == (Object)(object)self) && item.enemy != flag && !item.dead && !item.surrendered && item.Alive && item.Whole && !((Object)(object)item.Head == (Object)null))
			{
				Vector3 offset = ((Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
				list.Add(new Target
				{
					transform = ((Component)item.rig).transform,
					aim = item.Head,
					offset = offset,
					hostile = true,
					bot = item
				});
			}
		}
		return list;
	}

	public static List<Target> Hostiles()
	{
		List<Target> list = new List<Target>();
		foreach (Transform item in Zombies())
		{
			if ((Object)(object)item != (Object)null)
			{
				list.Add(new Target
				{
					transform = item.root,
					aim = item,
					offset = -Vector3.up * 0.3f,
					hostile = true,
					zombieHead = item
				});
			}
		}
		RefreshBrains();
		AIBrain[] array = brains;
		foreach (AIBrain val in array)
		{
			if (!((Object)(object)val == (Object)null) && !val.isDead && ((Component)val).gameObject.activeInHierarchy)
			{
				Vector3 offset;
				Transform aim = Chest(val, out offset);
				list.Add(new Target
				{
					transform = ((Component)val).transform,
					aim = aim,
					offset = offset,
					hostile = true,
					brain = val
				});
			}
		}
		return list;
	}

	private static void RefreshBrains()
	{
		if (Time.time - brainsAt <= 2f)
		{
			return;
		}
		brainsAt = Time.time;
		try
		{
			brains = Object.FindObjectsOfType<AIBrain>();
		}
		catch (Exception)
		{
			brains = (AIBrain[])(object)new AIBrain[0];
		}
	}

	public static bool Find(Vector3 eye, float range, RigManager self, out Target found)
	{
		found = default(Target);
		float best = float.MaxValue;
		if (BotManager.IsEnemy(self))
		{
			if (Tuning.F("enemy.you", 1f) > 0.5f && PlayerTarget(out var t))
			{
				Consider(eye, range, self, t, ref best, ref found);
			}
			foreach (Target item in OtherSide(self))
			{
				Consider(eye, range, self, item, ref best, ref found);
			}
			return (Object)(object)found.transform != (Object)null;
		}
		foreach (Target item2 in OtherSide(self))
		{
			Consider(eye, range, self, item2, ref best, ref found);
		}
		foreach (Transform item3 in Zombies())
		{
			if (!((Object)(object)item3 == (Object)null))
			{
				Vector3 offset = ((Random.value < 0.4f) ? Vector3.zero : (-Vector3.up * 0.3f));
				Consider(eye, range, self, new Target
				{
					transform = item3.root,
					aim = item3,
					offset = offset,
					hostile = true,
					zombieHead = item3
				}, ref best, ref found);
			}
		}
		RefreshBrains();
		AIBrain[] array = brains;
		foreach (AIBrain val in array)
		{
			if (!((Object)(object)val == (Object)null) && !val.isDead && ((Component)val).gameObject.activeInHierarchy)
			{
				Vector3 offset2;
				Transform aim = Chest(val, out offset2);
				Consider(eye, range, self, new Target
				{
					transform = ((Component)val).transform,
					aim = aim,
					offset = offset2,
					hostile = true,
					brain = val
				}, ref best, ref found);
			}
		}
		if ((Object)(object)found.transform != (Object)null || !Practice)
		{
			return (Object)(object)found.transform != (Object)null;
		}
		Vector3 val2;
		foreach (Target item4 in Hostiles())
		{
			val2 = item4.Point - eye;
			if (((Vector3)(ref val2)).magnitude < 35f)
			{
				return false;
			}
		}
		foreach (Collider item5 in Physics.OverlapSphere(eye, Mathf.Min(range, 22f), -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val3 = (((Object)(object)item5 != (Object)null) ? item5.attachedRigidbody : null);
			if ((Object)(object)val3 == (Object)null || val3.isKinematic || val3.mass < 0.3f || val3.mass > 40f || (Object)(object)((Component)val3).GetComponentInParent<RigManager>() != (Object)null || (Object)(object)((Component)val3).GetComponentInParent<Gun>() != (Object)null)
			{
				continue;
			}
			string text = ((Object)val3).name.ToLowerInvariant();
			if (text.Contains("cartridge") || text.Contains("case") || text.Contains("shell") || text.Contains("bullet") || text.StartsWith("mag") || text.Contains("valvebiped") || ((Object)((Component)val3).transform.root).name.ToLowerInvariant().Contains("zombie"))
			{
				continue;
			}
			Vector3 worldCenterOfMass = val3.worldCenterOfMass;
			val2 = worldCenterOfMass - eye;
			if (!(((Vector3)(ref val2)).magnitude < 4f) && (!shotProps.TryGetValue(((Il2CppObjectBase)(object)val3).Pointer, out var value) || !(Time.time - value < 25f)))
			{
				float num = best;
				Consider(eye, range, self, new Target
				{
					transform = ((Component)val3).transform,
					aim = ((Component)val3).transform,
					offset = ((Component)val3).transform.InverseTransformPoint(worldCenterOfMass),
					prop = val3,
					propWasAt = worldCenterOfMass
				}, ref best, ref found);
				if (best < num)
				{
					best += Random.Range(0f, 8f);
				}
			}
		}
		return (Object)(object)found.transform != (Object)null;
	}

	private static void Consider(Vector3 eye, float range, RigManager self, Target t, ref float best, ref Target found)
	{
		if ((Object)(object)t.aim == (Object)null)
		{
			return;
		}
		if ((Object)(object)t.prop != (Object)null)
		{
			t.offset = t.aim.TransformPoint(t.offset) - t.aim.position;
		}
		Vector3 point = t.Point;
		Vector3 val = point - eye;
		float magnitude = ((Vector3)(ref val)).magnitude;
		if (magnitude > range || magnitude < 1f)
		{
			return;
		}
		if (!t.player && NearPlayer(point, t.hostile ? 1.2f : 2.5f) && (!t.hostile || !ClearOfPlayer(eye, point)))
		{
			if (t.hostile && magnitude < 20f && (Object)(object)self != (Object)null)
			{
				OnPlayerFor = ((Il2CppObjectBase)(object)self).Pointer;
				OnPlayerAt = Time.time;
			}
		}
		else if (InSight(eye, point, t.transform, self))
		{
			float num = magnitude + (t.hostile ? 0f : 100f);
			if (num < best)
			{
				best = num;
				found = t;
			}
		}
	}

	public static bool ClearOfPlayer(Vector3 from, Vector3 point)
	{
		Vector3 val = point - from;
		if (((Vector3)(ref val)).magnitude > Tuning.F("fire.byplayer.far", 9f) || NearPlayer(point, Tuning.F("fire.byplayer.apart", 0.35f)))
		{
			return false;
		}
		return !PlayerInLine(from, ((Vector3)(ref val)).normalized, Tuning.F("fire.byplayer.clear", 0.5f));
	}

	public static bool NearPlayer(Vector3 point, float within)
	{
		Transform head = Player.Head;
		if ((Object)(object)head == (Object)null)
		{
			return false;
		}
		Vector3 position = head.position;
		Vector3 a = position - Vector3.up * 1.6f;
		return Distance(point, a, position) < within;
	}

	public static bool PlayerInLine(Vector3 muzzle, Vector3 dir, float within)
	{
		Transform head = Player.Head;
		if ((Object)(object)head == (Object)null)
		{
			return false;
		}
		for (float num = 0f; num <= 1.6f; num += 0.4f)
		{
			Vector3 val = head.position - Vector3.up * num - muzzle;
			float num2 = Vector3.Dot(val, dir);
			if (num2 > -0.3f)
			{
				Vector3 val2 = val - dir * num2;
				if (((Vector3)(ref val2)).magnitude < within + num2 * 0.02f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static float Distance(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 val = b - a;
		float num = Mathf.Clamp01(Vector3.Dot(p - a, val) / ((Vector3)(ref val)).sqrMagnitude);
		Vector3 val2 = a + val * num - p;
		return ((Vector3)(ref val2)).magnitude;
	}

	public static bool InSight(Vector3 eye, Vector3 point, Transform what, RigManager self)
	{
		Transform val = null;
		try
		{
			Seat val2 = (((Object)(object)self != (Object)null) ? self.activeSeat : null);
			Poolee val3 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponentInParent<Poolee>() : null);
			val = (((Object)(object)val3 != (Object)null) ? ((Component)val3).transform : null);
		}
		catch (Exception)
		{
		}
		Vector3 val4 = point - eye;
		RaycastHit[] array = Physics.RaycastAll(eye, ((Vector3)(ref val4)).normalized, ((Vector3)(ref val4)).magnitude, -1, (QueryTriggerInteraction)1);
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit val5 = array[i];
			if ((Object)(object)((RaycastHit)(ref val5)).collider == (Object)null)
			{
				continue;
			}
			Transform transform = ((Component)((RaycastHit)(ref val5)).collider).transform;
			if ((!((Object)(object)what != (Object)null) || (!transform.IsChildOf(what) && (!((Object)(object)((RaycastHit)(ref val5)).collider.attachedRigidbody != (Object)null) || !((Component)((RaycastHit)(ref val5)).collider.attachedRigidbody).transform.IsChildOf(what)))) && (!((Object)(object)self != (Object)null) || !transform.IsChildOf(((Component)self).transform)) && (!((Object)(object)val != (Object)null) || !transform.IsChildOf(val)) && (!((Object)(object)((RaycastHit)(ref val5)).collider.attachedRigidbody != (Object)null) || !((Object)(object)self != (Object)null) || !Held(((RaycastHit)(ref val5)).collider.attachedRigidbody, self)))
			{
				Vector3 val6 = ((RaycastHit)(ref val5)).point - point;
				if (!(((Vector3)(ref val6)).magnitude < 0.25f))
				{
					string[] obj = new string[6]
					{
						((Object)((RaycastHit)(ref val5)).collider).name,
						" of ",
						((Object)((Component)((RaycastHit)(ref val5)).collider).transform.root).name,
						", ",
						null,
						null
					};
					val6 = ((RaycastHit)(ref val5)).point - point;
					obj[4] = ((Vector3)(ref val6)).magnitude.ToString("0.0");
					obj[5] = " m short of it";
					LastBlocker = string.Concat(obj);
					return false;
				}
			}
		}
		return true;
	}

	private static bool Held(Rigidbody rb, RigManager self)
	{
		Hand[] array = (Hand[])(object)new Hand[2]
		{
			self.physicsRig.leftHand,
			self.physicsRig.rightHand
		};
		foreach (Hand val in array)
		{
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			if ((Object)(object)val2 != (Object)null && (Object)(object)val2.transform.root == (Object)(object)((Component)rb).transform.root)
			{
				return true;
			}
		}
		return false;
	}

	private static Transform Chest(AIBrain brain, out Vector3 offset)
	{
		offset = Vector3.zero;
		Animator componentInChildren = ((Component)brain).GetComponentInChildren<Animator>();
		if ((Object)(object)componentInChildren != (Object)null && componentInChildren.isHuman)
		{
			Transform boneTransform = componentInChildren.GetBoneTransform((HumanBodyBones)8);
			if ((Object)(object)boneTransform != (Object)null)
			{
				return boneTransform;
			}
		}
		offset = Vector3.up * 1.1f;
		return ((Component)brain).transform;
	}

	private static List<Transform> Zombies()
	{
		if (!lookedForZombies)
		{
			lookedForZombies = true;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type type = assemblies[i].GetType("ThirdEyeBonelabMod.Zombies.ZombieManager", throwOnError: false);
				MethodInfo methodInfo = ((type != null) ? type.GetMethod("LivingHeads", BindingFlags.Static | BindingFlags.Public) : null);
				if (methodInfo != null)
				{
					livingHeads = methodInfo;
					MelonLogger.Msg("Bot Player: found the Zombies mod");
					break;
				}
			}
		}
		if (livingHeads == null)
		{
			return new List<Transform>();
		}
		try
		{
			return (livingHeads.Invoke(null, null) as List<Transform>) ?? new List<Transform>();
		}
		catch (Exception)
		{
			return new List<Transform>();
		}
	}
}
