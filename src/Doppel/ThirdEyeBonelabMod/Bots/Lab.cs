using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Lab
{
	public static bool On;

	private static float switchAt;

	private static float pollAt;

	private static readonly Queue<string> queue = new Queue<string>();

	private static float waitUntil;

	private static float telemetryAt;

	private static StreamWriter telemetry;

	private static StreamWriter events;

	private static Camera camera;

	private static RenderTexture picture;

	private static int snaps;

	private static int roundsLeft;

	private static int round;

	private static float roundStart;

	private static float roundEndAt = -1f;

	private static int killsAtStart;

	private static int closeCallsAtStart;

	private static int shotsAtStart;

	private static int reloadsAtStart;

	private static MethodInfo labSpawn;

	private static MethodInfo labRemoveAll;

	private static readonly List<GameObject> fords = new List<GameObject>();

	public static string Who;

	public static bool Safe;

	private static float safeAt;

	private static string youWas = "";

	private static bool youClickWas;

	private static float youSpeedAt;

	private static readonly string[] chatter = new string[7] { "shot: ", "run: going", "head limit: head was", "fists: that blow", "climb lift", "flinch ", "you: going" };

	public static string Folder => AssetFiles.SaveFolder("lab");

	private static string Commands => Path.Combine(Folder, "commands.txt");

	public static void Switch()
	{
		if (Time.time < switchAt)
		{
			return;
		}
		switchAt = Time.time + 5f;
		try
		{
			On = File.Exists(Path.Combine(Folder, "enable.txt"));
		}
		catch (Exception)
		{
		}
	}

	public static void Update()
	{
		Tuning.Update();
		if (Time.time >= pollAt)
		{
			pollAt = Time.time + 0.4f;
			Poll();
		}
		while (queue.Count > 0 && Time.time >= waitUntil)
		{
			string text = queue.Dequeue();
			if (text.StartsWith("wait ", StringComparison.OrdinalIgnoreCase) && float.TryParse(text.Substring(5).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				waitUntil = Time.time + result;
				Event("> " + text);
			}
			else
			{
				Run(text);
			}
		}
		if (On && Time.time >= telemetryAt)
		{
			telemetryAt = Time.time + 0.2f;
			WriteTelemetry();
		}
		Train();
	}

	public static void Shutdown()
	{
		try
		{
			telemetry?.Dispose();
			events?.Dispose();
		}
		catch (Exception)
		{
		}
		telemetry = null;
		events = null;
		if (camera != null)
		{
			UnityEngine.Object.Destroy(camera.gameObject);
		}
		if (picture != null)
		{
			UnityEngine.Object.Destroy(picture);
		}
		camera = null;
		picture = null;
		queue.Clear();
		roundsLeft = 0;
		roundEndAt = -1f;
	}

	public static void ForgetLevel()
	{
		queue.Clear();
		waitUntil = 0f;
		roundsLeft = 0;
		roundEndAt = -1f;
		camera = null;
	}

	private static void Poll()
	{
		try
		{
			if (!File.Exists(Commands))
			{
				return;
			}
			string[] array = File.ReadAllLines(Commands);
			File.WriteAllText(Commands, "");
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string text = array2[i].Trim();
				if (text.Length > 0 && !text.StartsWith("#", StringComparison.Ordinal))
				{
					queue.Enqueue(text);
				}
			}
		}
		catch (IOException)
		{
		}
		catch (Exception ex2)
		{
			Event("commands failed: " + ex2.Message);
		}
	}

	public static void Run(string command)
	{
		string[] array = command.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string text = array[0].ToLowerInvariant();
		string text2 = ((command.Length > array[0].Length) ? command.Substring(array[0].Length).Trim() : "");
		Event("> " + command);
		try
		{
			switch (text)
			{
			case "lab":
				On = text2 != "off";
				Event("lab " + (On ? "on" : "off"));
				break;
			case "spawn":
				BotManager.SpawnBot();
				break;
			case "remove":
				BotManager.RemoveAllBots();
				break;
			case "bring":
				BotManager.BringAllBots();
				break;
			case "mode":
			{
				if (Enum.TryParse<Bot.Mode>(text2, ignoreCase: true, out var result))
				{
					BotManager.SetModeAll(result);
				}
				else
				{
					Event("no such mode: " + text2);
				}
				break;
			}
			case "gun":
			{
				foreach (Bot item in BotManager.All)
				{
					item.LabGive(text2);
				}
				break;
			}
			case "wave":
			{
				int times = ((array.Length > 1) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 3);
				{
					foreach (Bot item2 in BotManager.All)
					{
						item2.LabWave(times);
					}
					break;
				}
			}
			case "lesson":
				Event("lessons: " + Lessons.Summary() + " (" + Lessons.Names() + ")");
				Event("lessons are for: " + Lessons.Purposes());
				if (text2 != "list")
				{
					BotManager.UseLessons((text2.Length > 0) ? text2 : null);
				}
				break;
			case "hurt":
			{
				foreach (Bot item3 in BotManager.All)
				{
					item3.LabHurt((array.Length > 1) ? float.Parse(array[1], CultureInfo.InvariantCulture) : 0.1f);
				}
				break;
			}
			case "portal":
			{
				foreach (Bot item4 in BotManager.All)
				{
					item4.LabPortal();
				}
				break;
			}
			case "trip":
			{
				foreach (Bot item5 in BotManager.All)
				{
					item5.LabTrip((array.Length > 1) ? float.Parse(array[1], CultureInfo.InvariantCulture) : 0.4f);
				}
				break;
			}
			case "curious":
				Event("self-taught: " + SelfTaught.Summary());
				{
					foreach (Bot item6 in BotManager.All)
					{
						item6.LabCurious((text2.Length > 0) ? text2 : null);
					}
					break;
				}
			case "fresh":
				Event("fresh start: " + BotManager.FreshStart());
				break;
			case "moves":
				MoveSlots.LabArm((array.Length > 1) ? array[1] : "", (array.Length <= 2) ? 1 : int.Parse(array[2], CultureInfo.InvariantCulture));
				break;
			case "addslot":
				MoveSlots.LabAddRemove(text2, add: true);
				break;
			case "removeslot":
				MoveSlots.LabAddRemove(text2, add: false);
				break;
			case "signal":
				MoveSlots.LabSignal();
				break;
			case "climb":
			{
				foreach (Bot item7 in BotManager.All)
				{
					item7.LabClimb();
				}
				break;
			}
			case "ragdoll":
			{
				foreach (Bot item8 in BotManager.All)
				{
					PhysicsRig physicsRig = item8.rig.physicsRig;
					string text3 = "loco " + physicsRig.ballLocoEnabled + ", shutdown " + physicsRig.shutdown;
					physicsRig.RagdollRig();
					Event("ragdoll lab: " + item8.Name + " before " + text3 + "; after RagdollRig: loco " + physicsRig.ballLocoEnabled + ", shutdown " + physicsRig.shutdown);
				}
				break;
			}
			case "down":
			{
				using IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator();
				if (enumerator.MoveNext())
				{
					enumerator.Current.LabHurt(5f);
				}
				break;
			}
			case "order":
				Bot.LabOrder();
				break;
			case "crate":
				Bot.LabSpawnCrate(text2);
				break;
			case "seats":
				Bot.LabSeats();
				break;
			case "ride":
			{
				foreach (Bot item9 in BotManager.All)
				{
					switch (text2)
					{
					case "host":
						item9.labHost = true;
						item9.LabRide();
						return;
					case "host off":
						if (item9.labHost)
						{
							item9.labHost = false;
							item9.LabUnride();
						}
						break;
					case "off":
						item9.LabUnride();
						break;
					default:
						if (text2.StartsWith("say"))
						{
							item9.LabRideSay(text2);
						}
						else
						{
							item9.LabRide();
						}
						break;
					}
				}
				break;
			}
			case "catch":
			{
				using IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator();
				if (enumerator.MoveNext())
				{
					enumerator.Current.LabCatch();
				}
				break;
			}
			case "shove":
			{
				foreach (Bot item10 in BotManager.All)
				{
					item10.LabShove();
				}
				break;
			}
			case "bringme":
			{
				foreach (Bot item11 in BotManager.All)
				{
					item11.LabBring();
				}
				break;
			}
			case "battle":
				BotManager.LabBattle((array.Length > 1) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 5, (array.Length > 2) ? int.Parse(array[2], CultureInfo.InvariantCulture) : 5);
				break;
			case "race":
				if (text2 == "off")
				{
					Race.Stop("asked to");
				}
				else
				{
					Race.Start();
				}
				break;
			case "surrender":
			{
				foreach (Bot item12 in BotManager.All)
				{
					item12.LabSurrender();
				}
				break;
			}
			case "enemy":
				BotManager.SpawnEnemyBot();
				break;
			case "sit":
			{
				foreach (Bot item13 in BotManager.All)
				{
					item13.LabSit(text2 != "off");
				}
				break;
			}
			case "sitsay":
			{
				foreach (Bot item14 in BotManager.All)
				{
					item14.LabSitSay(text2);
				}
				break;
			}
			case "joints":
			{
				foreach (Bot item15 in BotManager.All)
				{
					item15.LabJoints();
				}
				break;
			}
			case "sprint":
			{
				foreach (Bot item16 in BotManager.All)
				{
					item16.LabSprint();
				}
				break;
			}
			case "legs":
				Event("legs, you: " + Bot.LegsOf(Player.RigManager) + ", last jump " + (Time.time - Recorder.PlayerJumpedAt).ToString("0.0") + " s ago, time x" + Time.timeScale.ToString("0.00") + ", gravity " + Physics.gravity.y.ToString("0.0") + ", avatar " + ((Player.RigManager != null && Player.RigManager.avatar != null) ? Player.RigManager.avatar.name : "?") + ", speed " + ((Player.PhysicsRig != null && Player.PhysicsRig.m_pelvis != null) ? Player.PhysicsRig.m_pelvis.GetComponent<Rigidbody>().velocity.ToString("0.0") : "?") + ", moves " + MoveSlots.State());
				{
					foreach (Bot item17 in BotManager.All)
					{
						Event("legs, bot " + item17.Name + ": game last asked for its hands " + (Time.frameCount - item17.LastDriveFrame) + " frame(s) ago, " + Bot.LegsOf(item17.rig) + ", same legs as yours: " + (Player.RigManager != null && item17.rig.remapHeptaRig.Pointer == Player.RigManager.remapHeptaRig.Pointer));
					}
					break;
				}
			case "voice":
			{
				foreach (Bot item18 in BotManager.All)
				{
					item18.LabVoice(text2);
				}
				break;
			}
			case "shoot":
				if (text2 == "reset")
				{
					ShootSkill.Reset();
				}
				Event("shoot skill: " + ShootSkill.Said());
				break;
			case "drive1":
			{
				using IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator();
				if (enumerator.MoveNext())
				{
					enumerator.Current.LabDrive(text2);
				}
				break;
			}
			case "drive":
				if (text2 == "learn")
				{
					Driving.Forget();
					Event("driving: " + Lessons.DriveSaid());
					break;
				}
				{
					foreach (Bot item19 in BotManager.All)
					{
						item19.LabDrive(text2);
					}
					break;
				}
			case "purpose":
			{
				foreach (Bot item20 in BotManager.All)
				{
					item20.LabPurpose();
				}
				break;
			}
			case "fidget":
			{
				foreach (Bot item21 in BotManager.All)
				{
					item21.LabFidget();
				}
				break;
			}
			case "startle":
			{
				foreach (Bot item22 in BotManager.All)
				{
					item22.LabStartle();
				}
				break;
			}
			case "wavehello":
			{
				foreach (Bot item23 in BotManager.All)
				{
					item23.LabWaveBack();
				}
				break;
			}
			case "toss":
			{
				string barcode = ((array.Length > 1) ? GunBarcode(array[1]) : "c1534c5a-fcfc-4f43-8fb0-d29531393131");
				{
					foreach (Bot item24 in BotManager.All)
					{
						item24.LabToss(barcode);
					}
					break;
				}
			}
			case "safe":
				Safe = text2 != "off";
				ApplySafe();
				Event("safe: the player " + (Safe ? "cannot be killed (for a test)" : "can be hurt again"));
				break;
			case "count":
				Event("count: " + Targets.Hostiles().Count + " hostile(s) alive, " + BotManager.All.Count() + " bot(s), ammo failures let pass " + BotManager.AmmoFailures);
				{
					foreach (Bot item25 in BotManager.All)
					{
						Event("status: " + item25.LabStatus());
					}
					break;
				}
			case "holster":
			{
				foreach (Bot item26 in BotManager.All)
				{
					item26.LabHolster();
				}
				break;
			}
			case "arm":
			{
				foreach (Bot item27 in BotManager.All)
				{
					item27.LabArm();
				}
				break;
			}
			case "lay":
			{
				foreach (Bot item28 in BotManager.All)
				{
					item28.LabLay(text2);
				}
				break;
			}
			case "drop":
			{
				foreach (Bot item29 in BotManager.All)
				{
					item29.StopPlaying();
				}
				break;
			}
			case "zombies":
			{
				int count = ((array.Length <= 1) ? 1 : int.Parse(array[1], CultureInfo.InvariantCulture));
				float far = ((array.Length > 2) ? float.Parse(array[2], CultureInfo.InvariantCulture) : 10f);
				SpawnEnemies(count, far, (array.Length > 3) ? array[3].ToLowerInvariant() : "zombie");
				break;
			}
			case "clear":
				ClearEnemies();
				ClearFords();
				break;
			case "ford":
				SpawnFords((array.Length <= 1) ? 1 : int.Parse(array[1], CultureInfo.InvariantCulture), (array.Length > 2) ? float.Parse(array[2], CultureInfo.InvariantCulture) : 20f);
				break;
			case "snap":
			{
				foreach (Bot item30 in BotManager.All)
				{
					Snap(item30, (text2.Length > 0) ? text2.ToLowerInvariant() : "front");
				}
				break;
			}
			case "train":
				roundsLeft = ((array.Length > 1) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 10);
				On = true;
				roundEndAt = Time.time;
				Event("training " + roundsLeft + " rounds");
				break;
			case "stop":
				roundsLeft = 0;
				roundEndAt = -1f;
				Event("training stopped");
				break;
			case "say":
				Spawnables.Notify(text2);
				break;
			case "avatarcheck":
				Event("avatar check: you " + Of(Player.RigManager));
				{
					foreach (Bot item31 in BotManager.All)
					{
						Event("avatar check: " + item31.Name + " " + Of(item31.rig));
					}
					break;
				}
			case "avatar":
			{
				foreach (Bot item32 in BotManager.All)
				{
					BotRig.CopyPlayerAvatar(item32.rig, null);
				}
				break;
			}
			case "exact":
				if (text2 == "record")
				{
					ExactClone.Start();
				}
				else if (text2 == "stop")
				{
					ExactClone.Stop();
				}
				else
				{
					BotManager.PlayExact();
				}
				break;
			case "clone":
				switch (text2)
				{
				case "record":
					Clone.Start();
					break;
				case "stop":
					Clone.Stop();
					break;
				case "loop":
					BotManager.SetCloneLoop(on: true);
					BotManager.PlayClone();
					break;
				default:
					BotManager.SetCloneLoop(on: false);
					BotManager.PlayClone();
					break;
				}
				break;
			case "teach":
				GunMoves.Teaching = text2 != "off";
				Event("teaching " + (GunMoves.Teaching ? "on" : "off") + ": " + GunMoves.Summary());
				break;
			case "gunmoves":
				Event("gun moves: " + GunMoves.Summary());
				break;
			case "learned":
				Event("learned: " + Knowledge.Summary());
				Event("moves: " + MotionLibrary.Summary() + "; to teach next: " + MotionLibrary.TeachNext());
				break;
			case "forget":
				Knowledge.Forget();
				break;
			default:
				Event("unknown command: " + text);
				break;
			}
		}
		catch (Exception ex)
		{
			Event("command failed: " + ex.Message);
		}
		static string Of(RigManager r)
		{
			if (!(r == null) && !(r.avatar == null))
			{
				return BotRig.AvatarOf(r) + " root " + r.avatar.transform.localScale.ToString("0.000") + " world " + r.avatar.transform.lossyScale.y.ToString("0.000") + " eyes " + (r.avatar.eyeHeight * r.avatar.transform.lossyScale.y).ToString("0.00") + " m (asset eyes " + r.avatar.eyeHeight.ToString("0.00") + ")";
			}
			return "none";
		}
	}

	public static string GunBarcode(string which)
	{
		if (which.Contains("."))
		{
			return which;
		}
		FieldInfo[] fields = typeof(CommonBarcodes.Guns).GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(string) && string.Equals(fieldInfo.Name, which, StringComparison.OrdinalIgnoreCase))
			{
				return fieldInfo.GetValue(null) as string;
			}
		}
		return "c1534c5a-fcfc-4f43-8fb0-d29531393131";
	}

	private static bool FindZombies()
	{
		if (labSpawn != null)
		{
			return true;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type type = assemblies[i].GetType("ThirdEyeBonelabMod.Zombies.ZombieManager", throwOnError: false);
			if (!(type == null))
			{
				labSpawn = type.GetMethod("LabSpawn", BindingFlags.Static | BindingFlags.Public);
				labRemoveAll = type.GetMethod("LabRemoveAll", BindingFlags.Static | BindingFlags.Public);
				return labSpawn != null;
			}
		}
		return false;
	}

	private static void SpawnEnemies(int count, float far, string kind)
	{
		if (!FindZombies())
		{
			Event("the Zombies mod is not there");
			return;
		}
		Bot bot = null;
		using (IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				bot = enumerator.Current;
			}
		}
		Transform transform = ((bot != null && bot.Head != null) ? bot.Head : Player.Head);
		if (transform == null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (!Navigator.RandomPlace(transform.position - Vector3.up * 1.4f, far * 0.75f, far * 1.25f, out var place))
			{
				place = transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward * far;
			}
			if (Player.Head != null && (place - Player.Head.position).magnitude < 3f)
			{
				place += (place - Player.Head.position).normalized * 3f;
			}
			labSpawn.Invoke(null, new object[2] { kind, place });
		}
		Event("spawned " + count + " " + kind + " about " + far.ToString("0") + " m out");
	}

	private static void SpawnFords(int count, float far)
	{
		Bot bot = null;
		using (IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				bot = enumerator.Current;
			}
		}
		Transform transform = ((bot != null && bot.Head != null) ? bot.Head : Player.Head);
		if (transform == null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (!Navigator.RandomPlace(transform.position - Vector3.up * 1.4f, far * 0.85f, far * 1.15f, out var place))
			{
				place = transform.position - Vector3.up * 1.4f + Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward * far;
			}
			if (Player.Head != null && (place - Player.Head.position).magnitude < 6f)
			{
				place += (place - Player.Head.position).normalized * 6f;
			}
			HelperMethods.SpawnCrate("c1534c5a-3fd8-4d50-9eaf-0695466f7264", place, Quaternion.identity, Vector3.one, ignorePolicy: false, delegate(GameObject made)
			{
				if (made != null)
				{
					fords.Add(made);
				}
			});
		}
		Event("spawned " + count + " Ford about " + far.ToString("0") + " m out");
	}

	private static void ClearFords()
	{
		foreach (GameObject ford in fords)
		{
			if (!(ford == null))
			{
				Poolee componentInParent = ford.GetComponentInParent<Poolee>();
				if (componentInParent != null)
				{
					componentInParent.Despawn();
				}
				else
				{
					UnityEngine.Object.Destroy(ford);
				}
			}
		}
		fords.Clear();
	}

	private static void ClearEnemies()
	{
		if (FindZombies() && labRemoveAll != null)
		{
			labRemoveAll.Invoke(null, null);
		}
	}

	private static void Train()
	{
		if (roundsLeft <= 0 && roundEndAt < 0f)
		{
			return;
		}
		Bot bot = null;
		using (IEnumerator<Bot> enumerator = BotManager.All.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				bot = enumerator.Current;
			}
		}
		if (bot == null)
		{
			return;
		}
		int count = Targets.Hostiles().Count;
		if (roundEndAt < 0f)
		{
			if (count == 0 || Time.time - roundStart > 60f)
			{
				float num = Time.time - roundStart;
				int num2 = Knowledge.Data.kills - killsAtStart;
				int num3 = Knowledge.Data.closeCalls - closeCallsAtStart;
				int num4 = bot.LabShots - shotsAtStart;
				int num5 = bot.LabReloads - reloadsAtStart;
				string text = round + "," + num.ToString("0.0", CultureInfo.InvariantCulture) + "," + num2 + "," + count + "," + num3 + "," + num4 + "," + num5;
				string path = Path.Combine(Folder, "episodes.csv");
				if (!File.Exists(path))
				{
					File.WriteAllText(path, "round,seconds,kills,left,closecalls,shots,reloads\n");
				}
				File.AppendAllText(path, text + "\n");
				Event("round " + round + " done: " + num.ToString("0") + " s, " + num2 + " kills, " + count + " left, " + num3 + " close calls, " + num4 + " shots, " + num5 + " reloads");
				ClearEnemies();
				roundEndAt = Time.time;
			}
		}
		else
		{
			if (Time.time - roundEndAt < 3f)
			{
				return;
			}
			if (roundsLeft <= 0)
			{
				roundEndAt = -1f;
				Event("training done: " + Knowledge.Summary());
				Knowledge.Save();
				return;
			}
			roundsLeft--;
			round++;
			if (bot.mode != Bot.Mode.Brain)
			{
				BotManager.SetModeAll(Bot.Mode.Brain);
			}
			SpawnEnemies(UnityEngine.Random.Range(1, 5), UnityEngine.Random.Range(6f, 18f), (UnityEngine.Random.value < 0.25f) ? "fast" : "zombie");
			roundStart = Time.time;
			roundEndAt = -1f;
			killsAtStart = Knowledge.Data.kills;
			closeCallsAtStart = Knowledge.Data.closeCalls;
			shotsAtStart = bot.LabShots;
			reloadsAtStart = bot.LabReloads;
		}
	}

	public static void ApplySafe()
	{
		try
		{
			Health health = ((Player.RigManager != null) ? Player.RigManager.health : null);
			if (!(health == null))
			{
				if (Safe && health.healthMode != 0)
				{
					health.SetHealthMode(0);
				}
				else if (!Safe && health.healthMode == Health.HealthMode.Invincible)
				{
					health.SetHealthMode(1);
				}
				if (Safe)
				{
					health.SetFullHealth();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void WatchYou()
	{
		try
		{
			RigManager rigManager = Player.RigManager;
			if (rigManager == null || rigManager.remapHeptaRig == null)
			{
				return;
			}
			OpenControllerRig openControllerRig = rigManager.ControllerRig.TryCast<OpenControllerRig>();
			bool flag = openControllerRig != null && (openControllerRig._primaryStick || openControllerRig._secondaryStick);
			bool flag2 = false;
			string text = "";
			if (openControllerRig != null)
			{
				BaseController[] array = new BaseController[2] { openControllerRig.leftController, openControllerRig.rightController };
				foreach (BaseController baseController in array)
				{
					if (!(baseController == null))
					{
						bool thumbStick = baseController.GetThumbStick();
						flag2 = flag2 || thumbStick;
						text = text + ((baseController == openControllerRig.leftController) ? " left " : " right ") + (thumbStick ? "DOWN" : "up");
					}
				}
			}
			string text2 = rigManager.remapHeptaRig.travState.ToString() + ", rig click " + ((openControllerRig != null) ? (openControllerRig._primaryStick + "/" + openControllerRig._secondaryStick) : "?") + ", sticks" + text + ", body " + rigManager.bodyState;
			Rigidbody rigidbody = ((Player.PhysicsRig != null && Player.PhysicsRig.m_pelvis != null) ? Player.PhysicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
			float num = ((rigidbody != null) ? new Vector2(rigidbody.velocity.x, rigidbody.velocity.z).magnitude : 0f);
			if (openControllerRig != null && openControllerRig._axisPrimary.magnitude > 0.5f && Time.time - youSpeedAt > 0.5f)
			{
				youSpeedAt = Time.time;
				Event("you: going " + num.ToString("0.0") + " m/s, " + rigManager.remapHeptaRig.travState.ToString() + ", legs' own " + rigManager.remapHeptaRig._currentVelocity.magnitude.ToString("0.0") + ", most now " + rigManager.remapHeptaRig.currentMaxVelocity.ToString("0.0") + " of " + rigManager.remapHeptaRig.maxVelocity.ToString("0.00") + ", crouch " + rigManager.remapHeptaRig._crouchTarget.ToString("0.00"));
			}
			if (text2 != youWas || (flag || flag2) != youClickWas)
			{
				youWas = text2;
				youClickWas = flag || flag2;
				Event("you: " + text2 + ", top speed " + rigManager.remapHeptaRig.maxVelocity.ToString("0.00") + ", stick " + ((openControllerRig != null) ? openControllerRig._axisPrimary.ToString("0.0") : "?"));
			}
		}
		catch (Exception)
		{
		}
	}

	public static void KeepSafe()
	{
		WatchYou();
		if (Safe && !(Time.time - safeAt < 2f))
		{
			safeAt = Time.time;
			ApplySafe();
		}
	}

	public static void Event(string what)
	{
		if (what != null && Tuning.F("lab.chatty", 0f) < 0.5f)
		{
			string[] array = chatter;
			foreach (string value in array)
			{
				if (what.StartsWith(value, StringComparison.Ordinal))
				{
					return;
				}
			}
		}
		if (Who != null)
		{
			what = "[" + Who + "] " + what;
		}
		try
		{
			if (events == null)
			{
				Directory.CreateDirectory(Folder);
				events = new StreamWriter(Path.Combine(Folder, "events.log"), append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
				{
					AutoFlush = true
				};
			}
			events.WriteLine(DateTime.Now.ToString("HH:mm:ss.f", CultureInfo.InvariantCulture) + " " + what);
		}
		catch (Exception)
		{
		}
	}

	private static void WriteTelemetry()
	{
		try
		{
			if (telemetry == null)
			{
				Directory.CreateDirectory(Folder);
				string text = Path.Combine(Folder, "telemetry.jsonl");
				bool flag = File.Exists(text) && new FileInfo(text).Length > 20000000;
				telemetry = new StreamWriter(text, !flag, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
				{
					AutoFlush = true
				};
			}
			foreach (Bot item in BotManager.All)
			{
				telemetry.WriteLine(item.LabState());
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player lab: " + ex.Message);
			telemetry = null;
		}
	}

	public static void Snap(Bot bot, string view)
	{
		Transform head = bot.Head;
		if (head == null)
		{
			return;
		}
		try
		{
			if (camera == null)
			{
				camera = new GameObject("ThirdEyeLabCamera").AddComponent<Camera>();
				camera.enabled = false;
				camera.fieldOfView = 55f;
				camera.nearClipPlane = 0.03f;
				camera.stereoTargetEye = StereoTargetEyeMask.None;
				picture = new RenderTexture(800, 600, 24);
				camera.targetTexture = picture;
			}
			Vector3 facing = bot.Facing;
			Vector3 vector = Vector3.Cross(Vector3.up, facing);
			Vector3 vector2 = head.position - Vector3.up * 0.35f;
			Vector3 vector3 = vector2;
			Vector3 vector4;
			switch (view)
			{
			case "side":
				vector4 = vector2 + vector * 2.2f + Vector3.up * 0.2f;
				break;
			case "back":
				vector4 = vector2 - facing * 2.2f + Vector3.up * 0.3f;
				break;
			case "top":
				vector4 = vector2 + Vector3.up * 2.5f + facing * 0.5f;
				break;
			case "map":
				vector4 = vector2 + Vector3.up * 40f - facing * 0.5f;
				break;
			case "far":
				vector4 = vector2 - facing * 9f + Vector3.up * 5f;
				break;
			case "hand":
			{
				Transform frontHandTransform = bot.FrontHandTransform;
				vector3 = ((frontHandTransform != null) ? frontHandTransform.position : vector2);
				vector4 = vector3 + facing * 0.45f - vector * 0.55f - Vector3.up * 0.12f;
				break;
			}
			case "gun":
			{
				Transform gunHandTransform = bot.GunHandTransform;
				vector3 = ((gunHandTransform != null) ? gunHandTransform.position : vector2);
				vector4 = vector3 + facing * 0.7f + vector * 0.6f + Vector3.up * 0.25f;
				break;
			}
			default:
				vector4 = vector2 + facing * 2.4f + vector * 0.6f + Vector3.up * 0.15f;
				break;
			}
			camera.transform.SetPositionAndRotation(vector4, Quaternion.LookRotation(vector3 - vector4, Vector3.up));
			camera.Render();
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = picture;
			Texture2D texture2D = new Texture2D(picture.width, picture.height, TextureFormat.RGB24, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, picture.width, picture.height), 0, 0);
			texture2D.Apply();
			RenderTexture.active = active;
			byte[] bytes = texture2D.EncodeToPNG();
			UnityEngine.Object.Destroy(texture2D);
			snaps++;
			string path = Path.Combine(Folder, "snap_" + snaps.ToString("000") + "_" + view + ".png");
			File.WriteAllBytes(path, bytes);
			Event("snap " + Path.GetFileName(path) + " | " + bot.LabSummary());
		}
		catch (Exception ex)
		{
			Event("snap failed: " + ex.Message);
		}
	}
}
