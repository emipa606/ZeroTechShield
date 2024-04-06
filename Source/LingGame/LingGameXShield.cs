using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LingGame;

public class LingGameXShield : Apparel
{
    private static readonly SoundDef energyShield_Broken = SoundDef.Named("EnergyShield_Broken");
    private Vector3 impactAngleVect;

    private int lastAbsorbDamageTick = -1;

    private int lastKeepDisplayTick = -9999;
    private float power;

    private int Sleeptick = -1;

    private int StartingTicksToReset => CompData.RestTime * 60;

    protected LingGameXShieldCompP CompData
    {
        get
        {
            var result = new LingGameXShieldCompP();
            if (GetComp<LingGameXShieldComp>() != null)
            {
                result = (LingGameXShieldCompP)GetComp<LingGameXShieldComp>().props;
            }

            return result;
        }
    }

    public virtual float MaxPower => this.GetStatValue(StatDefOf.EnergyShieldEnergyMax);

    public virtual float RePowerRate => this.GetStatValue(StatDefOf.EnergyShieldRechargeRate) / 60f;

    public float Power => power;

    public ShieldState shieldState => Sleeptick > 0 ? ShieldState.Resetting : ShieldState.Active;

    protected bool ShouldDisplay
    {
        get
        {
            if (!Wearer.Spawned || Wearer.Downed || Wearer.Dead)
            {
                return false;
            }

            if (!CompData.NeedPaoPao)
            {
                return false;
            }

            if (Wearer.InAggroMentalState)
            {
                return true;
            }

            if (Wearer.Drafted)
            {
                return true;
            }

            if (Wearer.Faction.HostileTo(Faction.OfPlayer) && !Wearer.IsPrisoner)
            {
                return true;
            }

            return Find.TickManager.TicksGame < lastKeepDisplayTick + 300 || Wearer.IsFighting();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref power, "power");
        Scribe_Values.Look(ref Sleeptick, "Sleeptick", -1);
        Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick");
        Scribe_Values.Look(ref lastAbsorbDamageTick, "lastAbsorbDamageTick");
    }

    public override IEnumerable<Gizmo> GetWornGizmos()
    {
        if (Find.Selector.SingleSelectedThing != Wearer)
        {
            yield break;
        }

        yield return new Gizmo_LingGameX_ShieldStatus
        {
            shield = this
        };
        yield return new Command_Action
        {
            defaultLabel = "PsyToPowerL".Translate(),
            defaultDesc = "PsyToPowerD".Translate(),
            icon = ContentFinder<Texture2D>.Get("LingUI/PsyToPower"),
            action = delegate
            {
                if (shieldState == ShieldState.Resetting)
                {
                    Wearer.psychicEntropy.TryAddEntropy(20f, null, false, true);
                    Reset();
                }
                else
                {
                    Wearer.psychicEntropy.TryAddEntropy(10f, null, false, true);
                    power += Math.Max(1f, MaxPower / 10f);
                }
            }
        };
    }

    public override void Tick()
    {
        base.Tick();
        if (Wearer == null)
        {
            power = 9999f;
        }
        else if (shieldState == ShieldState.Resetting)
        {
            Sleeptick--;
            if (Sleeptick <= 0)
            {
                Reset();
            }
        }
        else if (shieldState == ShieldState.Active)
        {
            power += RePowerRate;
            if (power >= MaxPower)
            {
                power = MaxPower;
            }
        }
    }

