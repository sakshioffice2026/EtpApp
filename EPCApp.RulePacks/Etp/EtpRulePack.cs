using EPCApp.Database.Enums;
using EPCApp.Engine.Abstractions;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Electrical;
using EPCApp.Engine.Mechanical;
using EPCApp.Engine.Models;
using EPCApp.Engine.Sizing.Biology;
using EPCApp.Engine.Sizing.Chemical;
using EPCApp.Engine.Sizing.Clarifiers;
using EPCApp.Engine.Sizing.Preliminary;
using EPCApp.Engine.Sizing.Separators;
using EPCApp.Engine.Sizing.Sludge;
using EPCApp.Engine.Sizing.Tanks;
using EPCApp.Engine.Sizing.Tertiary;
using EPCApp.Engine.Sizing.Zld;

namespace EPCApp.RulePacks.Etp;

public sealed class EtpRulePack : IPlantRulePack
{
    private const double BodCodRatioMin = 0.3;
    private const double TdsMembraneTriggerMgL = 2000;
    private const double OilGreaseTrapTriggerMgL = 50;
    private const double DafTriggerMgL = 250;
    private const double PrimaryTssTriggerMgL = 500;
    private const double CoagulationTssTriggerMgL = 1000;
    private const double AnaerobicCodTriggerMgL = 4000;
    private const double PhLow = 6.0;
    private const double PhHigh = 9.0;

    public PlantType PlantType => PlantType.Etp;

    public TrainDefinition SelectTrain(StreamBasis basis)
    {
        var biodegradable = basis.CodMgL <= 0 || basis.BodCodRatio >= BodCodRatioMin;
        var membrane = basis.TdsMgL > TdsMembraneTriggerMgL || basis.ZldRequired;
        var needsCoagulation = basis.TssMgL >= CoagulationTssTriggerMgL
                               || !biodegradable
                               || basis.Industry == IndustryType.Textile;
        var needsAnaerobic = biodegradable && basis.CodMgL >= AnaerobicCodTriggerMgL;

        var operations = new List<UnitOperationType>
        {
            UnitOperationType.ScreenChamber
        };

        if (basis.OilGreaseMgL >= OilGreaseTrapTriggerMgL)
            operations.Add(UnitOperationType.OilGreaseTrap);

        if (basis.OilGreaseMgL >= DafTriggerMgL)
            operations.Add(UnitOperationType.DafApiSeparator);

        operations.Add(UnitOperationType.Equalization);

        if (basis.Ph > 0 && (basis.Ph < PhLow || basis.Ph > PhHigh))
            operations.Add(UnitOperationType.PhNeutralization);

        if (needsCoagulation)
            operations.Add(UnitOperationType.CoagulationFlocculation);

        if (needsCoagulation || basis.TssMgL >= PrimaryTssTriggerMgL)
            operations.Add(UnitOperationType.PrimaryClarifier);

        operations.Add(UnitOperationType.ChemicalDosing);

        if (needsAnaerobic)
            operations.Add(UnitOperationType.AnaerobicReactor);

        operations.Add(biodegradable ? UnitOperationType.Aeration : UnitOperationType.Mbbr);
        operations.Add(UnitOperationType.SecondaryClarifier);

        operations.Add(UnitOperationType.PressureSandFilter);
        operations.Add(UnitOperationType.ActivatedCarbonFilter);

        if (membrane)
        {
            operations.Add(UnitOperationType.UltraFiltration);
            operations.Add(UnitOperationType.ReverseOsmosis);
        }
        else
        {
            operations.Add(UnitOperationType.Disinfection);
        }

        if (basis.ZldRequired)
        {
            operations.Add(UnitOperationType.Softener);
            operations.Add(UnitOperationType.MultiEffectEvaporator);
            operations.Add(UnitOperationType.AtfdCrystallizer);
        }

        operations.Add(UnitOperationType.SludgeThickener);
        operations.Add(UnitOperationType.FilterPress);

        return new TrainDefinition
        {
            Name = basis.ZldRequired ? "ETP with ZLD" : "ETP",
            Operations = operations,
            IncludesZld = basis.ZldRequired
        };
    }

