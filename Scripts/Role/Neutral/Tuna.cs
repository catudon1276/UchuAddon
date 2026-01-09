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

public class TunaU : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Team MyTeam = new Team("teams.tunaU", new Virial.Color(171, 245, 255), TeamRevealType.OnlyMe);
    private TunaU() : base("tunaU", new Virial.Color(171, 245, 255), RoleCategory.NeutralRole, MyTeam, [TunaAction,UseallyStopTime,TotalStopTime,MeetingTotalTimeReset,MeetingStopTime,VentOption,
         new GroupConfiguration("options.role.tuna.group.gauge",[RequiredGaugeToWin, StopGaugeRatio], GroupConfigurationColor.ToDarkenColor(MyTeam.UnityColor))])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation =>Nebula.Roles.Citations.SuperNewRoles;

    static private ValueConfiguration<int> TunaAction = NebulaAPI.Configurations.Configuration("options.role.tunaU.tunaAction", ["options.role.tunaU.tunaAction.usually", "options.role.tunaU.tunaAction.total"], 0);
    static private FloatConfiguration UseallyStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.UseallystopTime", (1f, 10f, 1f), 3f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 0);
    static private FloatConfiguration TotalStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.TotalstopTime", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second, static () => TunaAction.GetValue() == 1);
    static private BoolConfiguration MeetingTotalTimeReset = NebulaAPI.Configurations.Configuration("options.role.tunaU.meetingTotalTimeReset", false, static () => TunaAction.GetValue() == 1);
    static private FloatConfiguration MeetingStopTime = NebulaAPI.Configurations.Configuration("options.role.tunaU.MeetingstopTime", (1f, 10f, 1f), 10f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration VentOption = NebulaAPI.Configurations.Configuration("options.role.tunaU.canVent", false);
    static private FloatConfiguration RequiredGaugeToWin = NebulaAPI.Configurations.Configuration("options.role.tunaU.requiredGaugeToWin", (30f, 200f, 5f), 50f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration StopGaugeRatio = NebulaAPI.Configurations.Configuration("options.role.tunaU.stopGaugeRatio", (1f, 10f, 0.25f), 1.25f, FloatConfigurationDecorator.Ratio);

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Tuna.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public TunaU MyRole = new TunaU();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        private UnityEngine.Vector2 TunaPosition;
        float TunaTimerUse = UseallyStopTime;
        float TunaTimerTotal = TotalStopTime;
        bool TunaCount = false;
        float StartTimer = 10f;
        bool TimerBreak = true;
        TranslatableTag tunaDead = new("state.tuna.dead");
        static TunaGaugeAbility TunaGauge = null!; //ゲージ自体
        bool IsTunaStopped = false;
        private float StopBonus = (StopGaugeRatio - 1.0000000000f) / 60.0000000000f;
        private float TunaGaugeValue = 0f; //ゲージの値
        static private float TunaGaugeMax => RequiredGaugeToWin;
        private bool TunaGaugeActive { get; set; } = false;
        public static MultiImage GaugeIcons = NebulaAPI.AddonAsset.GetResource("TunaGaugeIcon.png")!.AsMultiImage(2, 1, 160f)!;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                TunaGauge = new TunaGaugeAbility(TunaGaugeMax,340f, GaugeIcons.AsLoader(0), GaugeIcons.AsLoader(1), () => TunaGaugeValue, () => TunaGaugeActive).Register(this);
                TunaGauge.SetActive(true);
            }
        }
        void GameStart(GameStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
        }

        void MeetingStart(MeetingStartEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            if (MeetingTotalTimeReset)
            {
                float TunaTimerTotal = TotalStopTime;
            }
        }

        void MeetingEnd(MeetingEndEvent ev)
        {
            TimerBreak = true;
            TunaCount = false;
            StartTimer = MeetingStopTime;
            if (MeetingTotalTimeReset)
            {
                float TunaTimerTotal = TotalStopTime;
            }
        }

        [Local]
        void PlayerRevive(PlayerReviveEvent ev)
        {
            float TunaTimerUse = UseallyStopTime;
            float TunaTimerTotal = TotalStopTime;
        }
        
        void GameUpdate(GameUpdateEvent ev)//自殺タイマーの管理
        {
            if (TimerBreak)
            {
                StartTimer -= Time.deltaTime;
                if (StartTimer <= 0f)
                {
                    TunaCount = true;
                    TimerBreak = false;
                }
            }
            //自殺タイマー
            /*if (hudText != null)
            {
                string label = NebulaAPI.Language.Translate("role.tunaU.stopTimeText");
                string special = NebulaAPI.Language.Translate("role.tunaU.stopTimeTextSpecial");
                if (!TunaCount)
                {
                    hudText.text = $"{label}: {special}";
                    return;
                }
                if (TunaAction.GetValue() == 1)
                {
                    if (TunaTimerTotal > 1)
                    {
                        hudText.text = $"{label}: {TunaTimerTotal:0.0}";
                    }
                    else
                    {
                        hudText.text = $"<color=#FF0000>{label}: {TunaTimerTotal:0.0}";
                    }
                }
                else
                {
                    if (TunaTimerUse > 1)
                    {
                        hudText.text = $"{label}: {TunaTimerUse:0.0}";
                    }
                    else
                    {
                        hudText.text = $"<color=#FF0000>{label}: {TunaTimerUse:0.0}";
                    }
                }             
            }*/
            if (!TunaCount) return;
            if (MyPlayer == null || MyPlayer.IsDead || !MyPlayer.CanMove || MeetingHud.Instance) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaPosition == default)
                    TunaPosition = MyPlayer.TruePosition;

                if (TunaPosition == MyPlayer.TruePosition)
                {
                    IsTunaStopped = true;
                    TunaTimerTotal -= Time.deltaTime;
                    if (TunaTimerTotal <= 0f)
                    {
                        MyPlayer.Suicide(PlayerState.Suicide, tunaDead, KillParameter.NormalKill, null);
                        return;
                    }
                }
                else
                {
                    IsTunaStopped = false;
                    TunaPosition = MyPlayer.TruePosition;
                }
                return;
            }

            if (TunaAction.GetValue() != 0) return;

            if (TunaPosition == default)
                TunaPosition = MyPlayer.TruePosition;

            if (TunaPosition == MyPlayer.TruePosition)
            {
                IsTunaStopped = true;
                TunaTimerUse -= Time.deltaTime;
                if (TunaTimerUse <= 0f)
                {
                    MyPlayer.Suicide(PlayerState.Suicide, tunaDead, KillParameter.NormalKill, null);
                    return;
                }
            }
            else
            {
                IsTunaStopped = false;
                TunaTimerUse = UseallyStopTime;
                TunaPosition = MyPlayer.TruePosition;
            }
        }

        float shareInterval = 0.5f;
        float progressGrace = 0f;
        [Local]
        void GaugeUpdate(GameUpdateEvent ev)
        {
            if (TunaGaugeValue >= TunaGaugeMax)
            {
                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.TunaTeamWin, 1 << MyPlayer.PlayerId);
                return;
            }
            if (MyPlayer.IsDead)
            {
                TunaGauge.SetActive(false);
                return;
            }
            TunaGauge.SetActive(true);
            TunaGaugeActive = false;

            if (MeetingHud.Instance || ExileController.Instance)
            {
                if (!(progressGrace > 0f))
                {
                    RpcShareTunaGauge.Invoke((MyPlayer, TunaGaugeValue));
                    progressGrace = 5f;
                    shareInterval = 0.5f;
                }
                return;
            }

            if (progressGrace > 0f)
            {
                progressGrace -= Time.deltaTime;
                return;
            }
            if (Player.AllPlayers.Any(p => p != MyPlayer && !p.IsDead && p.Position.Distance(MyPlayer.Position) < 2))
            {
                if (!IsTunaStopped)
                {
                    TunaGaugeValue = Math.Clamp(TunaGaugeValue + Time.deltaTime, 0f, TunaGaugeMax);
                }
                else
                {
                    TunaGaugeValue = Math.Clamp(TunaGaugeValue + Time.deltaTime + StopBonus,0f, TunaGaugeMax);
                }
                TunaGaugeActive = true;
            }
            else
            {
                TunaGaugeActive = false;
            }

        }
        static private RemoteProcess<(GamePlayer tuna, float gauge)> RpcShareTunaGauge = new("ShareTunaGauge", (message, _) =>
        {
            if (message.tuna.Role is Instance tunaInstance) tunaInstance.TunaGaugeValue = message.gauge;
        }, false);

        [Local,OnlyMyPlayer]
        void Name(PlayerDecorateNameEvent ev)
        {
            if (MyPlayer.IsDead)return;
            string label = NebulaAPI.Language.Translate("role.tunaU.stopTimeText");
            string special = NebulaAPI.Language.Translate("role.tunaU.stopTimeTextSpecial");
            if (!TunaCount)
            {
                ev.Name += $" ({special})".Color(Color.yellow);
                return;
            }
            if (!TunaCount) return;
            if (TunaAction.GetValue() == 1)
            {
                if (TunaTimerTotal > 1)
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Color.yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerTotal:0.0})".Color(Color.red);
                }
            }
            else
            {
                if (TunaTimerUse > 1)
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Color.yellow);
                }
                else
                {
                    ev.Name += $" ({TunaTimerUse:0.0})".Color(Color.red);
                }
            }

        }
        bool RuntimeRole.CanUseVent => VentOption;
        bool RuntimeRole.CanMoveInVent => VentOption;
    }
}