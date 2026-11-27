namespace EPCApp.Database.Enums;

public enum PlantType
{
    All = 0,
    Etp = 1,
    Stp = 2,
    Zld = 3
}

public enum EnquiryStatus
{
    Draft = 1,
    Submitted = 2,
    ProposalCreated = 3,
    Converted = 4,
    Closed = 5
}

public enum ProposalStatus
{
    Draft = 1,
    Calculated = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Revised = 6
}

public enum BoqCategory
{
    Civil = 1,
    Mechanical = 2,
    Electrical = 3
}

public enum DrawingType
{
    GeneralArrangement = 1,
    PandId = 2,
    CivilLayout = 3
}

public enum RuleCategory
{
    DischargeStd = 1,
    DesignCriteria = 2,
    TrainSelection = 3,
    PesoSafety = 4,
    StandardRef = 5
}

public enum IndustryType
{
    Textile = 1,
    Pharma = 2,
    Distillery = 3,
    Chemical = 4,
    FoodAndBeverage = 5,
    Municipal = 6,
    Other = 7
}

public enum UnitOperationType
{
    ScreenChamber = 1,
    OilGreaseTrap = 2,
    Equalization = 3,
    ChemicalDosing = 4,
    PrimaryClarifier = 5,
    Aeration = 6,
    Mbbr = 7,
    SecondaryClarifier = 8,
    SludgeThickener = 9,
    FilterPress = 10,
    PressureSandFilter = 11,
    ActivatedCarbonFilter = 12,
    UltraFiltration = 13,
    ReverseOsmosis = 14,
    MultiEffectEvaporator = 15,
    PhNeutralization = 16,
    CoagulationFlocculation = 17,
    DafApiSeparator = 18,
    AnaerobicReactor = 19,
    Disinfection = 20,
    Softener = 21,
    AtfdCrystallizer = 22
}

public enum MaterialOfConstruction
{
    RccM30 = 1,
    MildSteel = 2,
    Frp = 3,
    Ss304 = 4,
    Ss316 = 5
}