    public IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train, DesignBasis design, TrainPlan plan)
    {
        var equipment = new List<SizedEquipment>();

        plan.InletFlowM3PerDay = Math.Round(basis.FlowM3PerDay, 2);
        plan.TreatedWaterM3PerDay = Math.Round(basis.FlowM3PerDay, 2);

        var anaerobicRemoval = 0.0;
        var hasThickener = false;
        var hasFilterPress = false;

        foreach (var operation in train.Operations)
        {
            switch (operation)
            {
                case UnitOperationType.ScreenChamber:
                    equipment.Add(ScreenChamberSizer.Size("SCR-101", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.OilGreaseTrap:
                    equipment.Add(OilGreaseTrapSizer.Size("OGT-102", basis.FlowM3PerDay, basis.OilGreaseMgL, design));
                    break;

                case UnitOperationType.ChemicalDosing:
                    equipment.Add(ChemicalDosingSizer.Size("CD-141", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.DafApiSeparator:
                    equipment.Add(DafApiSeparatorSizer.Size("DAF-101", basis.FlowM3PerDay, basis.OilGreaseMgL, design));
                    break;

                case UnitOperationType.Equalization:
                    equipment.Add(EqualizationTankSizer.Size("EQ-101", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.PhNeutralization:
                    equipment.Add(PhNeutralizationSizer.Size("PH-111", basis.FlowM3PerDay, basis.Ph, design));
                    break;

                case UnitOperationType.CoagulationFlocculation:
                    equipment.Add(CoagulationFlocculationSizer.Size("CF-121", basis.FlowM3PerDay, basis.TssMgL, design));
                    break;

                case UnitOperationType.PrimaryClarifier:
                    equipment.Add(PrimaryClarifierSizer.Size("PC-131", basis.FlowM3PerDay, basis.TssMgL, design));
                    break;

                case UnitOperationType.AnaerobicReactor:
                    if (basis.CodMgL > 0)
                    {
                        var reactor = AnaerobicReactorSizer.Size("AR-211", basis.FlowM3PerDay, basis.CodMgL, design);
                        anaerobicRemoval = reactor.Parameters.TryGetValue("CodRemovalFraction", out var removal) ? removal : 0;
                        equipment.Add(reactor);
                    }
                    else
                    {
                        plan.AddNote("Anaerobic reactor skipped: COD not provided.");
                    }
                    break;

                case UnitOperationType.Aeration:
                    var bodIn = basis.BodMgL * (1.0 - anaerobicRemoval);
                    if (bodIn > 0)
                    {
                        equipment.Add(AerationTankSizer.Size("AT-201", basis.FlowM3PerDay, bodIn, design));
                    }
                    else
                    {
                        plan.AddNote("Aeration tank skipped: BOD not provided.");
                    }
                    break;

                case UnitOperationType.Mbbr:
                    var codIn = basis.CodMgL * (1.0 - anaerobicRemoval);
                    if (codIn > 0)
                    {
                        equipment.Add(MbbrSizer.Size("MB-201", basis.FlowM3PerDay, codIn, design));
                    }
                    else
                    {
                        plan.AddNote("MBBR skipped: COD not provided.");
                    }
                    break;

                case UnitOperationType.SecondaryClarifier:
                    equipment.Add(ClarifierSizer.Size("SC-301", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.PressureSandFilter:
                    equipment.Add(PressureSandFilterSizer.Size("PSF-311", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.ActivatedCarbonFilter:
                    equipment.Add(ActivatedCarbonFilterSizer.Size("ACF-321", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.Disinfection:
                    equipment.Add(DisinfectionSizer.Size("DIS-401", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.SludgeThickener:
                    hasThickener = true;
                    break;

                case UnitOperationType.FilterPress:
                    hasFilterPress = true;
                    break;
            }
        }

        if (hasThickener || hasFilterPress)
        {
            SludgeBalance.Compute(equipment, basis, design, plan);

            if (plan.SludgeDryKgPerDay > 0)
            {
                if (hasThickener)
                    equipment.Add(SludgeThickenerSizer.Size("ST-501", plan, design));

                if (hasFilterPress)
                    equipment.Add(FilterPressSizer.Size("FP-502", plan, design));
            }
        }

        if (train.Operations.Contains(UnitOperationType.ReverseOsmosis))
        {
            var membraneFeedM3PerDay = plan.TreatedWaterM3PerDay > 0 ? plan.TreatedWaterM3PerDay : basis.FlowM3PerDay;
            var handoff = ZldHandoff.Compute(basis, design, plan);

            if (train.Operations.Contains(UnitOperationType.UltraFiltration))
                equipment.Add(UltraFiltrationSizer.Size("UF-411", membraneFeedM3PerDay, design));

            equipment.Add(ReverseOsmosisSizer.Size("RO-421", handoff.RoFeedM3PerDay, basis.TdsMgL, design));

            if (basis.ZldRequired && plan.ZldFeedM3PerDay > 0)
            {
                var concentrateFraction = Math.Clamp(
                    design.Rule(DesignBasis.RuleKey("Zld", "ZLD_MEE_CONCENTRATE", "ConcentrateFraction"), 0.20),
                    0.05,
                    1.0);

                if (train.Operations.Contains(UnitOperationType.Softener))
                    equipment.Add(SoftenerSizer.Size("SF-601", plan.ZldFeedM3PerDay, design));

                if (train.Operations.Contains(UnitOperationType.MultiEffectEvaporator))
                    equipment.Add(MultiEffectEvaporatorSizer.Size("MEE-651", plan.ZldFeedM3PerDay, handoff.RejectTdsMgL, design));

                if (train.Operations.Contains(UnitOperationType.AtfdCrystallizer))
                {
                    var atfdFeedM3PerDay = plan.ZldFeedM3PerDay * concentrateFraction;
                    var atfdTdsMgL = handoff.RejectTdsMgL / concentrateFraction;

                    equipment.Add(AtfdCrystallizerSizer.Size("ATFD-701", atfdFeedM3PerDay, atfdTdsMgL, design));
                }
            }
        }

        return equipment;
    }

    public IReadOnlyList<BoqLineItem> BuildBoq(IEnumerable<SizedEquipment> equipment, DesignBasis design)
    {
        var list = equipment.ToList();
        var rates = design.Civil;
        var lines = new List<BoqLineItem>();

        foreach (var item in list.Where(x => x.Material == MaterialOfConstruction.RccM30))
        {
            var p = item.Parameters;

            if (item.Type == UnitOperationType.CoagulationFlocculation
                && p.TryGetValue("CoagulationLengthM", out var coagLength)
                && p.TryGetValue("CoagulationWidthM", out var coagWidth)
                && p.TryGetValue("FlocculationLengthM", out var floccLength)
                && p.TryGetValue("FlocculationWidthM", out var floccWidth)
                && p.TryGetValue("DepthM", out var tankDepth))
            {
                lines.AddRange(CivilBoq.Rectangular(item.Tag + "/C", coagLength, coagWidth, tankDepth, rates));
                lines.AddRange(CivilBoq.Rectangular(item.Tag + "/F", floccLength, floccWidth, tankDepth, rates));
            }
            else if (p.TryGetValue("DiameterM", out var diameter) && p.TryGetValue("DepthM", out var circularDepth))
            {
                var units = p.TryGetValue("Units", out var count) ? count : 1;

                foreach (var line in CivilBoq.Circular(item.Tag, diameter, circularDepth, rates))
                    lines.Add(line with { Quantity = Math.Round(line.Quantity * units, 2) });
            }
            else if (p.TryGetValue("LengthM", out var length)
                     && p.TryGetValue("WidthM", out var width)
                     && p.TryGetValue("DepthM", out var depth))
            {
                lines.AddRange(CivilBoq.Rectangular(item.Tag, length, width, depth, rates));
            }
        }

        var mechanical = MechanicalBoq.Build(list, design);
        var electrical = ElectricalBoq.Build(list, mechanical, design);

        lines.AddRange(mechanical);
        lines.AddRange(electrical);

        return lines;
    }
}