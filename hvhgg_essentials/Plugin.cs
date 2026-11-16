using System;
using CS2_CustomVotes.Shared;
using CSSharpUtils.Extensions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using hvhgg_essentials.Features;

namespace hvhgg_essentials;

public class Plugin : BasePlugin, IPluginConfig<Cs2EssentialsConfig>
{
	private ServiceProvider? _serviceProvider;

	public required MemoryFunctionVoid<CCSPlayer_MovementServices, nint> RunCommand;

	private bool _isCustomVotesLoaded;

	public override string ModuleName => "RezTop - Essentials";

	public override string ModuleVersion => "1.6.0";

	public override string ModuleAuthor => "imi-tat0r";

	public override string ModuleDescription => "RezTop cs HvH server";

	public Cs2EssentialsConfig Config { get; set; } = new Cs2EssentialsConfig();

	public static PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new PluginCapability<ICustomVoteApi>("custom_votes:api");

	public void OnConfigParsed(Cs2EssentialsConfig config)
	{
		Config = config;
		config.Update();
		RapidFire.hvh_restrict_rapidfire.Value = (int)Config.RapidFireFixMethod;
		RapidFire.hvh_rapidfire_reflect_scale.Value = Config.RapidFireReflectScale;
		FriendlyFire.hvh_unmatched_friendlyfire.Value = Config.UnmatchedFriendlyFire;
		TeleportFix.hvh_restrict_teleport.Value = Config.RestrictTeleport;
		WeaponRestrict.hvh_restrict_awp.Value = Config.AllowedAwpCount;
		WeaponRestrict.hvh_restrict_scout.Value = Config.AllowedScoutCount;
		WeaponRestrict.hvh_restrict_auto.Value = Config.AllowedAutoSniperCount;
		WeaponRestrict.hvh_bypass_weapon_restrict_flag.Value = Config.WeaponRestrictBypassFlags;
		ResetScore.hvh_resetscore.Value = Config.AllowResetScore;
		RageQuit.hvh_ragequit.Value = Config.AllowRageQuit;
	}

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);
		Console.WriteLine("[RezTop] Запустил загрузку RezTop Essentials plugin");
		ServiceCollection services = new ServiceCollection();
		services.AddLogging((ILoggingBuilder options) =>
		{
			options.AddConsole();
		});
		services.AddSingleton(this);
		services.AddSingleton<RageQuit>();
		services.AddSingleton<ResetScore>();
		services.AddSingleton<RapidFire>();
		services.AddSingleton<WeaponRestrict>();
		services.AddSingleton<FriendlyFire>();
		services.AddSingleton<TeleportFix>();
		services.AddSingleton<Misc>();
		_serviceProvider = services.BuildServiceProvider();
		UseWeaponRestrict();
		UseRapidFireRestrict();
		UseFriendlyFireRestrict();
		UseRageQuit();
		UseResetScore();
		UseMisc();
		UseTeleport();
		Console.WriteLine("[RezTop] Завершил загрузку RezTop Essentials plugin");
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		base.OnAllPluginsLoaded(hotReload);
		try
		{
			if (CustomVotesApi.Get() == null)
			{
				return;
			}
		}
		catch (Exception)
		{
			Console.WriteLine("[RezTop.cc] Плагин CS2-CustomVotes не найден. Пользовательские голосования не будут зарегистрированы.");
			return;
		}
		_isCustomVotesLoaded = true;
		Console.WriteLine("[RezTop.cc] Регистрирую пользовательские голосования");
		RapidFire.RegisterCustomVotes(this);
		FriendlyFire.RegisterCustomVotes(this);
		TeleportFix.RegisterCustomVotes(this);
	}

	private void UseTeleport()
	{
		TeleportFix requiredService = _serviceProvider.GetRequiredService<TeleportFix>();
		Console.WriteLine("[RezTop.cc] Ставлю хук на run command");
		RunCommand = new MemoryFunctionVoid<CCSPlayer_MovementServices, nint>(GameData.GetSignature("RunCommand"));
		RunCommand.Hook(requiredService.RunCommand, HookMode.Pre);
	}

	private void UseMisc()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую разные команды");
		Misc misc = _serviceProvider.GetRequiredService<Misc>();
		RegisterConsoleCommandAttributeHandlers(misc);
		RegisterEventHandler((EventPlayerSpawn eventPlayerSpawn, GameEventInfo _) =>
		{
			misc.AnnounceRules(eventPlayerSpawn.Userid);
			return HookResult.Continue;
		});
		Console.WriteLine("[RezTop.cc] Закончил регистрацию разных команд");
	}

	private void UseResetScore()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую команду сброса счёта");
		ResetScore requiredService = _serviceProvider.GetRequiredService<ResetScore>();
		RegisterConsoleCommandAttributeHandlers(requiredService);
		Console.WriteLine("[RezTop.cc] Закончил регистрацию команды сброса счёта");
	}

	private void UseRageQuit()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую команду rage quit");
		RageQuit requiredService = _serviceProvider.GetRequiredService<RageQuit>();
		RegisterConsoleCommandAttributeHandlers(requiredService);
		Console.WriteLine("[RezTop.cc] Закончил регистрацию команды rage quit");
	}

	private void UseFriendlyFireRestrict()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую слушатели дружественного огня");
		FriendlyFire requiredService = _serviceProvider.GetRequiredService<FriendlyFire>();
		VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(requiredService.OnTakeDamage, HookMode.Pre);
		Console.WriteLine("[RezTop.cc] Закончил регистрацию слушателей дружественного огня");
	}

	private void UseRapidFireRestrict()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую слушатели rapid fire");
		RapidFire requiredService = _serviceProvider.GetRequiredService<RapidFire>();
		RegisterEventHandler<EventBulletImpact>(requiredService.OnBulletImpact, HookMode.Pre);
		RegisterEventHandler<EventWeaponFire>(requiredService.OnWeaponFire);
		VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(requiredService.OnTakeDamage, HookMode.Pre);
		Console.WriteLine("[RezTop.cc] Закончил регистрацию слушателей rapid fire");
	}

	private void UseWeaponRestrict()
	{
		Console.WriteLine("[RezTop.cc] Регистрирую слушатели ограничения оружия");
		WeaponRestrict requiredService = _serviceProvider.GetRequiredService<WeaponRestrict>();
		VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(requiredService.OnWeaponCanAcquire, HookMode.Pre);
		Console.WriteLine("[RezTop.cc] Закончил регистрацию слушателей ограничения оружия");
	}

	public override void Unload(bool hotReload)
	{
		if (_serviceProvider != null)
		{
			FriendlyFire requiredService = _serviceProvider.GetRequiredService<FriendlyFire>();
			RapidFire requiredService2 = _serviceProvider.GetRequiredService<RapidFire>();
			VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(requiredService2.OnTakeDamage, HookMode.Pre);
			VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(requiredService.OnTakeDamage, HookMode.Pre);
			WeaponRestrict requiredService3 = _serviceProvider.GetRequiredService<WeaponRestrict>();
			VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(requiredService3.OnWeaponCanAcquire, HookMode.Pre);
			TeleportFix requiredService4 = _serviceProvider.GetRequiredService<TeleportFix>();
			RunCommand.Unhook(requiredService4.RunCommand, HookMode.Pre);
			if (_isCustomVotesLoaded)
			{
				Console.WriteLine("[RezTop.cc] Отменяю регистрацию пользовательских голосований");
				RapidFire.UnregisterCustomVotes(this);
				FriendlyFire.UnregisterCustomVotes(this);
				TeleportFix.UnregisterCustomVotes(this);
				base.Unload(hotReload);
			}
		}
	}
}
