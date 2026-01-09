using Cpp2IL.Core.Extensions;
using Hori.Core;
using Nebula;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using static Il2CppSystem.DateTimeParse;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Crewmate;

public class PolarisU : DefinedSingleAbilityRoleTemplate<PolarisU.Ability>, DefinedRole
{
    private PolarisU(): base("polarisU", new(130, 178, 255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfKILLOption,KillCooldownOption,CanKillHidingPlayerOption, SealAbilityUntilReportingDeadBodiesOption,MediumCooldown, MediumDuration,NumOfMedium,RewindTask,MediumRewindTask]) 
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public static IntegerConfiguration NumOfKILLOption = NebulaAPI.Configurations.Configuration("options.role.polarisU.numofKillOption", (1, 15), 3);
    public static FloatConfiguration KillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.polarisU.killCoolDownOption", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.canKillHidingPlayer", false);
    static internal readonly BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.sealAbilityUntilReportingDeadBodies", false);
    public static FloatConfiguration MediumCooldown = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MediumDuration = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumDuration", (0.25f, 10f, 0.25f), 3f, FloatConfigurationDecorator.Second);
    public static IntegerConfiguration NumOfMedium = NebulaAPI.Configurations.Configuration("options.role.polarisU.numOfMedium", (1, 15), 3);
    public static BoolConfiguration RewindTask = NebulaAPI.Configurations.Configuration("options.role.polarisU.rewindTask", true);
    public static IntegerConfiguration MediumRewindTask = NebulaAPI.Configurations.Configuration("options.role.polarisU.mediumRewindTask", (1, 15), 3);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(1));
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Polaris.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public PolarisU MyRole = new PolarisU();
    static private readonly GameStatsEntry StatsKill = NebulaAPI.CreateStatsEntry("stats.polarisU.statsKill", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsMedium = NebulaAPI.CreateStatsEntry("stats.polarisU.statsMedium", GameStatsCategory.Roles, MyRole);
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonCyan.png")!.AsImage(100f)!;
        static private readonly Virial.Media.Image MediumImage = NebulaAPI.AddonAsset.GetResource("PolarisMediumButton.png")!.AsImage(115f)!;
        int left = NumOfKILLOption;
        int leftMedium = NumOfMedium;
        int ImpostorMedium = 0;
        int ImpostorKill = 0;
        bool CrewKill = false;
        ModAbilityButton? killButton;
        SpriteRenderer? lockSprite = null;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, CanKillHidingPlayerOption, false);
                killTracker.SetColor(MyRole.RoleColor);
                var deadBodyTracker = ObjectTrackers.ForDeadBody(this, null, MyPlayer, (d) => GamePlayer.AllPlayers.All(p => p.HoldingDeadBody != d));
                deadBodyTracker.SetColor(MyRole.RoleColor);

                var mediumButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,MediumCooldown, "polarisU.Medium", MediumImage ,_ => deadBodyTracker.CurrentTarget != null, null, true);
                mediumButton.ShowUsesIcon(3, leftMedium.ToString());
                mediumButton.Visibility = (button) => !MyPlayer.IsDead && leftMedium > 0;
                mediumButton.EffectTimer = NebulaAPI.Modules.Timer(this, MediumDuration);
                mediumButton.OnClick = (button) => button.StartEffect();
                mediumButton.OnEffectStart = (button) =>
                {
                    StatsMedium.Progress();
                    MyPlayer.GainSpeedAttribute(0f, MediumDuration, false, 100);
                };

                mediumButton.OnEffectEnd = (button) =>
                {
                    var body = deadBodyTracker.CurrentTarget;
                    if (body == null) return;

                    Player deadPlayer = body.Player;

                    NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.polarisU.message.header")),
                        new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(deadPlayer.ColoredName + "<br>" + deadPlayer.Role.DisplayColoredName))),
                        MeetingOverlayHolder.IconsSprite[5], MyRole.RoleColor);

                    if (!deadPlayer.IsTrueCrewmate)
                    {
                        ImpostorMedium++;
                    }

                    if (RewindTask)
                    {
                        MyPlayer.Tasks.RewindTasks(MediumRewindTask);
                    }
                    NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess);
                    new StaticAchievementToken("polarisU.common2");//幻
                    leftMedium--;
                    mediumButton.UpdateUsesIcon(leftMedium.ToString());
                    mediumButton.StartCoolDown();
                };


                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(base.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.Ability, null).SetAsUsurpableButton(this);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && left > 0;
                killButton.SetImage(KillImage);
                killButton.ShowUsesIcon(3, left.ToString());
                if (SealAbilityUntilReportingDeadBodiesOption)
                {
                    if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                    {
                        lockSprite = (killButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                    }
                }
                killButton.OnClick = (button) =>
                {
                    left--;
                    killButton.UpdateUsesIcon(left.ToString());
                    GamePlayer? target = killTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.StartCoolDown();
                killButton.SetLabel("kill");
                (killButton as ModAbilityButtonImpl)!.OnMeeting = (button) =>
                {
                    if (lockSprite != null)
                    {
                        if (NebulaGameManager.Instance!.AllPlayerInfo.Any((Player p) => p.IsDead))
                        {
                            UnityEngine.Object.Destroy(lockSprite.gameObject);
                            lockSprite = null;
                        }
                    }
                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        [Local, OnlyMyPlayer]
        void Murder(PlayerMurderedEvent ev)
        {
            if (ev.Dead.IsTrueCrewmate)
            {
                CrewKill = true;
                new StaticAchievementToken("polarisU.another1"); //哀色の疵
            }
            else
            {
                ImpostorKill++;
                new StaticAchievementToken("polarisU.common1");//蒼天の導星
            }

            if (MyPlayer.IsInvisible)
            {
                new StaticAchievementToken("polarisU.hard1");//軌道上の英雄
            }
        }

        [Local, OnlyMyPlayer]
        void GameEnd(GameEndEvent ev)
        {
            if (ev.EndState.Winners.Test(MyPlayer) && ImpostorMedium >= 2 && ImpostorKill >= 2 && !CrewKill)
            {
                new StaticAchievementToken("polarisU.challenge1");
            }
        }
    }
}