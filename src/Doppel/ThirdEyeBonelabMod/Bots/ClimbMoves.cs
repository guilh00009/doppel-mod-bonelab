using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BoneLib;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class ClimbMoves
{
	public class Frame
	{
		public float t { get; set; }

		public float[] head { get; set; }

		public float[] left { get; set; }

		public float[] right { get; set; }

		public bool lh { get; set; }

		public bool rh { get; set; }
	}

	public class Climb
	{
		public float height { get; set; }

		public int slot { get; set; } = -1;


		public List<Frame> frames { get; set; } = new List<Frame>();


		public float Length
		{
			get
			{
				if (frames.Count <= 0)
				{
					return 0f;
				}
				return frames[frames.Count - 1].t;
			}
		}
	}

	public class Store
	{
		public List<Climb> climbs { get; set; } = new List<Climb>();

	}

	private static Store store;

	private static Climb taking;

	private static float startedAt;

	private static float sampleAt;

	private static float startHead;

	private static Vector3 edge;

	private static Quaternion facing;

	private static readonly bool[] holding = new bool[2];

	private static float lastHeldAt;

	private static readonly List<(float at, Climb climb)> learned = new List<(float, Climb)>();

	private static string File => Path.Combine(AssetFiles.SaveFolder(""), "climbs.json");

	private static Store Data
	{
		get
		{
			if (store != null)
			{
				return store;
			}
			store = new Store();
			try
			{
				if (System.IO.File.Exists(File))
				{
					store = JsonSerializer.Deserialize<Store>(System.IO.File.ReadAllText(File)) ?? new Store();
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Bot Player: the climbs could not be read: " + ex.Message);
			}
			return store;
		}
	}

	public static int Count => Data.climbs.Count;

	private static bool Learning
	{
		get
		{
			if (!Recorder.Recording)
			{
				return MoveSlots.Recording(MoveSlots.Kind.Climb);
			}
			return true;
		}
	}

	private static void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(File));
			System.IO.File.WriteAllText(File, JsonSerializer.Serialize(Data));
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the climbs could not be saved: " + ex.Message);
		}
	}

	public static Climb Pick(float height)
	{
		List<Climb> climbs = Data.climbs;
		if (climbs.Count == 0)
		{
			return null;
		}
		List<Climb> list = new List<Climb>(climbs);
		list.Sort((Climb a, Climb b) => Mathf.Abs(a.height - height).CompareTo(Mathf.Abs(b.height - height)));
		return list[UnityEngine.Random.Range(0, Mathf.Min(3, list.Count))];
	}

	public static void OnGrip(Grip grip, Hand hand, bool on)
	{
		if (!Learning || grip == null || hand == null)
		{
			return;
		}
		RigManager rigManager = Player.RigManager;
		if (rigManager == null || rigManager.worldGrip == null || grip.Pointer != rigManager.worldGrip.Pointer)
		{
			return;
		}
		int num = ((!(hand == Player.LeftHand)) ? ((hand == Player.RightHand) ? 1 : (-1)) : 0);
		if (num < 0)
		{
			return;
		}
		holding[num] = on;
		if (on)
		{
			lastHeldAt = Time.time;
			if (taking == null)
			{
				Begin(hand);
			}
		}
	}

	private static void Begin(Hand hand)
	{
		Transform head = Player.Head;
		if (!(head == null))
		{
			edge = hand.transform.position;
			facing = Quaternion.Euler(0f, Recorder.Yaw(head.forward), 0f);
			PhysicsRig physicsRig = Player.PhysicsRig;
			float num = ((physicsRig != null && physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position.y : (head.position.y - 1.6f));
			taking = new Climb
			{
				height = edge.y - num
			};
			startedAt = Time.time;
			sampleAt = 0f;
			startHead = head.position.y;
			Lab.Event("climb: watching (edge " + taking.height.ToString("0.00") + " m up)");
		}
	}

	public static void Update()
	{
		if (taking == null)
		{
			return;
		}
		float num = Time.time - startedAt;
		if (num > 12f || !Learning)
		{
			taking = null;
			return;
		}
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (controllerRig == null || controllerRig.headset == null)
		{
			return;
		}
		if (num >= sampleAt)
		{
			sampleAt = num + 1f / 30f;
			Bot.Pose pose = new Bot.Pose(edge, facing).Inverse();
			taking.frames.Add(new Frame
			{
				t = num,
				head = P(pose * Bot.Pose.Of(controllerRig.headset)),
				left = P(pose * Bot.Pose.Of(controllerRig.leftController.transform)),
				right = P(pose * Bot.Pose.Of(controllerRig.rightController.transform)),
				lh = holding[0],
				rh = holding[1]
			});
		}
		if (holding[0] || holding[1] || !(Time.time - lastHeldAt > 0.6f))
		{
			return;
		}
		float num2 = controllerRig.headset.position.y - startHead;
		if (num2 > 0.35f && taking.frames.Count > 10)
		{
			taking.slot = ((MoveSlots.RecordingSlot(MoveSlots.Kind.Climb) >= 0) ? MoveSlots.Tag(MoveSlots.Kind.Climb, MoveSlots.RecordingSlot(MoveSlots.Kind.Climb)) : (-1));
			Data.climbs.Add(taking);
			if (Data.climbs.Count > 20)
			{
				Data.climbs.RemoveAt(0);
			}
			learned.Add((Time.time, taking));
			Save();
			Lab.Event("climb: learned (" + taking.Length.ToString("0.0") + " s, rose " + num2.ToString("0.00") + " m)");
			Spawnables.Notify("Learned how you climb");
		}
		taking = null;
	}

	public static void ClearSlot(int slot)
	{
		if (Data.climbs.RemoveAll((Climb c) => c.slot == slot) > 0)
		{
			Save();
		}
	}

	public static void MoveSlot(int from, int to)
	{
		bool flag = false;
		foreach (Climb climb in Data.climbs)
		{
			if (climb.slot == from)
			{
				climb.slot = to;
				flag = true;
			}
		}
		if (flag)
		{
			Save();
		}
	}

	public static void Reset()
	{
		store = new Store();
		learned.Clear();
		taking = null;
	}

	public static int ForgetSince(float since)
	{
		int num = 0;
		int num2 = learned.Count - 1;
		while (num2 >= 0 && learned[num2].at >= since)
		{
			Data.climbs.Remove(learned[num2].climb);
			learned.RemoveAt(num2);
			num++;
			num2--;
		}
		if (num > 0)
		{
			Save();
		}
		return num;
	}

	public static string Summary()
	{
		return Count + " climbs";
	}

	private static float[] P(Bot.Pose p)
	{
		return new float[7]
		{
			p.position.x,
			p.position.y,
			p.position.z,
			p.rotation.x,
			p.rotation.y,
			p.rotation.z,
			p.rotation.w
		};
	}

	public static Bot.Pose Read(float[] v)
	{
		return new Bot.Pose(new Vector3(v[0], v[1], v[2]), new Quaternion(v[3], v[4], v[5], v[6]));
	}
}
