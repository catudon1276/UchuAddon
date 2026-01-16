using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
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
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class ProsecutorU : DefinedSingleAbilityRoleTemplate<ProsecutorU.Ability>, DefinedRole, HasCitation,IAssignableDocument
{
    public ProsecutorU() : base("prosecutorU", new(52, 119, 235), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfProsecute,ProsecuteCrewmateCheck])
    {
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfUs;

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private IntegerConfiguration NumOfProsecute = NebulaAPI.Configurations.Configuration("options.role.prosecutorU.numOfProsecute", (1, 15), 2);
    public static BoolConfiguration ProsecuteCrewmateCheck = NebulaAPI.Configurations.Configuration("options.role.prosecutorU.prosecuteCrewmateCheck", true);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public ProsecutorU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Prosecutor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private readonly Virial.Media.Image ProsecuteImage = NebulaAPI.AddonAsset.GetResource("ProsecutorButton.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ProsecuteImage, "role.prosecutorU.ability.prosecute");
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        bool usingProsecuteSkill = false;
        bool usedInTheMeeting = false;
        int leftProsecute = NumOfProsecute;

        
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                if (!usingProsecuteSkill) Nebula.Utilities.Helpers.TextHudContent("ProsecutorUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.prosecutorU.hudText") + ": " + leftProsecute, true);

                var prosecuteButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                prosecuteButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted && leftProsecute > 0;
                prosecuteButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.Discussion) && !usedInTheMeeting && leftProsecute > 0;
                prosecuteButton.SetLabel("prosecute");
                prosecuteButton.SetSprite(ProsecuteImage.GetSprite());
                prosecuteButton.OnClick = (button) =>
                {
                    usedInTheMeeting = true;
                    usingProsecuteSkill = true;
                };


                prosecuteButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };
            }
        }
        void MeetingEnd(MeetingEndEvent ev)
        {
            usingProsecuteSkill = false;
        }

        void ShowMessage(FixExileTextEvent ev)
        {
            if (!usingProsecuteSkill) return;
            ev.AddText(NebulaAPI.Language.Translate("role.prosecutorU.message1"));
        }

        [OnlyMyPlayer]
        void OnCastVoteLocal(PlayerVoteCastLocalEvent ev)
        {
            var player = GamePlayer.AllPlayers.Where(p => p != MyPlayer);
            if (!usingProsecuteSkill) return;
            leftProsecute--;
            MeetingHud.Instance.ResetPlayerState();
            ev.Vote = 10;
            usingProsecuteSkill = false;
            NebulaAPI.CurrentGame!.CurrentMeeting?.EndVotingForcibly(false);
        }

        [Local]
        void ExiledEx(PlayerExiledEvent ev)
        {
            if (!ProsecuteCrewmateCheck) return;
            if (!usingProsecuteSkill) return;

            var target = ev.Player;
            if (target.IsTrueCrewmate)
            {
                ExtraExileRoleSystem.MarkExtraVictim(target);
            } 
        }
    }
}