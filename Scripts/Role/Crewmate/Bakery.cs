using AmongUs;
using Hori.Core;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Helpers;
using Virial.Media;
using static Il2CppSystem.DateTimeParse;
using static UnityEngine.GraphicsBuffer;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Crewmate;

public class BakeryU : DefinedSingleAbilityRoleTemplate<BakeryU.Ability>, DefinedRole
{
    private BakeryU() : base("bakeryU", new(3, 252, 111), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [PassBreadCooldownOption, BreadPerMeetingOption]) 
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Bakery.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon, ConfigurationTags.TagBeginner);
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Bakery.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    //設定を定義
    static private FloatConfiguration PassBreadCooldownOption = NebulaAPI.Configurations.Configuration("options.role.bakeryU.passBreadCooldown", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration BreadPerMeetingOption = NebulaAPI.Configurations.Configuration("options.role.bakeryU.breadPerMeeting", (1, 10), 3);
    static public BakeryU MyRole = new BakeryU();



    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        //パンを持っているプレイヤーのリスト
        static List<GamePlayer> haveBread = new List<GamePlayer>();

        int left = BreadPerMeetingOption;

        //乱数生成器
        System.Random rand = new System.Random();

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        //パンを渡すボタンのいろいろ
        private Nebula.Modules.ScriptComponents.ModAbilityButtonImpl? passBreadButton = null;
        static private Image passBreadButtonSprite = NebulaAPI.AddonAsset.GetResource("passBreadButton.png")!.AsImage(115f)!;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {

                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);
                var passBreadButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, PassBreadCooldownOption, "bakeryU.passBread", passBreadButtonSprite, _ => playerTracker.CurrentTarget != null && !haveBread.Contains(playerTracker.CurrentTarget) && left != 0);
                passBreadButton.OnClick = (button) =>
                {
                    RpcPassBread.Invoke(playerTracker.CurrentTarget!);

                    left--;


                    passBreadButton.StartCoolDown();

                };

                passBreadButton.OnUpdate = (button) =>
                {
                    passBreadButton.UpdateUsesIcon(left.ToString());
                };

                passBreadButton.ShowUsesIcon(3, left.ToString());
            }
        }
        void ShowMessage(FixExileTextEvent ev)
        {

            //生存していて
            if (!MyPlayer.IsDead)
            {
                //他にメッセージが出ていなければ
                if (!ev.GetTexts().Contains(NebulaAPI.Language.Translate("role.bakeryU.message1")) || ev.GetTexts().Contains(NebulaAPI.Language.Translate("role.bakeryU.message2")) || ev.GetTexts().Contains(NebulaAPI.Language.Translate("role.bakeryU.message3")))
                    //抽選をし、表示
                    if (rand.Next(1000) < 106)
                    {
                        if (rand.Next(106) < 6)
                        {
                            ev.AddText(NebulaAPI.Language.Translate("role.bakeryU.message3"));
                        }
                        else
                        {
                            ev.AddText(NebulaAPI.Language.Translate("role.bakeryU.message2"));
                        }
                    }
                    else
                    {
                        ev.AddText(NebulaAPI.Language.Translate("role.bakeryU.message1"));
                    }

            }

        }

        void MeetingEndEvent(MeetingEndEvent ev)
        {
            haveBread.Clear();
            left = BreadPerMeetingOption;
        }

        void DecoratePlayerName(PlayerDecorateNameEvent ev)
        {
            if (haveBread.Contains(ev.Player) && MeetingHud.Instance) ev.Name += " <color=#03fc6f>@";
        }


        RemoteProcess<GamePlayer> RpcPassBread = new("PassBread", (message, _) =>
        {
            haveBread.Add(message);

        });
    }
}