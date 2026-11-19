using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using hvhgg_essentials.Enums;

namespace hvhgg_essentials.Features;

public class RapidFire
{
    private readonly Dictionary<uint, int> _lastPlayerShotTick = new();
    private readonly HashSet<uint> _rapidFireBlockUserIds = new();
    private readonly Dictionary<uint, float> _rapidFireBlockWarnings = new();

    private readonly Plugin _plugin;
    public static readonly FakeConVar<int> hvh_restrict_rapidfire = new("hvh_restrict_rapidfire", "Restrict rapid fire", 0, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<int>(0, 4));
    public static readonly FakeConVar<float> hvh_rapidfire_reflect_scale = new("hvh_rapidfire_reflect_scale", "Reflect scale", 1, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<float>(0, 1));

    public RapidFire(Plugin plugin)
    {
        _plugin = plugin;
        _plugin.RegisterFakeConVars(this);
        hvh_restrict_rapidfire.Value = (int) _plugin.Config.RapidFireFixMethod;
        hvh_rapidfire_reflect_scale.Value = _plugin.Config.RapidFireReflectScale;
    }

    
    

    public HookResult OnWeaponFire(EventWeaponFire eventWeaponFire, GameEventInfo info)
    {
        if (!eventWeaponFire.Userid.IsPlayer())
            return HookResult.Continue;

        if (hvh_restrict_rapidfire.Value == (int)FixMethod.Ignore)
            DelayNextAttack(eventWeaponFire.Userid!);

        // Не применяем проверку RapidFire к шприцу.
        var activeWeapon = eventWeaponFire.Userid!.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon?.DesignerName == "weapon_healthshot")
            return HookResult.Continue;
        
        var firedWeapon = eventWeaponFire.Userid!.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
        var weaponData = firedWeapon?.GetVData<CCSWeaponBaseVData>();
        
        var index = eventWeaponFire.Userid.Pawn.Index;
            
        if (!_lastPlayerShotTick.TryGetValue(index, out var lastShotTick))
        {
            _lastPlayerShotTick[index] = Server.TickCount;
            return HookResult.Continue;
        }
            
        _lastPlayerShotTick[index] = Server.TickCount;
        
        var shotTickDiff = Server.TickCount - lastShotTick;
        var possibleAttackDiff = (weaponData?.CycleTime.Values[0] * 64 ?? 0) - 1;

        // this is ghetto but should work for now
        if (shotTickDiff > possibleAttackDiff || 
            firedWeapon?.DesignerName == "weapon_revolver")
            return HookResult.Continue; 

        // no chat message if we allow rapid fire
        if (hvh_restrict_rapidfire.Value == (int)FixMethod.Allow)
            return HookResult.Continue;
            
        Console.WriteLine($"[RezTop] Detected rapid fire from {eventWeaponFire.Userid.PlayerName}");
            
        // clear list every frame (in case of misses)
        if (_rapidFireBlockUserIds.Count == 0)
            Server.NextFrame(_rapidFireBlockUserIds.Clear);
            
        _rapidFireBlockUserIds.Add(index);
            
        // skip warning if we already warned this player in the last 3 seconds
        if (_rapidFireBlockWarnings.TryGetValue(index, out var lastWarningTime) &&
            lastWarningTime + 3 > Server.CurrentTime) 
            return HookResult.Continue;
            
        if (!_plugin.Config.PrintWarnings) 
            return HookResult.Continue;
        
        // Уведомление о Rapid Fire отправляется только администраторам.
        var adminLogMessage = ChatUtils.FormatMessage(
            $"[{{Red}}AdminLog {{Gold}}R{{DarkRed}}T{{Default}}] Игрок {{Red}}{eventWeaponFire.Userid.PlayerName}{{Default}} попытался использовать {{Red}}Rapid Fire{{Default}}!");

        foreach (var admin in Utilities.GetPlayers())
        {
            if (!admin.IsValid || !admin.PlayerPawn.IsValid || !AdminManager.PlayerHasPermissions(admin, "@css/generic"))
                continue;

            admin.PrintToChat(adminLogMessage);
        }

