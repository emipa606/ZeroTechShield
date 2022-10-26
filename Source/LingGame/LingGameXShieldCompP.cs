using Verse;

namespace LingGame;

public class LingGameXShieldCompP : CompProperties
{
    public HediffDef BreakHediff;

    public bool CanXinLingChongNeng = false;

    public float ChongShengNengLiang = 1f;

    public float MeleeXishouPerHaoNeng = 1f;

    public bool NeedPaoPao = true;

    public string PaoPaoTex = "Other/ShieldBubble";

    public bool RandSkip = false;

    public float RangeXiShouPerHaoNeng = 1f;

    public int RestTime = 30;

    public float ShanBiLv = 0f;
    public float YiCiHaoNengShangXian = float.MaxValue;

    public LingGameXShieldCompP()
    {
        compClass = typeof(LingGameXShieldComp);
    }
}