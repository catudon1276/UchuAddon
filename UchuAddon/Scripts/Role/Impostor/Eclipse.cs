using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
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
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

//オブジェクトを専用のものにするのは後で
[NebulaRPCHolder]
public class EclipseU : DefinedSingleAbilityRoleTemplate<EclipseU.Ability>, DefinedRole
{
    private EclipseU() : base("eclipseU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [UseOfMeteor, MeteorMarkCooldown, MeteorBlastCooldown, MeteorWarningTime, MarkStopTime, MeteorSize, ResetBlastCool, MarkMeetingReset, MarkPlaySE, SERange])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Eclipse.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static private readonly IntegerConfiguration UseOfMeteor = NebulaAPI.Configurations.Configuration("options.role.eclipseU.useOfMeteor", (1, 99), 3);
    static private readonly FloatConfiguration MeteorMarkCooldown = NebulaAPI.Configurations.Configuration("options.role.eclipseU.meteorMarkCoolDown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration MeteorBlastCooldown = NebulaAPI.Configurations.Configuration("options.role.eclipseU.meteorBlastCoolDown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration MeteorWarningTime = NebulaAPI.Configurations.Configuration("options.role.eclipseU.meteorWarningTime", (0f, 3f, 0.25f), 1f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration MarkStopTime = NebulaAPI.Configurations.Configuration("options.role.eclipseU.markStopTime", (0.5f, 5f, 0.5f), 1f, FloatConfigurationDecorator.Second);
    static private readonly FloatConfiguration MeteorSize = NebulaAPI.Configurations.Configuration("options.role.eclipseU.meteorSize", (1f, 15f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private readonly BoolConfiguration ResetBlastCool = NebulaAPI.Configurations.Configuration("options.role.eclipseU.resetBlastCool", false);
    static private readonly BoolConfiguration MarkMeetingReset = NebulaAPI.Configurations.Configuration("options.role.eclipseU.markMeetingReset", true);
    static private readonly BoolConfiguration MarkPlaySE = NebulaAPI.Configurations.Configuration("options.role.eclipseU.markPlaySE", true);
    static private readonly FloatConfiguration SERange = NebulaAPI.Configurations.Configuration("options.role.eclipseU.seRange", (2.5f, 20f, 2.5f), 10f, FloatConfigurationDecorator.Ratio, () => MarkPlaySE);

    static private readonly Image MeteorMarkImage = NebulaAPI.AddonAsset.GetResource("MeteorMark.png")!.AsImage(115f)!;
    static private readonly Image MeteorBlastImage = NebulaAPI.AddonAsset.GetResource("MeteorButton.png")!.AsImage(115f)!;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Eclipse.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public EclipseU MyRole = new EclipseU();

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class EclipseMark : NebulaSyncStandardObject, IGameOperator
    {
        public const string MyTag = "EclipseMark";
        private static MultiImage markSprite = NebulaAPI.AddonAsset.GetResource("MeteorAntenna.png")!.AsMultiImage(4, 1, 200f)!;
        float indexT = 0f;
        int index = 0;
        public EclipseMark(Vector2 pos) : base(pos, ZOption.Back, true, markSprite.GetSprite(0))
        {
        }

        static EclipseMark()
        {
            RegisterInstantiater(MyTag, (args) => new EclipseMark(new Vector2(args[0], args[1])));
        }
        void OnUpdate(GameUpdateEvent ev)
        {
            indexT -= Time.deltaTime;
            if (indexT < 0f)
            {
                indexT = 0.21f;
                Sprite = markSprite.GetSprite(index % 4);
                index++;
            }
        }
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        NebulaSyncStandardObject? mark;
        int leftMeteor;


        //爆発
        private static RemoteProcess<(Vector2 pos, GamePlayer invoker, float size)> RpcBlastMeteor =
            new("BlastMeteor", (message, _) =>
            {
                void StartCoroutine()
                {
                    NebulaManager.Instance.StartCoroutine(CoBlastMeteor(message).WrapToIl2Cpp());
                }
                StartCoroutine();
            });

        private static IEnumerator CoBlastMeteor((Vector2 pos, GamePlayer invoker, float size) message)
        {
            float warningTime = (float)MeteorWarningTime;
            float meteorSize = message.size;
            NebulaAsset.PlaySE(NebulaAudioClip.MeteorAlert, true);

            var warningCircle = EffectCircle.SpawnEffectCircle(null, message.pos.AsVector3(-9f), Color.red.AlphaMultiplied(0.5f), message.size, null, true);
            yield return Effects.Wait(warningTime);
            warningCircle.Disappear();

            IDividedSpriteLoader ExplosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 100f, 4, 2);
            IDividedSpriteLoader MeteorSprite = DividedSpriteLoader.FromResource("Nebula.Resources.Meteor.png", 100f, 2, 1).SetPivot(new(0f, 0f));

            var explosion = UnityHelper.CreateObject<SpriteRenderer>("Explosion", null, message.pos.AsVector3(-10f));
            for (int i = 0; i < 2; i++)
            {
                explosion.sprite = MeteorSprite.GetSprite(i);
                yield return Effects.Wait(0.05f);
            }
            NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear, message.pos, 20f, 20f);
            foreach (var player in NebulaGameManager.Instance.AllPlayerInfo)
            {
                if (player.IsDead) continue;
                if (player.Position.Distance(message.pos) <= message.size)
                {
                    message.invoker.MurderPlayer(
                        player,
                        EclipseTags.Bomb,
                        EclipseTags.Bomb,
                        KillParameter.WithAssigningGhostRole | KillParameter.WithDeadBody,
                        KillCondition.TargetAlive,
                        result => { }
                    );
                }
            }


            for (int i = 0; i < 8; i++)
            {
                explosion.sprite = ExplosionSprite.GetSprite(i);
                yield return Effects.Wait(0.12f);
            }

            GameObject.Destroy(explosion.gameObject);
        }

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                leftMeteor = UseOfMeteor;
                mark = null!;
                var markButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, MeteorMarkCooldown, "Emark", MeteorMarkImage).SetAsUsurpableButton(this);
                ModAbilityButton blastButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, null, MeteorBlastCooldown, "Eblast", MeteorBlastImage, (ModAbilityButton _) => mark != null && leftMeteor > 0, (b) => !MyPlayer.IsDead, false);
                markButton.Visibility = b => !MyPlayer.IsDead;
                markButton.Availability = b => MyPlayer.CanMove;
                markButton.OnClick = m =>
                {
                    if (mark != null) NebulaSyncStandardObject.LocalDestroy(mark.ObjectId);

                    mark = (NebulaSyncObject.LocalInstantiate("EclipseMark", new float[]
                    {
                    PlayerControl.LocalPlayer.transform.localPosition.x,
                    PlayerControl.LocalPlayer.transform.localPosition.y
                    }).SyncObject as NebulaSyncStandardObject)!;

                    float markStopDuration = (float)MarkStopTime;
                    MyPlayer.GainSpeedAttribute(0f, markStopDuration, false, 10, "eclipseU::stop");
                    if (MarkPlaySE)
                    {
                        NebulaAsset.RpcPlaySE.Invoke((NebulaAudioClip.Destroyer1, MyPlayer.Position, 0.6f, SERange));
                    }
                    else if(!MarkPlaySE)
                    {
                        NebulaAsset.PlaySE(NebulaAudioClip.Destroyer1, MyPlayer.Position, 0.6f, SERange, 1f);
                    }
                        m.StartCoolDown();

                    if (ResetBlastCool)
                    {
                        blastButton.StartCoolDown();
                    }

                };

                markButton.SetLabelType(ModAbilityButton.LabelType.Utility);

                blastButton.ShowUsesIcon(0,leftMeteor.ToString());
                blastButton.OnClick = b =>
                {
                    if (mark == null) return;
                    if (leftMeteor <= 0) return;

                    Vector2 targetPos = mark.Position;
                    GamePlayer invoker = MyPlayer;

                    RpcBlastMeteor.Invoke((targetPos, invoker, MeteorSize));
                    b.StartCoolDown();
                    markButton.StartCoolDown();
                    leftMeteor--;

                    NebulaSyncStandardObject.LocalDestroy(mark.ObjectId);
                    mark = null;
                    blastButton.UpdateUsesIcon(leftMeteor.ToString());
                };
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!AmOwner) return;

            // MarkMeetingReset が ON の場合、既存のマークを破棄
            if (MarkMeetingReset)
            {
                if (mark != null)
                {
                    NebulaSyncStandardObject.LocalDestroy(mark.ObjectId);
                    mark = null;
                }
            }
        }
    }
}

public static class EclipseTags
{
    public static TranslatableTag Bomb = new("statistics.Bomb");
}