        _rapidFireBlockWarnings[index] = Server.CurrentTime;

        return HookResult.Continue;
    }
    
    public HookResult OnTakeDamagePre(CBaseEntity victim, CTakeDamageInfo damageInfo)
    {
        // Reflect modes still use the legacy hook below because this listener cannot change the damage target.
        if (hvh_restrict_rapidfire.Value == (int)FixMethod.Reflect ||
            hvh_restrict_rapidfire.Value == (int)FixMethod.ReflectSafe)
            return HookResult.Continue;

        if (hvh_restrict_rapidfire.Value == (int)FixMethod.Ignore ||
            hvh_restrict_rapidfire.Value == (int)FixMethod.Allow)
            return HookResult.Continue;

        if (damageInfo.Attacker.Value == null || !_rapidFireBlockUserIds.Contains(damageInfo.Attacker.Index))
            return HookResult.Continue;

        if (hvh_restrict_rapidfire.Value == (int)FixMethod.PreventDamage)
        {
            damageInfo.Damage = 0;
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }

    // Compatibility hook: the new listener does not expose a way to redirect the damage target to the attacker.
    public HookResult OnTakeDamage(DynamicHook h)
    {
        // the ignore method blocks the shot itself, damage stays untouched
        if (hvh_restrict_rapidfire.Value == (int)FixMethod.Ignore)
            return HookResult.Continue;

        var damageInfo = h.GetParam<CTakeDamageInfo>(1);

        // attacker is invalid
        if (damageInfo.Attacker.Value == null)
            return HookResult.Continue;

        // attacker is not in the list
        if (!_rapidFireBlockUserIds.Contains(damageInfo.Attacker.Index))
            return HookResult.Continue;
            
        // set damage according to config
        switch (hvh_restrict_rapidfire.Value)
        {
            case (int)FixMethod.Allow:
                break;
            case (int)FixMethod.PreventDamage:
                damageInfo.Damage = 0;
                break;
            case (int)FixMethod.Reflect:
            case (int)FixMethod.ReflectSafe:
                damageInfo.Damage *= hvh_rapidfire_reflect_scale.Value;
                h.SetParam<CEntityInstance>(0, damageInfo.Attacker.Value);
                if (hvh_restrict_rapidfire.Value == (int)FixMethod.ReflectSafe)
                    damageInfo.DamageFlags |= TakeDamageFlags_t.DFLAG_PREVENT_DEATH; //https://docs.cssharp.dev/api/CounterStrikeSharp.API.Core.TakeDamageFlags_t.html
                break;
            default:
                break;
        }

        return HookResult.Changed;
    }

    public HookResult OnBulletImpact(EventBulletImpact eventBulletImpact, GameEventInfo info)
    {
        if (hvh_restrict_rapidfire.Value != (int)FixMethod.Ignore)
            return HookResult.Continue;

        if (!eventBulletImpact.Userid.IsPlayer())
            return HookResult.Continue;

        DelayNextAttack(eventBulletImpact.Userid!);

        return HookResult.Continue;
    }

    // pushes the next primary attack out by the weapon's cycle time so a shifted second shot is never fired
    private static void DelayNextAttack(CCSPlayerController player)
    {
        var firedWeapon = player.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;

        if (firedWeapon == null || firedWeapon.DesignerName == "weapon_revolver")
            return;

        var weaponData = firedWeapon.GetVData<CCSWeaponBaseVData>();

        if (weaponData == null)
            return;

        var cycleTicks = (int)Math.Round(weaponData.CycleTime.Values[0] * 64) - 3;
        var nextAttackTick = (int)player.TickBase + cycleTicks;

        if (firedWeapon.NextPrimaryAttackTick >= nextAttackTick)
            return;

        firedWeapon.NextPrimaryAttackTick = nextAttackTick;
        Utilities.SetStateChanged(firedWeapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
    }
}