using AsmResolver.PE.DotNet.ReadyToRun;
using Hori.Core;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Image = Virial.Media.Image;

namespace Hori.Scripts.Role.Impostor;

public class WitchU : DefinedSingleAbilityRoleTemplate<WitchU.Ability>, DefinedRole, HasCitation
{
    public WitchU() : base("witchU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SpellCooldown, NumOfOneTurnSpell,WitchNormalKill])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration SpellCooldown = NebulaAPI.Configurations.Configuration("options.role.witchU.spellCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfOneTurnSpell = NebulaAPI.Configurations.Configuration("options.role.witchU.numOfOneTurnSpell", (1, 15), 1);
    static private BoolConfiguration WitchNormalKill = NebulaAPI.Configurations.Configuration("option.role.witchU.witchNormalKill", false);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Witch.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public WitchU MyRole = new();
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.sampleU.sampleSkill", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image SpellImage = NebulaAPI.AddonAsset.GetResource("WitchSpellButton.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        static List<GamePlayer>? WitchTargets = new List<GamePlayer>();
        bool meetingStarted = false;
        bool WitchCheck = false;
        int leftSpell = NumOfOneTurnSpell;
        TranslatableTag spell = new("witch.spell");
        bool IPlayerAbility.HideKillButton => !WitchNormalKill;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var SpellButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SpellCooldown, "Spell", SpellImage, _ => playerTracker.CurrentTarget != null && playerTracker.CurrentTarget.RealPlayer.Role.Role.Team != NebulaTeams.ImpostorTeam&& !WitchTargets!.Contains(playerTracker.CurrentTarget));
                SpellButton.Visibility = (button) => !MyPlayer.IsDead && leftSpell > 0;
                SpellButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                SpellButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null && target != MyPlayer)
                    {
                        WitchCheck = true;
                        leftSpell--;
                        SpellButton.UpdateUsesIcon(leftSpell.ToString());
                        RpcWitchTargets.Invoke(playerTracker.CurrentTarget!);
                        SpellButton.StartCoolDown();
                    }
                };

                SpellButton.OnUpdate = (button) =>
                {
                   SpellButton.UpdateUsesIcon(leftSpell.ToString());
                };

                SpellButton.ShowUsesIcon(0, leftSpell.ToString());
            }

        }
        [NebulaRPC]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingStarted = true;
            if (MyPlayer.IsDead) return;
            WitchTargets?.Remove(MyPlayer);
            /*
            if (WitchTargets != null)
            {
                foreach (var target in WitchTargets)
                {
                    target.RealPlayer.AddModifier(WitchSpellU.MyRole);
                }
            }*/

        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {         
            meetingStarted = false;
            leftSpell = NumOfOneTurnSpell;

            /*if (WitchTargets != null)
            {
                foreach (var target in WitchTargets)
                {
                    target.RealPlayer.RemoveModifier(WitchSpellU.MyRole);
                }
            }*/
            if (MyPlayer.IsDead)
            {
                if (WitchTargets?.Count >= 1)
                {
                    new StaticAchievementToken("witchU.another2");
                }
            }

            if (MyPlayer.IsDead) return;
            if (WitchTargets == null || WitchTargets.Count == 0) return;
            foreach (var target in WitchTargets.ToList())
            {
                if (target != null && !target.IsDead && target != MyPlayer)
                {
                    target.Suicide(spell, PlayerState.Suicide, KillParameter.RemoteKill);
                    new StaticAchievementToken("witchU.common1");

                    if (WitchTargets.Count >= 3)
                    {
                        new StaticAchievementToken("witchU.challenge1");
                    }
                }
            }
            WitchTargets.Clear();
        }
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (!meetingStarted) return;
            if (WitchTargets == null) return;

            if (WitchTargets.Contains(ev.Player) && ev.Player != MyPlayer)
            {
                ev.Name += " ∆".Color("#FF0000");
            }
        }

        RemoteProcess<GamePlayer> RpcWitchTargets = new("WitchTheSpell", (message, _) =>
        {
            WitchTargets?.Add(message);
        });

        void OnExiled(PlayerExiledEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (WitchTargets?.Count == 0) return;

            new StaticAchievementToken("witchU.hard1");
        }


        void GameEndCheck(GameEndEvent ev)
        {
            if (WitchCheck == false) return;

            new StaticAchievementToken("witchU.another1");
        }
    }
}



/*public class WitchSpellU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private WitchSpellU() : base("witchSpellU", "SPL", new(255, 0, 0),[], true, false)
    {
    }
    static public WitchSpellU MyRole = new WitchSpellU();
    bool DefinedAssignable.ShowOnHelpScreen => false;
    bool DefinedAssignable.ShowOnFreeplayScreen => false;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
    => new Instance(player, arguments.Get(0, 0), arguments.Get(1, 0) == 1);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player, int someId, bool isActive) : base(player)
        {

        }

        void RuntimeAssignable.OnActivated()
        {

        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            name += " ∆".Color(new UnityEngine.Color(255, 0, 0));
        }

    }
}*/