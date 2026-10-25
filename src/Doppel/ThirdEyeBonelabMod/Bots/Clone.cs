using System;
using System.Collections.Generic;
using System.IO;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.VRMK;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Clone
{
	public struct HandInput
	{
		public float index;

		public float middle;

		public float ring;

		public float pinky;

		public float thumb;

		public float grip;

		public float trigger;

		public bool primary;

		public bool secondary;

		public Vector2 stick;

		public bool a;

		public bool b;

		public bool menu;

		public bool click;

		public bool pad;

		public Vector2 padAxis;
	}

	public struct Frame
	{
		public float t;

		public Vector3 pelvis;

		public Vector3 pelvisVelocity;

		public Vector3 pelvisSpin;

		public Quaternion pelvisTurn;

		public Quaternion playspace;

		public Bot.Pose head;

		public Bot.Pose left;

		public Bot.Pose right;

		public HandInput leftIn;

		public HandInput rightIn;

		public bool aPrimary;

		public bool aSecondary;

		public float crouch;

		public float feet;

		public Vector3 root;

		public bool seated;

		public Bot.Pose seat;
	}

	public enum Kind
	{
		Spawn,
		Grab,
		Release,
		Fire,
		Avatar,
		Seat
	}

	public class Event
	{
		public float t;

		public Kind kind;

		public int hand;

		public int spawn = -1;

		public string barcode = "";

		public string name = "";

		public Vector3 at;

		public Quaternion turn = Quaternion.identity;

		public int grip = -1;
	}

	public class Take
	{
		public Vector3 startFeet;

		public string avatar = "";

		public Vector3 avatarScale = Vector3.one;

		public float startYaw;

		public string item = "";

		public string itemName = "";

		public string level = "";

		public float eyes;

		public List<Frame> frames = new List<Frame>();

		public List<Event> events = new List<Event>();

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

	public class Session
	{
		public Take taking;

		public float startedAt;

		public float nextFrameAt;

		public readonly Dictionary<IntPtr, int> spawned = new Dictionary<IntPtr, int>();

		public readonly Dictionary<IntPtr, int> things = new Dictionary<IntPtr, int>();

		public int spawns;

		public string wearing = "";

		public IntPtr seat;

		public HandInput leftLatch;

		public HandInput rightLatch;

		public bool On => taking != null;

		public float Now => Time.time - startedAt;
	}

	private const float Rate = 1f / 60f;

	private static readonly Session manual = new Session();

	public static readonly Session Lesson = new Session();

	private static readonly Session[] sessions = new Session[2] { manual, Lesson };

	private static Take last;

	public static float SuppressUntil;

	public static bool Recording => manual.taking != null;

	private static string Folder => AssetFiles.SaveFolder("clones");

	public static Take Last
	{
		get
		{
			if (last == null)
			{
				try
				{
					string text = Path.Combine(Folder, "clone_last.bin");
					if (File.Exists(text))
					{
						last = Load(text);
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Warning("Bot Player: the clone could not be read: " + ex.Message);
				}
			}
			return last;
		}
	}

	public static void Suppress()
	{
		SuppressUntil = Time.time + 1.5f;
	}

	public static void Set(bool on)
	{
		if (on)
		{
			Start();
		}
		else
		{
			Stop();
		}
	}

	public static void Start()
	{
		if (Begin(manual))
		{
			Spawnables.Notify("Clone recording: everything you do");
			Lab.Event("clone: recording");
		}
	}

	public static bool Begin(Session s)
	{
		if (s.taking != null || Player.PhysicsRig == null || Player.Head == null || Player.RigManager == null)
		{
			return false;
		}
		Take take = new Take();
		s.startedAt = Time.time;
		s.nextFrameAt = 0f;
		s.spawned.Clear();
		s.things.Clear();
		s.spawns = 0;
		s.leftLatch = (s.rightLatch = default(HandInput));
		s.seat = IntPtr.Zero;
		Rig physicsRig = Player.PhysicsRig;
		take.startFeet = ((physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position : (Player.Head.position - Vector3.up * 1.6f));
		take.startYaw = Recorder.Yaw(Player.Head.forward);
		take.avatar = BotRig.AvatarOf(Player.RigManager);
		Il2CppSLZ.VRMK.Avatar avatar = Player.RigManager.avatar;
		take.avatarScale = ((avatar != null) ? avatar.transform.localScale : Vector3.one);
		take.eyes = ((avatar != null) ? (avatar.eyeHeight * avatar.transform.lossyScale.y) : 0f);
		s.wearing = take.avatar;
		s.taking = take;
		return true;
	}

	public static Take End(Session s)
	{
		Take taking = s.taking;
		s.taking = null;
		return taking;
	}

	public static void Stop()
	{
		if (manual.taking == null)
		{
			return;
		}
		last = manual.taking;
		manual.taking = null;
		Spawnables.Notify("Clone recorded: " + last.Length.ToString("0") + " s, " + last.events.Count + " things done");
		Lab.Event("clone: recorded " + last.Length.ToString("0.0") + " s, " + last.frames.Count + " frames, " + last.events.Count + " events");
		try
		{
			Save(last, Path.Combine(Folder, "clone_last.bin"));
			Save(last, Path.Combine(Folder, "clone_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin"));
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the clone could not be saved: " + ex.Message);
		}
	}

	public static void Update()
	{
		Session[] array = sessions;
		for (int i = 0; i < array.Length; i++)
		{
			Sample(array[i]);
		}
	}

	private static void Sample(Session s)
	{
		Take taking = s.taking;
		if (taking == null)
		{
			return;
		}
		WatchAvatar(s);
		WatchSeat(s);
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (controllerRig != null && controllerRig.leftController != null && controllerRig.rightController != null)
		{
			Latch(ref s.leftLatch, controllerRig.leftController);
			Latch(ref s.rightLatch, controllerRig.rightController);
		}
		float num = Time.time - s.startedAt;
		if (num < s.nextFrameAt)
		{
			return;
		}
		s.nextFrameAt = num + 1f / 60f;
		PhysicsRig physicsRig = Player.PhysicsRig;
		RigManager rigManager = Player.RigManager;
		if (controllerRig == null || physicsRig == null || rigManager == null || controllerRig.vrRoot == null)
		{
			return;
		}
		Rigidbody rigidbody = ((physicsRig.m_pelvis != null) ? physicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
		if (!(rigidbody == null))
		{
			Frame frame = default(Frame);
			frame.t = num;
			frame.pelvis = rigidbody.position;
			frame.pelvisTurn = rigidbody.rotation;
			frame.pelvisVelocity = rigidbody.velocity;
			frame.pelvisSpin = rigidbody.angularVelocity;
			frame.playspace = controllerRig.vrRoot.rotation;
			frame.head = LocalOf(controllerRig.headset);
			frame.left = LocalOf(controllerRig.leftController.transform);
			frame.right = LocalOf(controllerRig.rightController.transform);
			frame.leftIn = InputOf(controllerRig.leftController, ref s.leftLatch);
			frame.rightIn = InputOf(controllerRig.rightController, ref s.rightLatch);
			frame.aPrimary = controllerRig._primaryAButton;
			frame.aSecondary = controllerRig._secondaryAButton;
			frame.crouch = rigManager.remapHeptaRig._crouchTarget;
			frame.feet = rigManager.remapHeptaRig._feetOffset;
			frame.root = controllerRig.vrRoot.position;
			Frame item = frame;
			Seat activeSeat = rigManager.activeSeat;
			if (activeSeat != null)
			{
				item.seated = true;
				item.seat = Bot.Pose.Of(activeSeat.transform);
			}
			taking.frames.Add(item);
		}
	}

	private static Bot.Pose LocalOf(Transform t)
	{
		return new Bot.Pose(t.localPosition, t.localRotation);
	}

	private static void Latch(ref HandInput latch, BaseController c)
	{
		latch.primary |= c._primaryInteractionButton;
		latch.secondary |= c._secondaryInteractionButton;
		latch.a |= c._aButton;
		latch.b |= c._bButton;
		latch.menu |= c._menuTap;
		latch.click |= c._thumbstick;
		latch.pad |= c._touchPad;
	}

	private static HandInput InputOf(BaseController c, ref HandInput latch)
	{
		HandInput handInput = latch;
		latch = default(HandInput);
		HandInput result = default(HandInput);
		result.a = c._aButton || handInput.a;
		result.b = c._bButton || handInput.b;
		result.menu = c._menuTap || handInput.menu;
		result.click = c._thumbstick || handInput.click;
		result.pad = c._touchPad || handInput.pad;
		result.padAxis = c._touchPadAxis;
		result.index = c._processedIndex;
		result.middle = c._processedMiddle;
		result.ring = c._processedRing;
		result.pinky = c._processedPinky;
		result.thumb = c._processedThumb;
		result.grip = c._solvedGrip;
		result.trigger = c._primaryAxis;
		result.primary = c._primaryInteractionButton || handInput.primary;
		result.secondary = c._secondaryInteractionButton || handInput.secondary;
		result.stick = c._thumbstickAxis;
		return result;
	}

	private static void WatchSeat(Session s)
	{
		try
		{
			RigManager rigManager = Player.RigManager;
			Seat seat = ((rigManager != null) ? rigManager.activeSeat : null);
			IntPtr intPtr = ((seat != null) ? seat.Pointer : IntPtr.Zero);
			if (intPtr == s.seat)
			{
				return;
			}
			s.seat = intPtr;
			Event @event = new Event
			{
				t = Time.time - s.startedAt,
				kind = Kind.Seat,
				hand = ((seat != null) ? 1 : 0)
			};
			if (seat != null)
			{
				Poolee componentInParent = seat.GetComponentInParent<Poolee>();
				Transform transform = ((componentInParent != null) ? componentInParent.transform : seat.transform.root);
				@event.barcode = ((componentInParent != null && componentInParent.SpawnableCrate != null) ? componentInParent.SpawnableCrate.Barcode.ID : "");
				IntPtr key = ((componentInParent != null) ? componentInParent.Pointer : transform.Pointer);
				if (!s.things.TryGetValue(key, out var value))
				{
					value = s.things.Count;
					s.things[key] = value;
				}
				@event.name = transform.name + "||" + value;
				@event.at = transform.position;
				@event.turn = transform.rotation;
				Seat[] array = transform.GetComponentsInChildren<Seat>(includeInactive: true);
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == seat)
					{
						@event.grip = i;
					}
				}
			}
			s.taking.events.Add(@event);
			Lab.Event(((s == Lesson) ? "lesson" : "clone") + ": you " + ((seat != null) ? ("sat down in the " + ThingName(@event)) : "got out"));
		}
		catch (Exception)
		{
		}
	}

	private static void WatchAvatar(Session s)
	{
		RigManager rigManager = Player.RigManager;
		if (!(rigManager == null) && !(rigManager.avatar == null))
		{
			string text = BotRig.AvatarOf(rigManager);
			if (!string.IsNullOrEmpty(text) && !(text == s.wearing))
			{
				s.wearing = text;
				s.taking.events.Add(new Event
				{
					t = Time.time - s.startedAt,
					kind = Kind.Avatar,
					barcode = text,
					at = rigManager.avatar.transform.localScale
				});
				Lab.Event("clone: avatar " + text);
			}
		}
	}

	public static bool IsBody(string barcode, string name)
	{
		if ((barcode ?? "").IndexOf("RigManager", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return (name ?? "").IndexOf("RigManager", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	public static void OnSpawn(Poolee poolee)
	{
		Session[] array = sessions;
		for (int i = 0; i < array.Length; i++)
		{
			OnSpawn(array[i], poolee);
		}
	}

	private static void OnSpawn(Session s, Poolee poolee)
	{
		Take taking = s.taking;
		if (taking == null || poolee == null || Time.time < SuppressUntil)
		{
			return;
		}
		try
		{
			if (poolee.SpawnableCrate == null)
			{
				return;
			}
			GameObject gameObject = poolee.gameObject;
			if (IsBody(poolee.SpawnableCrate.Barcode.ID, gameObject.name) || gameObject.GetComponentInChildren<RigManager>(includeInactive: true) != null || gameObject.GetComponentInChildren<Rigidbody>() == null)
			{
				return;
			}
			string text = gameObject.name.ToLowerInvariant();
			if (!text.Contains("cartridge") && !text.Contains("casing") && !text.Contains("shell") && !text.Contains("bullet") && !text.StartsWith("vfx", StringComparison.Ordinal) && !text.Contains("muzzle") && !text.Contains("flare") && !text.Contains("decal") && !text.Contains("blood") && !text.Contains("impact") && !text.Contains("spawn_gun") && !text.Contains("spawngun") && !text.Contains("nimbus") && !(gameObject.GetComponentInChildren<Magazine>() != null))
			{
				int num = s.spawns++;
				s.spawned[poolee.Pointer] = num;
				taking.events.Add(new Event
				{
					t = Time.time - s.startedAt,
					kind = Kind.Spawn,
					spawn = num,
					barcode = poolee.SpawnableCrate.Barcode.ID,
					name = gameObject.name,
					at = gameObject.transform.position,
					turn = gameObject.transform.rotation
				});
				if (s == manual)
				{
					Lab.Event("clone: spawn " + gameObject.name);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void OnGrip(Grip grip, Hand hand, bool on)
	{
		Session[] array = sessions;
		for (int i = 0; i < array.Length; i++)
		{
			OnGrip(array[i], grip, hand, on);
		}
	}

	private static void OnGrip(Session s, Grip grip, Hand hand, bool on)
	{
		Take taking = s.taking;
		float startedAt = s.startedAt;
		if (taking == null || grip == null || hand == null)
		{
			return;
		}
		int num = ((!(hand == Player.LeftHand)) ? ((hand == Player.RightHand) ? 1 : (-1)) : 0);
		if (num < 0)
		{
			return;
		}
		if (!on)
		{
			taking.events.Add(new Event
			{
				t = Time.time - startedAt,
				kind = Kind.Release,
				hand = num
			});
			return;
		}
		try
		{
			if (grip.GetComponentInParent<RigManager>() != null)
			{
				return;
			}
			Poolee componentInParent = grip.GetComponentInParent<Poolee>();
			if (componentInParent != null && componentInParent.SpawnableCrate != null && IsBody(componentInParent.SpawnableCrate.Barcode.ID, componentInParent.gameObject.name))
			{
				return;
			}
			Event @event = new Event
			{
				t = Time.time - startedAt,
				kind = Kind.Grab,
				hand = num,
				grip = GripIndex(grip)
			};
			if (componentInParent != null && s.spawned.TryGetValue(componentInParent.Pointer, out var value))
			{
				@event.spawn = value;
				@event.name = "|" + grip.name;
			}
			else
			{
				Transform transform = ((componentInParent != null) ? componentInParent.transform : grip.transform.root);
				@event.barcode = ((componentInParent != null && componentInParent.SpawnableCrate != null) ? componentInParent.SpawnableCrate.Barcode.ID : "");
				IntPtr key = ((componentInParent != null) ? componentInParent.Pointer : transform.Pointer);
				if (!s.things.TryGetValue(key, out var value2))
				{
					value2 = s.things.Count;
					s.things[key] = value2;
				}
				@event.name = transform.name + "|" + grip.name + "|" + value2;
				@event.at = transform.position;
				@event.turn = transform.rotation;
				@event.spawn = -1;
			}
			string text = ((componentInParent != null) ? componentInParent.gameObject.name : grip.name).ToUpperInvariant();
			if (!text.Contains("SPAWN") && !text.Contains("NIMBUS"))
			{
				taking.events.Add(@event);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void OnFired(Gun gun)
	{
		if (gun == null || gun.triggerGrip == null || gun.triggerGrip.attachedHands == null)
		{
			return;
		}
		int num = ((!gun.triggerGrip.attachedHands.Contains(Player.LeftHand)) ? (gun.triggerGrip.attachedHands.Contains(Player.RightHand) ? 1 : (-1)) : 0);
		if (num < 0)
		{
			return;
		}
		Session[] array = sessions;
		foreach (Session session in array)
		{
			if (session.taking != null)
			{
				session.taking.events.Add(new Event
				{
					t = Time.time - session.startedAt,
					kind = Kind.Fire,
					hand = num
				});
			}
		}
	}

	public static string ThingName(Event e)
	{
		string text = e.name ?? "";
		int num = text.IndexOf('|');
		if (num < 0)
		{
			return text;
		}
		return text.Substring(0, num);
	}

	public static string GripName(Event e)
	{
		string[] array = (e.name ?? "").Split('|');
		if (array.Length <= 1 || array[1].Length <= 0)
		{
			return null;
		}
		return array[1];
	}

	public static int ThingNumber(Event e)
	{
		string[] array = (e.name ?? "").Split('|');
		if (array.Length <= 2 || !int.TryParse(array[2], out var result))
		{
			return -1;
		}
		return result;
	}

	public static int GripIndex(Grip grip)
	{
		Grip[] array = GripsOf(grip.transform);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == grip)
			{
				return i;
			}
		}
		return -1;
	}

	public static Grip[] GripsOf(Transform part)
	{
		MarrowEntity componentInParent = part.GetComponentInParent<MarrowEntity>();
		return ((componentInParent != null) ? ((Component)componentInParent) : ((Component)part.root)).GetComponentsInChildren<Grip>(includeInactive: true);
	}

	public static void Save(Take take, string file)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		using BinaryWriter binaryWriter = new BinaryWriter(File.Create(file));
		binaryWriter.Write(4);
		V(binaryWriter, take.startFeet);
		binaryWriter.Write(take.startYaw);
		binaryWriter.Write(take.avatar ?? "");
		V(binaryWriter, take.avatarScale);
		binaryWriter.Write(take.item ?? "");
		binaryWriter.Write(take.itemName ?? "");
		binaryWriter.Write(take.level ?? "");
		binaryWriter.Write(take.eyes);
		binaryWriter.Write(take.frames.Count);
		foreach (Frame frame in take.frames)
		{
			binaryWriter.Write(frame.t);
			V(binaryWriter, frame.pelvis);
			V(binaryWriter, frame.pelvisVelocity);
			V(binaryWriter, frame.pelvisSpin);
			Q(binaryWriter, frame.pelvisTurn);
			Q(binaryWriter, frame.playspace);
			P(binaryWriter, frame.head);
			P(binaryWriter, frame.left);
			P(binaryWriter, frame.right);
			H(binaryWriter, frame.leftIn);
			H(binaryWriter, frame.rightIn);
			binaryWriter.Write(frame.aPrimary);
			binaryWriter.Write(frame.aSecondary);
			binaryWriter.Write(frame.crouch);
			binaryWriter.Write(frame.feet);
			V(binaryWriter, frame.root);
			binaryWriter.Write(frame.seated);
			if (frame.seated)
			{
				P(binaryWriter, frame.seat);
			}
		}
		binaryWriter.Write(take.events.Count);
		foreach (Event @event in take.events)
		{
			binaryWriter.Write(@event.t);
			binaryWriter.Write((int)@event.kind);
			binaryWriter.Write(@event.hand);
			binaryWriter.Write(@event.spawn);
			binaryWriter.Write(@event.barcode ?? "");
			binaryWriter.Write(@event.name ?? "");
			V(binaryWriter, @event.at);
			Q(binaryWriter, @event.turn);
			binaryWriter.Write(@event.grip);
		}
	}

	public static Take Load(string file)
	{
		using BinaryReader binaryReader = new BinaryReader(File.OpenRead(file));
		int num = binaryReader.ReadInt32();
		Take take = new Take
		{
			startFeet = V(binaryReader),
			startYaw = binaryReader.ReadSingle()
		};
		if (num >= 2)
		{
			take.avatar = binaryReader.ReadString();
			take.avatarScale = V(binaryReader);
		}
		if (num >= 3)
		{
			take.item = binaryReader.ReadString();
			take.itemName = binaryReader.ReadString();
			take.level = binaryReader.ReadString();
			take.eyes = binaryReader.ReadSingle();
		}
		int num2 = binaryReader.ReadInt32();
		for (int i = 0; i < num2; i++)
		{
			Frame frame = default(Frame);
			frame.t = binaryReader.ReadSingle();
			Frame item = frame;
			item.pelvis = V(binaryReader);
			item.pelvisVelocity = V(binaryReader);
			item.pelvisSpin = V(binaryReader);
			item.pelvisTurn = Q(binaryReader);
			item.playspace = Q(binaryReader);
			item.head = P(binaryReader);
			item.left = P(binaryReader);
			item.right = P(binaryReader);
			item.leftIn = H(binaryReader, num);
			item.rightIn = H(binaryReader, num);
			item.aPrimary = binaryReader.ReadBoolean();
			item.aSecondary = binaryReader.ReadBoolean();
			item.crouch = binaryReader.ReadSingle();
			item.feet = binaryReader.ReadSingle();
			if (num >= 4)
			{
				item.root = V(binaryReader);
				item.seated = binaryReader.ReadBoolean();
				if (item.seated)
				{
					item.seat = P(binaryReader);
				}
			}
			take.frames.Add(item);
		}
		int num3 = binaryReader.ReadInt32();
		for (int j = 0; j < num3; j++)
		{
			Event @event = new Event
			{
				t = binaryReader.ReadSingle(),
				kind = (Kind)binaryReader.ReadInt32(),
				hand = binaryReader.ReadInt32(),
				spawn = binaryReader.ReadInt32()
			};
			@event.barcode = binaryReader.ReadString();
			@event.name = binaryReader.ReadString();
			@event.at = V(binaryReader);
			@event.turn = Q(binaryReader);
			@event.grip = binaryReader.ReadInt32();
			take.events.Add(@event);
		}
		return take;
	}

	private static void V(BinaryWriter w, Vector3 v)
	{
		w.Write(v.x);
		w.Write(v.y);
		w.Write(v.z);
	}

	private static void Q(BinaryWriter w, Quaternion q)
	{
		w.Write(q.x);
		w.Write(q.y);
		w.Write(q.z);
		w.Write(q.w);
	}

	private static void P(BinaryWriter w, Bot.Pose p)
	{
		V(w, p.position);
		Q(w, p.rotation);
	}

	private static void H(BinaryWriter w, HandInput h)
	{
		w.Write(h.index);
		w.Write(h.middle);
		w.Write(h.ring);
		w.Write(h.pinky);
		w.Write(h.thumb);
		w.Write(h.grip);
		w.Write(h.trigger);
		w.Write(h.primary);
		w.Write(h.secondary);
		w.Write(h.stick.x);
		w.Write(h.stick.y);
		w.Write(h.a);
		w.Write(h.b);
		w.Write(h.menu);
		w.Write(h.click);
		w.Write(h.pad);
		w.Write(h.padAxis.x);
		w.Write(h.padAxis.y);
	}

	private static Vector3 V(BinaryReader r)
	{
		return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	private static Quaternion Q(BinaryReader r)
	{
		return new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	private static Bot.Pose P(BinaryReader r)
	{
		return new Bot.Pose(V(r), Q(r));
	}

	private static HandInput H(BinaryReader r, int version)
	{
		HandInput handInput = default(HandInput);
		handInput.index = r.ReadSingle();
		handInput.middle = r.ReadSingle();
		handInput.ring = r.ReadSingle();
		handInput.pinky = r.ReadSingle();
		handInput.thumb = r.ReadSingle();
		handInput.grip = r.ReadSingle();
		handInput.trigger = r.ReadSingle();
		handInput.primary = r.ReadBoolean();
		handInput.secondary = r.ReadBoolean();
		HandInput result = handInput;
		result.stick = new Vector2(r.ReadSingle(), r.ReadSingle());
		if (version >= 3)
		{
			result.a = r.ReadBoolean();
			result.b = r.ReadBoolean();
			result.menu = r.ReadBoolean();
			result.click = r.ReadBoolean();
			result.pad = r.ReadBoolean();
			result.padAxis = new Vector2(r.ReadSingle(), r.ReadSingle());
		}
		return result;
	}
}
