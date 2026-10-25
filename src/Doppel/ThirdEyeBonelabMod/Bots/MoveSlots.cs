using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BoneLib;
using BoneLib.BoneMenu;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.VRMK;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class MoveSlots
{
	public enum Kind
	{
		Idle,
		Walk,
		FastWalk,
		Run,
		Jump,
		Crouch,
		Climb,
		Wave,
		Reload,
		Fire,
		Punch,
		Melee,
		PickUp
	}

	public class Slot
	{
		public bool filled;

		public Frame[] clip;

		public float length;

		public float speed;

		public float jumpAt;

		public string gun = "";

		public string gunName = "";

		public int count;

		public float eyes;
	}

	public class Set
	{
		public string avatar = "";

		public string name = "";

		public string folder = "";

		public int id;

		public List<Slot>[] slots;

		public int Filled
		{
			get
			{
				int num = 0;
				List<Slot>[] array = slots;
				for (int i = 0; i < array.Length; i++)
				{
					foreach (Slot item in array[i])
					{
						num += (item.filled ? 1 : 0);
					}
				}
				return num;
			}
		}

		public bool Has(Kind kind)
		{
			foreach (Slot item in slots[(int)kind])
			{
				if (item.clip != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	public enum Button
	{
		LeftStickClick,
		RightStickClick,
		LeftX,
		LeftY,
		RightA,
		RightB,
		LeftTrigger,
		RightTrigger,
		LeftGrip,
		RightGrip,
		None
	}

	public enum Press
	{
		Hold,
		DoubleTap
	}

	public const int Each = 5;

	public const int Most = 20;

	private static readonly string[] Names = new string[13]
	{
		"Idle", "Walk", "Fast walk", "Run", "Jump", "Crouch", "Climb", "Wave", "Reload", "Fire",
		"Punch", "Melee", "Pick up"
	};

	private static readonly string[] Hints = new string[13]
	{
		"stand as you do, hands empty", "walk slowly", "walk at your usual pace", "sprint (click the stick)", "jump once", "crouch down, stay, and stand up", "climb up onto a ledge", "wave", "reload the gun in your hand", "aim and fire the gun in your hand",
		"punch at something in front of you, hands empty", "hit at something in front of you with the weapon in your hand", "bend down, pick something up off the ground, and stand up with it"
	};

	private static Dictionary<string, Set> sets;

	private static readonly Dictionary<Frame[], Slot> byClip = new Dictionary<Frame[], Slot>();

	private static Set editing;

	private static float lookedAt = -10f;

	private static int armedKind = -1;

	private static int armedSlot;

	private static bool recording;

	private static bool counting;

	private static float countUntil;

	private static float startedAt;

	private static float nextFrameAt;

	private static float lastJumpSeen;

	private static int counted;

	private static readonly List<Frame> taking = new List<Frame>();

	private static float bothSince = -1f;

	private static bool bothUsed;

	private static bool taughtBefore;

	private static int climbsBefore;

	private static int reloadsBefore;

	private static int aimsBefore;

	private static float shotsBefore;

	private static Button firstButton = Button.LeftStickClick;

	private static Button secondButton = Button.RightStickClick;

	private static Press pressKind = Press.Hold;

	private static float holdFor = 0.7f;

	private static bool keysRead;

	private static float tappedAt = -10f;

	private static float endedAt = -10f;

	private static TextMeshPro hud;

	private static string hudNote = "";

	private static float hudNoteUntil;

	private static Page movesPage;

	private static readonly Page[] kindPages = new Page[32];

	private static string Root => AssetFiles.SaveFolder("moves");

	private static Dictionary<string, Set> Sets
	{
		get
		{
			if (sets != null)
			{
				return sets;
			}
			sets = new Dictionary<string, Set>();
			byClip.Clear();
			try
			{
				if (Directory.Exists(Root))
				{
					string[] directories = Directory.GetDirectories(Root);
					foreach (string text in directories)
					{
						string path = Path.Combine(text, "set.txt");
						if (File.Exists(path))
						{
							string[] array = File.ReadAllLines(path);
							if (array.Length != 0 && array[0].Length != 0)
							{
								Set set = Make(array[0], (array.Length > 1) ? array[1] : array[0], text);
								sets[set.avatar] = set;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Bot Player: the moves could not be read: " + ex.Message);
			}
			return sets;
		}
	}

	private static Set Editing
	{
		get
		{
			if (editing != null && Time.time - lookedAt < 1f)
			{
				return editing;
			}
			lookedAt = Time.time;
			PlayerAvatar(out var code, out var name, out var _);
			if (editing != null && editing.avatar == code)
			{
				return editing;
			}
			if (!Sets.TryGetValue(code, out var value))
			{
				value = Make(code, name, Path.Combine(Root, Safe(code)));
				Sets[code] = value;
			}
			bool flag = editing != null && editing.avatar != "default";
			bool num = editing != null && !flag;
			editing = value;
			if (num)
			{
				Menu();
			}
			if (flag)
			{
				if (recording || counting)
				{
					recording = (counting = false);
					GunMoves.Teaching = taughtBefore;
					Spawnables.Notify("Recording called off: you changed avatar");
				}
				armedKind = -1;
				Spawnables.Notify("Moves: now those of " + value.name + " (" + value.Filled + " recorded)");
				Menu();
			}
			return editing;
		}
	}

	private static List<Slot>[] Slots => Editing.slots;

	private static string Folder => Editing.folder;

	private static string KeysFile => Path.Combine(AssetFiles.SaveFolder("moves"), "record_keys.txt");

	private static bool Two
	{
		get
		{
			if (secondButton != Button.None)
			{
				return secondButton != firstButton;
			}
			return false;
		}
	}

	private static float SignalTakes
	{
		get
		{
			if (pressKind != 0)
			{
				return 0.5f;
			}
			return holdFor;
		}
	}

	public static int Tag(Kind kind, int slot)
	{
		return Editing.id * 1000 + (int)kind * 100 + slot;
	}

	private static string Safe(string code)
	{
		char[] array = code.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (!char.IsLetterOrDigit(array[i]) && array[i] != '.' && array[i] != '-')
			{
				array[i] = '_';
			}
		}
		return new string(array);
	}

	private static int IdOf(string code)
	{
		uint num = 2166136261u;
		foreach (char c in code)
		{
			num = (num ^ c) * 16777619;
		}
		return (int)(num % 100000 + 1);
	}

	private static Set Make(string avatar, string name, string folder)
	{
		Set set = new Set
		{
			avatar = avatar,
			name = name,
			folder = folder,
			id = IdOf(avatar),
			slots = new List<Slot>[Names.Length]
		};
		int[] array = ReadCounts(set);
		for (int i = 0; i < Names.Length; i++)
		{
			set.slots[i] = new List<Slot>();
			for (int j = 0; j < array[i]; j++)
			{
				set.slots[i].Add(new Slot());
				try
				{
					Read(set, (Kind)i, j);
				}
				catch (Exception ex)
				{
					string[] obj = new string[8] { "Bot Player: a move could not be read (", name, ", ", null, null, null, null, null };
					Kind kind = (Kind)i;
					obj[3] = kind.ToString();
					obj[4] = " ";
					obj[5] = (j + 1).ToString();
					obj[6] = "): ";
					obj[7] = ex.Message;
					MelonLogger.Warning(string.Concat(obj));
				}
			}
		}
		return set;
	}

	private static void PlayerAvatar(out string code, out string name, out float eyes)
	{
		code = "default";
		name = "your avatar";
		eyes = 0f;
		try
		{
			RigManager rigManager = Player.RigManager;
			if (!(rigManager == null))
			{
				string text = BotRig.AvatarOf(rigManager);
				if (!string.IsNullOrEmpty(text))
				{
					code = text;
				}
				Il2CppSLZ.VRMK.Avatar avatar = rigManager.avatar;
				if (avatar != null)
				{
					name = avatar.name.Replace("(Clone)", "").Trim();
					eyes = avatar.eyeHeight * avatar.transform.lossyScale.y;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static string FileOf(Kind kind, int i)
	{
		return FileOf(Editing, kind, i);
	}

	private static string FileOf(Set set, Kind kind, int i)
	{
		return Path.Combine(set.folder, kind.ToString() + "_" + (i + 1) + ".csv");
	}

	private static int[] ReadCounts(Set set)
	{
		int[] array = new int[Names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 5;
		}
		try
		{
			string path = Path.Combine(set.folder, "slots.txt");
			if (File.Exists(path))
			{
				string[] array2 = File.ReadAllLines(path);
				for (int j = 0; j < array2.Length; j++)
				{
					string[] array3 = array2[j].Split('=');
					if (array3.Length == 2 && Enum.TryParse<Kind>(array3[0], out var result) && int.TryParse(array3[1], out var result2))
					{
						array[(int)result] = Mathf.Clamp(result2, 1, 20);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return array;
	}

	private static void EnsureFolder(Set set)
	{
		Directory.CreateDirectory(set.folder);
		string path = Path.Combine(set.folder, "set.txt");
		if (!File.Exists(path))
		{
			File.WriteAllLines(path, new string[2] { set.avatar, set.name });
		}
	}

	private static void WriteCounts()
	{
		try
		{
			Set set = Editing;
			EnsureFolder(set);
			List<string> list = new List<string>();
			for (int i = 0; i < Names.Length; i++)
			{
				Kind kind = (Kind)i;
				list.Add(kind.ToString() + "=" + set.slots[i].Count);
			}
			File.WriteAllLines(Path.Combine(set.folder, "slots.txt"), list);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the number of slots could not be saved: " + ex.Message);
		}
	}

	public static void Reload()
	{
		sets = null;
		editing = null;
		armedKind = -1;
		Menu();
	}

	private static void Read(Set set, Kind kind, int i)
	{
		string text = FileOf(set, kind, i);
		if (!File.Exists(text))
		{
			return;
		}
		Slot slot = set.slots[(int)kind][i];
		foreach (string item in File.ReadLines(text))
		{
			if (!item.StartsWith("#,", StringComparison.Ordinal))
			{
				break;
			}
			string[] array = item.Substring(2).Split(';');
			foreach (string text2 in array)
			{
				int num = text2.IndexOf('=');
				if (num > 0)
				{
					string text3 = text2.Substring(0, num);
					string text4 = text2.Substring(num + 1);
					switch (text3)
					{
					case "length":
						slot.length = float.Parse(text4, CultureInfo.InvariantCulture);
						break;
					case "speed":
						slot.speed = float.Parse(text4, CultureInfo.InvariantCulture);
						break;
					case "jumpAt":
						slot.jumpAt = float.Parse(text4, CultureInfo.InvariantCulture);
						break;
					case "eyes":
						slot.eyes = float.Parse(text4, CultureInfo.InvariantCulture);
						break;
					case "gun":
						slot.gun = text4;
						break;
					case "gunName":
						slot.gunName = text4;
						break;
					case "count":
						slot.count = int.Parse(text4, CultureInfo.InvariantCulture);
						break;
					}
				}
			}
		}
		List<Frame> list = Recorder.Load(text);
		if (list.Count > 1)
		{
			slot.clip = list.ToArray();
			byClip[slot.clip] = slot;
		}
		slot.filled = slot.clip != null || slot.count > 0;
	}

	private static void Write(Kind kind, int i, Slot slot)
	{
		EnsureFolder(Editing);
		using StreamWriter streamWriter = new StreamWriter(FileOf(kind, i), append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		streamWriter.WriteLine("#,length=" + slot.length.ToString("0.###", CultureInfo.InvariantCulture) + ";speed=" + slot.speed.ToString("0.###", CultureInfo.InvariantCulture) + ";jumpAt=" + slot.jumpAt.ToString("0.###", CultureInfo.InvariantCulture) + ";eyes=" + slot.eyes.ToString("0.###", CultureInfo.InvariantCulture) + ";gun=" + slot.gun + ";gunName=" + slot.gunName.Replace(';', ' ').Replace('=', ' ') + ";count=" + slot.count.ToString(CultureInfo.InvariantCulture));
		if (slot.clip != null)
		{
			Frame[] clip = slot.clip;
			foreach (Frame f in clip)
			{
				streamWriter.WriteLine(Recorder.Write(f));
			}
		}
	}

	public static bool Any(Kind kind)
	{
		foreach (Slot item in Slots[(int)kind])
		{
			if (item.filled)
			{
				return true;
			}
		}
		return false;
	}

	private static Frame[] PickIn(Set set, Kind kind, List<Frame[]> lately)
	{
		if (set == null)
		{
			return null;
		}
		List<Frame[]> list = new List<Frame[]>();
		List<Frame[]> list2 = new List<Frame[]>();
		foreach (Slot item in set.slots[(int)kind])
		{
			if (item.clip != null)
			{
				list.Add(item.clip);
				if (lately == null || !lately.Contains(item.clip))
				{
					list2.Add(item.clip);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		List<Frame[]> list3 = ((list2.Count > 0) ? list2 : list);
		return list3[UnityEngine.Random.Range(0, list3.Count)];
	}

	public static Frame[] Pick(Kind kind, List<Frame[]> lately, string avatar)
	{
		Set value = null;
		if (!string.IsNullOrEmpty(avatar))
		{
			Sets.TryGetValue(avatar, out value);
		}
		Frame[] array = PickIn(value, kind, lately);
		if (array != null)
		{
			return array;
		}
		Set set = null;
		foreach (Set value2 in Sets.Values)
		{
			if (value2 != value && value2.Has(kind) && (set == null || value2.Filled > set.Filled))
			{
				set = value2;
			}
		}
		return PickIn(set, kind, lately);
	}

	public static Frame[] For(MotionLibrary.Gait gait, bool slow, List<Frame[]> lately, string avatar)
	{
		return gait switch
		{
			MotionLibrary.Gait.Idle => Pick(Kind.Idle, lately, avatar), 
			MotionLibrary.Gait.Walk => Pick(slow ? Kind.Walk : Kind.FastWalk, lately, avatar) ?? Pick((!slow) ? Kind.Walk : Kind.FastWalk, lately, avatar) ?? Pick(Kind.Run, lately, avatar), 
			_ => Pick(Kind.Run, lately, avatar) ?? Pick(Kind.FastWalk, lately, avatar) ?? Pick(Kind.Walk, lately, avatar), 
		};
	}

	public static bool IsSlot(Frame[] clip)
	{
		if (clip != null)
		{
			return byClip.ContainsKey(clip);
		}
		return false;
	}

	public static float SpeedOf(Frame[] clip)
	{
		if (clip == null || !byClip.TryGetValue(clip, out var value))
		{
			return 0f;
		}
		return value.speed;
	}

	public static float JumpAt(Frame[] clip)
	{
		if (clip == null || !byClip.TryGetValue(clip, out var value))
		{
			return 0f;
		}
		return value.jumpAt;
	}

	public static float Size(Frame[] clip, float eyes)
	{
		if (clip == null || eyes < 0.3f || !byClip.TryGetValue(clip, out var value))
		{
			return 1f;
		}
		float num = ((value.eyes > 0.3f) ? value.eyes : clip[0].height);
		if (!(num > 0.3f))
		{
			return 1f;
		}
		return Mathf.Clamp(eyes / num, 0.3f, 3.5f);
	}

	public static string EmptyKinds()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < Names.Length; i++)
		{
			if (!Any((Kind)i))
			{
				list.Add(Names[i]);
			}
		}
		return string.Join(", ", list);
	}

	public static string Summary()
	{
		int num = 0;
		List<string> list = new List<string>();
		for (int i = 0; i < Names.Length; i++)
		{
			int num2 = 0;
			for (int j = 0; j < Slots[i].Count; j++)
			{
				num2 += (Slots[i][j].filled ? 1 : 0);
			}
			num += num2;
			if (num2 > 0)
			{
				list.Add(Names[i] + " " + num2);
			}
		}
		int num3 = 0;
		foreach (Set value in Sets.Values)
		{
			num3 += ((value != Editing && value.Filled > 0) ? 1 : 0);
		}
		return ((num == 0) ? ("no moves recorded for " + Editing.name + " yet") : ("moves of " + Editing.name + ": " + string.Join(", ", list))) + ((num3 > 0) ? (" (" + num3 + " other avatars have moves)") : "");
	}

	public static bool Recording(Kind kind)
	{
		if (recording || counting)
		{
			return armedKind == (int)kind;
		}
		return false;
	}

	public static int RecordingSlot(Kind kind)
	{
		if (!recording || armedKind != (int)kind)
		{
			return -1;
		}
		return armedSlot;
	}

	private static void ReadKeys()
	{
		if (keysRead)
		{
			return;
		}
		keysRead = true;
		try
		{
			if (File.Exists(KeysFile))
			{
				string[] array = File.ReadAllText(KeysFile).Trim().Split(' ');
				if (array.Length >= 4 && Enum.TryParse<Button>(array[0], out var result) && Enum.TryParse<Button>(array[1], out var result2) && Enum.TryParse<Press>(array[2], out var result3) && float.TryParse(array[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var result4) && result != Button.None)
				{
					firstButton = result;
					secondButton = result2;
					pressKind = result3;
					holdFor = Mathf.Clamp(result4, 0.2f, 3f);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void SaveKeys()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(KeysFile));
			File.WriteAllText(KeysFile, firstButton.ToString() + " " + secondButton.ToString() + " " + pressKind.ToString() + " " + holdFor.ToString("0.00", CultureInfo.InvariantCulture));
		}
		catch (Exception)
		{
		}
	}

	private static bool Down(OpenControllerRig controls, Button button)
	{
		BaseController leftController = controls.leftController;
		BaseController rightController = controls.rightController;
		return button switch
		{
			Button.LeftStickClick => leftController._thumbstick, 
			Button.RightStickClick => rightController._thumbstick, 
			Button.LeftX => leftController._aButton, 
			Button.LeftY => leftController._bButton, 
			Button.RightA => rightController._aButton, 
			Button.RightB => rightController._bButton, 
			Button.LeftTrigger => leftController._primaryInteractionButton, 
			Button.RightTrigger => rightController._primaryInteractionButton, 
			Button.LeftGrip => leftController._secondaryInteractionButton, 
			Button.RightGrip => rightController._secondaryInteractionButton, 
			_ => false, 
		};
	}

	private static string Said(Button button)
	{
		return button switch
		{
			Button.LeftStickClick => "left stick click", 
			Button.RightStickClick => "right stick click", 
			Button.LeftX => "left X", 
			Button.LeftY => "left Y", 
			Button.RightA => "right A", 
			Button.RightB => "right B", 
			Button.LeftTrigger => "left trigger", 
			Button.RightTrigger => "right trigger", 
			Button.LeftGrip => "left grip", 
			Button.RightGrip => "right grip", 
			_ => "", 
		};
	}

	public static string HowToSignal()
	{
		ReadKeys();
		string text = ((firstButton == Button.LeftStickClick && secondButton == Button.RightStickClick) ? "both sticks" : (Two ? (Said(firstButton) + " + " + Said(secondButton)) : Said(firstButton)));
		if (pressKind != 0)
		{
			return "press " + text + " twice";
		}
		return "hold " + text;
	}

	public static void Update()
	{
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (controllerRig == null || controllerRig.leftController == null || controllerRig.rightController == null)
		{
			return;
		}
		ReadKeys();
		_ = Editing;
		bool flag = Down(controllerRig, firstButton) && (!Two || Down(controllerRig, secondButton));
		bool flag2 = flag && bothSince < 0f;
		if (!flag)
		{
			bothSince = -1f;
			bothUsed = false;
		}
		else if (bothSince < 0f)
		{
			bothSince = Time.time;
		}
		if (pressKind == Press.Hold)
		{
			if (flag && !bothUsed && Time.time - bothSince >= holdFor)
			{
				bothUsed = true;
				Signal();
			}
		}
		else if (flag2)
		{
			if (Time.time - tappedAt < 0.45f)
			{
				tappedAt = -10f;
				Signal();
			}
			else
			{
				tappedAt = Time.time;
			}
		}
		if (counting && Time.time >= countUntil)
		{
			Begin();
		}
		if (recording)
		{
			Sample();
		}
		Hud();
	}

	private static void Signal()
	{
		if (recording)
		{
			End();
		}
		else if (counting)
		{
			counting = false;
			Spawnables.Notify("Recording called off");
		}
		else if (armedKind < 0)
		{
			Spawnables.Notify("Pick a slot first: Bot Player, Moves");
		}
		else if (!(Time.time - endedAt < 1.5f))
		{
			counting = true;
			counted = 0;
			countUntil = Time.time + 3f;
		}
	}

	private static void Begin()
	{
		counting = false;
		recording = true;
		startedAt = Time.time;
		nextFrameAt = 0f;
		lastJumpSeen = Recorder.PlayerJumpedAt;
		taking.Clear();
		Kind kind = (Kind)armedKind;
		taughtBefore = GunMoves.Teaching;
		if (kind == Kind.Reload || kind == Kind.Fire)
		{
			GunMoves.Teaching = true;
		}
		GunMoves.SlotFresh(kind == Kind.Fire);
		if (kind == Kind.Climb)
		{
			ClimbMoves.ClearSlot(Tag(kind, armedSlot));
		}
		climbsBefore = ClimbMoves.Count;
		GunMoves.Lessons(out var _, out reloadsBefore, out var _);
		aimsBefore = GunMoves.AimMoments();
		shotsBefore = PlayerWatch.ShotsSeen;
		Lab.Event("moves: recording " + kind.ToString() + " " + (armedSlot + 1));
	}

	private static void Sample()
	{
		float num = Time.time - startedAt;
		if (num < nextFrameAt)
		{
			return;
		}
		nextFrameAt = num + 1f / 60f;
		if (Recorder.Capture(out var frame))
		{
			frame.time = num;
			frame.enemyDistance = -1f;
			frame.leftHeld = (frame.rightHeld = "");
			frame.jumped = Recorder.PlayerJumpedAt > lastJumpSeen;
			if (frame.jumped)
			{
				lastJumpSeen = Recorder.PlayerJumpedAt;
			}
			taking.Add(frame);
			if (num > Tuning.F("moves.longest", 30f))
			{
				End();
			}
		}
	}

	private static void End()
	{
		recording = false;
		endedAt = Time.time;
		Kind kind = (Kind)armedKind;
		GunMoves.Teaching = taughtBefore;
		GunMoves.SlotFresh(on: false);
		float num = Time.time - startedAt;
		float from = 0.25f;
		float to = num - SignalTakes - 0.35f;
		List<Frame> list = taking.FindAll((Frame f) => f.time >= from && f.time <= to);
		Tighten(kind, list);
		Slot slot = new Slot();
		PlayerAvatar(out var _, out var _, out slot.eyes);
		string text = null;
		switch (kind)
		{
		case Kind.Climb:
			slot.count = ClimbMoves.Count - climbsBefore;
			slot.length = to;
			if (slot.count <= 0)
			{
				text = "no climb was seen (take hold of a ledge and pull yourself up onto it)";
			}
			break;
		case Kind.Reload:
		case Kind.Fire:
		{
			GunMoves.Lessons(out var _, out var reloads, out var _);
			slot.count = ((kind == Kind.Reload) ? (reloads - reloadsBefore) : (GunMoves.AimMoments() - aimsBefore + Mathf.RoundToInt(PlayerWatch.ShotsSeen - shotsBefore)));
			slot.length = to;
			HeldGun(out slot.gun, out slot.gunName);
			if (slot.count <= 0)
			{
				text = ((kind == Kind.Reload) ? "no reload was seen (magazine out, a new one in, the slide worked)" : "no aiming or firing was seen (hold the gun up and fire it)");
			}
			break;
		}
		default:
		{
			if (list.Count < 2 || list[list.Count - 1].time - list[0].time < 0.8f)
			{
				text = "too short";
				break;
			}
			float time = list[0].time;
			float num2 = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				Frame value = list[i];
				value.time -= time;
				if (value.jumped && slot.jumpAt <= 0f)
				{
					slot.jumpAt = value.time;
				}
				if (i > 0)
				{
					num2 += new Vector2(value.headWorld.x - list[i - 1].headWorld.x, value.headWorld.z - list[i - 1].headWorld.z).magnitude;
				}
				list[i] = value;
			}
			slot.clip = list.ToArray();
			slot.length = list[list.Count - 1].time;
			slot.speed = num2 / Mathf.Max(0.1f, slot.length);
			if ((kind == Kind.Walk || kind == Kind.FastWalk || kind == Kind.Run) && slot.speed < 0.4f)
			{
				text = "you were hardly moving in it (" + slot.speed.ToString("0.0") + " m/s)";
			}
			else if (kind == Kind.Idle && slot.speed > 0.5f)
			{
				text = "you were moving in it (" + slot.speed.ToString("0.0") + " m/s)";
			}
			else if (kind == Kind.Jump && slot.jumpAt <= 0f)
			{
				text = "there was no jump in it";
			}
			else if (Glitched(slot.clip))
			{
				text = "your tracking jumped in it";
			}
			break;
		}
		}
		hudNote = ((text != null) ? "<color=#ff6060>Not saved</color>" : "<color=#70ff90>Saved</color>");
		hudNoteUntil = Time.time + 1.6f;
		if (text != null)
		{
			if (kind == Kind.Climb)
			{
				ClimbMoves.ClearSlot(Tag(kind, armedSlot));
			}
			Spawnables.Notify(Names[armedKind] + " " + (armedSlot + 1) + " not saved: " + text + ". To try again: " + HowToSignal() + ".");
			Lab.Event("moves: " + kind.ToString() + " " + (armedSlot + 1) + " not saved: " + text);
			return;
		}
		slot.filled = true;
		Slot slot2 = Slots[armedKind][armedSlot];
		if (slot2.clip != null)
		{
			byClip.Remove(slot2.clip);
		}
		Slots[armedKind][armedSlot] = slot;
		if (slot.clip != null)
		{
			byClip[slot.clip] = slot;
		}
		try
		{
			Write(kind, armedSlot, slot);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the move could not be saved: " + ex.Message);
		}
		string text2 = Names[armedKind] + " " + (armedSlot + 1) + " saved: " + Describe(kind, slot);
		Lab.Event("moves: " + text2);
		int num3 = armedSlot;
		if (slot.clip != null)
		{
			BotManager.Preview(slot.clip);
		}
		int count = Slots[armedKind].Count;
		for (int j = 1; j <= count; j++)
		{
			int num4 = (num3 + j) % count;
			if (!Slots[armedKind][num4].filled)
			{
				armedSlot = num4;
				text2 = text2 + ". Next: slot " + (num4 + 1);
				break;
			}
		}
		Spawnables.Notify(text2);
		Menu();
	}

	private static void Tighten(Kind kind, List<Frame> frames)
	{
		if (frames.Count < 20 || kind == Kind.Idle || kind == Kind.Climb || kind == Kind.Reload || kind == Kind.Fire)
		{
			return;
		}
		int i = 0;
		int num = frames.Count - 1;
		bool moving;
		float need;
		if (kind == Kind.Jump)
		{
			int num2 = frames.FindIndex((Frame f) => f.jumped);
			if (num2 < 0)
			{
				return;
			}
			float at = frames[num2].time;
			i = frames.FindIndex((Frame f) => f.time >= at - 0.7f);
			num = frames.FindLastIndex((Frame f) => f.time <= at + 1.3f);
		}
		else
		{
			moving = kind == Kind.Walk || kind == Kind.FastWalk || kind == Kind.Run;
			need = (moving ? 0.45f : 0.35f);
			for (; i < num && !Busy(i); i++)
			{
			}
			while (num > i && !Busy(num))
			{
				num--;
			}
			float num3 = (moving ? 0f : 0.25f);
			float start = frames[i].time - num3;
			float stop = frames[num].time + num3;
			i = frames.FindIndex((Frame f) => f.time >= start);
			num = frames.FindLastIndex((Frame f) => f.time <= stop);
		}
		if (i >= 0 && num > i && !(frames[num].time - frames[i].time < 0.8f))
		{
			frames.RemoveRange(num + 1, frames.Count - num - 1);
			frames.RemoveRange(0, i);
		}
		bool Busy(int j)
		{
			int index = Mathf.Max(0, j - 6);
			int index2 = Mathf.Min(frames.Count - 1, j + 6);
			float num4 = frames[index2].time - frames[index].time;
			if (num4 <= 0f)
			{
				return false;
			}
			if (moving)
			{
				return new Vector2(frames[index2].headWorld.x - frames[index].headWorld.x, frames[index2].headWorld.z - frames[index].headWorld.z).magnitude / num4 > need;
			}
			return Mathf.Max((frames[index2].leftPos - frames[index].leftPos).magnitude, (frames[index2].rightPos - frames[index].rightPos).magnitude, Mathf.Abs(frames[index2].height - frames[index].height)) / num4 > need;
		}
	}

	public static void Note(string text, float seconds)
	{
		hudNote = text;
		hudNoteUntil = Time.time + seconds;
	}

	private static void Hud()
	{
		string text = null;
		float num = 0.1f;
		if (counting)
		{
			text = "<size=260%>" + Mathf.Max(1, Mathf.CeilToInt(countUntil - Time.time)) + "</size>\n<size=70%>" + Names[armedKind] + " " + (armedSlot + 1) + ": " + Hints[armedKind] + "</size>";
			num = 0.02f;
		}
		else if (recording)
		{
			text = "<color=#ff4040>● REC</color> " + (Time.time - startedAt).ToString("0.0") + " s\n<size=60%>" + Names[armedKind] + " " + (armedSlot + 1) + " - " + HowToSignal() + " to stop</size>";
			num = 0.2f;
		}
		else if (Time.time < hudNoteUntil)
		{
			text = hudNote;
		}
		Transform head = Player.Head;
		if (text == null || head == null)
		{
			if (hud != null)
			{
				hud.gameObject.SetActive(value: false);
			}
			return;
		}
		try
		{
			if (hud == null)
			{
				GameObject gameObject = GameObject.Find("ThirdEyeMovesHud");
				if (gameObject != null)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
				hud = new GameObject("ThirdEyeMovesHud").AddComponent<TextMeshPro>();
				foreach (TMP_FontAsset item in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
				{
					if (item != null)
					{
						hud.font = item;
						break;
					}
				}
				hud.fontSize = 0.45f;
				hud.rectTransform.sizeDelta = new Vector2(2f, 0.8f);
				hud.alignment = TextAlignmentOptions.Center;
				hud.enableWordWrapping = false;
				hud.richText = true;
				hud.color = Color.white;
			}
			hud.gameObject.SetActive(value: true);
			hud.text = text;
			Transform transform = hud.transform;
			transform.position = head.position + head.forward * 0.75f - head.up * num;
			transform.rotation = Quaternion.LookRotation(head.forward, head.up);
		}
		catch (Exception)
		{
		}
	}

	public static void Shutdown()
	{
		try
		{
			if (hud != null)
			{
				UnityEngine.Object.Destroy(hud.gameObject);
			}
		}
		catch (Exception)
		{
		}
		hud = null;
		recording = (counting = false);
	}

	private static bool Glitched(Frame[] clip)
	{
		for (int i = 1; i < clip.Length; i++)
		{
			float num = clip[i].time - clip[i - 1].time;
			if (!(num <= 0f) && ((clip[i].leftPos - clip[i - 1].leftPos).magnitude / num > 9f || (clip[i].rightPos - clip[i - 1].rightPos).magnitude / num > 9f || Mathf.Abs(clip[i].height - clip[i - 1].height) / num > 5f))
			{
				return true;
			}
		}
		return false;
	}

	private static void HeldGun(out string barcode, out string name)
	{
		barcode = (name = "");
		try
		{
			Gun gun = Player.GetComponentInHand<Gun>(Player.RightHand) ?? Player.GetComponentInHand<Gun>(Player.LeftHand);
			Poolee poolee = ((gun != null) ? gun.GetComponentInParent<Poolee>() : null);
			if (!(poolee == null) && !(poolee.SpawnableCrate == null))
			{
				barcode = poolee.SpawnableCrate.Barcode.ID;
				name = Lessons.NameOf(poolee);
			}
		}
		catch (Exception)
		{
		}
	}

	private static string Describe(Kind kind, Slot slot)
	{
		if (!slot.filled)
		{
			return "empty";
		}
		switch (kind)
		{
		case Kind.Climb:
			return slot.count + ((slot.count == 1) ? " climb" : " climbs");
		case Kind.Reload:
			return slot.count + ((slot.count == 1) ? " reload" : " reloads") + ((slot.gunName.Length > 0) ? (", " + slot.gunName) : "");
		case Kind.Fire:
			return "aim and fire" + ((slot.gunName.Length > 0) ? (", " + slot.gunName) : "");
		case Kind.Walk:
		case Kind.FastWalk:
		case Kind.Run:
			return slot.length.ToString("0.0") + " s at " + slot.speed.ToString("0.0") + " m/s";
		default:
			return slot.length.ToString("0.0") + " s";
		}
	}

	private static void Clear(Kind kind, int i)
	{
		Slot slot = Slots[(int)kind][i];
		if (slot.filled)
		{
			if (slot.clip != null)
			{
				byClip.Remove(slot.clip);
			}
			if (kind == Kind.Climb)
			{
				ClimbMoves.ClearSlot(Tag(kind, i));
			}
			if ((kind == Kind.Reload || kind == Kind.Fire) && slot.gun.Length > 0)
			{
				GunMoves.ForgetGun(slot.gun, kind == Kind.Reload, kind == Kind.Fire);
			}
			Slots[(int)kind][i] = new Slot();
			try
			{
				File.Delete(FileOf(kind, i));
			}
			catch (Exception)
			{
			}
			Spawnables.Notify(Names[(int)kind] + " " + (i + 1) + " cleared");
			Menu();
		}
	}

	private static void Add(Kind kind)
	{
		List<Slot> list = Slots[(int)kind];
		if (!recording && !counting)
		{
			if (list.Count >= 20)
			{
				Spawnables.Notify(Names[(int)kind] + " has " + 20 + " slots: no more");
				return;
			}
			list.Add(new Slot());
			armedKind = (int)kind;
			armedSlot = list.Count - 1;
			WriteCounts();
			Spawnables.Notify(Names[(int)kind] + " now has " + list.Count + " slots. Slot " + list.Count + " picked: " + HowToSignal() + " to record");
			Menu();
		}
	}

	private static void Remove(Kind kind, int i)
	{
		List<Slot> list = Slots[(int)kind];
		if (recording || counting || i < 0 || i >= list.Count)
		{
			return;
		}
		if (list.Count <= 1)
		{
			Spawnables.Notify(Names[(int)kind] + " keeps one slot at least (clear it instead)");
			return;
		}
		Slot slot = list[i];
		if (slot.clip != null)
		{
			byClip.Remove(slot.clip);
		}
		if (kind == Kind.Climb)
		{
			ClimbMoves.ClearSlot(Tag(kind, i));
		}
		if ((kind == Kind.Reload || kind == Kind.Fire) && slot.gun.Length > 0)
		{
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				flag |= j != i && list[j].filled && list[j].gun == slot.gun;
			}
			if (!flag)
			{
				GunMoves.ForgetGun(slot.gun, kind == Kind.Reload, kind == Kind.Fire);
			}
		}
		list.RemoveAt(i);
		try
		{
			File.Delete(FileOf(kind, i));
			for (int k = i; k < list.Count; k++)
			{
				if (File.Exists(FileOf(kind, k + 1)))
				{
					File.Move(FileOf(kind, k + 1), FileOf(kind, k));
				}
				if (kind == Kind.Climb)
				{
					ClimbMoves.MoveSlot(Tag(kind, k + 1), Tag(kind, k));
				}
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the slots' files could not be moved up: " + ex.Message);
		}
		armedSlot = Mathf.Clamp(armedSlot, 0, list.Count - 1);
		WriteCounts();
		Spawnables.Notify(Names[(int)kind] + " slot " + (i + 1) + " removed: " + list.Count + " left");
		Menu();
	}

	public static void LabAddRemove(string kindName, bool add)
	{
		if (Enum.TryParse<Kind>(kindName, ignoreCase: true, out var result))
		{
			if (add)
			{
				Add(result);
			}
			else if (armedKind == (int)result)
			{
				Remove(result, armedSlot);
			}
			Lab.Event("moves: " + result.ToString() + " has " + Slots[(int)result].Count + " slots");
		}
	}

	public static void BuildMenu(Page page)
	{
		movesPage = page.CreatePage("Moves (record in slots)", Color.green);
		movesPage.CreateFunction("Whose moves are these?", Color.cyan, delegate
		{
			Spawnables.Notify("Each avatar has its own moves. " + Summary() + ". An avatar with none of a kind borrows another's, sized to it.");
		});
		ReadKeys();
		Page page2 = movesPage.CreatePage("Record shortcut (buttons)", Color.yellow);
		page2.CreateFunction("What is it now?", Color.cyan, delegate
		{
			Spawnables.Notify("To start and stop recording a move: " + HowToSignal() + ".");
		});
		page2.CreateEnum("Button 1", Color.white, firstButton, delegate(Enum v)
		{
			firstButton = (((Button)(object)v != Button.None) ? ((Button)(object)v) : Button.LeftStickClick);
			SaveKeys();
			Spawnables.Notify("Record shortcut: " + HowToSignal());
		});
		page2.CreateEnum("Button 2 (None = one button only)", Color.white, secondButton, delegate(Enum v)
		{
			secondButton = (Button)(object)v;
			SaveKeys();
			Spawnables.Notify("Record shortcut: " + HowToSignal());
		});
		page2.CreateEnum("Hold, or press twice", Color.white, pressKind, delegate(Enum v)
		{
			pressKind = (Press)(object)v;
			SaveKeys();
			Spawnables.Notify("Record shortcut: " + HowToSignal());
		});
		page2.CreateFloat("Hold for (seconds)", Color.white, holdFor, 0.1f, 0.2f, 3f, delegate(float v)
		{
			holdFor = v;
			SaveKeys();
		});
		page2.CreateFunction("Back to both sticks, held", Color.red, delegate
		{
			firstButton = Button.LeftStickClick;
			secondButton = Button.RightStickClick;
			pressKind = Press.Hold;
			holdFor = 0.7f;
			SaveKeys();
			Spawnables.Notify("Record shortcut: " + HowToSignal() + " (the menu shows it after it is opened again)");
		});
		for (int i = 0; i < Names.Length; i++)
		{
			kindPages[i] = movesPage.CreatePage(Names[i], Color.white);
		}
		Menu();
	}

	private static void Menu()
	{
		if (movesPage == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < Names.Length; i++)
			{
				Page page = kindPages[i];
				if (page == null)
				{
					continue;
				}
				page.RemoveAll();
				int kind = i;
				for (int j = 0; j < Slots[kind].Count; j++)
				{
					int index = j;
					bool flag = armedKind == kind && armedSlot == index;
					page.CreateFunction((flag ? "> " : "") + "Slot " + (j + 1) + ": " + Describe((Kind)kind, Slots[kind][j]), flag ? Color.green : (Slots[kind][j].filled ? Color.white : Color.gray), delegate
					{
						armedKind = kind;
						armedSlot = index;
						Spawnables.Notify(Names[kind] + " " + (index + 1) + " picked. To record: " + HowToSignal() + ". It counts 3 and records (" + Hints[kind] + "). The same again to stop.");
						Menu();
					});
				}
				page.CreateFunction("Show the picked slot on a bot", Color.yellow, delegate
				{
					Slot slot = ((armedKind == kind) ? Slots[kind][armedSlot] : null);
					if (slot == null || slot.clip == null)
					{
						Spawnables.Notify("Pick a slot of this kind with a move in it first");
					}
					else
					{
						BotManager.Preview(slot.clip);
					}
				});
				page.CreateFunction("Clear the picked slot", Color.red, delegate
				{
					if (armedKind == kind)
					{
						Clear((Kind)kind, armedSlot);
					}
					else
					{
						Spawnables.Notify("Pick a slot of this kind first");
					}
				});
				page.CreateFunction("Add a slot", Color.cyan, delegate
				{
					Add((Kind)kind);
				});
				page.CreateFunction("Remove the picked slot", Color.red, delegate
				{
					if (armedKind == kind)
					{
						Remove((Kind)kind, armedSlot);
					}
					else
					{
						Spawnables.Notify("Pick a slot of this kind first");
					}
				});
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the moves menu could not be made: " + ex.Message);
		}
	}

	public static void LabArm(string kindName, int slot)
	{
		if (Enum.TryParse<Kind>(kindName, ignoreCase: true, out var result))
		{
			armedKind = (int)result;
			armedSlot = Mathf.Clamp(slot - 1, 0, Slots[armedKind].Count - 1);
			Menu();
			Lab.Event("moves: picked " + result.ToString() + " " + (armedSlot + 1) + "; " + Summary());
		}
		else
		{
			Lab.Event("moves: " + Summary());
		}
	}

	public static void LabSignal()
	{
		Signal();
	}

	public static string State()
	{
		if (!recording)
		{
			if (!counting)
			{
				return "idle";
			}
			return "counting";
		}
		return "recording " + (Time.time - startedAt).ToString("0.0") + " s";
	}
}
