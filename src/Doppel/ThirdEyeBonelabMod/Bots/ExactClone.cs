using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class ExactClone
{
	public class Frame
	{
		public float t;

		public Bot.Pose root;

		public Bot.Pose head;

		public Bot.Pose left;

		public Bot.Pose right;

		public Vector2 move;

		public bool moving;

		public float[] leftIn;

		public float[] rightIn;

		public Vector3 pelvis;

		public Bot.Pose leftHand;

		public Bot.Pose rightHand;

		public int leftHeld = -1;

		public int rightHeld = -1;
	}

	public class Item
	{
		public int id;

		public string barcode;

		public Bot.Pose at;

		public float spawnT = -1f;

		public bool pouch;

		public int pouchHand = -1;

		public string pouchGrip;

		public float pouchT;

		public bool touched;

		public Bot.Pose handAt;
	}

	public class Grab
	{
		public float t;

		public int hand;

		public int item;

		public string grip;

		public bool on;

		public IntPtr thing;
	}

	public class Take
	{
		public string avatar;

		public Vector3 avatarScale = Vector3.one;

		public Vector3 startFeet;

		public string[] fields;

		public readonly List<Frame> frames = new List<Frame>();

		public readonly List<Item> items = new List<Item>();

		public readonly List<Grab> grabs = new List<Grab>();

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

	private enum Sort
	{
		Float,
		Bool,
		Int,
		Vector2
	}

	private static PropertyInfo[] props;

	private static Sort[] sorts;

	private static int width;

	public static Take Last;

	private static Take taking;

	private static float startedAt;

	private static Vector2 move;

	private static bool moving;

	private static int nextId;

	private static readonly Dictionary<IntPtr, Item> live = new Dictionary<IntPtr, Item>();

	private static float suppressUntil;

	private const int Version = 2;

	public static string[] FieldNames => Array.ConvertAll(props, (PropertyInfo p) => p.Name);

	public static bool Recording => taking != null;

	public static void Fields()
	{
		if (props != null)
		{
			return;
		}
		List<PropertyInfo> list = new List<PropertyInfo>();
		List<Sort> list2 = new List<Sort>();
		PropertyInfo[] properties = typeof(BaseController).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!propertyInfo.Name.StartsWith("_") || !propertyInfo.CanRead || !propertyInfo.CanWrite || propertyInfo.GetIndexParameters().Length != 0)
			{
				continue;
			}
			string text = propertyInfo.Name.ToLowerInvariant();
			if (!text.Contains("haptic") && !text.Contains("menu"))
			{
				Type propertyType = propertyInfo.PropertyType;
				Sort? sort = ((propertyType == typeof(float)) ? new Sort?(Sort.Float) : ((propertyType == typeof(bool)) ? new Sort?(Sort.Bool) : ((propertyType == typeof(int)) ? new Sort?(Sort.Int) : ((propertyType == typeof(Vector2)) ? new Sort?(Sort.Vector2) : null))));
				if (sort.HasValue)
				{
					list.Add(propertyInfo);
					list2.Add(sort.Value);
				}
			}
		}
		props = list.ToArray();
		sorts = list2.ToArray();
		width = 0;
		Sort[] array = sorts;
		foreach (Sort sort2 in array)
		{
			width += ((sort2 != Sort.Vector2) ? 1 : 2);
		}
		MelonLogger.Msg("Bot Player: exact clone copies " + props.Length + " controller values: " + string.Join(" ", Array.ConvertAll(props, (PropertyInfo p) => p.Name)));
	}

	private static float[] Read(BaseController c)
	{
		float[] array = new float[width];
		if ((Object)(object)c == (Object)null)
		{
			return array;
		}
		int num = 0;
		for (int i = 0; i < props.Length; i++)
		{
			try
			{
				object value = props[i].GetValue(c);
				switch (sorts[i])
				{
				case Sort.Float:
					array[num] = (float)value;
					break;
				case Sort.Bool:
					array[num] = (((bool)value) ? 1f : 0f);
					break;
				case Sort.Int:
					array[num] = (int)value;
					break;
				case Sort.Vector2:
				{
					Vector2 val = (Vector2)value;
					array[num] = val.x;
					array[num + 1] = val.y;
					break;
				}
				}
			}
			catch (Exception)
			{
			}
			num += ((sorts[i] != Sort.Vector2) ? 1 : 2);
		}
		return array;
	}

	public static void Put(BaseController c, Take take, int from, int a, int b, float k, bool left)
	{
		if ((Object)(object)c == (Object)null || props == null || take.fields == null)
		{
			return;
		}
		float[] array = (left ? take.frames[a].leftIn : take.frames[a].rightIn);
		float[] array2 = (left ? take.frames[b].leftIn : take.frames[b].rightIn);
		int num = 0;
		for (int i = 0; i < props.Length; i++)
		{
			try
			{
				switch (sorts[i])
				{
				case Sort.Float:
					props[i].SetValue(c, Mathf.Lerp(array[num], array2[num], k));
					break;
				case Sort.Int:
					props[i].SetValue(c, (int)array[num]);
					break;
				case Sort.Vector2:
					props[i].SetValue(c, Vector2.Lerp(new Vector2(array[num], array[num + 1]), new Vector2(array2[num], array2[num + 1]), k));
					break;
				case Sort.Bool:
				{
					bool flag = array[num] > 0.5f;
					string name = props[i].Name;
					if (!flag && (name.EndsWith("Down") || name.EndsWith("Up") || name.EndsWith("Tap")))
					{
						for (int j = Mathf.Max(from + 1, 0); j <= a; j++)
						{
							if (flag)
							{
								break;
							}
							flag = (left ? take.frames[j].leftIn : take.frames[j].rightIn)[num] > 0.5f;
						}
					}
					props[i].SetValue(c, flag);
					break;
				}
				}
			}
			catch (Exception)
			{
			}
			num += ((sorts[i] != Sort.Vector2) ? 1 : 2);
		}
	}

	public static void Suppress()
	{
		suppressUntil = Time.time + 1f;
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
		RigManager rigManager = Player.RigManager;
		if ((Object)(object)rigManager == (Object)null || taking != null)
		{
			return;
		}
		Fields();
		taking = new Take
		{
			fields = FieldNames
		};
		startedAt = Time.time;
		nextId = 0;
		live.Clear();
		Rig physicsRig = (Rig)(object)Player.PhysicsRig;
		taking.startFeet = (((Object)(object)physicsRig != (Object)null && (Object)(object)physicsRig.centerOfPressure != (Object)null) ? physicsRig.centerOfPressure.position : (Player.Head.position - Vector3.up * 1.6f));
		taking.avatar = BotRig.AvatarOf(rigManager);
		if ((Object)(object)rigManager.avatar != (Object)null)
		{
			taking.avatarScale = ((Component)rigManager.avatar).transform.localScale;
		}
		foreach (Poolee item in Object.FindObjectsOfType<Poolee>())
		{
			if (!((Object)(object)item == (Object)null) && !((Object)(object)item.SpawnableCrate == (Object)null) && ((Component)item).gameObject.activeInHierarchy && !((Object)(object)((Component)item).GetComponentInParent<RigManager>() != (Object)null))
			{
				live[((Il2CppObjectBase)(object)item).Pointer] = new Item
				{
					id = nextId++,
					barcode = ((Scannable)item.SpawnableCrate).Barcode.ID,
					at = Bot.Pose.Of(((Component)item).transform)
				};
			}
		}
		for (int i = 0; i < 2; i++)
		{
			Hand val = ((i == 0) ? Player.LeftHand : Player.RightHand);
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			Poolee val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponentInParent<Poolee>() : null);
			Grip val4 = (((Object)(object)val2 != (Object)null) ? (val2.GetComponent<Grip>() ?? val2.GetComponentInParent<Grip>()) : null);
			if (!((Object)(object)val3 == (Object)null) && !((Object)(object)val4 == (Object)null) && live.TryGetValue(((Il2CppObjectBase)(object)val3).Pointer, out var value))
			{
				value.touched = (value.pouch = true);
				value.pouchHand = i;
				value.pouchGrip = ((Object)val4).name;
				value.pouchT = 0f;
				value.handAt = Bot.Pose.Of(((Component)val).transform);
			}
		}
		Spawnables.Notify("Recording an exact clone of you");
		Lab.Event("exact: recording (" + live.Count + " things about)");
	}

	public static void Stop()
	{
		Take take = taking;
		taking = null;
		if (take == null)
		{
			return;
		}
		HashSet<Item> hashSet = new HashSet<Item>();
		foreach (Item value in live.Values)
		{
			if (value.touched)
			{
				hashSet.Add(value);
			}
		}
		foreach (Item item in take.items)
		{
			if (item.touched)
			{
				hashSet.Add(item);
			}
		}
		take.items.Clear();
		take.items.AddRange(hashSet);
		take.items.Sort((Item x, Item y) => x.spawnT.CompareTo(y.spawnT));
		live.Clear();
		if (take.frames.Count < 10)
		{
			Spawnables.Notify("Exact clone too short, not kept");
			return;
		}
		Last = take;
		try
		{
			Save(take, Path.Combine(AssetFiles.SaveFolder("clones"), "exact_last.bin"));
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: exact clone not saved: " + ex.Message);
		}
		int num = 0;
		foreach (Item item2 in take.items)
		{
			if (item2.pouch)
			{
				num++;
			}
		}
		Spawnables.Notify("Exact clone kept: " + take.Length.ToString("0") + " s");
		Lab.Event("exact: kept " + take.Length.ToString("0.0") + " s, " + take.frames.Count + " frames, " + take.items.Count + " things (" + num + " from your pouch), " + take.grabs.Count + " grabs and lets go");
	}

	public static void NoteMove(Vector2 axis, bool pressed)
	{
		move = axis;
		moving = pressed;
	}

	public static void NoteFrame(OpenControllerRig controls)
	{
		Take take = taking;
		if (take == null || (Object)(object)controls == (Object)null || (Object)(object)controls.vrRoot == (Object)null || (Object)(object)controls.headset == (Object)null)
		{
			return;
		}
		float num = Time.time - startedAt;
		if (take.frames.Count > 0 && num <= take.frames[take.frames.Count - 1].t)
		{
			return;
		}
		if (num > 600f)
		{
			Stop();
			return;
		}
		Bot.Pose root = Bot.Pose.Of(controls.vrRoot);
		Bot.Pose pose = root.Inverse();
		Frame frame = new Frame
		{
			t = num,
			root = root,
			head = pose * Bot.Pose.Of(controls.headset),
			left = pose * Bot.Pose.Of(((Component)((ControllerRig)controls).leftController).transform),
			right = pose * Bot.Pose.Of(((Component)((ControllerRig)controls).rightController).transform),
			move = move,
			moving = moving,
			leftIn = Read(((ControllerRig)controls).leftController),
			rightIn = Read(((ControllerRig)controls).rightController)
		};
		Rig physicsRig = (Rig)(object)Player.PhysicsRig;
		if ((Object)(object)physicsRig != (Object)null && (Object)(object)physicsRig.m_pelvis != (Object)null)
		{
			frame.pelvis = physicsRig.m_pelvis.position;
		}
		Hand leftHand = Player.LeftHand;
		Hand rightHand = Player.RightHand;
		if ((Object)(object)leftHand != (Object)null)
		{
			frame.leftHand = Bot.Pose.Of(((Component)leftHand).transform);
			frame.leftHeld = HeldId(leftHand);
		}
		if ((Object)(object)rightHand != (Object)null)
		{
			frame.rightHand = Bot.Pose.Of(((Component)rightHand).transform);
			frame.rightHeld = HeldId(rightHand);
		}
		take.frames.Add(frame);
	}

	private static int HeldId(Hand hand)
	{
		GameObject currentAttachedGO = hand.m_CurrentAttachedGO;
		Poolee val = (((Object)(object)currentAttachedGO != (Object)null) ? currentAttachedGO.GetComponentInParent<Poolee>() : null);
		if (!((Object)(object)val != (Object)null) || !live.TryGetValue(((Il2CppObjectBase)(object)val).Pointer, out var value))
		{
			return -1;
		}
		return value.id;
	}

	public static void OnSpawn(Poolee p)
	{
		Take take = taking;
		if (take == null || (Object)(object)p == (Object)null || (Object)(object)p.SpawnableCrate == (Object)null || Time.time < suppressUntil)
		{
			return;
		}
		string iD = ((Scannable)p.SpawnableCrate).Barcode.ID;
		if (!Clone.IsBody(iD, ((Object)((Component)p).gameObject).name))
		{
			Item item = new Item
			{
				id = nextId++,
				barcode = iD,
				at = Bot.Pose.Of(((Component)p).transform),
				spawnT = Time.time - startedAt
			};
			if (live.TryGetValue(((Il2CppObjectBase)(object)p).Pointer, out var value) && value.touched && !take.items.Contains(value))
			{
				take.items.Add(value);
			}
			live[((Il2CppObjectBase)(object)p).Pointer] = item;
			take.items.Add(item);
		}
	}

	public static void OnGrip(Grip grip, Hand hand, bool on)
	{
		Take take = taking;
		if (take == null || (Object)(object)grip == (Object)null || (Object)(object)hand == (Object)null)
		{
			return;
		}
		int num = ((!((Object)(object)hand == (Object)(object)Player.LeftHand)) ? (((Object)(object)hand == (Object)(object)Player.RightHand) ? 1 : (-1)) : 0);
		if (num < 0 || (Object)(object)((Component)grip).GetComponentInParent<RigManager>() != (Object)null)
		{
			return;
		}
		Poolee componentInParent = ((Component)grip).GetComponentInParent<Poolee>();
		if ((Object)(object)componentInParent == (Object)null || !live.TryGetValue(((Il2CppObjectBase)(object)componentInParent).Pointer, out var value))
		{
			return;
		}
		float num2 = Time.time - startedAt;
		if (on)
		{
			if (!value.touched && value.spawnT >= 0f && num2 - value.spawnT < 0.25f)
			{
				Vector3 val = value.at.position - ((Component)hand).transform.position;
				if (((Vector3)(ref val)).magnitude < 0.3f)
				{
					value.pouch = true;
					value.pouchHand = num;
					value.pouchGrip = ((Object)grip).name;
					value.pouchT = num2;
				}
			}
			value.touched = true;
		}
		take.grabs.Add(new Grab
		{
			t = num2,
			hand = num,
			item = value.id,
			grip = ((Object)grip).name,
			on = on
		});
	}

	private static void Save(Take take, string file)
	{
		using BinaryWriter binaryWriter = new BinaryWriter(File.Create(file));
		binaryWriter.Write(2);
		binaryWriter.Write(take.avatar ?? "");
		V(binaryWriter, take.avatarScale);
		V(binaryWriter, take.startFeet);
		binaryWriter.Write(take.fields.Length);
		string[] fields = take.fields;
		foreach (string value in fields)
		{
			binaryWriter.Write(value);
		}
		binaryWriter.Write(take.frames.Count);
		foreach (Frame frame in take.frames)
		{
			binaryWriter.Write(frame.t);
			P(binaryWriter, frame.root);
			P(binaryWriter, frame.head);
			P(binaryWriter, frame.left);
			P(binaryWriter, frame.right);
			binaryWriter.Write(frame.move.x);
			binaryWriter.Write(frame.move.y);
			binaryWriter.Write(frame.moving);
			binaryWriter.Write(frame.leftIn.Length);
			float[] leftIn = frame.leftIn;
			foreach (float value2 in leftIn)
			{
				binaryWriter.Write(value2);
			}
			leftIn = frame.rightIn;
			foreach (float value3 in leftIn)
			{
				binaryWriter.Write(value3);
			}
			V(binaryWriter, frame.pelvis);
			P(binaryWriter, frame.leftHand);
			P(binaryWriter, frame.rightHand);
			binaryWriter.Write(frame.leftHeld);
			binaryWriter.Write(frame.rightHeld);
		}
		binaryWriter.Write(take.items.Count);
		foreach (Item item in take.items)
		{
			binaryWriter.Write(item.id);
			binaryWriter.Write(item.barcode);
			P(binaryWriter, item.at);
			binaryWriter.Write(item.spawnT);
			binaryWriter.Write(item.pouch);
			binaryWriter.Write(item.pouchHand);
			binaryWriter.Write(item.pouchGrip ?? "");
			binaryWriter.Write(item.pouchT);
			P(binaryWriter, item.handAt);
		}
		binaryWriter.Write(take.grabs.Count);
		foreach (Grab grab in take.grabs)
		{
			binaryWriter.Write(grab.t);
			binaryWriter.Write(grab.hand);
			binaryWriter.Write(grab.item);
			binaryWriter.Write(grab.grip ?? "");
			binaryWriter.Write(grab.on);
		}
	}

	public static Take Load(string file)
	{
		using BinaryReader binaryReader = new BinaryReader(File.OpenRead(file));
		if (binaryReader.ReadInt32() != 2)
		{
			return null;
		}
		Take take = new Take
		{
			avatar = binaryReader.ReadString(),
			avatarScale = V(binaryReader),
			startFeet = V(binaryReader)
		};
		take.fields = new string[binaryReader.ReadInt32()];
		for (int i = 0; i < take.fields.Length; i++)
		{
			take.fields[i] = binaryReader.ReadString();
		}
		int num = binaryReader.ReadInt32();
		for (int j = 0; j < num; j++)
		{
			Frame frame = new Frame
			{
				t = binaryReader.ReadSingle(),
				root = P(binaryReader),
				head = P(binaryReader),
				left = P(binaryReader),
				right = P(binaryReader)
			};
			frame.move = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
			frame.moving = binaryReader.ReadBoolean();
			int num2 = binaryReader.ReadInt32();
			frame.leftIn = new float[num2];
			frame.rightIn = new float[num2];
			for (int k = 0; k < num2; k++)
			{
				frame.leftIn[k] = binaryReader.ReadSingle();
			}
			for (int l = 0; l < num2; l++)
			{
				frame.rightIn[l] = binaryReader.ReadSingle();
			}
			frame.pelvis = V(binaryReader);
			frame.leftHand = P(binaryReader);
			frame.rightHand = P(binaryReader);
			frame.leftHeld = binaryReader.ReadInt32();
			frame.rightHeld = binaryReader.ReadInt32();
			take.frames.Add(frame);
		}
		num = binaryReader.ReadInt32();
		for (int m = 0; m < num; m++)
		{
			take.items.Add(new Item
			{
				id = binaryReader.ReadInt32(),
				barcode = binaryReader.ReadString(),
				at = P(binaryReader),
				spawnT = binaryReader.ReadSingle(),
				pouch = binaryReader.ReadBoolean(),
				pouchHand = binaryReader.ReadInt32(),
				pouchGrip = binaryReader.ReadString(),
				pouchT = binaryReader.ReadSingle(),
				handAt = P(binaryReader),
				touched = true
			});
		}
		num = binaryReader.ReadInt32();
		for (int n = 0; n < num; n++)
		{
			take.grabs.Add(new Grab
			{
				t = binaryReader.ReadSingle(),
				hand = binaryReader.ReadInt32(),
				item = binaryReader.ReadInt32(),
				grip = binaryReader.ReadString(),
				on = binaryReader.ReadBoolean()
			});
		}
		return take;
	}

	public static Take Latest()
	{
		if (Last != null)
		{
			return Last;
		}
		try
		{
			string text = Path.Combine(AssetFiles.SaveFolder("clones"), "exact_last.bin");
			if (File.Exists(text))
			{
				Last = Load(text);
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: exact clone not read: " + ex.Message);
		}
		return Last;
	}

	private static void V(BinaryWriter w, Vector3 v)
	{
		w.Write(v.x);
		w.Write(v.y);
		w.Write(v.z);
	}

	private static void P(BinaryWriter w, Bot.Pose p)
	{
		V(w, p.position);
		w.Write(p.rotation.x);
		w.Write(p.rotation.y);
		w.Write(p.rotation.z);
		w.Write(p.rotation.w);
	}

	private static Vector3 V(BinaryReader r)
	{
		return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	private static Bot.Pose P(BinaryReader r)
	{
		return new Bot.Pose(V(r), new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()));
	}
}
