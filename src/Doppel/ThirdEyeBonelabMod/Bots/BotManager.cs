using System;
using System.Collections.Generic;
using System.IO;
using BoneLib;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSystem;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class BotManager
{
	public static float FollowDistance = 2f;

	public static bool SpawnGuns = true;

	public static bool OwnSpawns;

	public static bool StayNear;

	private static System.Action<Grip, Hand> onGripOn;

	private static System.Action<Grip, Hand> onGripOff;

	private static bool loopClone;

	private static BoolElement cloneToggle;

	public static bool ShowThoughts;

	private static readonly Dictionary<System.IntPtr, Bot> bots = new Dictionary<System.IntPtr, Bot>();

	private static bool spawning;

	private static float netSaidAt = -100f;

	private static BoolElement recordToggle;

	private static bool botDestroyed;

	private static int botDestroyedFrame;

	private static bool shutDown;

	private static System.Action<Gun> onGunFired;

	private static System.Action onLevelUnloaded;

	private const string Keep = "ThirdEyeBotPlayer.";

	private static BoolElement exactToggle;

	private static bool ammoSaid;

	public static int AmmoFailures;

	private static readonly Queue<(bool enemy, Vector3 feet, Vector3 facing)> toMake = new Queue<(bool, Vector3, Vector3)>();

	private static readonly List<(Bot bot, float at)> toArm = new List<(Bot, float)>();

	public static int PrefixCalls;

	public static int PrefixNoBot;

	public static int Missed;

	public static IEnumerable<Bot> All => bots.Values;

	public static void Initialize(Page page, HarmonyLib.Harmony harmony)
	{
		Patch(harmony, typeof(OpenControllerRig), "OnRealHeptaEarlyUpdate", "DrivePrefix", "RecordPostfix");
		Patch(harmony, typeof(OpenControllerRig), "OnEarlyUpdate", "EarlyPrefix", "EarlyPostfix");
		Patch(harmony, typeof(OpenControllerRig), "OnBeginCameraRendering", "CameraPrefix", null);
		Patch(harmony, typeof(RemapRig), "ApplyMovement", "MovePrefix", null);
		Patch(harmony, typeof(RigManager), "OnDestroy", "DestroyPrefix", null);
		Patch(harmony, typeof(Hooking), "OnRigManagerDestroyed", "BoneLibDestroyedPrefix", null);
		Patch(harmony, typeof(Poolee), "OnSpawn", null, "SpawnPostfix");
		Patch(harmony, typeof(Player_Health), "OnReceivedDamage", "BotHitPrefix", null);
		Patch(harmony, typeof(Player_Health), "TAKEDAMAGE", "BotDamagePrefix", null);
		Patch(harmony, typeof(PlayerDamageReceiver), "OnCollisionEnter", "BotBumpPrefix", null);
		Patch(harmony, typeof(Magazine), "OnGrab", "BotMagazinePrefix", null);
		PatchExact(harmony, typeof(AmmoInventory), "AddCartridge", new System.Type[2]
		{
			typeof(CartridgeData),
			typeof(int)
		}, "BotAmmoAddPrefix");
		PatchExact(harmony, typeof(AmmoInventory), "GetCartridgeCount", new System.Type[1] { typeof(CartridgeData) }, "BotAmmoCountPrefix");
		try
		{
			harmony.Patch(AccessTools.Method(typeof(AmmoInventory), "RemoveCartridge", new System.Type[2]
			{
				typeof(CartridgeData),
				typeof(int)
			}), new HarmonyMethod(typeof(BotManager), "BotAmmoAddPrefix"), null, null, new HarmonyMethod(typeof(BotManager), "AmmoFinalizer"), null);
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not hook AmmoInventory.RemoveCartridge: " + ex.Message);
		}
		onGunFired = delegate(Gun gun)
		{
			PlayerWatch.OnGunFired(gun);
			Clone.OnFired(gun);
			foreach (Bot value in bots.Values)
			{
				value.OnCloneGunFired(gun);
			}
			foreach (Bot value2 in bots.Values)
			{
				if (!value2.Puppet && value2.Holds(gun))
				{
					try
					{
						NetBots.GunShot(gun);
					}
					catch (System.Exception)
					{
					}
					break;
				}
			}
			Bot.LastShotAt = Time.time;
			foreach (Bot value3 in bots.Values)
			{
				value3.OnAnyGunFired(gun);
			}
			if (gun != null && gun.firePointTransform != null)
			{
				foreach (Bot value4 in bots.Values)
				{
					if (!value4.Holds(gun))
					{
						value4.Heard(gun.firePointTransform.position);
					}
				}
			}
		};
		Hooking.OnPostFireGun += onGunFired;
		Entry.NetStart();
		onGripOn = delegate(Grip grip, Hand hand)
		{
			Lessons.OnGrip(grip, hand, on: true);
			Clone.OnGrip(grip, hand, on: true);
			ExactGrip(grip, hand, on: true);
			ClimbMoves.OnGrip(grip, hand, on: true);
		};
		onGripOff = delegate(Grip grip, Hand hand)
		{
			Clone.OnGrip(grip, hand, on: false);
			ExactGrip(grip, hand, on: false);
			ClimbMoves.OnGrip(grip, hand, on: false);
		};
		Hooking.OnGripAttached += onGripOn;
		Hooking.OnGripDetached += onGripOff;
		onLevelUnloaded = delegate
		{
			Lab.ForgetLevel();
			SelfTaught.Save();
			Lessons.ForgetLevel();
			HeadsetGuard.ForgetLevel();
			Knowledge.Save();
			GunMoves.Save();
			Bot.ForgetDropped();
			Targets.ForgetLevel();
			Tactics.ForgetLevel();
			NetBots.Forget();
			Movers.Forget();
			bots.Clear();
			spawning = false;
			Recorder.Stop();
		};
		Hooking.OnLevelUnloaded += onLevelUnloaded;
		Carried();
		page.CreateFunction("Spawn bot", Color.green, delegate
		{
			Spawn(enemy: false);
		});
		page.CreateFunction("Spawn enemy bot (fights you)", Color.red, delegate
		{
			Spawn(enemy: true);
		});
		page.CreateFunction("Battle: 5 friends vs 5 enemies (random guns)", Color.yellow, Battle);
		page.CreateFunction("Remove bots", Color.red, RemoveAll);
		page.CreateFunction("Bring bots to me", Color.white, BringAll);
		page.CreateFunction("Play on its own (brain)", Color.magenta, delegate
		{
			SetMode(Bot.Mode.Brain);
		});
		page.CreateBool("Practise driving (learns by itself)", Color.magenta, DriveSkill.Practice, delegate(bool v)
		{
			DriveSkill.Practice = v;
		});
		page.CreateBool("Show what it is thinking", Color.magenta, ShowThoughts, delegate(bool v)
		{
			ShowThoughts = v;
		});
		page.CreateBool("Pop-up messages", Color.white, Spawnables.Popups, delegate(bool v)
		{
			Spawnables.Popups = v;
		});
		recordToggle = page.CreateBool("Learn from me (watches everything you do)", Color.green, Recorder.Recording, SetLearning);
		page.CreateFunction("Forget the last 30 seconds", Color.yellow, ForgetLast);
		page.CreateFunction("Forget what it learned", Color.red, delegate
		{
			Knowledge.Forget();
			SelfTaught.Forget();
			Spawnables.Notify("It has forgotten everything it learned");
		});
		MoveSlots.BuildMenu(page);
		page.CreateFunction("Stand still", Color.white, delegate
		{
			SetMode(Bot.Mode.Stand);
		});
		page.CreateFunction("Follow me", Color.white, delegate
		{
			SetMode(Bot.Mode.Follow);
		});
		cloneToggle = page.CreateBool("Record clone (exact copy of me)", Color.red, Clone.Recording, Clone.Set);
		page.CreateFunction("Play clone", Color.green, PlayClone);
		exactToggle = page.CreateBool("Record EXACT clone (new)", Color.red, ExactClone.Recording, ExactClone.Set);
		page.CreateFunction("Play EXACT clone (new)", Color.green, PlayExact);
		page.CreateBool("Loop clone", Color.white, loopClone, delegate(bool v)
		{
			loopClone = v;
		});
		page.CreateFunction("Copy my avatar", Color.white, delegate
		{
			foreach (Bot value5 in bots.Values)
			{
				BotRig.CopyPlayerAvatar(value5.rig, null);
			}
		});
	}

	private static void SetLearning(bool on)
	{
		Recorder.Set(on);
		GunMoves.Teaching = on;
		if (on)
		{
			Spawnables.Notify("Learning from you: play as you like, it sorts it out (pick up any tool and use it: it learns that too)");
			return;
		}
		MotionLibrary.Reload();
		GripBook.Save();
		Spawnables.Notify("Learned: " + MotionLibrary.Summary() + "; " + GripBook.Summary() + "; " + Lessons.Summary() + ". Next: " + MotionLibrary.TeachNext());
	}

	private static void ForgetLast()
	{
		int num = GunMoves.ForgetSince(Time.time - 30f) + ClimbMoves.ForgetSince(Time.time - 30f) + Lessons.ForgetSince(Time.time - 30f);
		bool num2 = Recorder.ForgetLast(30f);
		MotionLibrary.Reload();
		Spawnables.Notify((num2 || num > 0) ? ("Forgot the last 30 seconds" + ((num > 0) ? (" (" + num + " lessons)") : "")) : "Nothing to forget");
	}

	public static void Shutdown()
	{
		Entry.NetStop();
		WallGrab.DropAll();
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.lab", Lab.On);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.safe", Lab.Safe);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.thoughts", ShowThoughts);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.ownSpawns", OwnSpawns);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.stayNear", StayNear);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.drivePractice", DriveSkill.Practice);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.practice", Targets.Practice);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.follow", FollowDistance);
		System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.teach", GunMoves.Teaching);
		foreach (Bot value in bots.Values)
		{
			System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.mode", value.KeptMode.ToString());
			System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.gun." + value.rig.Pointer, value.OwnGun);
			System.AppDomain.CurrentDomain.SetData("ThirdEyeBotPlayer.stow." + value.rig.Pointer, value.KeepStowed());
			try
			{
				value.Detach();
			}
			catch (System.Exception)
			{
			}
		}
		bots.Clear();
		shutDown = true;
		Hooking.OnPostFireGun -= onGunFired;
		Hooking.OnLevelUnloaded -= onLevelUnloaded;
		Hooking.OnGripAttached -= onGripOn;
		Hooking.OnGripDetached -= onGripOff;
		Clone.Stop();
		MoveSlots.Shutdown();
		GripBook.Shutdown();
		SelfTaught.Save();
		Lessons.Cancel();
		Recorder.Stop();
		Knowledge.Save();
		GunMoves.Save();
		Lab.Shutdown();
	}

	private static void Carried()
	{
		System.AppDomain currentDomain = System.AppDomain.CurrentDomain;
		if (currentDomain.GetData("ThirdEyeBotPlayer.lab") is bool on)
		{
			Lab.On = on;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.safe") is bool safe)
		{
			Lab.Safe = safe;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.thoughts") is bool showThoughts)
		{
			ShowThoughts = showThoughts;
		}
		OwnSpawns = false;
		if (currentDomain.GetData("ThirdEyeBotPlayer.stayNear") is bool stayNear)
		{
			StayNear = stayNear;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.drivePractice") is bool practice)
		{
			DriveSkill.Practice = practice;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.practice") is bool practice2)
		{
			Targets.Practice = practice2;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.follow") is float followDistance)
		{
			FollowDistance = followDistance;
		}
		if (currentDomain.GetData("ThirdEyeBotPlayer.teach") is bool teaching)
		{
			GunMoves.Teaching = teaching;
		}
		Bot.Mode result = Bot.Mode.Stand;
		if (currentDomain.GetData("ThirdEyeBotPlayer.mode") is string value)
		{
			System.Enum.TryParse<Bot.Mode>(value, out result);
		}
		try
		{
			foreach (RigManager item in UnityEngine.Object.FindObjectsOfType<RigManager>())
			{
				if (!BotRig.IsBot(item) || bots.ContainsKey(item.Pointer))
				{
					continue;
				}
				if (item.transform.Find("ThirdEyeNetPuppet") != null)
				{
					UnityEngine.Object.Destroy(item.gameObject);
					continue;
				}
				OpenControllerRig openControllerRig = item.ControllerRig.TryCast<OpenControllerRig>();
				float yaw = ((openControllerRig != null && openControllerRig.vrRoot != null) ? openControllerRig.vrRoot.eulerAngles.y : item.transform.eulerAngles.y);
				Bot bot = new Bot(item, yaw);
				bots[item.Pointer] = bot;
				bot.TakeStowed(currentDomain.GetData("ThirdEyeBotPlayer.stow." + item.Pointer) as object[]);
				currentDomain.SetData("ThirdEyeBotPlayer.stow." + item.Pointer, null);
				if (result != 0)
				{
					bot.SetMode(result);
				}
				Gun gun = ((item.physicsRig != null) ? Player.GetComponentInHand<Gun>(item.physicsRig.rightHand) : null);
				Gun gun2 = gun ?? (currentDomain.GetData("ThirdEyeBotPlayer.gun." + item.Pointer) as Gun);
				if (gun2 != null)
				{
					bot.AdoptGun(gun2, gun != null);
				}
			}
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: the bots about could not be taken over: " + ex.Message);
		}
		if (bots.Count > 0)
		{
			Lab.Event("new build took over " + bots.Count + " bot(s), mode " + result);
		}
	}

	private static void PatchExact(HarmonyLib.Harmony harmony, System.Type type, string method, System.Type[] takes, string prefix)
	{
		try
		{
			harmony.Patch(AccessTools.Method(type, method, takes), new HarmonyMethod(typeof(BotManager), prefix));
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not hook " + type.Name + "." + method + ": " + ex.Message);
		}
	}

	private static void Patch(HarmonyLib.Harmony harmony, System.Type type, string method, string prefix, string postfix)
	{
		try
		{
			harmony.Patch(AccessTools.Method(type, method), (prefix != null) ? new HarmonyMethod(typeof(BotManager), prefix) : null, (postfix != null) ? new HarmonyMethod(typeof(BotManager), postfix) : null);
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not hook " + type.Name + "." + method + ": " + ex.Message);
		}
	}

	public static void Update()
	{
		if (shutDown)
		{
			return;
		}
		try
		{
			GunWeight.Sweep();
			Race.Step();
			CarDoors.Sweep();
			Orders.Step();
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: gun weight: " + ex.Message);
		}
		try
		{
			MakeBattle();
		}
		catch (System.Exception ex2)
		{
			MelonLogger.Warning("Bot Player: battle: " + ex2.Message);
			toMake.Clear();
			toArm.Clear();
		}
		HeadsetGuard.Update();
		Lab.Update();
		Recorder.Watch();
		Recorder.Update();
		Clone.Update();
		MoveSlots.Update();
		Lessons.Update();
		Lab.Switch();
		Lab.KeepSafe();
		WallGrab.Update();
		ClimbMoves.Update();
		Knowledge.Tick(Time.deltaTime, bots.Count > 0);
		SelfTaught.Tick();
		GunMoves.Update();
		GripBook.Update();
		if (bots.Count > 0 || GunMoves.Teaching)
		{
			PlayerWatch.Update();
		}
		if (bots.Count > 0)
		{
			Gestures.Update();
			Movers.Update();
			PortalWays.Update();
		}
		Bot.ClearDropped();
		try
		{
			NetBots.Update(bots.Values);
		}
		catch (System.Exception ex3)
		{
			if (Time.time - netSaidAt > 10f)
			{
				netSaidAt = Time.time;
				MelonLogger.Warning("Bot Player: sharing the bots with the other players went wrong: " + ex3.Message);
			}
		}
		if (bots.Count == 0)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		List<System.IntPtr> list = null;
		foreach (KeyValuePair<System.IntPtr, Bot> bot in bots)
		{
			if (!bot.Value.Alive)
			{
				(list ?? (list = new List<System.IntPtr>())).Add(bot.Key);
				continue;
			}
			try
			{
				Lab.Who = bot.Value.Name;
				bot.Value.Update(deltaTime);
				if (Time.frameCount - bot.Value.LastDriveFrame > 2)
				{
					bot.Value.Drive();
					Missed++;
				}
			}
			finally
			{
				Lab.Who = null;
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (System.IntPtr item in list)
		{
			if (bots.ContainsKey(item))
			{
				try
				{
					bots[item].Remove();
				}
				catch (System.Exception)
				{
				}
				bots.Remove(item);
			}
		}
	}

	private static Bot Find(RigManager rig)
	{
		if (!(rig != null) || !bots.TryGetValue(rig.Pointer, out var value))
		{
			return null;
		}
		return value;
	}

	public static void FixedUpdate()
	{
		if (shutDown)
		{
			return;
		}
		foreach (Bot value in bots.Values)
		{
			try
			{
				if (value.Puppet)
				{
					value.NetFixed(Time.fixedDeltaTime);
				}
				else if (!value.Exact)
				{
					Lab.Who = value.Name;
					value.CloneFixed(Time.fixedDeltaTime);
					value.ClimbFixed(Time.fixedDeltaTime);
					value.StowFollow();
					value.SpeedGuard();
					Lab.Who = null;
					value.GunFixed(Time.fixedDeltaTime);
				}
			}
			catch (System.Exception ex)
			{
				MelonLogger.Error("Bot Player clone: " + ex.Message);
			}
		}
	}

	private static void RecordPostfix(OpenControllerRig __instance)
	{
		if (!shutDown && ExactClone.Recording && __instance != null && __instance.manager == Player.RigManager)
		{
			ExactClone.NoteFrame(__instance);
		}
	}

	private static void ExactGrip(Grip grip, Hand hand, bool on)
	{
		ExactClone.OnGrip(grip, hand, on);
		((hand != null) ? Find(hand.manager) : null)?.ExactGrip(grip, hand, on);
	}

	public static void PlayExact()
	{
		if (ExactClone.Recording)
		{
			ExactClone.Stop();
			if (exactToggle != null)
			{
				exactToggle.Value = false;
			}
		}
		if (bots.Count == 0)
		{
			Spawnables.Notify("Spawn a bot first");
			return;
		}
		using Dictionary<System.IntPtr, Bot>.ValueCollection.Enumerator enumerator = bots.Values.GetEnumerator();
		if (enumerator.MoveNext())
		{
			enumerator.Current.PlayExact(ExactClone.Latest(), loopClone);
		}
	}

	public static void PlayClone()
	{
		if (Clone.Recording)
		{
			Clone.Stop();
			if (cloneToggle != null)
			{
				cloneToggle.Value = false;
			}
		}
		if (bots.Count == 0)
		{
			Spawnables.Notify("Spawn a bot first");
			return;
		}
		using Dictionary<System.IntPtr, Bot>.ValueCollection.Enumerator enumerator = bots.Values.GetEnumerator();
		if (enumerator.MoveNext())
		{
			enumerator.Current.PlayClone(Clone.Last, loopClone);
		}
	}

	public static void UseLessons(string named)
	{
		if (bots.Count == 0)
		{
			Spawnables.Notify("Spawn a bot first");
			return;
		}
		if (Lessons.Count == 0)
		{
			Spawnables.Notify("Nothing learned yet: turn on Learn from me, pick a tool up and use it");
			return;
		}
		foreach (Bot value in bots.Values)
		{
			Lessons.Lesson lesson = Lessons.Pick(null, named);
			if (lesson == null)
			{
				Spawnables.Notify("No lesson for that");
			}
			else if (value.UseLesson(lesson, atSpot: false))
			{
				Spawnables.Notify("Using the " + lesson.name + " as you did");
			}
		}
	}

	public static void Preview(Frame[] clip)
	{
		Transform head = Player.Head;
		Bot bot = null;
		float num = float.MaxValue;
		foreach (Bot value in bots.Values)
		{
			if (!(value.Head == null) && !value.dead && !value.Down && !(head == null))
			{
				float magnitude = (value.Head.position - head.position).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					bot = value;
				}
			}
		}
		bot?.Preview(clip);
	}

	public static string FreshStart()
	{
		Recorder.Stop();
		if (recordToggle != null)
		{
			recordToggle.Value = false;
		}
		GunMoves.Save();
		string path = AssetFiles.SaveFolder("");
		string text = Path.Combine(path, "backup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
		Directory.CreateDirectory(text);
		int num = 0;
		string[] array = new string[3] { "recordings", "lessons", "moves" };
		foreach (string path2 in array)
		{
			string text2 = Path.Combine(path, path2);
			if (Directory.Exists(text2) && Directory.GetFileSystemEntries(text2).Length != 0)
			{
				Directory.Move(text2, Path.Combine(text, path2));
				num++;
			}
		}
		array = new string[2] { "gunmoves.json", "climbs.json" };
		foreach (string path3 in array)
		{
			string text3 = Path.Combine(path, path3);
			if (File.Exists(text3))
			{
				File.Move(text3, Path.Combine(text, path3));
				num++;
			}
		}
		GunMoves.Reset();
		ClimbMoves.Reset();
		Lessons.Reset();
		MoveSlots.Reload();
		MotionLibrary.Reload();
		Lab.Event("fresh start: " + num + " things put away in " + text);
		return text;
	}

	public static void SetCloneLoop(bool on)
	{
		loopClone = on;
	}

	private static Bot BotOf(Player_Health health)
	{
		RigManager rigManager = ((health != null) ? health.GetComponentInParent<RigManager>() : null);
		if (!(rigManager != null) || !BotRig.IsBot(rigManager) || !bots.TryGetValue(rigManager.Pointer, out var value))
		{
			return null;
		}
		return value;
	}

	private static bool BotMagazinePrefix(Hand hand)
	{
		try
		{
			return shutDown || hand == null || !BotRig.IsBot(hand.GetComponentInParent<RigManager>());
		}
		catch (System.Exception)
		{
			return true;
		}
	}

	private static bool AmmoWorks(AmmoInventory pouch)
	{
		if (shutDown)
		{
			return true;
		}
		try
		{
			bool flag = pouch != null && BotRig.IsBot(pouch.GetComponentInParent<RigManager>());
			if ((pouch == null || flag) && !ammoSaid)
			{
				ammoSaid = true;
				Lab.Event("ammo: a call to " + ((pouch == null) ? "no pouch at all" : "a bot's pouch") + " left out");
			}
			return pouch != null && !flag;
		}
		catch (System.Exception)
		{
			return true;
		}
	}

	private static bool BotAmmoAddPrefix(AmmoInventory __instance)
	{
		return AmmoWorks(__instance);
	}

	private static System.Exception AmmoFinalizer(System.Exception __exception)
	{
		if (__exception != null)
		{
			AmmoFailures++;
		}
		if (!shutDown)
		{
			return null;
		}
		return __exception;
	}

	private static bool BotAmmoCountPrefix(AmmoInventory __instance, ref int __result)
	{
		if (AmmoWorks(__instance))
		{
			return true;
		}
		__result = 999;
		return false;
	}

	private static void BotBumpPrefix(PlayerDamageReceiver __instance, Collision collision)
	{
		if (shutDown || bots.Count == 0 || __instance == null || collision == null)
		{
			return;
		}
		try
		{
			if (!(collision.impulse.sqrMagnitude < 25f))
			{
				Find(__instance.GetComponentInParent<RigManager>())?.Bumped(__instance.bodyPart, collision);
			}
		}
		catch (System.Exception)
		{
		}
	}

	private static bool BotHitPrefix(Player_Health __instance, Attack attack, PlayerDamageReceiver.BodyPart part)
	{
		if (shutDown)
		{
			return true;
		}
		Bot bot = BotOf(__instance);
		if (bot == null)
		{
			return true;
		}
		try
		{
			bot.TakeHit(attack?.damage ?? 0f, attack, part);
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private static bool BotDamagePrefix(Player_Health __instance, float damage)
	{
		if (shutDown)
		{
			return true;
		}
		Bot bot = BotOf(__instance);
		if (bot == null)
		{
			return true;
		}
		bot.TakeKnock(damage);
		return false;
	}

	private static void SpawnPostfix(Poolee __instance)
	{
		if (!shutDown)
		{
			HeadsetGuard.OnSpawn(__instance);
			Bot.NoteSpawn(__instance);
			Clone.OnSpawn(__instance);
			ExactClone.OnSpawn(__instance);
		}
	}

	public static Bot Of(RigManager rig)
	{
		if (!(rig != null) || !bots.TryGetValue(rig.Pointer, out var value))
		{
			return null;
		}
		return value;
	}

	public static void SpawnBot()
	{
		Spawn(enemy: false);
	}

	public static void SpawnEnemyBot()
	{
		Spawn(enemy: true);
	}

	public static bool IsEnemy(RigManager rig)
	{
		if (rig != null && bots.TryGetValue(rig.Pointer, out var value))
		{
			return value.enemy;
		}
		return false;
	}

	public static void RemoveAllBots()
	{
		RemoveAll();
	}

	public static void BringAllBots()
	{
		BringAll();
	}

	public static void SetModeAll(Bot.Mode mode)
	{
		SetMode(mode);
	}

	private static void SetMode(Bot.Mode mode)
	{
		if (bots.Count == 0)
		{
			Spawnables.Notify("Spawn a bot first");
			return;
		}
		if (mode == Bot.Mode.Replay && Recorder.Recording)
		{
			Recorder.Stop();
			if (recordToggle != null)
			{
				recordToggle.Value = false;
			}
		}
		foreach (Bot value in bots.Values)
		{
			if (!value.Puppet)
			{
				value.SetMode(mode);
			}
		}
	}

	private static void BringAll()
	{
		Transform head = Player.Head;
		if (head == null)
		{
			return;
		}
		Vector3 forward = head.forward;
		forward.y = 0f;
		forward = ((forward.sqrMagnitude < 0.001f) ? Vector3.forward : forward.normalized);
		int num = 0;
		foreach (Bot value in bots.Values)
		{
			if (!value.Puppet)
			{
				Vector3 vector = Vector3.Cross(Vector3.up, forward) * (((num % 2 == 0) ? 1f : (-1f)) * (float)(num + 1) / 2f * 1.2f);
				Vector3 vector2 = head.position + forward * 1.8f + vector;
				if (Physics.Raycast(vector2, Vector3.down, out var hitInfo, 4f, -1, QueryTriggerInteraction.Ignore))
				{
					vector2 = hitInfo.point;
				}
				value.BringTo(vector2, -forward);
				num++;
			}
		}
	}

	private static void Spawn(bool enemy)
	{
		Transform head = Player.Head;
		if (!(head == null) && !spawning)
		{
			Vector3 forward = head.forward;
			forward.y = 0f;
			forward = ((forward.sqrMagnitude < 0.001f) ? Vector3.forward : forward.normalized);
			Vector3 vector = head.position + forward * 2f;
			if (Physics.Raycast(vector, Vector3.down, out var hitInfo, 4f, -1, QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point;
			}
			else
			{
				vector.y -= 1.6f;
			}
			SpawnAt(enemy, vector, -forward, null);
		}
	}

	private static void Battle()
	{
		int num = Mathf.Clamp(Mathf.RoundToInt(Tuning.F("battle.each", 5f)), 1, 8);
		Battle(num, num);
	}

	public static void LabBattle(int friends, int enemies)
	{
		Battle(Mathf.Clamp(friends, 0, 8), Mathf.Clamp(enemies, 1, 8));
	}

	private static void Battle(int friends, int enemies)
	{
		Transform head = Player.Head;
		if (head == null)
		{
			return;
		}
		Vector3 forward = head.forward;
		forward.y = 0f;
		forward = ((forward.sqrMagnitude < 0.001f) ? Vector3.forward : forward.normalized);
		Vector3 vector = Vector3.Cross(Vector3.up, forward);
		int num = Mathf.Max(friends, enemies);
		float num2 = Tuning.F("battle.apart", 40f);
		float num3 = Tuning.F("battle.gap", 2.2f);
		Vector3 vector2 = head.position + forward * 3f;
		float num4;
		for (num4 = num2; num4 >= 8f; num4 -= 3f)
		{
			int num5 = 0;
			for (int i = 0; i < num; i++)
			{
				if (Ground(vector2 + forward * num4 + vector * ((float)i - (float)(num - 1) * 0.5f) * num3, out var _))
				{
					num5++;
				}
			}
			if (num5 == num)
			{
				break;
			}
		}
		if (num4 < 8f)
		{
			Spawnables.Notify("No open ground ahead for a battle: face somewhere with room");
			return;
		}
		for (int j = 0; j < num; j++)
		{
			Vector3 vector3 = vector * ((float)j - (float)(num - 1) * 0.5f) * num3;
			if (j < friends && Ground(vector2 + vector3, out var feet2))
			{
				toMake.Enqueue((false, feet2, forward));
			}
			if (j < enemies && Ground(vector2 + forward * num4 + vector3, out var feet3))
			{
				toMake.Enqueue((true, feet3, -forward));
			}
		}
		Spawnables.Notify("Battle: " + friends + " against " + enemies + ", " + num4.ToString("0") + " m apart");
		Lab.Event("battle: " + friends + " against " + enemies + ", " + num4.ToString("0") + " m apart");
	}

	private static bool Ground(Vector3 near, out Vector3 feet)
	{
		feet = near;
		if (!Physics.Raycast(near + Vector3.up * 3f, Vector3.down, out var hitInfo, 10f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (hitInfo.normal.y < 0.7f || Physics.CheckSphere(hitInfo.point + Vector3.up * 1.1f, 0.3f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		feet = hitInfo.point;
		return true;
	}

	private static void MakeBattle()
	{
		if (!spawning && toMake.Count > 0)
		{
			(bool, Vector3, Vector3) tuple = toMake.Dequeue();
			SpawnAt(tuple.Item1, tuple.Item2, tuple.Item3, delegate(Bot bot)
			{
				bot.SetMode(Bot.Mode.Brain);
				toArm.Add((bot, Time.time + 1.5f));
			});
		}
		for (int num = toArm.Count - 1; num >= 0; num--)
		{
			(Bot, float) tuple2 = toArm[num];
			if (!tuple2.Item1.Alive)
			{
				toArm.RemoveAt(num);
			}
			else if (!(Time.time < tuple2.Item2))
			{
				toArm.RemoveAt(num);
				string text = Bot.RandomGun();
				if (text != null)
				{
					tuple2.Item1.LabGive(text);
				}
			}
		}
	}

	private static void SpawnAt(bool enemy, Vector3 feet, Vector3 facing, System.Action<Bot> made)
	{
		if (spawning)
		{
			return;
		}
		Vector3 vector = -facing;
		float yaw = Recorder.Yaw(-vector);
		spawning = true;
		BotRig.Create(feet, -vector, delegate(RigManager rig)
		{
			spawning = false;
			if (!shutDown)
			{
				if (enemy)
				{
					new GameObject("BotEnemyMark").transform.SetParent(rig.transform, worldPositionStays: false);
				}
				Bot bot = new Bot(rig, yaw);
				bots[rig.Pointer] = bot;
				BotRig.CopyPlayerAvatar(rig, null);
				if (enemy)
				{
					bot.SetMode(Bot.Mode.Brain);
					if (made == null)
					{
						Spawnables.Notify("Enemy bot spawned (" + bot.personality.Describe() + "): it is coming for you");
					}
				}
				else if (made == null)
				{
					Spawnables.Notify("Bot spawned: " + bot.personality.Describe());
				}
				made?.Invoke(bot);
			}
		});
	}

	private static void RemoveAll()
	{
		toMake.Clear();
		toArm.Clear();
		List<System.IntPtr> list = new List<System.IntPtr>();
		foreach (KeyValuePair<System.IntPtr, Bot> bot in bots)
		{
			if (!bot.Value.Puppet)
			{
				bot.Value.Remove();
				list.Add(bot.Key);
			}
		}
		foreach (System.IntPtr item in list)
		{
			bots.Remove(item);
		}
	}

	public static bool NetSpawn(bool enemy, string avatar, Vector3 scale, Vector3 feet, float yaw, System.Action<Bot> made)
	{
		if (spawning || shutDown || Player.Head == null)
		{
			return false;
		}
		spawning = true;
		BotRig.Create(feet, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, delegate(RigManager rig)
		{
			spawning = false;
			if (shutDown || rig == null)
			{
				made(null);
			}
			else
			{
				if (enemy)
				{
					new GameObject("BotEnemyMark").transform.SetParent(rig.transform, worldPositionStays: false);
				}
				Bot bot = new Bot(rig, yaw);
				bots[rig.Pointer] = bot;
				made(bot);
				bot.NetWear(avatar, scale);
			}
		});
		return true;
	}

	public static void NetRemove(Bot bot)
	{
		if (bot == null)
		{
			return;
		}
		System.IntPtr key = System.IntPtr.Zero;
		bool flag = false;
		foreach (KeyValuePair<System.IntPtr, Bot> bot2 in bots)
		{
			if (bot2.Value == bot)
			{
				key = bot2.Key;
				flag = true;
				break;
			}
		}
		try
		{
			bot.Remove();
		}
		catch (System.Exception)
		{
		}
		if (flag)
		{
			bots.Remove(key);
		}
	}

	private static void DrivePrefix(OpenControllerRig __instance)
	{
		if (shutDown)
		{
			return;
		}
		PrefixCalls++;
		Bot bot = Find(__instance.manager);
		if (bot == null)
		{
			PrefixNoBot++;
			return;
		}
		try
		{
			Lab.Who = bot.Name;
			bot.Drive();
		}
		finally
		{
			Lab.Who = null;
		}
	}

	private static void EarlyPrefix(OpenControllerRig __instance, ref Il2CppSystem.Action<bool> __state)
	{
		__state = OpenControllerRig.OnPauseStateChange;
		if (!shutDown && BotRig.IsBot(__instance.manager))
		{
			OpenControllerRig.OnPauseStateChange = null;
		}
	}

	private static void EarlyPostfix(ref Il2CppSystem.Action<bool> __state)
	{
		OpenControllerRig.OnPauseStateChange = __state;
	}

	private static bool CameraPrefix(OpenControllerRig __instance)
	{
		if (!shutDown)
		{
			return !BotRig.IsBot(__instance.manager);
		}
		return true;
	}

	private static void MovePrefix(RemapRig __instance, ref Vector2 axis, ref bool inputPressed)
	{
		if (shutDown)
		{
			return;
		}
		Bot bot = Find(__instance.manager);
		if (bot == null)
		{
			if (ExactClone.Recording && __instance.manager == Player.RigManager)
			{
				ExactClone.NoteMove(axis, inputPressed);
			}
		}
		else if (!bot.ExactMove(ref axis, ref inputPressed))
		{
			axis = bot.Walk;
			inputPressed = axis.sqrMagnitude > 0.0004f;
		}
	}

	private static void DestroyPrefix(RigManager __instance)
	{
		if (!shutDown && BotRig.IsBot(__instance))
		{
			botDestroyed = true;
			botDestroyedFrame = Time.frameCount;
		}
	}

	private static bool BoneLibDestroyedPrefix()
	{
		if (shutDown || !botDestroyed)
		{
			return true;
		}
		botDestroyed = false;
		return botDestroyedFrame != Time.frameCount;
	}
}
