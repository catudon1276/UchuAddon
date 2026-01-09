using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Rewired.Utils.Classes.Utility;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

public class XinobiU : DefinedRoleTemplate, DefinedRole
{
    private XinobiU() : base("xinobiU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [KillCoolDown,XinobiVentCooldown, ClairvoyanceOption,VentConfiguration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    static private IRelativeCoolDownConfiguration KillCoolDown = NebulaAPI.Configurations.KillConfiguration("options.role.xinobiU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static private FloatConfiguration XinobiVentCooldown = NebulaAPI.Configurations.Configuration("options.role.xinobiU.xinobiUVentCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration ClairvoyanceOption = NebulaAPI.Configurations.Configuration("options.role.spectre.clairvoyance", true);
    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.VentConfiguration("role.reaper.vent", false, null, -1, (0f, 60f, 2.5f), 10f, (0f, 30f, 2.5f), 20f);
    static public float KillCooldown => KillCoolDown.CoolDown;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Xinobi.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public XinobiU MyRole = new XinobiU();

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        bool RuntimeRole.HasVanillaKillButton => false;
        static private readonly Virial.Media.Image PossessImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
        static private readonly Virial.Media.Image ReleaseImage = NebulaAPI.AddonAsset.GetResource("XinobiButton.png")!.AsImage(115f)!;
        static private readonly Virial.Media.Image VentImage = NebulaAPI.AddonAsset.GetResource("XinobiVentButton.png")!.AsImage(115f)!;
        bool Possess = false;
        float min = 0f;
        Vent? ventLocal = null;
        Vector3 previousPosition = Vector3.zero;
        public bool EyesightIgnoreWalls { get; private set; } = ClairvoyanceOption;
        void IGameOperator.OnReleased()
        {
            AmongUsUtil.SetCamTarget(null);
        }
        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                var abilityVent = new VentArrowAbility().Register(this);
                var ability = new AllVentConnectAbility().Register(this);
                ability.AllVentConnects(true);

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);
                ModAbilityButton killPossessButton = null!;
                ModAbilityButton killButton = null!;
                ModAbilityButton ventNinjaButton = null!;
                ModAbilityButton ventPossessButton = null!;
                ModAbilityButton possessButton = null!;
                ModAbilityButton breakButton = null!;

                killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!, (target, _) =>
                {
                    if (Possess)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.RemoteKill);
                    }
                    else if (!Possess)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    }
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    killPossessButton.StartCoolDown();
                    killButton.StartCoolDown();
                },
                null,
                _ => playerTracker.CurrentTarget != null && !MyPlayer.IsDived,
                _ => MyPlayer.AllowToShowKillButtonByAbilities
                );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                killButton.Visibility = (button) => !MyPlayer.IsDead && !Possess;

                killPossessButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!, (target, _) =>
                {
                    if (Possess)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.RemoteKill);
                    }
                    else if (!Possess)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    }
                    killButton.StartCoolDown();
                    killPossessButton.StartCoolDown();
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                },
                null,
                _ => playerTracker.CurrentTarget != null && !MyPlayer.IsDived,
                _ => MyPlayer.AllowToShowKillButtonByAbilities
                );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                killPossessButton.Visibility = (button) => !MyPlayer.IsDead && Possess;
                killPossessButton.Availability = (button) => !MyPlayer.IsDead && Possess && playerTracker.CurrentTarget != null;

                //通常ベン遁ボタン
                ventNinjaButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility, XinobiVentCooldown, "xinobiU.vent", VentImage);
                ventNinjaButton.Visibility = (button) => !MyPlayer.IsDead && !Possess;
                var ventNinjaTimer = ventNinjaButton.CoolDownTimer as TimerImpl;
                var ventNinjaLastPredicate = ventNinjaTimer!.Predicate;
                ventNinjaTimer.SetPredicate(() => ventNinjaLastPredicate!.Invoke() || HudManager.Instance.PlayerCam.Target != null);
                ventNinjaButton.OnClick = (button) =>
                {
                    
                    Possess = false;
                    AmongUsUtil.ToggleCamTarget(null);
                    XinobiVent();
                    ventNinjaButton.StartCoolDown();
                    ventPossessButton.StartCoolDown();
                };

                //変化中のベン遁ボタン
                ventPossessButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility, XinobiVentCooldown, "xinobiU.vent", VentImage);
                ventPossessButton.Visibility = (button) => !MyPlayer.IsDead && Possess;
                ventPossessButton.Availability = (button) => true;
                var ventPossessTimer = ventPossessButton.CoolDownTimer as TimerImpl;
                var ventPossessLastPredicate = ventPossessTimer!.Predicate;
                ventPossessTimer.SetPredicate(() => ventPossessLastPredicate!.Invoke() || HudManager.Instance.PlayerCam.Target != null);
                ventPossessButton.OnClick = (button) =>
                {
                    Possess = false;
                    AmongUsUtil.ToggleCamTarget(null);
                    XinobiVent();
                    ventPossessButton.StartCoolDown();
                    ventNinjaButton.StartCoolDown();
                };

                Virial.Components.ObjectTracker<Console> consoleTracker = new ObjectTrackerUnityImpl<Console, Console>(MyPlayer.VanillaPlayer, AmongUsUtil.VanillaKillDistance, 
                    () => ShipStatus.Instance.AllConsoles,c => true, c => true, c => c,
                    c => [c.transform.position], c => c.Image, Color.red,true, false).Register(this);

                possessButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0.5f, "xinobiU.possess", PossessImage, _ => consoleTracker.CurrentTarget != null);
                possessButton.Visibility = (button) => !MyPlayer.IsDead && !Possess;
                possessButton.OnClick = (button) =>
                {
                    NameSeclet();

                    possessButton.StartCoolDown();
                    breakButton.StartCoolDown();
                    Possess = true;
                    var target = consoleTracker.CurrentTarget;
                    if (target != null)
                    {
                        AmongUsUtil.ToggleCamTarget(target);
                        Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
                        ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(),null,myPos,1f).StartOnScene();
                    }
                };

                breakButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0.5f, "xinobiU.release", ReleaseImage);
                breakButton.Visibility = (button) => !MyPlayer.IsDead && Possess;
                breakButton.Availability = (button) => Possess;
                var breakPosTimer = breakButton.CoolDownTimer as TimerImpl;
                var breakPosPredicate = breakPosTimer!.Predicate;
                breakPosTimer.SetPredicate(() => breakPosPredicate!.Invoke() || HudManager.Instance.PlayerCam.Target != null);
                breakButton.OnClick = (button) =>
                {
                    NamePublic();

                    possessButton.StartCoolDown();
                    breakButton.StartCoolDown();
                    AmongUsUtil.ToggleCamTarget(null);
                    Possess = false;
                    var target = consoleTracker.CurrentTarget;
                    if (target != null)
                    {
                        Vector3 effectPos = target.transform.position;
                        effectPos.z -= 0.0000001f;
                        ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(), null, effectPos, 1.1f).StartOnScene();
                    }
                };
            }
        }
        void Update(GameUpdateEvent ev)
        {
            if (!Possess) return;
            MyPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.1f, false, 1);
            MyPlayer.GainSpeedAttribute(0f, 0.1f, false, 1);
        }

        void XinobiVent()
        {
            var ability = new AllVentConnectAbility().Register(this);
            ability.AllVentConnects(true);
            Possess = false;
            MyPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.39f, false, 1);
            foreach (var v in ShipStatus.Instance.AllVents)
            {
                float d = PlayerControl.LocalPlayer.transform.position
                    .Distance(v.gameObject.transform.position);
                if (ventLocal == null || d < min)
                {
                    min = d;
                    ventLocal = v;
                }
            }
            if (ventLocal != null)
            {            
                var player = PlayerControl.LocalPlayer;
                var physics = player.MyPhysics;

                Vector2 ventPos = ventLocal.transform.position;
                Vector2 warpPos = ventPos + new Vector2(0f, 0.35f);

                physics.body.velocity = Vector2.zero;

                player.NetTransform.SnapTo(warpPos);

                ability.AllVentConnects(true);
                physics.RpcEnterVent(ventLocal.Id);
                ventLocal.SetButtons(true);
                ability.AllVentConnects(true);
            }
            Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
            myPos.z -= 0.0000001f;
            ManagedEffects.CoDisappearEffect(LayerExpansion.GetObjectsLayer(), null, myPos, 1f).StartOnScene();
            ventLocal = null;
            min = 0f;
        }

        void NameSeclet()
        {
            var name = MyPlayer.VanillaPlayer.transform.Find("Names");
            name.Find("NameText_TMP").gameObject.SetActive(false);
            name.Find("ColorblindName_TMP").gameObject.SetActive(false);
        }

        void NamePublic()
        {
            var name = MyPlayer.VanillaPlayer.transform.Find("Names");
            name.Find("NameText_TMP").gameObject.SetActive(true);
            name.Find("ColorblindName_TMP").gameObject.SetActive(true);
        }

        [OnlyMyPlayer]
        void CameraDead(PlayerDieEvent ev)
        {
            AmongUsUtil.ToggleCamTarget(null);
            NamePublic();
            Possess = false;
        }

        void CameraMeeting(MeetingPreStartEvent ev)
        {
            AmongUsUtil.ToggleCamTarget(null);
            NamePublic();
            Possess = false;
        }

        void a(PlayerTryToChangeRoleEvent ev)
        {
            AmongUsUtil.ToggleCamTarget(null);
            Possess = false;
            NamePublic();
        }
    }
}





