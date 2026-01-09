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

[AddonDocument("role.EraserU")]
public class EraserUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("EraserEraseButton.png", "role.EraserU.ability.erase");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.evildecoratorU")]
public class DecoratorDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("DecorationButton.png", "role.evildecoratorU.ability.decoration");
    }

    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.hawkU")]
public class HawkUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override string BuildAbilityText(string original)
    {
        var hawkmove = RoleDocumentHelper.ConfigBool("options.role.hawkU.hawkeyeStop", "role.hawkU.ability.left", "role.hawkU.ability.noleft");
        return original.Replace("#MOVE", hawkmove);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("HawkButton.png", "role.hawkU.ability.hawkeye");
    }

    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.eclipseU")]
public class EclipseUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override string BuildAbilityText(string original)
    {
        var eclipseMeeting = RoleDocumentHelper.ConfigBool("options.role.eclipseU.markMeetingReset", "role.eclipseU.meeting.left", "role.eclipseU.meeting.noleft");
        var eclipseSE = RoleDocumentHelper.ConfigBool("options.role.eclipseU.markPlaySE", "role.eclipseU.se.left", "role.eclipseU.se.noleft");
        return original.Replace("#MEET", eclipseMeeting).Replace("#SE", eclipseSE);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("MeteorMark.png", "role.eclipseU.ability.mark");
        yield return RoleDocumentHelper.GetImageLocalizedContent("MeteorButton.png", "role.eclipseU.ability.meteor");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.rocketU")]
public class rocketUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override string BuildAbilityText(string original)
    {
        var rocketKill = RoleDocumentHelper.ConfigBool("options.role.rocketU.canUseNormalKill", "role.rocketU.ability.canKill", "role.rocketU.ability.cantKill");
        return original.Replace("#KILL", rocketKill);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("HoldButton.png", "role.rocketU.ability.hold");
        yield return RoleDocumentHelper.GetImageLocalizedContent("RocketLaunchButton.png", "role.rocketU.ability.launch");
    }

    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.xinobiU")]
public class xinobiUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("XinobiButton.png", "role.xinobiU.ability.possess");
        yield return RoleDocumentHelper.GetImageLocalizedContent("XinobiVentButton.png", "role.xinobiU.ability.vent");
    }

    public override RoleType RoleType => RoleType.Role;
}