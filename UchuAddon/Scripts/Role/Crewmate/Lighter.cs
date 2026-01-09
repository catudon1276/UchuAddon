using Hori.Core;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules.ScriptComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Virial;
using Virial.Assignable;
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

public class LighterU : DefinedSingleAbilityRoleTemplate<LighterU.Ability>, DefinedRole, HasCitation
{
    public LighterU() : base("lighterU", new(219, 203, 55), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [LightCoolDownOption, LightDurationOption, LightRatioOption])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration LightCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration LightDurationOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightDuration", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration LightRatioOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightRatio", (0.25f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Lighter.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public LighterU MyRole = new();
    static private readonly GameStatsEntry StatsLight = NebulaAPI.CreateStatsEntry("stats.lighterU.light", GameStatsCategory.Roles, MyRole);
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image lightImage = NebulaAPI.AddonAsset.GetResource("LightButton.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        public bool IgnoreBlackout { get; private set; } = true;

        private int lightGameCount = 0;
        bool isLighting = false;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var lightButton = NebulaAPI.Modules.AbilityButton(this).BindKey(Virial.Compat.VirtualKeyInput.Ability).SetImage(lightImage).SetLabel("light").SetAsUsurpableButton(this);
                lightButton.Availability = (button) => MyPlayer.CanMove;
                lightButton.Visibility = (button) => !MyPlayer.IsDead;
                lightButton.OnClick = (button) => button.StartEffect();
                lightButton.OnEffectStart = (button) =>
                {
                    StatsLight.Progress();
                    isLighting = true;
                    lightGameCount++;
                    if (lightGameCount >= 5)
                    {
                        new StaticAchievementToken("ligtherU.common1");
                    }

                    if (LightRatioOption < 1)
                    {
                        new StaticAchievementToken("ligtherU.another1");
                    }
                };
                lightButton.OnEffectStart = (button) =>
                {
                    using (RPCRouter.CreateSection("LighterULight"))
                    {
                        MyPlayer.GainAttribute(PlayerAttributes.Eyesight, LightDurationOption, LightRatioOption, false, 100);
                    }
                };
                lightButton.OnEffectEnd = (button) =>
                {
                    isLighting = false;
                    lightButton.StartCoolDown();
                };
                lightButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, LightCoolDownOption).SetAsAbilityTimer().Start();
                lightButton.EffectTimer = NebulaAPI.Modules.Timer(this, LightDurationOption);
            }
        }
        [Local]
        void OnUpdateCamera(CameraUpdateEvent ev)
        {
            if (!isLighting) return;

            int count = NebulaGameManager.Instance.AllPlayerInfo
                .Where(p => !p.IsDead && p != MyPlayer)
                .Count(p => UnityEngine.Vector2.Distance(MyPlayer.TruePosition.ToUnityVector(), p.VanillaPlayer.transform.position) <= LightRatioOption);

            if (count >= 5)
            {
                new StaticAchievementToken("lighterU.challenge1");
            }
        }
    }
}









//public class LighterU : DefinedRoleTemplate, DefinedRole, HasCitation
//{
//private LighterU() : base("lighterU", new(219, 203, 55),RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [LightCoolDownOption,LightDurationOption,LightRatioOption])
//{
//}
//AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
//RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
//static private FloatConfiguration LightCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
//static private FloatConfiguration LightDurationOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightDuration", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
//static private FloatConfiguration LightRatioOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightRatio", (0.25f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);

//Citation? HasCitation.Citation { get { return Citations.TheOtherRoles; } }

//static public LighterU MyRole = new LighterU();
//public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
//{
//private AchievementToken<int>? achCommon1Token;
//DefinedRole RuntimeRole.Role => MyRole;
//private ModAbilityButton? lightButton = null;
//static private Virial.Media.Image lightImage = NebulaAPI.AddonAsset.GetResource("LightButton.png")!.AsImage(115f)!;
//public Instance(GamePlayer player) : base(player)
//{
//}
//void RuntimeAssignable.OnActivated()
//{
//if (AmOwner)
//{
//achCommon1Token = new AchievementToken<int>("lighterU.common1",0,(count, _) => count >= 5);
//lightButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, LightCoolDownOption, "light", lightImage, (button) => MyPlayer.CanMove, (button) => !MyPlayer.IsDead, false);
//lightButton.OnClick = (button) =>
//{
//button.StartEffect();
//};
//lightButton.OnEffectStart = (button) =>
//{


//MyPlayer.GainAttribute(PlayerAttributes.Eyesight, LightDurationOption, LightRatioOption, false, 1, "lighter::light");
//};
//lightButton.OnEffectEnd = (button) =>
//{
//button.StartCoolDown();
//achCommon1Token.Value++;
//};
//lightButton.StartCoolDown();
//lightButton.EffectTimer = NebulaAPI.Modules.Timer(this, LightDurationOption);
//lightButton.SetLabel("light");
//}
//}
//}
//}