/*public class NinjaU : DefinedSingleAbilityRoleTemplate<NinjaU.Ability>, DefinedRole
{
    public NinjaU() : base("ninjaU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam)
    {
    }

    public static FloatConfiguration PossessCooldown = NebulaAPI.Configurations.Configuration("options.role.ninjU.possessCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public NinjaU MyRole = new();
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.sampleU.sampleSkill", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image PossessImage = NebulaAPI.AddonAsset.GetResource("EpsilonScanButton.png")!.AsImage(115f)!;
        static private readonly Virial.Media.Image ReleaseImage = NebulaAPI.AddonAsset.GetResource("EpsilonCovertButton.png")!.AsImage(115f)!;
        bool Possess = false;
        bool RuntimeRole.CanUseVent => false;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var sprite = HudManager.Instance.UseButton.fastUseSettings[ImageNames.VentButton].Image;
                var posVent = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Vent, 0f, "Vent", new WrapSpriteLoader(() => sprite));
                posVent.Visibility = (button) => !MyPlayer.IsDead && Possess;
                posVent.Availability = (button) => !MyPlayer.IsDead && MyPlayer.IsDead;

                Virial.Components.ObjectTracker<Console> consoleTracker = new ObjectTrackerUnityImpl<Console, Console>(MyPlayer.VanillaPlayer, AmongUsUtil.VanillaKillDistance, () => ShipStatus.Instance.AllConsoles, c => !ModSingleton<BalloonManager>.Instance.ConsoleHasTrap(c), c => !c.TryCast<VentCleaningConsole>() && !c.TryCast<AutoTaskConsole>() && !c.TryCast<StoreArmsTaskConsole>(), c => c, c => [c.transform.position], c => c.Image, Color.red,true, false).Register(this);
                var possessButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,10f,  "balloon", PossessImage, _ => consoleTracker.CurrentTarget != null).SetAsUsurpableButton(this);
                possessButton.Visibility = (button) => !MyPlayer.IsDead && !Possess;
                possessButton.OnClick = (button) =>
                {
                    Possess = true;
                };
                var breakButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "Break", ReleaseImage);
                breakButton.Visibility = (button) => !MyPlayer.IsDead && Possess;
                breakButton.OnClick = (button) =>
                {
                    Possess = false;
                };
            }
        }


       void Update(GameUpdateEvent ev)
        {
            if (!Possess) return;
            MyPlayer.GainAttribute(PlayerAttributes.InternalInvisible, 0.1f, false, 1);
            MyPlayer.GainSpeedAttribute(0f, 0.1f, false, 1);
        }

        [OnlyMyPlayer]
        void name(PlayerDecorateNameEvent ev)
        {
            if (!Possess) return;
            ev.Name = "";
        }
    }
}*/