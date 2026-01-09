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
using Nebula.Roles.Abilities;
using Nebula.Roles.Neutral;
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
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class AmnesiaU : DefinedRoleTemplate, DefinedRole, HasCitation
{
    public static Team MyTeam = new Team("teams.amnesiaU", new Virial.Color(79, 110, 115), TeamRevealType.OnlyMe);
    private AmnesiaU() : base("amnesiaU", new(79, 110, 115), RoleCategory.NeutralRole, MyTeam, [AmnesiaAction,DeadBodyArrow,VentConfiguration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;


    static private ValueConfiguration<int> AmnesiaAction = NebulaAPI.Configurations.Configuration("options.role.amnesiaU.amnesiaAction", ["options.role.amnesiaU.amnesiaAction.meetingStart", "options.role.amnesiaU.amnesiaAction.meetingEnd"], 0);
    static private BoolConfiguration DeadBodyArrow = NebulaAPI.Configurations.Configuration("options.role.amnesiaU.deadBodyArrow", true);
    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.amnesiaU.vent", true);

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static private readonly GameStatsEntry StatsReport = NebulaAPI.CreateStatsEntry("stats.amnesiaU.report", GameStatsCategory.Roles, MyRole);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Amnesia.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public AmnesiaU MyRole = new AmnesiaU();
    public class Instance : RuntimeVentRoleTemplate, RuntimeRole
    {
        public override DefinedRole Role => MyRole;

        private GamePlayer? reportedDead = null;
        public Instance(GamePlayer player) : base(player, VentConfiguration)
        {
        }

        public override void OnActivated()
        {        
            if (AmOwner)
            {
                if (DeadBodyArrow)
                {
                    var ability = new DeadbodyArrowAbility().Register(this);
                    GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev => ability.ShowArrow = !MyPlayer.IsDead, this);
                }
            }
        }

        [OnlyMyPlayer]
        void Report(ReportDeadBodyEvent ev)
        {
            if (ev.Reporter.AmOwner && ev.Reported != null)
                reportedDead = ev.Reported;
            StatsReport.Progress();
        }

        [Local]
        void MeetingStart(MeetingPreStartEvent ev)
        {
            if (AmnesiaAction.GetValue() != 0) return;
            if (reportedDead == null) return;

            var dead = reportedDead.Unbox();
            if (dead == null) return;

            var targetRole = dead.Role.Role;
            int[] targetArgs = [];

            dead.CoGetRoleArgument(args => targetArgs = args);

            using (RPCRouter.CreateSection("AmnesiaCopy"))
            {
                MyPlayer.Unbox().RpcInvokerSetRole(targetRole, targetArgs).InvokeSingle();
            }

            reportedDead = null;
        }

        [Local]
        void MeetingEnd(MeetingPreEndEvent ev)
        {
            if (AmnesiaAction.GetValue() != 1) return;
            if (reportedDead == null) return;

            var dead = reportedDead.Unbox();
            if (dead == null) return;

            var targetRole = dead.Role.Role;
            int[] targetArgs = [];

            dead.CoGetRoleArgument(args => targetArgs = args);

            using (RPCRouter.CreateSection("AmnesiaCopy"))
            {
                MyPlayer.Unbox().RpcInvokerSetRole(targetRole, targetArgs).InvokeSingle();
            }

            reportedDead = null;
        }
    }
}