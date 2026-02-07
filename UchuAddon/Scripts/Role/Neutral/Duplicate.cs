using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
using static Nebula.Roles.Modifier.Bloody;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Modifier;

public class DuplicateU : DefinedAllocatableModifierTemplate,DefinedAllocatableModifier, HasCitation
{
    private DuplicateU() : base("DuplicateU", "DUP", new(0, 128, 128), [VoteCount])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private IntegerConfiguration VoteCount = NebulaAPI.Configurations.Configuration("options.role.DuplicateU.VoteCount", (1, 9), 1);
    static public DuplicateU MyRole = new DuplicateU();
    /*static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Duplicate.png")!.AsImage(100f)!;*/
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;
    /*Image? DefinedAssignable.IconImage => IconImage;*/
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);



    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
        }
        [Local]
        void OnCastVoteLocal(PlayerVoteCastLocalEvent ev)
        {
            if (AmOwner)
            {
                ev.Vote = VoteCount + 1;
            }
        }
    }
}