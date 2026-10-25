using System;
using System.Collections.Generic;
using System.Reflection;
using BoneLib;
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
				if (!(aim != null))
				{
					if (!(transform != null))
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

	private static AIBrain[] brains = new AIBrain[0];

	private static float brainsAt = -10f;

	public static IntPtr OnPlayerFor;

	public static float OnPlayerAt = -10f;

	public static string LastBlocker = "";

	public static bool Alive(Target t)
	{
		if (t.player)
		{
			if (t.transform != null && t.transform.gameObject.activeInHierarchy)
			{
				return t.aim != null;
			}
			return false;
		}
		if (t.transform == null || t.aim == null || !t.transform.gameObject.activeInHierarchy)
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
		if (t.zombieHead != null)
		{
			return Zombies().Contains(t.zombieHead);
		}
		if (t.brain != null)
		{
			if (!t.brain.isDead)
			{
				return t.brain.gameObject.activeInHierarchy;
			}
			return false;
		}
		if (t.prop != null)
		{
			return (t.prop.worldCenterOfMass - t.propWasAt).magnitude < 0.5f;
		}
		return true;
	}

	public static void Shot(Target t)
	{
		if (t.prop != null)
		{
			shotProps[t.prop.Pointer] = Time.time;
		}
	}

	public static void ForgetLevel()
	{
		brains = new AIBrain[0];
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
				Transform transform = ((item != null && item.physicsRig != null) ? item.physicsRig.m_head : null);
				if (!(transform == null) && item.gameObject.activeInHierarchy)
				{
					Vector3 offset = ((UnityEngine.Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
					list.Add(new Target
					{
						transform = item.transform,
						aim = transform,
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
		Transform transform = Player.Head;
		float num = ((transform != null) ? (transform.position - from).sqrMagnitude : float.MaxValue);
		foreach (RigManager item in NetBots.OtherPlayers())
		{
			try
			{
				Transform transform2 = ((item != null && item.physicsRig != null) ? item.physicsRig.m_head : null);
				if (!(transform2 == null) && item.gameObject.activeInHierarchy)
				{
					float sqrMagnitude = (transform2.position - from).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						transform = transform2;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		return transform;
	}

	public static Transform MuzzleOf(Transform playerBody)
	{
		try
		{
			RigManager rigManager = ((playerBody != null) ? playerBody.GetComponent<RigManager>() : null);
			if (rigManager == null || rigManager == Player.RigManager || rigManager.physicsRig == null)
			{
				return Tactics.PlayersMuzzle();
			}
			Gun componentInHand = Player.GetComponentInHand<Gun>(rigManager.physicsRig.rightHand);
			if (componentInHand == null)
			{
				componentInHand = Player.GetComponentInHand<Gun>(rigManager.physicsRig.leftHand);
			}
			return (componentInHand != null) ? componentInHand.firePointTransform : null;
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
		if (rigManager == null || head == null || !rigManager.gameObject.activeInHierarchy)
		{
			return false;
		}
		Vector3 offset = ((UnityEngine.Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
		t = new Target
		{
			transform = rigManager.transform,
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
			if (!(item.rig == self) && item.enemy != flag && !item.dead && !item.surrendered && item.Alive && item.Whole && !(item.Head == null))
			{
				Vector3 offset = ((UnityEngine.Random.value < 0.3f) ? Vector3.zero : (-Vector3.up * 0.3f));
				list.Add(new Target
				{
					transform = item.rig.transform,
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
			if (item != null)
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
		foreach (AIBrain aIBrain in array)
		{
			if (!(aIBrain == null) && !aIBrain.isDead && aIBrain.gameObject.activeInHierarchy)
			{
				Vector3 offset;
				Transform aim = Chest(aIBrain, out offset);
				list.Add(new Target
				{
					transform = aIBrain.transform,
					aim = aim,
					offset = offset,
					hostile = true,
					brain = aIBrain
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
			brains = UnityEngine.Object.FindObjectsOfType<AIBrain>();
		}
		catch (Exception)
		{
			brains = new AIBrain[0];
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
			return found.transform != null;
		}
		foreach (Target item2 in OtherSide(self))
		{
			Consider(eye, range, self, item2, ref best, ref found);
		}
		foreach (Transform item3 in Zombies())
		{
			if (!(item3 == null))
			{
				Vector3 offset = ((UnityEngine.Random.value < 0.4f) ? Vector3.zero : (-Vector3.up * 0.3f));
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
		foreach (AIBrain aIBrain in array)
		{
			if (!(aIBrain == null) && !aIBrain.isDead && aIBrain.gameObject.activeInHierarchy)
			{
				Vector3 offset2;
				Transform aim = Chest(aIBrain, out offset2);
				Consider(eye, range, self, new Target
				{
					transform = aIBrain.transform,
					aim = aim,
					offset = offset2,
					hostile = true,
					brain = aIBrain
				}, ref best, ref found);
			}
		}
		if (found.transform != null || !Practice)
		{
			return found.transform != null;
		}
		foreach (Target item4 in Hostiles())
		{
			if ((item4.Point - eye).magnitude < 35f)
			{
				return false;
			}
		}
		foreach (Collider item5 in Physics.OverlapSphere(eye, Mathf.Min(range, 22f), -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item5 != null) ? item5.attachedRigidbody : null);
			if (rigidbody == null || rigidbody.isKinematic || rigidbody.mass < 0.3f || rigidbody.mass > 40f || rigidbody.GetComponentInParent<RigManager>() != null || rigidbody.GetComponentInParent<Gun>() != null)
			{
				continue;
			}
			string text = rigidbody.name.ToLowerInvariant();
			if (text.Contains("cartridge") || text.Contains("case") || text.Contains("shell") || text.Contains("bullet") || text.StartsWith("mag") || text.Contains("valvebiped") || rigidbody.transform.root.name.ToLowerInvariant().Contains("zombie"))
			{
				continue;
			}
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			if (!((worldCenterOfMass - eye).magnitude < 4f) && (!shotProps.TryGetValue(rigidbody.Pointer, out var value) || !(Time.time - value < 25f)))
			{
				float num = best;
				Consider(eye, range, self, new Target
				{
					transform = rigidbody.transform,
					aim = rigidbody.transform,
					offset = rigidbody.transform.InverseTransformPoint(worldCenterOfMass),
					prop = rigidbody,
					propWasAt = worldCenterOfMass
				}, ref best, ref found);
				if (best < num)
				{
					best += UnityEngine.Random.Range(0f, 8f);
				}
			}
		}
		return found.transform != null;
	}

	private static void Consider(Vector3 eye, float range, RigManager self, Target t, ref float best, ref Target found)
	{
		if (t.aim == null)
		{
			return;
		}
		if (t.prop != null)
		{
			t.offset = t.aim.TransformPoint(t.offset) - t.aim.position;
		}
		Vector3 point = t.Point;
		float magnitude = (point - eye).magnitude;
		if (magnitude > range || magnitude < 1f)
		{
			return;
		}
		if (!t.player && NearPlayer(point, t.hostile ? 1.2f : 2.5f) && (!t.hostile || !ClearOfPlayer(eye, point)))
		{
			if (t.hostile && magnitude < 20f && self != null)
			{
				OnPlayerFor = self.Pointer;
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
		Vector3 vector = point - from;
		if (vector.magnitude > Tuning.F("fire.byplayer.far", 9f) || NearPlayer(point, Tuning.F("fire.byplayer.apart", 0.35f)))
		{
			return false;
		}
		return !PlayerInLine(from, vector.normalized, Tuning.F("fire.byplayer.clear", 0.5f));
	}

	public static bool NearPlayer(Vector3 point, float within)
	{
		Transform head = Player.Head;
		if (head == null)
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
		if (head == null)
		{
			return false;
		}
		for (float num = 0f; num <= 1.6f; num += 0.4f)
		{
			Vector3 vector = head.position - Vector3.up * num - muzzle;
			float num2 = Vector3.Dot(vector, dir);
			if (num2 > -0.3f && (vector - dir * num2).magnitude < within + num2 * 0.02f)
			{
				return true;
			}
		}
		return false;
	}

	private static float Distance(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 vector = b - a;
		float num = Mathf.Clamp01(Vector3.Dot(p - a, vector) / vector.sqrMagnitude);
		return (a + vector * num - p).magnitude;
	}

	public static bool InSight(Vector3 eye, Vector3 point, Transform what, RigManager self)
	{
		Transform transform = null;
		try
		{
			Seat seat = ((self != null) ? self.activeSeat : null);
			Poolee poolee = ((seat != null) ? seat.GetComponentInParent<Poolee>() : null);
			transform = ((poolee != null) ? poolee.transform : null);
		}
		catch (Exception)
		{
		}
		Vector3 vector = point - eye;
		RaycastHit[] array = Physics.RaycastAll(eye, vector.normalized, vector.magnitude, -1, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if (!(raycastHit.collider == null))
			{
				Transform transform2 = raycastHit.collider.transform;
				if ((!(what != null) || (!transform2.IsChildOf(what) && (!(raycastHit.collider.attachedRigidbody != null) || !raycastHit.collider.attachedRigidbody.transform.IsChildOf(what)))) && (!(self != null) || !transform2.IsChildOf(self.transform)) && (!(transform != null) || !transform2.IsChildOf(transform)) && (!(raycastHit.collider.attachedRigidbody != null) || !(self != null) || !Held(raycastHit.collider.attachedRigidbody, self)) && !((raycastHit.point - point).magnitude < 0.25f))
				{
					LastBlocker = raycastHit.collider.name + " of " + raycastHit.collider.transform.root.name + ", " + (raycastHit.point - point).magnitude.ToString("0.0") + " m short of it";
					return false;
				}
			}
		}
		return true;
	}

	private static bool Held(Rigidbody rb, RigManager self)
	{
		Hand[] array = new Hand[2]
		{
			self.physicsRig.leftHand,
			self.physicsRig.rightHand
		};
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject != null && gameObject.transform.root == rb.transform.root)
			{
				return true;
			}
		}
		return false;
	}

	private static Transform Chest(AIBrain brain, out Vector3 offset)
	{
		offset = Vector3.zero;
		Animator componentInChildren = brain.GetComponentInChildren<Animator>();
		if (componentInChildren != null && componentInChildren.isHuman)
		{
			Transform boneTransform = componentInChildren.GetBoneTransform(HumanBodyBones.Chest);
			if (boneTransform != null)
			{
				return boneTransform;
			}
		}
		offset = Vector3.up * 1.1f;
		return brain.transform;
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
