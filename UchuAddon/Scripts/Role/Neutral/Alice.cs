using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class AliceU : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Team MyTeam = new Team("teams.aliceU", new Virial.Color(255, 255, 15), TeamRevealType.OnlyMe);
    private AliceU() : base("aliceU", new(255, 255, 15), RoleCategory.NeutralRole, MyTeam, [NumOfWin,CanFixLightOption,CanFixCommsOption,ExtraWin,VentConfiguration,
      new GroupConfiguration("options.role.aliceU.group.echo",[EchoCooldownOption,EchoRangeOption,NumOfEcho],GroupConfigurationColor.ToDarkenColor(Color.yellow)),
      new GroupConfiguration("options.role.aliceU.group.winKill",[WinForKill,KillCoolDownOption,NumOfWinKill,KillVentPlayer],GroupConfigurationColor.ToDarkenColor(Color.yellow)),
        TaskConfiguration.AsGroup(new(GroupConfigurationColor.ToDarkenColor(MyTeam.Color.ToUnityColor()))) ])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.ExtremeRoles;

    static private IntegerConfiguration NumOfWin = NebulaAPI.Configurations.Configuration("options.role.aliceU.numOfWin", (1, 15), 1);
    static private BoolConfiguration CanFixLightOption = NebulaAPI.Configurations.Configuration("options.role.aliceU.canFixLight", false);
    static private BoolConfiguration CanFixCommsOption = NebulaAPI.Configurations.Configuration("options.role.aliceU.canFixComms", false);
    static private BoolConfiguration ExtraWin = NebulaAPI.Configurations.Configuration("options.role.aliceU.extraWin", false);
    static private readonly FloatConfiguration EchoCooldownOption = NebulaAPI.Configurations.Configuration("options.role.aliceU.echoCooldown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration EchoRangeOption = NebulaAPI.Configurations.Configuration("options.role.aliceU.echoRange", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Ratio);
    static private IntegerConfiguration NumOfEcho = NebulaAPI.Configurations.Configuration("options.role.aliceU.numOfEcho", (0, 99), 5);

    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("role.aliceU.vent", true);
    static private BoolConfiguration WinForKill = NebulaAPI.Configurations.Configuration("options.role.aliceU.winForKill", false);
    static private IRelativeCoolDownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.aliceU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f, static () => WinForKill);
    static private IntegerConfiguration NumOfWinKill = NebulaAPI.Configurations.Configuration("options.role.aliceU.numOfWinKill", (0, 15), 2, static () => WinForKill);
    static private BoolConfiguration KillVentPlayer = NebulaAPI.Configurations.Configuration("options.role.aliceU.KillVentPlayer", false, static () => WinForKill);
    static private ITaskConfiguration TaskConfiguration = NebulaAPI.Configurations.TaskConfiguration("options.role.aliceU.task", false, true, translationKey: "options.role.aliceU.task");
    static public bool RequiresTasksForWin => TaskConfiguration.RequiresTasks;
    static public float KillCooldown => KillCoolDownOption.CoolDown;


    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public AliceU MyRole = new AliceU();

    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonYellow.png")!.AsImage(100f)!;
        static private Image EchoImage = NebulaAPI.AddonAsset.GetResource("AlicePremonitionButton.png")!.AsImage(115f)!;
        int totalKill = 0;
        int numDead = NumOfWin;
        int leftEcho = NumOfEcho;
        int leftKill = NumOfWinKill;
        bool extra = false;
        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }




        public override void OnActivated()
        {
            if (AmOwner)
            {
                if (WinForKill)
                {
                    ModAbilityButton killButton = null!;

                    var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, KillVentPlayer);
                    myTracker.SetColor(MyRole.RoleColor);

                    killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                        KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                        (target, _) =>
                        {
                            var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                            if (!(cancelable?.IsCanceled ?? false))
                            {
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                                totalKill++;
                            }
                            if (leftKill > 0)
                            {
                                leftKill--;
                                killButton.UpdateUsesIcon(leftKill.ToString());
                            }
                            if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                        },
                        null,
                        _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                        _ => MyPlayer.AllowToShowKillButtonByAbilities
                        );
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                    killButton.SetImage(KillImage);
                    killButton.ShowUsesIcon(1, leftKill.ToString());
                }
   

                var searchButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, EchoCooldownOption, "Premonition", EchoImage);
                searchButton.Visibility = (button) => !MyPlayer.IsDead && leftEcho > 0;
                searchButton.ShowUsesIcon(1, leftEcho.ToString());
                searchButton.OnClick = (button) =>
                {
                    NebulaManager.Instance.StartCoroutine(CoSearch(MyPlayer.Position).WrapToIl2Cpp());
                    leftEcho--;
                    searchButton.UpdateUsesIcon(leftEcho.ToString());
                    button.StartCoolDown();
                };

            }
        }
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            numDead--;
            if (numDead > 0)
            {
                ev.Result = KillResult.ObviousGuard;
            }
            else if(!ExtraWin)
            {
                if (RequiresTasksForWin && !(MyPlayer.Tasks.IsCompletedCurrentTasks))return;
                if (WinForKill && totalKill < NumOfWinKill)return;
                NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.AliceWin,GameEndReason.Special,BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
            }
            else if (ExtraWin)
            {
                if (RequiresTasksForWin && !(MyPlayer.Tasks.IsCompletedCurrentTasks)) return;
                if (WinForKill && totalKill < NumOfWinKill) return;
                extra = true;
            }
        }

        IEnumerator CoSearch(Vector2 position)
        {
            EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
            float radious = 0f;
            var circle = EffectCircle.SpawnEffectCircle(null, MyPlayer.Position.ToUnityVector(), Color.yellow, 0f, null, true);
            this.BindGameObject(circle.gameObject);
            circle.OuterRadius = () => radious;

            MyRole.UnityColor.ToHSV(out var hue, out _, out _);
            bool isFirst = true;
            while (radious < EchoRangeOption)
            {
                if (MeetingHud.Instance) break;

                radious += Time.deltaTime * 5f;
                foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                {
                    if (!p.AmOwner && !p.IsDead && (p.IsKiller /*|| p.HasRole<Sheriff>()*/) && !pMask.Test(p) && p.Position.Distance(position) < radious)
                    {
                        pMask.Add(p);
                        AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                        isFirst = false;
                    }
                }
                yield return null;
            }

            circle.Disappear();
        }


        void SetAliceTasks()
        {
            if (!TaskConfiguration.RequiresTasks) return;
            if (AmOwner)
            {
                using (RPCRouter.CreateSection("AliceTask"))
                {
                    TaskConfiguration.GetTasks(out var s, out var l, out var c);
                    MyPlayer.Tasks.Unbox().ReplaceTasksAndRecompute(s, l, c);
                    MyPlayer.Tasks.Unbox().BecomeToOutsider();
                }
            }
        }
        void OnGameStart(GameStartEvent ev) => SetAliceTasks();
        RoleTaskType RuntimeRole.TaskType => TaskConfiguration.RequiresTasks ? RoleTaskType.RoleTask : RoleTaskType.NoTask;

        [Local]
        void CheckAliceExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (!ExtraWin) return;
            if (!extra) return;
            ev.SetWin(true);
            ev.ExtraWinMask.Add(UchuGameEnd.AliceExtra);
        }

        bool RuntimeAssignable.CanFixComm => CanFixCommsOption;
        bool RuntimeAssignable.CanFixLight => CanFixLightOption;
    }
}