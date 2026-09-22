namespace SentinelCore.Jobs;

public enum JobCategory
{
    Tank,
    Healer,
    MeleeDps,
    PhysicalRangedDps,
    MagicalRangedDps,
    LimitedJob,
    Crafter,
    Gatherer,
    Other,
}

public enum RoleHue
{
    Neutral,
    Tank,
    Healer,
    Melee,
    PhysicalRanged,
    MagicalRanged,
}

public sealed record JobMetadata(
    uint ClassJobId,
    string Name,
    string Abbreviation,
    byte UiPriority,
    JobCategory Category,
    RoleHue RoleHue,
    bool IsLimitedJob);

public readonly record struct JobClassificationInput(
    byte Role,
    byte PrimaryStat,
    bool IsLimitedJob,
    bool IsCrafter,
    bool IsGatherer);

public static class JobClassifier
{
    public static JobCategory Classify(JobClassificationInput input)
    {
        if (input.IsLimitedJob) return JobCategory.LimitedJob;
        if (input.IsCrafter) return JobCategory.Crafter;
        if (input.IsGatherer) return JobCategory.Gatherer;

        return (input.Role, input.PrimaryStat) switch
        {
            (1, _) => JobCategory.Tank,
            (4, _) => JobCategory.Healer,
            (2, _) => JobCategory.MeleeDps,
            (3, 2) => JobCategory.PhysicalRangedDps,
            (3, 4) => JobCategory.MagicalRangedDps,
            _ => JobCategory.Other,
        };
    }

    public static RoleHue ClassifyRoleHue(JobClassificationInput input)
        => (input.Role, input.PrimaryStat) switch
        {
            (1, _) => RoleHue.Tank,
            (4, _) => RoleHue.Healer,
            (2, _) => RoleHue.Melee,
            (3, 2) => RoleHue.PhysicalRanged,
            (3, 4) => RoleHue.MagicalRanged,
            _ => RoleHue.Neutral,
        };
}

