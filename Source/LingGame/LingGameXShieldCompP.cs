using Verse;

namespace LingGame;

public class LingGameXShieldCompP : CompProperties
{
    public readonly float ChongShengNengLiang = 1f;

    public readonly float MeleeXishouPerHaoNeng = 1f;

    public readonly bool NeedPaoPao = true;

    public readonly string PaoPaoTex = "Other/ShieldBubble";

    public readonly bool RandSkip = false;

    public readonly float RangeXiShouPerHaoNeng = 1f;

    public readonly int RestTime = 30;
    public readonly float YiCiHaoNengShangXian = float.MaxValue;
    public HediffDef BreakHediff;

    public bool CanXinLingChongNeng = false;

    public float ShanBiLv = 0f;

    public LingGameXShieldCompP()
    {
        compClass = typeof(LingGameXShieldComp);
    }
}