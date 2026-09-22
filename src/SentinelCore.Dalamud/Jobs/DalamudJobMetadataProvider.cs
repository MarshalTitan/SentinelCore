using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using SentinelCore.Jobs;

namespace SentinelCore.Dalamud.Jobs;

public sealed class DalamudJobMetadataProvider(IDataManager dataManager)
{
    private const uint DisciplesOfTheLandCategoryId = 32;
    private const uint DisciplesOfTheHandCategoryId = 33;
    private readonly IDataManager dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

    public IReadOnlyList<JobMetadata> Snapshot()
    {
        var jobs = new List<JobMetadata>();
        foreach (var classJob in dataManager.GetExcelSheet<ClassJob>())
        {
            if (classJob.RowId == 0)
                continue;

            var name = classJob.Name.ToString();
            var abbreviation = classJob.Abbreviation.ToString();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbreviation))
                continue;

            jobs.Add(Create(classJob));
        }

        return jobs
            .OrderBy(job => job.Category)
            .ThenBy(job => job.UiPriority)
            .ThenBy(job => job.ClassJobId)
            .ToArray();
    }

    public bool TryGet(uint classJobId, out JobMetadata? metadata)
    {
        var sheet = dataManager.GetExcelSheet<ClassJob>();
        if (!sheet.TryGetRow(classJobId, out var classJob))
        {
            metadata = null;
            return false;
        }

        metadata = Create(classJob);
        return true;
    }

    private static JobMetadata Create(ClassJob classJob)
    {
        var input = new JobClassificationInput(
            classJob.Role,
            classJob.PrimaryStat,
            classJob.IsLimitedJob,
            classJob.ClassJobCategory.RowId == DisciplesOfTheHandCategoryId,
            classJob.ClassJobCategory.RowId == DisciplesOfTheLandCategoryId);

        return new JobMetadata(
            classJob.RowId,
            classJob.Name.ToString(),
            classJob.Abbreviation.ToString(),
            classJob.UIPriority,
            JobClassifier.Classify(input),
            JobClassifier.ClassifyRoleHue(input),
            classJob.IsLimitedJob);
    }
}

