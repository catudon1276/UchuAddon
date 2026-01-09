using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
//タスク完了で確白設定追加

namespace Hori.Scripts.Role.Crewmate;

public class AdmiralU : DefinedSingleAbilityRoleTemplate<AdmiralU.Ability>, DefinedRole
{
    public AdmiralU() : base("admiralU", new(61, 88, 179), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfAdmiral,PublickName, NumOfPublickTask, VoteWatching, NotGuess, PrivateNameGuess])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Admiral.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private IntegerConfiguration NumOfAdmiral = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfAdmiral", (1, 15), 2);
    static private BoolConfiguration PublickName = NebulaAPI.Configurations.Configuration("options.role.admiralU.publicName", true);
    static private IntegerConfiguration NumOfPublickTask = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfPublicTask", (1, 12), 8,() => PublickName);
    //static private FloatConfiguration NumOfPublickTask = NebulaAPI.Configurations.Configuration("options.role.admiralU.numOfPublicTask", (10f, 100f, 10f), 80f, FloatConfigurationDecorator.Percentage, () => PublickName);
    static private BoolConfiguration VoteWatching = NebulaAPI.Configurations.Configuration("options.role.admiralU.voteWatching", true);
    static private BoolConfiguration NotGuess = NebulaAPI.Configurations.Configuration("options.role.admiralU.notGuess", false);
    static private BoolConfiguration PrivateNameGuess = NebulaAPI.Configurations.Configuration("options.role.admiralU.privateNameGuess", false,() => NotGuess);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Admiral.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public AdmiralU MyRole = new();
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        private bool isColorActivated = false;
        bool usingAdmiralSkill = false;
        bool myRevive = false;
        int leftAdmiral = NumOfAdmiral;
        static List<GamePlayer> AdmiralName = new List<GamePlayer>();
        static List<GamePlayer> ExiledPlayer = new List<GamePlayer>();
        float TaskPercent = NumOfPublickTask / 10f;

        static private Image admiralButtonSprite = NebulaAPI.AddonAsset.GetResource("AdmiralSkillButton.png")!.AsImage(115f)!;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                if (!usingAdmiralSkill) Nebula.Utilities.Helpers.TextHudContent("AdmiralUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.admiralU.hudText") + ": " + leftAdmiral, true);

                bool usedInTheMeeting = false;
                var admiralSkillButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                admiralSkillButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted && leftAdmiral > 0;
                admiralSkillButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.Discussion) && !usedInTheMeeting && leftAdmiral > 0;
                admiralSkillButton.SetLabel("admiral");
                admiralSkillButton.SetSprite(admiralButtonSprite.GetSprite());
                admiralSkillButton.OnClick = (button) =>
                {
                    usedInTheMeeting = true;
                    usingAdmiralSkill = true;
                    leftAdmiral--;
                };

                admiralSkillButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };

                if (VoteWatching)
                {
                    var harmony = new HarmonyLib.Harmony("com.hori.ceo.anonymousvote");
                    AnonymousVoteBypass.Patch(harmony);
                }
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];


        void OnExiled(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (usingAdmiralSkill == false) return;

            //能力を使用してたら追放されたプレイヤーをリストに
            if (AmOwner && usingAdmiralSkill)
            {
                GamePlayer exiled = ev.Player;  

                if (exiled != null && exiled.IsDead)
                {
                    if (!ExiledPlayer.Contains(exiled))ExiledPlayer.Add(exiled);
                }
            }
        }

        [OnlyMyPlayer]
        void MyRevive(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsTrueCrewmate)
            {
                if (ev.Player != MyPlayer) return;
                MyPlayer.Revive(MyPlayer, MyPlayer.TruePosition, true, true);
            }
        }

        void MeetingEnd(MeetingPreEndEvent ev)
        {
            usingAdmiralSkill = false;

            foreach (var p in ExiledPlayer)
            {
                if (p != null && p.IsDead)
                {
                    p.Revive(p, p.TruePosition, true, true);
                }
            }
            ExiledPlayer.Clear();
        }

        void ShowMessage(FixExileTextEvent ev)
        {
            if (!usingAdmiralSkill) return;

            if (ev.Exiled.Any())
            {
                ev.AddText(NebulaAPI.Language.Translate("role.admiralU.message1"));
            }
        }

        [Local]
        void TaskCompleted(PlayerTaskCompleteEvent ev)
        {
            if (!PublickName) return;
            if (MyPlayer.Tasks.CurrentCompleted >= NumOfPublickTask)
            {
                RpcAdmiralName.Invoke(MyPlayer);
            }
            /*if (MyPlayer.Tasks.CurrentCompleted / MyPlayer.Tasks.CurrentTasks >= TaskPercent)
            {
                RpcAdmiralName.Invoke(MyPlayer);
            }*/
        }

        [OnlyMyPlayer]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (!PublickName) return;
            if (!AdmiralName.Contains(ev.Player)) return;
            ev.Color = new Virial.Color(61f / 255f, 88f / 255f, 179f / 255f);
        }

        RemoteProcess<GamePlayer> RpcAdmiralName = new("PublicName", (message, _) =>
        {
            AdmiralName.Add(message);
        });

        [OnlyMyPlayer]
        void Guess(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (NotGuess)
            {
                if (PrivateNameGuess)
                {
                    if (AdmiralName.Count == 0) return;
                    ev.CanGuess = false;
                }
                else
                {
                    ev.CanGuess = false;
                }
            }
        }
    }
}