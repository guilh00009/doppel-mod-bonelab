using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
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
		if ((Object)(object)camera != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)camera).gameObject);
		}
		if ((Object)(object)picture != (Object)null)
		{
			Object.Destroy((Object)(object)picture);
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
			{
				string[] obj = new string[14]
				{
					"legs, you: ",
					Bot.LegsOf(Player.RigManager),
					", last jump ",
					(Time.time - Recorder.PlayerJumpedAt).ToString("0.0"),
					" s ago, time x",
					Time.timeScale.ToString("0.00"),
					", gravity ",
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				Vector3 val = Physics.gravity;
				obj[7] = val.y.ToString("0.0");
				obj[8] = ", avatar ";
				obj[9] = (((Object)(object)Player.RigManager != (Object)null && (Object)(object)Player.RigManager.avatar != (Object)null) ? ((Object)Player.RigManager.avatar).name : "?");
				obj[10] = ", speed ";
				object obj2;
				if (!((Object)(object)Player.PhysicsRig != (Object)null) || !((Object)(object)((Rig)Player.PhysicsRig).m_pelvis != (Object)null))
				{
					obj2 = "?";
				}
				else
				{
					val = ((Component)((Rig)Player.PhysicsRig).m_pelvis).GetComponent<Rigidbody>().velocity;
					obj2 = ((Vector3)(ref val)).ToString("0.0");
				}
				obj[11] = (string)obj2;
				obj[12] = ", moves ";
				obj[13] = MoveSlots.State();
				Event(string.Concat(obj));
				{
					foreach (Bot item17 in BotManager.All)
					{
						Event("legs, bot " + item17.Name + ": game last asked for its hands " + (Time.frameCount - item17.LastDriveFrame) + " frame(s) ago, " + Bot.LegsOf(item17.rig) + ", same legs as yours: " + ((Object)(object)Player.RigManager != (Object)null && ((Il2CppObjectBase)(object)item17.rig.remapHeptaRig).Pointer == ((Il2CppObjectBase)(object)Player.RigManager.remapHeptaRig).Pointer));
					}
					break;
				}
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
			if (!((Object)(object)r == (Object)null) && !((Object)(object)r.avatar == (Object)null))
			{
				string[] obj3 = new string[10]
				{
					BotRig.AvatarOf(r),
					" root ",
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				Vector3 val2 = ((Component)r.avatar).transform.localScale;
				obj3[2] = ((Vector3)(ref val2)).ToString("0.000");
				obj3[3] = " world ";
				val2 = ((Component)r.avatar).transform.lossyScale;
				obj3[4] = val2.y.ToString("0.000");
				obj3[5] = " eyes ";
				obj3[6] = (r.avatar.eyeHeight * ((Component)r.avatar).transform.lossyScale.y).ToString("0.00");
				obj3[7] = " m (asset eyes ";
				obj3[8] = r.avatar.eyeHeight.ToString("0.00");
				obj3[9] = ")";
				return string.Concat(obj3);
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
		FieldInfo[] fields = typeof(Guns).GetFields(BindingFlags.Static | BindingFlags.Public);
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
		Transform val = ((bot != null && (Object)(object)bot.Head != (Object)null) ? bot.Head : Player.Head);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (!Navigator.RandomPlace(val.position - Vector3.up * 1.4f, far * 0.75f, far * 1.25f, out var place))
			{
				place = val.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * far;
			}
			if ((Object)(object)Player.Head != (Object)null)
			{
				Vector3 val2 = place - Player.Head.position;
				if (((Vector3)(ref val2)).magnitude < 3f)
				{
					Vector3 val3 = place;
					val2 = place - Player.Head.position;
					place = val3 + ((Vector3)(ref val2)).normalized * 3f;
				}
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
		Transform val = ((bot != null && (Object)(object)bot.Head != (Object)null) ? bot.Head : Player.Head);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (!Navigator.RandomPlace(val.position - Vector3.up * 1.4f, far * 0.85f, far * 1.15f, out var place))
			{
				place = val.position - Vector3.up * 1.4f + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * far;
			}
			if ((Object)(object)Player.Head != (Object)null)
			{
				Vector3 val2 = place - Player.Head.position;
				if (((Vector3)(ref val2)).magnitude < 6f)
				{
					Vector3 val3 = place;
					val2 = place - Player.Head.position;
					place = val3 + ((Vector3)(ref val2)).normalized * 6f;
				}
			}
			HelperMethods.SpawnCrate("c1534c5a-3fd8-4d50-9eaf-0695466f7264", place, Quaternion.identity, Vector3.one, false, (Action<GameObject>)delegate(GameObject made)
			{
				if ((Object)(object)made != (Object)null)
				{
					fords.Add(made);
				}
			}, (Action<GameObject>)null);
		}
		Event("spawned " + count + " Ford about " + far.ToString("0") + " m out");
	}

	private static void ClearFords()
	{
		foreach (GameObject ford in fords)
		{
			if (!((Object)(object)ford == (Object)null))
			{
				Poolee componentInParent = ford.GetComponentInParent<Poolee>();
				if ((Object)(object)componentInParent != (Object)null)
				{
					componentInParent.Despawn();
				}
				else
				{
					Object.Destroy((Object)(object)ford);
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
			SpawnEnemies(Random.Range(1, 5), Random.Range(6f, 18f), (Random.value < 0.25f) ? "fast" : "zombie");
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
			Health val = (((Object)(object)Player.RigManager != (Object)null) ? Player.RigManager.health : null);
			if (!((Object)(object)val == (Object)null))
			{
				if (Safe && (int)val.healthMode != 0)
				{
					val.SetHealthMode(0);
				}
				else if (!Safe && (int)val.healthMode == 0)
				{
					val.SetHealthMode(1);
				}
				if (Safe)
				{
					val.SetFullHealth();
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
			if ((Object)(object)rigManager == (Object)null || (Object)(object)rigManager.remapHeptaRig == (Object)null)
			{
				return;
			}
			OpenControllerRig val = ((Il2CppObjectBase)(object)rigManager.ControllerRig).TryCast<OpenControllerRig>();
			bool flag = (Object)(object)val != (Object)null && (((ControllerRig)val)._primaryStick || ((ControllerRig)val)._secondaryStick);
			bool flag2 = false;
			string text = "";
			if ((Object)(object)val != (Object)null)
			{
				BaseController[] array = (BaseController[])(object)new BaseController[2]
				{
					((ControllerRig)val).leftController,
					((ControllerRig)val).rightController
				};
				foreach (BaseController val2 in array)
				{
					if (!((Object)(object)val2 == (Object)null))
					{
						bool thumbStick = val2.GetThumbStick();
						flag2 = flag2 || thumbStick;
						text = text + (((Object)(object)val2 == (Object)(object)((ControllerRig)val).leftController) ? " left " : " right ") + (thumbStick ? "DOWN" : "up");
					}
				}
			}
			string[] array2 = new string[7];
			TraversalState travState = rigManager.remapHeptaRig.travState;
			array2[0] = ((object)(TraversalState)(ref travState)).ToString();
			array2[1] = ", rig click ";
			array2[2] = (((Object)(object)val != (Object)null) ? (((ControllerRig)val)._primaryStick + "/" + ((ControllerRig)val)._secondaryStick) : "?");
			array2[3] = ", sticks";
			array2[4] = text;
			array2[5] = ", body ";
			BodyState bodyState = rigManager.bodyState;
			array2[6] = ((object)(BodyState)(ref bodyState)).ToString();
			string text2 = string.Concat(array2);
			Rigidbody val3 = (((Object)(object)Player.PhysicsRig != (Object)null && (Object)(object)((Rig)Player.PhysicsRig).m_pelvis != (Object)null) ? ((Component)((Rig)Player.PhysicsRig).m_pelvis).GetComponent<Rigidbody>() : null);
			float num;
			Vector2 val4;
			if (!((Object)(object)val3 != (Object)null))
			{
				num = 0f;
			}
			else
			{
				val4 = new Vector2(val3.velocity.x, val3.velocity.z);
				num = ((Vector2)(ref val4)).magnitude;
			}
			float num2 = num;
			if ((Object)(object)val != (Object)null)
			{
				val4 = ((ControllerRig)val)._axisPrimary;
				if (((Vector2)(ref val4)).magnitude > 0.5f && Time.time - youSpeedAt > 0.5f)
				{
					youSpeedAt = Time.time;
					string[] obj = new string[12]
					{
						"you: going ",
						num2.ToString("0.0"),
						" m/s, ",
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null
					};
					travState = rigManager.remapHeptaRig.travState;
					obj[3] = ((object)(TraversalState)(ref travState)).ToString();
					obj[4] = ", legs' own ";
					val4 = rigManager.remapHeptaRig._currentVelocity;
					obj[5] = ((Vector2)(ref val4)).magnitude.ToString("0.0");
					obj[6] = ", most now ";
					obj[7] = rigManager.remapHeptaRig.currentMaxVelocity.ToString("0.0");
					obj[8] = " of ";
					obj[9] = rigManager.remapHeptaRig.maxVelocity.ToString("0.00");
					obj[10] = ", crouch ";
					obj[11] = rigManager.remapHeptaRig._crouchTarget.ToString("0.00");
					Event(string.Concat(obj));
				}
			}
			if (text2 != youWas || (flag || flag2) != youClickWas)
			{
				youWas = text2;
				youClickWas = flag || flag2;
				string[] obj2 = new string[6]
				{
					"you: ",
					text2,
					", top speed ",
					rigManager.remapHeptaRig.maxVelocity.ToString("0.00"),
					", stick ",
					null
				};
				object obj3;
				if (!((Object)(object)val != (Object)null))
				{
					obj3 = "?";
				}
				else
				{
					val4 = ((ControllerRig)val)._axisPrimary;
					obj3 = ((Vector2)(ref val4)).ToString("0.0");
				}
				obj2[5] = (string)obj3;
				Event(string.Concat(obj2));
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
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		try
		{
			if ((Object)(object)camera == (Object)null)
			{
				camera = new GameObject("ThirdEyeLabCamera").AddComponent<Camera>();
				((Behaviour)camera).enabled = false;
				camera.fieldOfView = 55f;
				camera.nearClipPlane = 0.03f;
				camera.stereoTargetEye = (StereoTargetEyeMask)0;
				picture = new RenderTexture(800, 600, 24);
				camera.targetTexture = picture;
			}
			Vector3 facing = bot.Facing;
			Vector3 val = Vector3.Cross(Vector3.up, facing);
			Vector3 val2 = head.position - Vector3.up * 0.35f;
			Vector3 val3 = val2;
			Vector3 val4;
			switch (view)
			{
			case "side":
				val4 = val2 + val * 2.2f + Vector3.up * 0.2f;
				break;
			case "back":
				val4 = val2 - facing * 2.2f + Vector3.up * 0.3f;
				break;
			case "top":
				val4 = val2 + Vector3.up * 2.5f + facing * 0.5f;
				break;
			case "map":
				val4 = val2 + Vector3.up * 40f - facing * 0.5f;
				break;
			case "far":
				val4 = val2 - facing * 9f + Vector3.up * 5f;
				break;
			case "hand":
			{
				Transform frontHandTransform = bot.FrontHandTransform;
				val3 = (((Object)(object)frontHandTransform != (Object)null) ? frontHandTransform.position : val2);
				val4 = val3 + facing * 0.45f - val * 0.55f - Vector3.up * 0.12f;
				break;
			}
			case "gun":
			{
				Transform gunHandTransform = bot.GunHandTransform;
				val3 = (((Object)(object)gunHandTransform != (Object)null) ? gunHandTransform.position : val2);
				val4 = val3 + facing * 0.7f + val * 0.6f + Vector3.up * 0.25f;
				break;
			}
			default:
				val4 = val2 + facing * 2.4f + val * 0.6f + Vector3.up * 0.15f;
				break;
			}
			((Component)camera).transform.SetPositionAndRotation(val4, Quaternion.LookRotation(val3 - val4, Vector3.up));
			camera.Render();
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = picture;
			Texture2D val5 = new Texture2D(((Texture)picture).width, ((Texture)picture).height, (TextureFormat)3, false);
			val5.ReadPixels(new Rect(0f, 0f, (float)((Texture)picture).width, (float)((Texture)picture).height), 0, 0);
			val5.Apply();
			RenderTexture.active = active;
			byte[] bytes = ImageConversion.EncodeToPNG(val5);
			Object.Destroy((Object)val5);
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
