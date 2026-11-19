using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CSSharpUtils.Extensions;
using hvhgg_essentials.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace hvhgg_essentials;

public class Plugin : BasePlugin, IPluginConfig<Cs2EssentialsConfig>
{
    public override string ModuleName => "Reztop-essentials";
    public override string ModuleVersion => "1.6.0";
    public override string ModuleAuthor => "RezTop";
    public override string ModuleDescription => "Основные функции для CS2 сервера RezTop";
    public Cs2EssentialsConfig Config { get; set; } = new();
    
    private ServiceProvider? _serviceProvider;
    
    public required MemoryFunctionVoid<CCSPlayer_MovementServices, IntPtr> RunCommand;

    public void OnConfigParsed(Cs2EssentialsConfig config)
    {
        Config = config;
        config.Update();
        
        RapidFire.hvh_restrict_rapidfire.Value = (int) Config.RapidFireFixMethod;
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

        Console.WriteLine("[RezTop] Начинаю загрузку Reztop-essentials");

        var services = new ServiceCollection();
        
        services.AddLogging(options =>
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
        
        // register features
        UseWeaponRestrict();
        UseRapidFireRestrict();
        UseFriendlyFireRestrict();
        UseRageQuit();
        UseResetScore();
        UseMisc();
        UseTeleport();
        
        Console.WriteLine("[RezTop] Загрузка Reztop-essentials завершена");
    }



    private void UseTeleport()
    {
        var teleportFix = _serviceProvider!.GetRequiredService<TeleportFix>();
        
        Console.WriteLine("[RezTop] Устанавливаю хук RunCommand");
        
        RunCommand = new MemoryFunctionVoid<CCSPlayer_MovementServices, IntPtr>(GameData.GetSignature("RunCommand"));
        RunCommand.Hook(teleportFix.RunCommand, HookMode.Pre);
    }
    private void UseMisc()
    {
        Console.WriteLine("[RezTop] Регистрирую дополнительные команды");
        
        var misc = _serviceProvider!.GetRequiredService<Misc>();
        RegisterConsoleCommandAttributeHandlers(misc);
        
        RegisterEventHandler<EventPlayerSpawn>((eventPlayerSpawn, _) =>
        {
            misc.AnnounceRules(eventPlayerSpawn.Userid);
            
            return HookResult.Continue;
        });
        
        Console.WriteLine("[RezTop] Регистрация дополнительных команд завершена");
    }
    private void UseResetScore()
    {
        Console.WriteLine("[RezTop] Регистрирую команду обнуления счёта");
        
        var resetScore = _serviceProvider!.GetRequiredService<ResetScore>();
        RegisterConsoleCommandAttributeHandlers(resetScore);
        
        Console.WriteLine("[RezTop] Регистрация команды обнуления счёта завершена");
    }
    private void UseRageQuit()
    {
        Console.WriteLine("[RezTop] Регистрирую команду выхода с сервера");
        
        var rageQuit = _serviceProvider!.GetRequiredService<RageQuit>();
        RegisterConsoleCommandAttributeHandlers(rageQuit);
        
        Console.WriteLine("[RezTop] Регистрация команды выхода с сервера");
    }
    private void UseFriendlyFireRestrict()
    {
        Console.WriteLine("[RezTop] Регистрирую обработчик дружественного огня");
        
        var friendlyFire = _serviceProvider!.GetRequiredService<FriendlyFire>();
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(friendlyFire.OnTakeDamage, HookMode.Pre);
        
        Console.WriteLine("[RezTop] Обработчик дружественного огня зарегистрирован");
    }
    private void UseRapidFireRestrict()
    {
        Console.WriteLine("[RezTop] Регистрирую обработчики Rapid Fire");
        
        var rapidFire = _serviceProvider!.GetRequiredService<RapidFire>();
        RegisterEventHandler<EventBulletImpact>(rapidFire.OnBulletImpact, HookMode.Pre);
        RegisterEventHandler<EventWeaponFire>(rapidFire.OnWeaponFire);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(rapidFire.OnTakeDamage, HookMode.Pre);
        
        Console.WriteLine("[RezTop] Обработчики Rapid Fire зарегистрированы");
    }
    private void UseWeaponRestrict()
    {
        Console.WriteLine("[RezTop] Регистрирую обработчики ограничения оружия");
        
        var weaponRestrict = _serviceProvider!.GetRequiredService<WeaponRestrict>();
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(weaponRestrict.OnWeaponCanAcquire, HookMode.Pre);
        
        Console.WriteLine("[RezTop] Обработчики ограничения оружия зарегистрированы");
    }
    
    public override void Unload(bool hotReload)
    {
        if (_serviceProvider is null)
            return;
        
        var friendlyFire = _serviceProvider.GetRequiredService<FriendlyFire>();
        var rapidFire = _serviceProvider.GetRequiredService<RapidFire>();
        
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(rapidFire.OnTakeDamage, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(friendlyFire.OnTakeDamage, HookMode.Pre);
        
        var weaponRestrict = _serviceProvider.GetRequiredService<WeaponRestrict>();
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(weaponRestrict.OnWeaponCanAcquire, HookMode.Pre);
        
        var teleportFix = _serviceProvider.GetRequiredService<TeleportFix>();
        RunCommand.Unhook(teleportFix.RunCommand, HookMode.Pre);
        
        base.Unload(hotReload);
    }
}