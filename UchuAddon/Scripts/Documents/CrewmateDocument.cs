using Nebula;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;

namespace Hori.Documents;

[AddonDocument("role.admiralU")]
public class AdmiralUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }

    public override string BuildAbilityText(string original)
    {
        var watch = RoleDocumentHelper.ConfigBool("options.role.admiralU.publicName", "role.admiralU.ability.left", "role.admiralU.ability.noleft");
        return original.Replace("#NAME", watch);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("AdmiralSkillButton.png", "role.admiralU.ability.admiralSkill");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.NicehawkU")]
public class NiceHawkUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override string BuildAbilityText(string original)
    {
        var watch = RoleDocumentHelper.ConfigBool("options.role.hawkU.hawkeyeStop", "role.NicehawkU.ability.left", "role.NicehawkU.ability.noleft");
        return original.Replace("#MOVE", watch);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("HawkButtonNice.png", "role.NicehawkU.ability.hawkeye");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.nicedecoratorU")]
public class NiceDecoratorUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("DecorationButtonNice.png", "role.NiceDecoratorU.ability.NiceDecorate");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.polarisU")]
public class PolarisUDocument : AbstractAssignableDocument
{
    public override string BuildAbilityText(string original)
    {
        var Rewind = RoleDocumentHelper.ConfigBool("options.role.polarisU.rewindTask", "role.polarisU.ability.left", "role.polarisU.ability.noleft");
        return original.Replace("#REWIND", Rewind);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("KillButtonCyan.png", "role.polarisU.ability.kill");
        yield return RoleDocumentHelper.GetImageLocalizedContent("PolarisMediumButton.png", "role.polarisU.ability.medium");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.slimeU")]
public class SlimeUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("SlimeJumboButton.png", "role.slimeU.ability.jumbo");
        yield return RoleDocumentHelper.GetImageLocalizedContent("SlimeMiniButton.png", "role.slimeU.ability.mini");
    }
    public override RoleType RoleType => RoleType.Role;
}