    public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
    {
        if (Wearer.Downed)
        {
            return false;
        }

        if (shieldState == ShieldState.Resetting)
        {
            return false;
        }

        if (!dinfo.Def.harmsHealth)
        {
            return true;
        }

        if (dinfo.Instigator == Wearer)
        {
            AbsorbedDamage(dinfo);
            return true;
        }

        if (dinfo.Def == DamageDefOf.SurgicalCut)
        {
            return false;
        }

        AbsorbedDamage(dinfo);
        var num = Mathf.Min(dinfo.Amount, CompData.YiCiHaoNengShangXian);
        num = !Wearer.Position.InHorDistOf(dinfo.Instigator.Position, 2f)
            ? num * CompData.RangeXiShouPerHaoNeng
            : num * CompData.MeleeXishouPerHaoNeng;
        power -= num;
        if (Wearer.Spawned && CompData.RandSkip)
        {
            var intVec = !Rand.Chance(0.5f) || dinfo.Instigator == null
                ? Wearer.Position.RandomAdjacentCell8Way().RandomAdjacentCell8Way().RandomAdjacentCell8Way()
                    .RandomAdjacentCell8Way()
                : new IntVec3((dinfo.Instigator.Position.x + Wearer.Position.x) / 2, 0,
                    (dinfo.Instigator.Position.z + Wearer.Position.z) / 2);
            if (!intVec.InBounds(Wearer.Map))
            {
                if (intVec.x < 0)
                {
                    intVec.x = 0;
                }
                else if (intVec.x > Math.Sqrt(Wearer.Map.Area))
                {
                    intVec.x = (int)Math.Sqrt(Wearer.Map.Area);
                }

                if (intVec.z < 0)
                {
                    intVec.z = 0;
                }
                else if (intVec.z > Math.Sqrt(Wearer.Map.Area))
                {
                    intVec.z = (int)Math.Sqrt(Wearer.Map.Area);
                }
            }

            Wearer.Position = intVec;
            Wearer.Notify_Teleported(false);
        }

        if (power <= 0f)
        {
            Break();
        }

        return true;
    }

    protected void AbsorbedDamage(DamageInfo dinfo)
    {
        if (Wearer.Map != null)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            var vector = Wearer.TrueCenter() + (impactAngleVect.RotatedBy(180f) * 0.5f);
            var num = Mathf.Min(10f, 2f + (dinfo.Amount / 10f));
            FleckMaker.Static(vector, Wearer.Map, FleckDefOf.ExplosionFlash, num);
            var num2 = (int)num;
            for (var i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(vector, Wearer.Map, Rand.Range(0.8f, 1.2f));
            }
        }

        lastAbsorbDamageTick = Find.TickManager.TicksGame;
    }

    public virtual void Break()
    {
        if (Wearer.Map != null)
        {
            energyShield_Broken.PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
            FleckMaker.Static(Wearer.TrueCenter(), Wearer.Map, FleckDefOf.ExplosionFlash, 12f);
            for (var i = 0; i < 6; i++)
            {
                var vector = Wearer.TrueCenter() + (Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) *
                                                    Rand.Range(0.3f, 0.6f));
                FleckMaker.ThrowDustPuff(vector, Wearer.Map, Rand.Range(0.8f, 1.2f));
            }

            if (CompData.BreakHediff != null)
            {
                Wearer.health.AddHediff(CompData.BreakHediff);
            }
        }

        power = 0f;
        Sleeptick = StartingTicksToReset;
    }

    public void Reset()
    {
        if (Wearer.Spawned)
        {
            SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
            FleckMaker.ThrowLightningGlow(Wearer.TrueCenter(), Wearer.Map, 3f);
        }

        Sleeptick = -1;
        power = Math.Max(CompData.ChongShengNengLiang, MaxPower / 5f);
    }

    public override void DrawWornExtras()
    {
        if (shieldState != ShieldState.Active || !ShouldDisplay)
        {
            return;
        }

        var num = Mathf.Lerp(1.5f, 1.7f, power);
        var drawPos = Wearer.Drawer.DrawPos;
        drawPos.y = AltitudeLayer.PawnUnused.AltitudeFor();
        var num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
        if (num2 < 8)
        {
            var num3 = (8 - num2) / 8f * 0.05f;
            drawPos += impactAngleVect * num3;
            num -= num3;
        }

        float angle = Rand.Range(0, 180);
        var s = new Vector3(num, 1f, num);
        var matrix = default(Matrix4x4);
        matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
        Graphics.DrawMesh(MeshPool.plane10, matrix,
            MaterialPool.MatFrom(CompData.PaoPaoTex, ShaderDatabase.Transparent), 0);
    }
}