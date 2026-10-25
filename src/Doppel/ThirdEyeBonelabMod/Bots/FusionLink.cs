using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using LabFusion.Entities;
using LabFusion.Marrow.Extenders;
using LabFusion.Marrow.Messages;
using LabFusion.Marrow.Patching;
using LabFusion.MonoBehaviours;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;
using LabFusion.Scene;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class FusionLink
{
	private const long Tag = 5929072799880532017L;

	public static void Start()
	{
		if (ModuleMessageManager.TagToHandlerLookup.TryGetValue(5929072799880532017L, out var value))
		{
			ModuleMessageManager.TagToHandlerLookup.Remove(5929072799880532017L);
			ModuleMessageManager.Handlers.Remove(value);
			ModuleMessageManager.TypeToHandlerLookup.Remove(((object)value).GetType());
		}
		DoppelMessage doppelMessage = new DoppelMessage();
		FieldInfo? field = typeof(ModuleMessageHandler).GetField("_tag", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new Exception("this Fusion keeps its handlers' numbers some other way");
		}
		field.SetValue(doppelMessage, 5929072799880532017L);
		((MessageHandler)doppelMessage).NetAttributes = Array.Empty<NetAttribute>();
		ModuleMessageManager.Handlers.Add((ModuleMessageHandler)(object)doppelMessage);
		ModuleMessageManager.TypeToHandlerLookup[typeof(DoppelMessage)] = (ModuleMessageHandler)(object)doppelMessage;
		ModuleMessageManager.TagToHandlerLookup[5929072799880532017L] = (ModuleMessageHandler)(object)doppelMessage;
		NetBots.Online = () => NetworkSceneManager.IsLevelNetworked && PlayerIDManager.HasOtherPlayers;
		NetBots.Send = delegate(byte[] bytes, bool sure)
		{
			MessageRelay.RelayModule<DoppelMessage, DoppelData>(new DoppelData
			{
				bytes = bytes
			}, sure ? CommonMessageRoutes.ReliableToOtherClients : CommonMessageRoutes.UnreliableToOtherClients);
		};
		NetBots.WithUs = delegate(byte who)
		{
			PlayerID playerID = PlayerIDManager.GetPlayerID(who);
			return playerID != null && NetworkSceneManager.InCurrentLevel(playerID);
		};
		NetBots.GunShot = Shot;
		NetBots.Me = () => PlayerIDManager.LocalSmallID;
		NetBots.Others = delegate
		{
			List<RigManager> list = new List<RigManager>();
			foreach (NetworkPlayer player in NetworkPlayer.Players)
			{
				if (player != null && player.NetworkEntity != null && !player.NetworkEntity.IsOwner && player.HasRig)
				{
					RigManager val2 = ((player.RigRefs != null) ? player.RigRefs.RigManager : null);
					if ((Object)(object)val2 != (Object)null)
					{
						list.Add(val2);
					}
				}
			}
			return list;
		};
		NetBots.Own = Own;
		NetBots.Hide = Hide;
		NetBots.Shared = delegate(ushort number)
		{
			NetworkEntity entity = NetworkEntityManager.IDManager.RegisteredEntities.GetEntity(number);
			IMarrowEntityExtender val = ((entity != null) ? entity.GetExtender<IMarrowEntityExtender>() : null);
			return (val == null || !((Object)(object)val.MarrowEntity != (Object)null)) ? null : ((Component)val.MarrowEntity).transform;
		};
	}

	public static void Stop()
	{
		NetBots.Online = () => false;
		NetBots.Send = delegate
		{
		};
		if (ModuleMessageManager.TagToHandlerLookup.TryGetValue(5929072799880532017L, out var value) && value is DoppelMessage)
		{
			ModuleMessageManager.TagToHandlerLookup.Remove(5929072799880532017L);
			ModuleMessageManager.Handlers.Remove(value);
			ModuleMessageManager.TypeToHandlerLookup.Remove(typeof(DoppelMessage));
		}
	}

	private static int Hide(RigManager rig)
	{
		if ((Object)(object)rig == (Object)null)
		{
			return 0;
		}
		if ((Object)(object)((Component)rig).GetComponent<AntiHasher>() == (Object)null)
		{
			((Component)rig).gameObject.AddComponent<AntiHasher>();
		}
		int num = 0;
		NetworkEntity val = default(NetworkEntity);
		foreach (MarrowEntity componentsInChild in ((Component)rig).GetComponentsInChildren<MarrowEntity>(true))
		{
			if (!((Object)(object)componentsInChild == (Object)null))
			{
				MarrowEntityPatches.HashTable.RemoveComponent(componentsInChild);
				if (IMarrowEntityExtender.Cache.TryGet(componentsInChild, ref val) && val != null && val.GetExtender<NetworkPlayer>() == null)
				{
					NetworkEntityManager.IDManager.UnregisterEntity(val);
					num++;
				}
			}
		}
		return num;
	}

	private static ushort Own(MarrowEntity thing)
	{
		NetworkEntity val = IMarrowEntityExtender.Cache.Get(thing);
		if (val == null || !val.IsRegistered)
		{
			return ushort.MaxValue;
		}
		if (!val.IsOwner)
		{
			NetworkEntityManager.TakeOwnership(val);
		}
		return val.ID;
	}

	private static void Shot(Gun gun)
	{
		NetworkEntity val = GunExtender.Cache.Get(gun);
		if (val == null || !NetworkInfo.HasServer)
		{
			return;
		}
		if (!val.IsOwner)
		{
			NetworkEntityManager.TakeOwnership(val);
		}
		GunExtender extender = val.GetExtender<GunExtender>();
		if (extender != null)
		{
			ushort? index = ((EntityComponentArrayExtender<Gun>)(object)extender).GetIndex(gun);
			if (index.HasValue)
			{
				MessageRelay.RelayModule<GunShotMessage, GunShotData>(GunShotData.Create((byte)((gun._magState != null) ? ((uint)Mathf.Clamp(gun._magState.AmmoCount, 0, 255)) : 0u), val.ID, (byte)index.Value), CommonMessageRoutes.ReliableToOtherClients);
			}
		}
	}
}
