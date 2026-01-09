using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices.RTCAudio;
using Hori.Core;
using Hori.Scripts.Abilities;
using Il2CppSystem.Xml;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
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
using UnityEngine.UI;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Role.Crewmate;

public class SuperVisorU : DefinedSingleAbilityRoleTemplate<SuperVisorU.Ability>, DefinedRole, HasCitation
{
    public SuperVisorU() : base("superVisorU", new(29, 48, 145), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CanMoveWithMapWatchingOption, CanUseAdminOnMeetingOption, CanIdentifyDeadBodiesOption,
    new GroupConfiguration("options.role.superVisorU.group.charge",[Charge,AdminCharge,ChargesPerTasks],GroupConfigurationColor.ToDarkenColor(new UnityEngine.Color(0.1137f, 0.1882f, 0.5686f)))])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [VisorModifierU.MyRole.ConfigurationHolder!]);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public BoolConfiguration CanMoveWithMapWatchingOption = NebulaAPI.Configurations.Configuration("options.role.jailer.canMoveWithMapWatching", false);
    static public BoolConfiguration CanUseAdminOnMeetingOption = NebulaAPI.Configurations.Configuration("options.role.jailer.canUseAdminOnMeeting", true);
    static public BoolConfiguration CanIdentifyDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.jailer.canIdentifyDeadBodies", false);
    static public BoolConfiguration Charge = NebulaAPI.Configurations.Configuration("options.role.superVisorU.charge", true);
    static public FloatConfiguration AdminCharge = NebulaAPI.Configurations.Configuration("options.role.superVisorU.AdminCharge", (0f, 100f, 5f), 30f, FloatConfigurationDecorator.Percentage, () => Charge);
    static public FloatConfiguration ChargesPerTasks = NebulaAPI.Configurations.Configuration("options.role.superVisorU.chargesPerTasks", (0f, 100f, 1f), 5f, FloatConfigurationDecorator.Percentage, () => Charge);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Visor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public SuperVisorU MyRole = new();
    Citation? HasCitation.Citation => Hori.Core.Citations.ExtremeRoles;

    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && assignable != VisorModifierU.MyRole;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image AdminImage = NebulaAPI.AddonAsset.GetResource("VisorAdminButton .png")!.AsImage(115f)!;

        bool IsEnabled = false;
        private SuperVisorAbility? boundAbility;
        int chargeAdmin = (int)AdminCharge;
        int plusAdmin = (int)ChargesPerTasks;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                //var adminButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "Admirn", AdminImage).SetAsUsurpableButton(this);
                var adminButton = NebulaAPI.Modules.AbilityButton(this, alwaysShow: true).BindKey(Virial.Compat.VirtualKeyInput.Ability).SetImage(AdminImage).SetLabel("Admin").SetAsUsurpableButton(this);
                adminButton.Visibility = (button) => !MyPlayer.IsDead;
                adminButton.Availability = (button) => true;
                if(Charge)adminButton.ShowUsesIcon(3, $"{chargeAdmin:0}%".ToString());
                adminButton.OnClick = (button) =>
                {
                    if (chargeAdmin > 0)
                    {
                        IsEnabled = true;
                    }
                    SuperVisorAbility.TryAddAndBind(() => !this.IsDeadObject && !this.IsUsurped && IsEnabled);
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        HudManager.Instance.InitMap();
                        MapBehaviour.Instance.ShowNormalMap();
                        if (chargeAdmin > 0)
                        {
                            MapBehaviour.Instance.taskOverlay.gameObject.SetActive(false);
                        }
                    });
                };
                adminButton.OnUpdate = (button) =>
                {
                    if(Charge)adminButton.UpdateUsesIcon($"{chargeAdmin:0}%".ToString());
                };
            }
        }

        private float timer = 0f;
        void Update(GameUpdateEvent ev)
        {
            if (!Charge) return;
            if (!IsEnabled) return;

            timer += ev.DeltaTime;
            if (timer >= 1f)
            {
                chargeAdmin--;
                if (chargeAdmin <= 0)
                {
                    MapBehaviour.Instance.Close();
                    IsEnabled = false;
                    SuperVisorAbility.TryAddAndBind(() => !this.IsDeadObject && !this.IsUsurped && IsEnabled);
                }
                timer = 0f;
            }
        }

        void MapClose(MapCloseEvent ev)
        {
            timer = 0f;
            IsEnabled = false;
            SuperVisorAbility.TryAddAndBind(() => !this.IsDeadObject && !this.IsUsurped && IsEnabled);
        }

        [OnlyMyPlayer]
        void TaskComplet(PlayerTaskCompleteLocalEvent ev)
        {
            chargeAdmin += plusAdmin;
        }

        [OnlyMyPlayer]
        void Game(GameStartEvent ev)
        {
            if (Charge) return;
            chargeAdmin = 100;
        }
    }
}


public class VisorModifierU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private VisorModifierU() : base("visorModifierU", "VIS", new(29, 48, 145), [SuperVisorU.CanMoveWithMapWatchingOption, SuperVisorU.CanUseAdminOnMeetingOption, SuperVisorU.CanIdentifyDeadBodiesOption], allocateToImpostor: false)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [SuperVisorU.MyRole.ConfigurationHolder!]);
  
    }
    string DefinedAssignable.InternalName => "visorModifierU";
    string DefinedAssignable.GeneralBlurb => (SuperVisorU.MyRole as DefinedAssignable).GeneralBlurb;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Visor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    int HasAssignmentRoutine.AssignPriority => 2;

    static public VisorModifierU MyRole = new VisorModifierU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        public Instance(GamePlayer myPlayer) : base(myPlayer)
        {
        }

        DefinedModifier RuntimeModifier.Modifier => MyRole;

        /*string? RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort, bool canSeeAllInfo)
        {
            if (isShort) return null;
            return lastRoleName + " " + (this as RuntimeModifier).DisplayColoredName;
        }*/


        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner) SuperVisorAbility.TryAddAndBind(() => !this.IsDeadObject);
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}