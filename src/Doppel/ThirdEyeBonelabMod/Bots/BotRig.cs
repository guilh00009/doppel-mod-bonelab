using System;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using Il2CppSystem;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class BotRig
{
	public const string Name = "[RigManager (Bot)]";

	public static bool IsBot(RigManager rig)
	{
		if ((Object)(object)rig != (Object)null)
		{
			return ((Object)((Component)rig).gameObject).name == "[RigManager (Bot)]";
		}
		return false;
	}

	public static void Create(Vector3 feet, Vector3 forward, Action<RigManager> made)
	{
		MarrowSettings runtimeInstance = MarrowSettings.RuntimeInstance;
		SpawnableCrate val = (((Object)(object)runtimeInstance != (Object)null && runtimeInstance.DefaultPlayerRig != null) ? ((CrateReferenceT<SpawnableCrate>)(object)runtimeInstance.DefaultPlayerRig).Crate : null);
		if ((Object)(object)val == (Object)null)
		{
			Spawnables.Notify("The game's player rig could not be found");
			return;
		}
		((MarrowAssetT<GameObject>)(object)((GameObjectCrate)val).MainGameObject).LoadAsset(Action<GameObject>.op_Implicit((Action<GameObject>)delegate(GameObject asset)
		{
			try
			{
				made(Make(asset, feet, forward));
			}
			catch (Exception ex)
			{
				MelonLogger.Error("The bot's body could not be made: " + ex);
			}
		}));
	}

	private static RigManager Make(GameObject asset, Vector3 feet, Vector3 forward)
	{
		RigManager obj = (((Object)(object)asset != (Object)null) ? asset.GetComponentInChildren<RigManager>(true) : null);
		if ((Object)(object)obj == (Object)null)
		{
			throw new InvalidOperationException("the player rig has no RigManager");
		}
		GameObject val = new GameObject("ThirdEyeBotHolder");
		val.SetActive(false);
		GameObject obj2 = Object.Instantiate<GameObject>(((Component)obj).gameObject, val.transform);
		((Object)obj2).name = "[RigManager (Bot)]";
		obj2.SetActive(false);
		obj2.transform.position = feet;
		obj2.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
		RigManager component = obj2.GetComponent<RigManager>();
		Strip(component);
		obj2.transform.parent = null;
		Object.Destroy((Object)(object)val);
		obj2.SetActive(true);
		Hear(component);
		return component;
	}

	private static void Strip(RigManager rig)
	{
		OpenControllerRig val = ((Il2CppObjectBase)(object)rig.ControllerRig).TryCast<OpenControllerRig>();
		PhysicsRig physicsRig = rig.physicsRig;
		RemovePlayerTag(physicsRig.marrowEntity);
		foreach (MarrowBody componentsInChild in ((Component)val).GetComponentsInChildren<MarrowBody>(true))
		{
			Object.DestroyImmediate((Object)(object)componentsInChild);
		}
		foreach (Tracker componentsInChild2 in ((Component)val).GetComponentsInChildren<Tracker>(true))
		{
			Object.DestroyImmediate((Object)(object)((Component)componentsInChild2).gameObject);
		}
		MarrowEntity component = ((Component)val).GetComponent<MarrowEntity>();
		if ((Object)(object)component != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)component);
		}
		Player_Health val2 = (((Object)(object)rig.health != (Object)null) ? ((Il2CppObjectBase)(object)rig.health).TryCast<Player_Health>() : null);
		if ((Object)(object)val2 != (Object)null)
		{
			val2.reloadLevelOnDeath = false;
			val2.slowMoOnDeath = false;
			((Health)val2).healthMode = (HealthMode)0;
			if ((Object)(object)val2.Vignetter != (Object)null)
			{
				GameObject val3 = Object.Instantiate<GameObject>(val2.Vignetter);
				SkinnedMeshRenderer component2 = val3.GetComponent<SkinnedMeshRenderer>();
				if ((Object)(object)component2 != (Object)null)
				{
					((Renderer)component2).enabled = false;
				}
				((Object)val3).name = "ThirdEyeBotVignetter";
				val3.SetActive(false);
				val2.Vignetter = val3;
			}
		}
		val.quickmenuEnabled = false;
		val._timeInput = false;
		rig.remapHeptaRig.doubleJump = false;
		Transform headset = val.headset;
		Blind(headset);
		val.cameras = new Il2CppReferenceArray<Camera>(0L);
		val.onLastCameraUpdate = null;
		((Component)headset).tag = "Untagged";
		PlayerAvatarArt componentInChildren = ((Component)rig).GetComponentInChildren<PlayerAvatarArt>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)componentInChildren);
		}
		InventoryAmmoReceiver componentInChildren2 = ((Component)rig).GetComponentInChildren<InventoryAmmoReceiver>(true);
		if ((Object)(object)componentInChildren2 != (Object)null && (Object)(object)((Component)componentInChildren2).GetComponent<Collider>() != (Object)null)
		{
			((Component)componentInChildren2).GetComponent<Collider>().enabled = false;
		}
		Hand[] array = (Hand[])(object)new Hand[2] { physicsRig.leftHand, physicsRig.rightHand };
		foreach (Hand val4 in array)
		{
			PhysHand val5 = (((Object)(object)val4 != (Object)null) ? ((Component)val4).GetComponent<PhysHand>() : null);
			if ((Object)(object)val5 != (Object)null && (Object)(object)val5.inventoryPlug != (Object)null)
			{
				((Collider)val5.inventoryPlug).enabled = false;
				((Component)val5.inventoryPlug).gameObject.SetActive(false);
			}
		}
		foreach (InventorySlotReceiver componentsInChild3 in ((Component)rig).GetComponentsInChildren<InventorySlotReceiver>(true))
		{
			foreach (BoxCollider componentsInChild4 in ((Component)componentsInChild3).GetComponentsInChildren<BoxCollider>(true))
			{
				if (((Collider)componentsInChild4).isTrigger)
				{
					componentsInChild4.size *= 0.4f;
				}
			}
		}
		rig.ControllerRig.leftController = Detach(rig.ControllerRig, rig.ControllerRig.leftController, (Handedness)1);
		rig.ControllerRig.rightController = Detach(rig.ControllerRig, rig.ControllerRig.rightController, (Handedness)2);
		if ((Object)(object)((Component)physicsRig).GetComponent<Poolee>() == (Object)null)
		{
			((Component)physicsRig).gameObject.AddComponent<Poolee>();
		}
	}

	private static BaseController Detach(ControllerRig controls, BaseController original, Handedness side)
	{
		GameObject gameObject = ((Component)original).gameObject;
		Haptor haptor = original.haptor;
		UIControllerInput component = gameObject.GetComponent<UIControllerInput>();
		if ((Object)(object)component != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)component);
		}
		BaseController val = gameObject.AddComponent<BaseController>();
		val.contRig = controls;
		val.handedness = side;
		if ((Object)(object)haptor != (Object)null)
		{
			haptor.device_Controller = val;
			haptor.hapticsAllowed = false;
			((Behaviour)haptor).enabled = false;
		}
		return val;
	}

	private static void RemovePlayerTag(MarrowEntity entity)
	{
		MarrowSettings runtimeInstance = MarrowSettings.RuntimeInstance;
		if ((Object)(object)entity == (Object)null || (Object)(object)runtimeInstance == (Object)null || runtimeInstance.PlayerTag == null || ((ScannableReference)runtimeInstance.PlayerTag).Barcode == (Barcode)null)
		{
			return;
		}
		string iD = ((ScannableReference)runtimeInstance.PlayerTag).Barcode.ID;
		TagList tags = entity.Tags;
		if (tags == null || tags.Tags == null)
		{
			return;
		}
		for (int num = tags.Tags.Count - 1; num >= 0; num--)
		{
			BoneTagReference val = tags.Tags[num];
			if (val != null && ((ScannableReference)val).Barcode != (Barcode)null && ((ScannableReference)val).Barcode.ID == iD)
			{
				tags.Tags.RemoveAt(num);
			}
		}
		tags.UpdateCache(false);
	}

	private static int StripSenses(Transform root)
	{
		int num = 0;
		foreach (Camera componentsInChild in ((Component)root).GetComponentsInChildren<Camera>(true))
		{
			foreach (Component component in ((Component)componentsInChild).GetComponents<Component>())
			{
				if (!((Object)(object)component == (Object)null) && !(component is Transform) && !(component is Camera))
				{
					Object.DestroyImmediate((Object)(object)component);
					num++;
				}
			}
			try
			{
				Object.DestroyImmediate((Object)(object)componentsInChild);
			}
			catch (Exception)
			{
				((Behaviour)componentsInChild).enabled = false;
			}
			num++;
		}
		foreach (AudioListener componentsInChild2 in ((Component)root).GetComponentsInChildren<AudioListener>(true))
		{
			Object.DestroyImmediate((Object)(object)componentsInChild2);
			num++;
		}
		return num;
	}

	private static void Blind(Transform headset)
	{
		foreach (Camera componentsInChild in ((Component)headset).GetComponentsInChildren<Camera>(true))
		{
			foreach (Component component in ((Component)componentsInChild).GetComponents<Component>())
			{
				if (!((Object)(object)component == (Object)null) && !(component is Transform) && !(component is Camera))
				{
					Object.DestroyImmediate((Object)(object)component);
				}
			}
			try
			{
				Object.DestroyImmediate((Object)(object)componentsInChild);
			}
			catch (Exception)
			{
				((Behaviour)componentsInChild).enabled = false;
			}
		}
		foreach (AudioListener componentsInChild2 in ((Component)headset).GetComponentsInChildren<AudioListener>(true))
		{
			Object.DestroyImmediate((Object)(object)componentsInChild2);
		}
	}

	private static void Hear(RigManager rig)
	{
		foreach (AudioSource componentsInChild in ((Component)rig).GetComponentsInChildren<AudioSource>(true))
		{
			if ((Object)(object)componentsInChild != (Object)null)
			{
				componentsInChild.spatialBlend = 1f;
			}
		}
	}

	public static void HearLate(RigManager rig)
	{
		if ((Object)(object)rig != (Object)null)
		{
			Hear(rig);
		}
	}

	public static void CopyPlayerAvatar(RigManager rig, Action done)
	{
		RigManager rigManager = Player.RigManager;
		if (!((Object)(object)rig == (Object)null) && !((Object)(object)rigManager == (Object)null) && rigManager.AvatarCrate != null && !(((ScannableReference)rigManager.AvatarCrate).Barcode == (Barcode)null))
		{
			Vector3 scale = (((Object)(object)rigManager.avatar != (Object)null) ? ((Component)rigManager.avatar).transform.localScale : Vector3.one);
			UseAvatar(rig, ((ScannableReference)rigManager.AvatarCrate).Barcode.ID, scale, done);
		}
	}

	public static string AvatarOf(RigManager rig)
	{
		try
		{
			return ((Object)(object)rig != (Object)null && rig._avatarCrate != null && ((ScannableReference)rig._avatarCrate).Barcode != (Barcode)null) ? ((ScannableReference)rig._avatarCrate).Barcode.ID : "";
		}
		catch (Exception)
		{
			return "";
		}
	}

	public static void UseAvatar(RigManager rig, string barcode, Vector3 scale, Action done)
	{
		AvatarCrate crate = null;
		AssetWarehouse instance = AssetWarehouse.Instance;
		if ((Object)(object)rig == (Object)null || string.IsNullOrEmpty(barcode) || instance == null || !instance.TryGetCrate<AvatarCrate>(new Barcode(barcode), ref crate) || (Object)(object)crate == (Object)null)
		{
			done?.Invoke();
			return;
		}
		((MarrowAssetT<GameObject>)(object)((GameObjectCrate)crate).MainGameObject).LoadAsset(Action<GameObject>.op_Implicit((Action<GameObject>)delegate(GameObject asset)
		{
			try
			{
				if (!((Object)(object)rig == (Object)null) && !((Object)(object)asset == (Object)null))
				{
					GameObject val = new GameObject("ThirdEyeBotAvatarHolder");
					val.SetActive(false);
					GameObject val2 = Object.Instantiate<GameObject>(asset, val.transform);
					val2.SetActive(false);
					((Object)val2).name = ((Object)asset).name;
					int num = StripSenses(val2.transform);
					if (num > 0)
					{
						MelonLogger.Msg("Bot Player: took " + num + " camera/listener parts out of the avatar " + ((Object)asset).name);
					}
					val2.transform.localScale = scale;
					val2.transform.parent = ((Component)rig).transform;
					Object.Destroy((Object)(object)val);
					val2.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					Avatar componentInParent = val2.GetComponentInParent<Avatar>(true);
					if ((Object)(object)componentInParent == (Object)null)
					{
						Object.Destroy((Object)(object)val2);
					}
					else
					{
						HeadsetGuard.SwappedAt = Time.time;
						rig.SwapAvatar(componentInParent);
						rig._avatarCrate = new AvatarCrateReference(((Scannable)crate).Barcode);
						Hear(rig);
						done?.Invoke();
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("The bot could not take the player's avatar: " + ex.Message);
			}
		}));
	}
}
