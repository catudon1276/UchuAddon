using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
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
using System.Diagnostics;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using Virial.Text;
using static Il2CppSystem.DateTimeParse;
using static Nebula.Roles.Impostor.Cannon;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Crewmate;


public class NimrodU : DefinedSingleAbilityRoleTemplate<NimrodU.Ability>, DefinedRole,HasCitation
{
    public NimrodU() : base("nimrodU", new(199, 162, 70), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfInvolve,MaxLeftTimer])
    {
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    static public readonly IntegerConfiguration NumOfInvolve = NebulaAPI.Configurations.Configuration("options.role.nimrodU.numOfInvolve", (1, 15), 5);
    static private readonly FloatConfiguration MaxLeftTimer = NebulaAPI.Configurations.Configuration("options.role.nimrodU.maxLeftTimer", (0f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public NimrodU MyRole = new();

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image InvolveImage = NebulaAPI.AddonAsset.GetResource("NimrodButton.png")!.AsImage(115f)!;
        static private Image ExiledImage = NebulaAPI.AddonAsset.GetResource("AdmiralSkillButton.png")!.AsImage(115f)!;
        int left = NumOfInvolve;
        bool InvolveCheck = false;
        bool ExiledCheck = false;
        List<GamePlayer> InvolvePlayer = new List<GamePlayer>();

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                Nebula.Utilities.Helpers.TextHudContent("NimrodUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.nimrodU.hudText") + ": " + left, true);

               /* bool usedInTheMeeting = false;
                var ExiledButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                ExiledButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted;
                ExiledButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.Discussion) && !usedInTheMeeting;
                ExiledButton.SetLabel("nimrodU.exiled");
                ExiledButton.SetSprite(ExiledImage.GetSprite());
                ExiledButton.OnClick = (button) =>
                {
                    
                    usedInTheMeeting = true;
                };

                ExiledButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };*/
            }
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            var involveButton = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            involveButton?.RegisterMeetingAction(new(InvolveImage,p =>
            {
                GamePlayer target = p.MyPlayer;
                if (target != null)
                {
                    InvolvePlayer.Add(target);
                    left--;
                    InvolveCheck = true;
                }
            }, p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner&& left > 0  && !InvolveCheck && MeetingHudExtension.LeftTime > MaxLeftTimer && !PlayerControl.LocalPlayer.Data.IsDead &&
            GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess));
        }

        [OnlyMyPlayer,Local]
        void OnPlayerExiled(PlayerExiledEvent ev)
        {
            if (InvolvePlayer.Count == 0) return;

            foreach (var target in InvolvePlayer)
            {
                if (target.IsDead) continue;

                target.VanillaPlayer.ModMarkAsExtraVictim(ev.Player.VanillaPlayer,PlayerState.Embroiled,EventDetail.Embroil);
            }
        }

        [Local]
        void MeetingLeset(MeetingPreStartEvent ev)
        {
            InvolvePlayer.Clear();
            InvolveCheck = false;
        }
    }
}