using Nebula.Modules;
using Nebula.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Virial.Attributes;
using Virial.Compat;
using Virial.Media;
using Virial.Runtime;

namespace Hori.Documents;

[AddonDocument("role.bakeryU", RoleType.Role, (string[])[], false, true)]
public class StandardDocument : IDocumentWithId
{
    string documentId;
    bool withTips;
    bool withWinCond;
    string[][] abilityContents;
    RoleType roleType;
    public StandardDocument(RoleType roleType, string[] abilityContents, bool withWinCond, bool withTips)
    {
        this.roleType = roleType;
        this.withTips = withTips;
        this.withWinCond = withWinCond;
        this.abilityContents = abilityContents.Select(str => str.Split()).ToArray();
    }

    void IDocumentWithId.OnSetId(string documentId) { 
        this.documentId = documentId;
    }

    Virial.Media.GUIWidget? IDocument.Build(Artifact<GUIScreen>? target)
    {
        var gui = NebulaAPI.GUI;
        return
            RoleDocumentHelper.GetAssignableWidget(roleType, documentId.Split('.', 2).Last(),
            withWinCond ? RoleDocumentHelper.GetWinCondChapter(documentId) : null,
            abilityContents.Length > 0 ? RoleDocumentHelper.GetChapter($"{documentId}.ability", [
                RoleDocumentHelper.GetDocumentLocalizedText($"{documentId}.ability.main"),
                ..abilityContents.Select(c => RoleDocumentHelper.GetImageLocalizedContent(c[0], c[1])),
                ]) : null,
            withTips ? RoleDocumentHelper.GetTipsChapter(documentId) : null,
            RoleDocumentHelper.GetConfigurationCaption()
            );
    }
}

public abstract class AbstractAssignableDocument : IDocumentWithId
{
    public string DocumentId { get; private set; }
    void IDocumentWithId.OnSetId(string documentId) => DocumentId = documentId;

    public virtual bool WithWinCond => false;
    public virtual GUIWidget GetCustomWinCondWidget() => RoleDocumentHelper.GetWinCondChapter(DocumentId);
    public virtual GUIWidget? GetTipsWidget() => null;
    public virtual IEnumerable<GUIWidget> GetAbilityWidget() { yield break; }
    public abstract RoleType RoleType { get; }
    public virtual string BuildAbilityText(string original) => original;
    Virial.Media.GUIWidget? IDocument.Build(Artifact<GUIScreen>? target)
    {
        var tipsWidget = GetTipsWidget();
        var abilityWidget = GetAbilityWidget().ToArray();
        var gui = NebulaAPI.GUI;
        return
            RoleDocumentHelper.GetAssignableWidget(RoleType, DocumentId.Split('.', 2).Last(),
            WithWinCond ? GetCustomWinCondWidget() : null,
            abilityWidget.Length > 0 ? RoleDocumentHelper.GetChapter($"{DocumentId}.ability", [
                RoleDocumentHelper.GetDocumentLocalizedText($"{DocumentId}.ability.main", BuildAbilityText),
                ..abilityWidget,
                ]) : null,
            tipsWidget != null ? RoleDocumentHelper.GetChapter("document.tips", [tipsWidget]) : null,
            RoleDocumentHelper.GetConfigurationCaption()
            );
    }
}

[NebulaPreprocess(Virial.Attributes.PreprocessPhase.FixStructure)]
public class DocumentLoader
{
    public static void Preprocess(NebulaPreprocessor preprocess)
    {
        foreach(var r in Nebula.Roles.Roles.AllRoles)
        {
            if (DocumentManager.GetDocument("role." + r.InternalName) == null)
            {
                var doc = new StandardDocument(RoleType.Role, [], false, false);
                (doc as IDocumentWithId).OnSetId("role." + r.InternalName);
                DocumentManager.Register("role." + r.InternalName, doc);
            }
        }
        foreach (var r in Nebula.Roles.Roles.AllModifiers)
        {
            if (DocumentManager.GetDocument("role." + r.InternalName) == null)
            {
                var doc = new StandardDocument(RoleType.Modifier, [], false, false);
                (doc as IDocumentWithId).OnSetId("role." + r.InternalName);
                DocumentManager.Register("role." + r.InternalName, doc);
            }
        }
        foreach (var r in Nebula.Roles.Roles.AllGhostRoles)
        {
            if (DocumentManager.GetDocument("role." + r.InternalName) == null)
            {
                var doc = new StandardDocument(RoleType.GhostRole, [], false, false);
                (doc as IDocumentWithId).OnSetId("role." + r.InternalName);
                DocumentManager.Register("role." + r.InternalName, doc);
            }
        }
    }
}