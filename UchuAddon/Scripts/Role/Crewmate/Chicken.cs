using AsmResolver.PE.DotNet.ReadyToRun;
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
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Crewmate;

internal class ChickenU : DefinedSingleAbilityRoleTemplate<ChickenU.Ability>, DefinedRole,IAssignableDocument
{
    private ChickenU() : base("chickenU", new(255, 238, 0), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [DeadPercentVent,DeadPercentLadder, DeadPercentZipLine, DeadPercentPlatform,DeadPercentDoor,DeadElectricSecond,DeadNumOfVote,VentRange])
    {
    }
    static public readonly FloatConfiguration DeadPercentVent = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentVent", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentLadder = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentLadder", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentZipLine = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentZipLine", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentPlatform = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentPlatform", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentDoor = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentDoor", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadElectricSecond = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadElectricSecond", (10f, 180f, 5f), 40f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration DeadNumOfVote = NebulaAPI.Configurations.Configuration("options.role.admiralU.deadNumOfVote", (1, 10), 2);
    static public readonly FloatConfiguration VentRange = NebulaAPI.Configurations.Configuration("options.role.chickenU.ventRange", (0.25f, 5f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public ChickenU MyRole = new ChickenU();

    private static MultiImage DocumentImage = NebulaAPI.AddonAsset.GetResource("ChickenDocument.png")!.AsMultiImage(7, 1, 100f)!;
    static private Image DoorIcon => DocumentImage.AsLoader(0);static private Image VentIcon => DocumentImage.AsLoader(1);static private Image MessIcon => DocumentImage.AsLoader(2);static private Image DropIcon => DocumentImage.AsLoader(3);static private Image FallIcon => DocumentImage.AsLoader(4);static private Image WussIcon => DocumentImage.AsLoader(5);

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DoorIcon, "role.chicken.ability.Door");
        yield return new(VentIcon, "role.chicken.ability.Vent");
        yield return new(MessIcon, "role.chicken.ability.Mess");
        yield return new(DropIcon, "role.chicken.ability.Drop");
        yield return new(FallIcon, "role.chicken.ability.Fall");
        yield return new(WussIcon, "role.chicken.ability.Wuss");
    }




    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        public static TranslatableTag ChickenVentDead = new("state.chickenDeadVent"); //転倒死  ベント
        public static TranslatableTag ChickenFallDead = new("state.chickenDeadFall"); //転落死　梯子・ジップライン
        public static TranslatableTag ChickenDropDead = new("state.chickenDeadDrop"); //落下死　昇降機
        public static TranslatableTag ChickenDoorDead = new("state.chickenDeadDoor"); //挟圧死　ドア
        public static TranslatableTag ChickenMessDead = new("state.chickenDeadWuss"); //錯乱死　停電
        public static TranslatableTag ChickenWussDead = new("state.chickenDeadMess"); //恐怖死　投票

        bool FrameVent = false;
        float VentDead = DeadPercentVent;
        float LadderDead = DeadPercentLadder;
        float ZipLineDead = DeadPercentZipLine;
        float PlatformDead = DeadPercentPlatform;
        float DoorDead = DeadPercentDoor;
        float Electric = 0;
        private ObjectTracker<Vent>? ventTracker;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
           
        }

        void Update(GameUpdateEvent ev)
        {
            ventTracker ??= ObjectTrackers.ForVents(this, VentRange, MyPlayer, v => true, Color.yellow, true);
            bool isTrackingNow = ventTracker.CurrentTarget != null;
            if (FrameVent && isTrackingNow)
            {
                FrameVent = isTrackingNow;
                return;
            }
            if (isTrackingNow)
            {
                if (ventTracker.CurrentTarget != null)
                {
                    Vent targetVent = ventTracker.CurrentTarget;
                    float Vent = UnityEngine.Random.Range(1f, 100f);
                    if (Vent <= VentDead)
                    {
                        MyPlayer.Suicide(PlayerState.Suicide, ChickenVentDead, KillParameter.RemoteKill);
                    }
                }
            }

            FrameVent = isTrackingNow;
        }

        [OnlyMyPlayer]
        void LadderCheck(PlayerClimbLadderEvent ev)
        {
            float Ladder = UnityEngine.Random.Range(1f, 100f);
            if (Ladder <= LadderDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenFallDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void ZipCheck(PlayerUseZiplineEvent ev)
        {
            float ZipLine = UnityEngine.Random.Range(1f, 100f);
            if (ZipLine <= ZipLineDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenDoorDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void Platform(PlayerUseMovingPlatformEvent ev)
        {
            float Platform = UnityEngine.Random.Range(1f, 100f);
            if (Platform <= PlatformDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenDropDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void Door(PlayerBeginMinigameByDoorLocalEvent ev)
        {
            float Door = UnityEngine.Random.Range(1f, 100f);
            if (Door <= DoorDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenDoorDead, KillParameter.RemoteKill);
            }
        }

        void OnVotedForMeLocal(PlayerVotedLocalEvent ev)
        {
            if (ev.Voters.Count >= DeadNumOfVote)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenWussDead, KillParameter.RemoteKill);
            }
        }


        [OnlyMyPlayer]
        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (ev.EndReason != GameEndReason.Sabotage) ev.TryOverwriteEnd(UchuGameEnd.CrewmateChickenWin, GameEndReason.Special);
        }
        
        void update(GameUpdateEvent ev)
        {
            bool ElectricSabo = PlayerTask.PlayerHasTaskOfType<ElectricTask>(PlayerControl.LocalPlayer);
            if (MyPlayer.IsDead) return;
            if (ElectricSabo)
            {
                Electric += Time.deltaTime;
                if (Electric >= DeadElectricSecond)
                {
                    MyPlayer.Suicide(PlayerState.Suicide, ChickenMessDead, KillParameter.RemoteKill);
                }
            }
            else
            {
                Electric = 0f;
            }
        }
    }
}

[NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
public class ChickenWinAssignRule : AbstractModule<IGameModeStandard>, IGameOperator
{
    static ChickenWinAssignRule() => DIManager.Instance.RegisterModule(() => new ChickenWinAssignRule());

    public ChickenWinAssignRule() => this.RegisterPermanently();

    void CheckWin(PlayerCheckWinEvent ev)
    {
        if (ev.GameEnd != UchuGameEnd.CrewmateChickenWin) return;

        if (ev.Player.Role.Role.Team == NebulaTeams.CrewmateTeam)
        {
            ev.SetWinIf(true);
        }
    }
}