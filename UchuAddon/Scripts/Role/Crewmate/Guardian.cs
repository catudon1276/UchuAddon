using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
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
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using UnityColor = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class GuardianU : DefinedSingleAbilityRoleTemplate<GuardianU.Ability>, DefinedRole
{
    public GuardianU() : base("guardianU", new(222, 223, 255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [ShieldTask,ShieldLimit,NumOfShield/*,BlessingCooldown,NumOfBlessing*/])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Guardian.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static FloatConfiguration ShieldTask = NebulaAPI.Configurations.Configuration("options.role.guardianU.shieldTask", (0f, 100f, 10f), 100f, FloatConfigurationDecorator.Percentage);
    static BoolConfiguration ShieldLimit = NebulaAPI.Configurations.Configuration("options.role.guardianU.shieldLimit", false);
    static IntegerConfiguration NumOfShield = NebulaAPI.Configurations.Configuration("options.role.guardianU.numOfShield", (1, 15), 2, () => ShieldLimit);
    //static FloatConfiguration BlessingCooldown = NebulaAPI.Configurations.Configuration("options.role.guardianU.blessingCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    //static IntegerConfiguration NumOfBlessing = NebulaAPI.Configurations.Configuration("options.role.guardianU.numOfBlessing", (1, 15), 2, () => !ShieldLimit);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Guardian.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public GuardianU MyRole = new();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image BlessingImage = NebulaAPI.AddonAsset.GetResource("WitchSpellButton.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        bool hasGuarded = false;
        int leftShield = NumOfShield;
        //int leftBlessing = NumOfBlessing;
        bool Guard = false;
        static List<GamePlayer>? BlessingTargets = new List<GamePlayer>();

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                /*var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                var blessingButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    BlessingCooldown, "Blessing", BlessingImage, _ => playerTracker.CurrentTarget != null&& !BlessingTargets!.Contains(playerTracker.CurrentTarget));
                blessingButton.Visibility = (button) => !MyPlayer.IsDead && leftShield > 0 && leftBlessing > 0 && Guard;
                blessingButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Crewmate);
                blessingButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        if (ShieldLimit)
                        {
                            leftShield--;
                            RpcShield.Invoke(playerTracker.CurrentTarget!);
                            blessingButton.StartCoolDown();
                        }
                        else
                        {
                            leftBlessing--;
                            blessingButton.UpdateUsesIcon(leftBlessing.ToString());
                            RpcShield.Invoke(playerTracker.CurrentTarget!);
                            blessingButton.StartCoolDown();
                        }
                    }
                };

                if (!ShieldLimit)
                {
                    blessingButton.ShowUsesIcon(3, leftBlessing.ToString());
                }*/
            }
        }
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if(Guard)
            {
                if (ShieldLimit)
                {
                    if (leftShield == 0) return;
                }
                if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
                if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;
                ev.Result = KillResult.ObviousGuard;
                hasGuarded = true;

                leftShield--;

                //キルを防ぐ称号
                new StaticAchievementToken("guardianU.challenge1");
            }
            else
            {
                //キル防げなかった称号
                new StaticAchievementToken("guardianU.another1");
            }
        }

        void BlessingGuard(PlayerCheckKilledEvent ev)
        {
            if (ev.Player == MyPlayer) return;
            if (BlessingTargets == null) return;
            if (!BlessingTargets.Contains(ev.Player)) return;
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;

            ev.Result = KillResult.ObviousGuard;

            BlessingTargets.Remove(ev.Player);
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            //追放称号
            new StaticAchievementToken("guardianU.another2");
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!MyPlayer.IsDead && ev.EndState?.EndCondition == NebulaGameEnd.CrewmateWin && !hasGuarded)
            {
                //一度もキルガードせず勝利
                new StaticAchievementToken("guardianU.common1");
            }
        }

        [Local, OnlyMyPlayer]
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (!Guard) return;

            if (ShieldLimit)
            {
                ev.Name += $" ({leftShield})".Color("#ffff00");
            }
            /*else
            {
                ev.Name += $" {leftBlessing}".Color("#FF0000");
            }*/
        }

        /*void TargetsName(PlayerDecorateNameEvent ev)
        {
            if (BlessingTargets == null) return;
            if (BlessingTargets.Contains(ev.Player))
            {
                ev.Name += " ♦".Color("#FFFF00");
            }
        }*/

       
        /*RemoteProcess<GamePlayer> RpcShield = new("Shield", (message, _) =>
        {
            var myplayer = GamePlayer.LocalPlayer!;
            BlessingTargets?.Add(message);
            BlessingTargets?.Remove(myplayer);
        });*/

        void GuardTask(GameUpdateEvent ev)
        {
            float taskRatio = ShieldTask / 100f;

            if (MyPlayer.Tasks.CurrentCompleted / (float)MyPlayer.Tasks.CurrentTasks >= taskRatio)
            {
                Guard = true;
            }
        }
    }
}