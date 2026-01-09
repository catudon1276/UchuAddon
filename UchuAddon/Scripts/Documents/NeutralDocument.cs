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

[AddonDocument("role.loversbreakerU")]
public class LoversBreakerUUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("ExpButton.png", "role.loversbreakerU.ability.Exp");
    }
    public override RoleType RoleType => RoleType.Role;
}
[AddonDocument("role.seedU")]
public class seedUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override string BuildAbilityText(string original)
    {
        var seedGauge = RoleDocumentHelper.ConfigBool("options.role.seedU.seedGauge", "role.seedU.ability.left", "role.seedU.ability.noleft");
        return original.Replace("#GAUGE", seedGauge);
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("SeedButton.png", "role.seedU.ability.seedSkill");
    }
    public override RoleType RoleType => RoleType.Role;
}

[AddonDocument("role.tunaU")]
public class tunaUDocument : AbstractAssignableDocument
{
    public override GUIWidget? GetTipsWidget()
    {
        return RoleDocumentHelper.GetDocumentLocalizedText(DocumentId + ".tips");
    }
    public override IEnumerable<GUIWidget> GetAbilityWidget()
    {
        yield return RoleDocumentHelper.GetImageLocalizedContent("TunaGaugeDocuments.png", "role.tunaU.ability.tunaGauge");
    }
    public override RoleType RoleType => RoleType.Role;
}